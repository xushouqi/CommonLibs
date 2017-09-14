using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using CommonLibs;

namespace CommonServices.Caching
{
    public interface ICacheClient<T> : IDisposable where T: Entity
    {
        Task<int> RemoveAllAsync(IEnumerable<string> keys = null);
        Task<int> RemoveByPrefixAsync(string prefix);
        Task<T> GetAsync(int id);
        Task<T> GetAsync(string key);
        Task<T> GetAsync(string prefix, int id);
        Task<IDictionary<int, T>> GetAllAsync(IEnumerable<int> ids);
        Task<IDictionary<string, T>> GetAllAsync(IEnumerable<string> keys);
        Task<ICollection<T>> GetCollectionAsync(IEnumerable<int> ids);
        Task<bool> AddAsync(string key, T value, TimeSpan? expiresIn = null);
        Task<bool> AddAsync(int id, T value, TimeSpan? expiresIn = null);
        Task<bool> AddAsync(string prefix, int id, T value, TimeSpan? expiresIn = null);
        Task<bool> SetAsync(string key, T value, TimeSpan? expiresIn = null);
        Task<bool> SetAsync(int id, T value, TimeSpan? expiresIn = null);
        Task<bool> SetAsync(string prefix, int id, T value, TimeSpan? expiresIn = null);
        Task<int> SetAllAsync(IDictionary<string, T> values, TimeSpan? expiresIn = null);
        Task<bool> ReplaceAsync(string key, T value, TimeSpan? expiresIn = null);
        Task<bool> ExistsAsync(string key);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(string prefix, int id);
        Task<TimeSpan?> GetExpirationAsync(string key);
        Task SetExpirationAsync(string key, TimeSpan expiresIn);
        //Task<double> SetIfHigherAsync(string key, double value, TimeSpan? expiresIn = null);
        //Task<double> SetIfLowerAsync(string key, double value, TimeSpan? expiresIn = null);
        //Task<long> SetAddAsync(string key, IEnumerable<T> value, TimeSpan? expiresIn = null);
        //Task<long> SetRemoveAsync(string key, IEnumerable<T> value, TimeSpan? expiresIn = null);
        //Task<ICollection<T>> GetSetAsync(string key);

        Task OnRemoteCacheItemExpiredAsync(InvalidateCache message);


        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> MaxAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order);
        Task<T> MinAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken token = default(CancellationToken));
        IQueryable<T> Where<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order);
        IQueryable<T> Where(Expression<Func<T, bool>> where);
        Task<T[]> WhereToArrayAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order);
        Task<T[]> WhereToArrayAsync(Expression<Func<T, bool>> where);
        Task<List<T>> WhereToListAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order);
        Task<List<T>> WhereToListAsync(Expression<Func<T, bool>> where);
        Task AddAsync(T item);
        Task UpdateAsync(T item);
        Task RemoveAsync(T item);
        Task AddRangeAsync(IEnumerable<T> datas);
        Task UpdateRangeAsync(IEnumerable<T> datas);
        Task RemoveRangeAsync(IEnumerable<T> datas);
        Task<int> RemoveWhereAsync(Expression<Func<T, bool>> predicate);
    }


    public class InvalidateCache
    {
        public string CacheId { get; set; }
        public string[] Keys { get; set; }
        public bool FlushAll { get; set; } = false;
        public bool Expired { get; set; } = false;
    }
}
