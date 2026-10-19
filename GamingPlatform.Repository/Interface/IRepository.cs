using System.Linq.Expressions;
using GamingPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore.Query;

namespace GamingPlatform.Repository.Interface
{
    public interface IRepository<T> where T : BaseEntity
    {
        T Insert(T entity);
        void InsertRange(IEnumerable<T> entities);

        // Returns false (instead of throwing) when a unique index rejects the row,
        // e.g. two requests adding the same game to a library at the same time
        bool TryInsert(T entity);

        T Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        T Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);

        bool Any(Expression<Func<T, bool>> predicate);
        int Count(Expression<Func<T, bool>>? predicate = null);

        E? Get<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);

        // skip/take page through the results in SQL (use with orderBy, so pages are stable)
        List<E> GetAll<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            int skip = 0,
            int? take = null);
    }
}
