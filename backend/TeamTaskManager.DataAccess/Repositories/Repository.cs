using Microsoft.EntityFrameworkCore;

namespace TeamTaskManager.DataAccess.Repositories;

// Generic repository, ilgili entity türünün DbSet'i üzerinde temel CRUD yapar.
public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    // Primary key üzerinden tek kayıt arar.
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    // Tüm kayıtları belleğe alır; filtreleme çağıran servis tarafından yapılır.
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    // Yeni entity'yi context'e ekler; veritabanına yazmak için SaveChanges gerekir.
    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    // Entity'yi değişmiş olarak işaretler; kayıt UnitOfWork.SaveChangesAsync ile yapılır.
    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    // Entity'yi silinmek üzere işaretler; ilişkili davranışı EF model ayarları belirler.
    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
