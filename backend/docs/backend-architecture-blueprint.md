# TeamTaskManager Backend Architecture Blueprint

## 1. Amaç
Bu belge, TeamTaskManager projesinin backend katmanlarını ve ileride uygulanacak sorumluluk dağılımını açıklar. Amaç, N-Layered Architecture prensiplerine sadık kalmak ve her katmanın tek bir sorumluluğu olmasını sağlamaktır.

## 2. Katman yapısı

### TeamTaskManager.API
- HTTP istek/yanıt akışının başladığı katmandır.
- Controller, endpoint ve route düzenlemeleri burada yer alır.
- Business katmanındaki servisleri çağırır.
- Entity nesnelerini doğrudan dönmez; DTO kullanır.
- Veritabanı doğrudan erişmez.

### TeamTaskManager.Business
- İş kuralları ve varsa servis mantığı burada bulunur.
- DataAccess katmanına erişir.
- DTO ve entity yapılarından gerekli verileri işler.
- Controller içinde iş mantığı yazılmaz.

### TeamTaskManager.DataAccess
- Veritabanı erişimi burada yapılır.
- DbContext, repository ve veri sağlayıcı sınıfları burada konumlanır.
- Entity ve Core katmanlarını kullanır.

### TeamTaskManager.Entities
- Veritabanı domain modelleri burada yer alır.
- Her entity tek bir iş alanı ve sorumluluk taşır.
- İleride EF Core ile eşlenir.

### TeamTaskManager.DTO
- API request/response modelleri burada tanımlanır.
- Entity ile API arasında köprü görevi görür.
- Dışarıya açılan veriler DTO ile kontrol edilir.

### TeamTaskManager.Core
- Ortak altyapı yapıları burada bulunur.
- Base entity, ortak arayüzler, sabitler ve yardımcı kod burada toplanır.

### TeamTaskManager.Tests
- Unit test ve integration test altyapısı için kullanılır.
- Gelecekte testler burada yazılır.

## 3. Domain model planı
Aşağıdaki entityler ileride veri tabanı modeline dönüşecektir:
- ApplicationUser
- Project
- ProjectMember
- TaskItem
- TaskComment
- TaskAttachment
- Notification
- ActivityLog

## 4. İlişki planı
- Project, birden fazla ProjectMember içerir.
- Project, birden fazla TaskItem içerir.
- TaskItem, birden fazla TaskComment içerir.
- TaskItem, birden fazla TaskAttachment içerir.
- ApplicationUser, birden fazla proje üyeliğine sahip olabilir.
- ApplicationUser, birden fazla task ataması ve notification kaydı olabilir.
- ActivityLog, tüm sistem aktivitelerini izleyen merkezi kayıt tablosu olarak düşünülür.

## 5. DTO planı
- ProjectCreateRequest / ProjectUpdateRequest / ProjectResponse
- ProjectMemberRequest / ProjectMemberResponse
- TaskItemCreateRequest / TaskItemUpdateRequest / TaskItemResponse
- TaskCommentCreateRequest / TaskCommentResponse
- TaskAttachmentResponse
- NotificationResponse
- ActivityLogResponse

## 6. DbContext planı
- ApplicationDbContext sınıfı DataAccess katmanında oluşturulacak.
- DbSet<Project>, DbSet<TaskItem>, DbSet<ProjectMember>, DbSet<TaskComment>, DbSet<TaskAttachment>, DbSet<Notification>, DbSet<ActivityLog>, DbSet<ApplicationUser> tanımlanacak.
- Connection string ve ilişkilendirmeler sonraki adımda eklenecek.

## 7. Sonraki adım önerisi
Bir sonraki adımda aşağıları uygulayacağız:
1. Entity sınıflarını üretmek
2. Core base sınıflarını eklemek
3. DTO modelini tanımlamak
4. DbContext taslağını kurmak
5. Sonrasında EF Core configuration ve migration hazırlığına geçmek

Bu belge, kodlamaya başlamadan önce mimari anlayışın netleşmesi içindir.
