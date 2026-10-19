using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Repository.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class UnitOfWorkTests
    {
        private readonly TestDatabase _db;
        public UnitOfWorkTests(TestDatabase db) => _db = db;

        private bool GenreExists(string name)
        {
            using var check = _db.NewScope();
            return check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Genres.Any(g => g.Name == name);
        }

        [Fact]
        public void Everything_is_committed_together()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var genres = sp.GetRequiredService<IRepository<Genre>>();
            string a = "A " + Guid.NewGuid(), b = "B " + Guid.NewGuid();

            sp.GetRequiredService<IUnitOfWork>().InTransaction(() =>
            {
                genres.Insert(new Genre { Name = a });
                genres.Insert(new Genre { Name = b });
            });

            Assert.True(GenreExists(a) && GenreExists(b));
        }

        [Fact]
        public void An_exception_undoes_the_saves_made_before_it()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var genres = sp.GetRequiredService<IRepository<Genre>>();
            var name = "Rolled back " + Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(() =>
                sp.GetRequiredService<IUnitOfWork>().InTransaction(() =>
                {
                    genres.Insert(new Genre { Name = name }); // already saved to the DB at this point...
                    throw new InvalidOperationException("second step failed");
                }));

            Assert.False(GenreExists(name)); // ...but rolled back
        }

        [Fact]
        public void CommitIf_false_rolls_back()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var genres = sp.GetRequiredService<IRepository<Genre>>();
            var name = "Not committed " + Guid.NewGuid();

            sp.GetRequiredService<IUnitOfWork>().InTransaction(
                () => { genres.Insert(new Genre { Name = name }); return false; },
                commitIf: ok => ok);

            Assert.False(GenreExists(name));
        }

        [Fact]
        public void A_rejected_duplicate_inside_a_transaction_doesnt_spoil_it()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var genres = sp.GetRequiredService<IRepository<Genre>>();
            string name = "Dup " + Guid.NewGuid(), after = "After " + Guid.NewGuid();
            genres.Insert(new Genre { Name = name });

            sp.GetRequiredService<IUnitOfWork>().InTransaction(() =>
            {
                Assert.False(genres.TryInsert(new Genre { Name = name })); // unique index says no
                genres.Insert(new Genre { Name = after });                // the transaction carries on
            });

            Assert.True(GenreExists(after));
        }
    }
}
