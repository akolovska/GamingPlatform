using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    // One real SQL Server database for the whole test run, built from the current model.
    // A real database (not EF's in-memory provider) so unique indexes, foreign keys and
    // transactions behave exactly as in the app.
    public class TestDatabase : IDisposable
    {
        public const string WebhookSecret = "whsec_test_secret";

        private const string DefaultConnection =
            @"Server=(localdb)\MSSQLLocalDB;Database=GamingPlatform_Tests;Trusted_Connection=True;MultipleActiveResultSets=true";

        public ServiceProvider Services { get; }
        public string WebRoot { get; }

        public TestDatabase()
        {
            // Override with TEST_CONNECTION_STRING to use another server (e.g. in CI)
            var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? DefaultConnection;

            // This class deletes and recreates the database, so never point it at real data
            var database = new SqlConnectionStringBuilder(connection).InitialCatalog;
            if (!database.EndsWith("_Tests", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Refusing to use database '{database}' for tests: its name must end with _Tests.");

            WebRoot = Directory.CreateTempSubdirectory("gamingplatform-tests-").FullName;

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection(); // the web host adds this itself; Identity's email tokens need it
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["SiteUrl"] = "https://store.test" })
                .Build());
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connection, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            services.AddIdentityCore<GamingPlatformUser>(options => options.User.RequireUniqueEmail = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment(WebRoot));
            services.AddSingleton<IEmailSender, FakeEmailSender>();
            services.Configure<StripeSettings>(s =>
            {
                s.SecretKey = "sk_test_not_used";
                s.WebhookSecret = WebhookSecret;
            });
            services.AddGamingPlatformServices(); // the same registrations as Program.cs
            Services = services.BuildServiceProvider();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { Roles.Admin, Roles.User, Roles.Dev })
                roles.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }

        // A fresh scope = a fresh DbContext, like one web request
        public IServiceScope NewScope() => Services.CreateScope();

        public void Dispose()
        {
            using (var scope = Services.CreateScope())
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureDeleted();
            Services.Dispose();
            Directory.Delete(WebRoot, recursive: true);
        }
    }

    // All test classes share one database; xUnit runs classes in the same collection one at a time
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabase> { }
}
