using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

// Görev dosyalarını listeleme, yükleme ve indirme işlemlerinin sözleşmesi.
public interface ITaskAttachmentService
{
    Task<IEnumerable<TaskAttachmentResponse>> GetByTaskAsync(Guid taskId);
    Task<TaskAttachmentResponse?> GetByIdAsync(Guid attachmentId);
    Task<TaskAttachmentResponse> UploadAsync(Guid taskId, Guid userId, IFormFile file);
    Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(Guid attachmentId);
}

// Dosyayı uygulama içi klasörde saklar; veritabanında yalnızca metadata tutar.
public class TaskAttachmentService : ITaskAttachmentService
{
    // Sunucu tarafındaki izin verilen uzantı, MIME ve boyut sınırları.
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".txt"] = "text/plain"
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly string _storageRoot;

    public TaskAttachmentService(IUnitOfWork unitOfWork, IHostEnvironment environment)
    {
        _unitOfWork = unitOfWork;
        _storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "TaskAttachments"));
    }

    // Göreve bağlı dosya metadata'sını yükleme tarihine göre sıralar.
    public async Task<IEnumerable<TaskAttachmentResponse>> GetByTaskAsync(Guid taskId)
    {
        var attachments = (await _unitOfWork.TaskAttachments.GetAllAsync())
            .Where(attachment => attachment.TaskItemId == taskId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .ToList();
        return attachments.Select(ToResponse);
    }

    public async Task<TaskAttachmentResponse?> GetByIdAsync(Guid attachmentId)
    {
        var attachment = await _unitOfWork.TaskAttachments.GetByIdAsync(attachmentId);
        return attachment is null ? null : ToResponse(attachment);
    }

    // Dosya kontrollerini yapar, rastgele saklama adıyla yazar ve metadata/aktivite kaydeder.
    public async Task<TaskAttachmentResponse> UploadAsync(Guid taskId, Guid userId, IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
            throw new InvalidOperationException("Dosya boş olamaz ve en fazla 10 MB olabilir.");

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Length > 255 ||
            !AllowedTypes.TryGetValue(extension, out var contentType) ||
            !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Desteklenmeyen dosya türü.");
        }

        Directory.CreateDirectory(_storageRoot);
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = GetSafePath(storedFileName);

        await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(output);
        }

        var attachment = new TaskAttachment
        {
            TaskItemId = taskId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FilePath = storedFileName,
            ContentType = contentType,
            FileSize = file.Length,
            UploadedByUserId = userId
        };

        try
        {
            await _unitOfWork.TaskAttachments.AddAsync(attachment);
            await _unitOfWork.ActivityLogs.AddAsync(new ActivityLog
            {
                UserId = userId,
                EntityType = nameof(TaskItem),
                EntityId = taskId,
                Action = "TaskAttachmentUploaded",
                Description = $"'{originalFileName}' dosyası göreve eklendi."
            });
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        return ToResponse(attachment);
    }

    // Metadata ile diskteki güvenli dosya adını eşleştirip indirme içeriğini döndürür.
    public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(Guid attachmentId)
    {
        var attachment = await _unitOfWork.TaskAttachments.GetByIdAsync(attachmentId);
        if (attachment is null) return null;

        if (!string.Equals(attachment.FilePath, attachment.StoredFileName, StringComparison.Ordinal))
            return null;

        var safePath = GetSafePath(attachment.StoredFileName);
        if (!File.Exists(safePath))
        {
            return null;
        }

        return (await File.ReadAllBytesAsync(safePath), attachment.ContentType, attachment.OriginalFileName);
    }

    // Kullanıcıdan gelen yol bileşenlerinin saklama klasörü dışına çıkmasını önler.
    private string GetSafePath(string storedFileName)
    {
        if (Path.GetFileName(storedFileName) != storedFileName)
            throw new InvalidOperationException("Geçersiz dosya yolu.");

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, storedFileName));
        var rootWithSeparator = _storageRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _storageRoot
            : _storageRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Geçersiz dosya yolu.");

        return fullPath;
    }

    // Dahili disk yolu ve rastgele saklama adını istemci yanıtına dahil etmez.
    private static TaskAttachmentResponse ToResponse(TaskAttachment attachment) => new()
    {
        Id = attachment.Id,
        TaskItemId = attachment.TaskItemId,
        OriginalFileName = attachment.OriginalFileName,
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        UploadedByUserId = attachment.UploadedByUserId,
        CreatedAt = attachment.CreatedAt
    };
}
