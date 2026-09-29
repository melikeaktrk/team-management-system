namespace TeamTaskManager.DataAccess.Repositories;

// EF Core entity'leri için ortak temel CRUD işlemlerini tanımlar.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
