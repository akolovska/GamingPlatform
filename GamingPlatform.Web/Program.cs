using GamingPlatform.Domain.Email;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

// Secrets (connection string, Stripe and SMTP settings) live in GamingPlatform.Web/.env, which git ignores.
// TraversePath also finds the file when the app is started from a subfolder such as bin/.
DotNetEnv.Env.TraversePath().Load();
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set ConnectionStrings__DefaultConnection in .env.");
// Split queries: a game with genres, studios, reviews and achievements loads as a few small queries
// instead of one query whose rows multiply (genres x reviews x achievements)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<GamingPlatformUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // people can log in at once; some features need a confirmed email
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;    // used by the login page (lockoutOnFailure: true)
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Re-check every signed-in user against the database each minute (default: 30), so a ban,
// a removed admin role or a changed password signs them out quickly
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

// "Log in with Google" is on only when its keys are configured (Authentication__Google__ClientId and
// Authentication__Google__ClientSecret in .env). Redirect URI to register at Google: https://<site>/signin-google
var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleId;
        options.ClientSecret = googleSecret;
    });
}

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    // Turn off the built-in Identity "Manage account" pages. They bypass this app's rules: their
    // account deletion fails on friendships, and their email change also renames the user.
    // Account settings (profile, password, delete account) live in AccountController instead.
    options.Conventions.AddAreaFolderRouteModelConvention("Identity", "/Account/Manage",
        model => model.Selectors.Clear());
});

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddTransient<IEmailSender, EmailService>();

builder.Services.AddGamingPlatformServices(); // repositories, unit of work and all services
builder.Services.AddHostedService<GamingPlatform.Web.Jobs.SaleAnnouncementJob>(); // tells wishlisters when scheduled sales start

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { Roles.Admin, Roles.User, Roles.Dev })
    {
        if (await roleManager.RoleExistsAsync(role)) continue;

        var result = await roleManager.CreateAsync(new IdentityRole(role));
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    // The first admin: set AdminUserName in .env to an existing account's username
    var adminUserName = builder.Configuration["AdminUserName"];
    if (!string.IsNullOrWhiteSpace(adminUserName))
    {
        var userManager = services.GetRequiredService<UserManager<GamingPlatformUser>>();
        var admin = await userManager.FindByNameAsync(adminUserName);
        if (admin != null && !await userManager.IsInRoleAsync(admin, Roles.Admin))
            await userManager.AddToRoleAsync(admin, Roles.Admin);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Friendly pages for 404 and other errors without a body
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// One fixed culture ('.' decimals) so server formatting/model binding match jQuery Validation.
// Only en-US is supported, so the browser's Accept-Language can't switch it.
var appCulture = new[] { "en-US" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(appCulture[0])
    .AddSupportedCultures(appCulture)
    .AddSupportedUICultures(appCulture));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
