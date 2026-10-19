namespace GamingPlatform.Repository.Interface
{
    // Groups several saves into one database transaction: they're all committed, or none are.
    // Every repository (and UserManager) in a request shares the same DbContext, so
    // everything they save inside `work` is part of the transaction.
    public interface IUnitOfWork
    {
        void InTransaction(Action work);

        // Commits when work finishes and commitIf (if given) returns true; otherwise rolls back.
        // An exception always rolls back.
        T InTransaction<T>(Func<T> work, Func<T, bool>? commitIf = null);
        Task<T> InTransactionAsync<T>(Func<Task<T>> work, Func<T, bool>? commitIf = null);
    }
}
