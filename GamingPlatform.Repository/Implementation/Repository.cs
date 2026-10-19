using System.Linq.Expressions;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Repository.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GamingPlatform.Repository.Implementation
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _entities;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _entities = _context.Set<T>();
        }

        public T Insert(T entity)
        {
            _context.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public void InsertRange(IEnumerable<T> entities)
        {
            _entities.AddRange(entities);
            _context.SaveChanges();
        }

        public bool TryInsert(T entity)
        {
            _context.Add(entity);
            try
            {
                _context.SaveChanges();
                return true;
            }
            // 2601/2627 = SQL Server unique index / unique constraint violation
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Stop tracking the rejected row so a later SaveChanges doesn't retry it
                _context.Entry(entity).State = EntityState.Detached;
                return false;
            }
        }

        public T Update(T entity)
        {
            // Entities loaded through this repository are already tracked, so EF saves only
            // what changed. Update() would mark every loaded related row as modified too.
            if (_context.Entry(entity).State == EntityState.Detached)
                _context.Update(entity);
            _context.SaveChanges();
            return entity;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
                if (_context.Entry(entity).State == EntityState.Detached)
                    _context.Update(entity);
            _context.SaveChanges();
        }

        public T Delete(T entity)
        {
            _context.Remove(entity);
            _context.SaveChanges();
            return entity;
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _entities.RemoveRange(entities);
            _context.SaveChanges();
        }

        public bool Any(Expression<Func<T, bool>> predicate) => _entities.Any(predicate);

        public int Count(Expression<Func<T, bool>>? predicate = null) =>
            predicate == null ? _entities.Count() : _entities.Count(predicate);

        public E? Get<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            return Query(predicate, orderBy, include).Select(selector).FirstOrDefault();
        }

        public List<E> GetAll<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            int skip = 0,
            int? take = null)
        {
            var query = Query(predicate, orderBy, include);
            if (skip > 0) query = query.Skip(skip);
            if (take != null) query = query.Take(take.Value);
            return query.Select(selector).ToList();
        }

        // Everything is applied to the IQueryable, so filtering and ordering run in SQL
        private IQueryable<T> Query(
            Expression<Func<T, bool>>? predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include)
        {
            IQueryable<T> query = _entities;
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (orderBy != null) query = orderBy(query);
            return query;
        }
    }
}
