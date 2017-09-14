using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibs
{
    public interface IRepository<TEntity> : IDisposable
    {
        Task<List<TEntity>> GetAllAsync();
        TEntity GetDefault();
        TEntity Find(int id);
        TEntity Find(string key);
        bool Any(int id);
        bool Any(string key);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default(CancellationToken));
        Task<TEntity> MaxAsync<TKey>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TKey>> order);
        Task<TEntity> MinAsync<TKey>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TKey>> order);
        IQueryable<TEntity> Where<TKey>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TKey>> order);
        IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> where);
        Task<TEntity[]> WhereToArrayAsync<TKey>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TKey>> order);
        Task<TEntity[]> WhereToArrayAsync(Expression<Func<TEntity, bool>> where);
        Task<List<TEntity>> WhereToListAsync<TKey>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TKey>> order);
        Task<List<TEntity>> WhereToListAsync(Expression<Func<TEntity, bool>> where);
        void Add(TEntity data);
        //bool AddNoCacheNotSave(TEntity data);
        //bool AddNoCache(TEntity data);
        //Task<bool> SaveChangesAsync();
        void Update(TEntity data);
        void Remove(TEntity data);
        bool Remove(int id);
        bool RemoveRange(params TEntity[] datas);
        Task<int> RemoveWhereAsync(Expression<Func<TEntity, bool>> predicate);
        Task SaveChangesAsync();
    }
}
