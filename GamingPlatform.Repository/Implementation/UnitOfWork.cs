using GamingPlatform.Repository.Data;
using GamingPlatform.Repository.Interface;

namespace GamingPlatform.Repository.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public void InTransaction(Action work) =>
            InTransaction(() => { work(); return true; });

        public T InTransaction<T>(Func<T> work, Func<T, bool>? commitIf = null)
        {
            // Already inside a transaction: the outer call decides whether to commit
            if (_context.Database.CurrentTransaction != null) return work();

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var result = work();
                if (commitIf == null || commitIf(result))
                    transaction.Commit();
                else
                    RollBack(transaction);
                return result;
            }
            catch
            {
                RollBack(transaction);
                throw;
            }
        }

        public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, Func<T, bool>? commitIf = null)
        {
            if (_context.Database.CurrentTransaction != null) return await work();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await work();
                if (commitIf == null || commitIf(result))
                    await transaction.CommitAsync();
                else
                    RollBack(transaction);
                return result;
            }
            catch
            {
                RollBack(transaction);
                throw;
            }
        }

        private void RollBack(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        {
            // If the connection itself failed, Rollback can throw too; the original error matters more,
            // and SQL Server rolls back an unfinished transaction on its own anyway
            try { transaction.Rollback(); } catch { }
            // The tracked entities still hold the rolled-back changes; forget them so nothing re-saves them
            _context.ChangeTracker.Clear();
        }
    }
}
