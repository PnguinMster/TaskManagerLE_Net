using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using TaskManagerLE.Models;
using TaskManagerLE.Services;
using TaskManagerLE.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Inject Services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TrashService>();
builder.Services.AddScoped<DashboardServices>();

// Email: logs to the console for now. Registered before AddIdentity so Identity's no-op default doesn't win.
builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();

// Authorization
builder.Services.AddAuthorizationBuilder()
	.AddPolicy(Policies.ProjectOwner, policy => policy.Requirements.Add(new ProjectOwnerRequirement()));
builder.Services.AddScoped<IAuthorizationHandler, ProjectOwnerHandler>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.EnableRetryOnFailure()));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
	{
		options.Password.RequiredLength = 8;
		options.Password.RequireDigit = true;
		options.Password.RequireUppercase = true;
		options.Password.RequireNonAlphanumeric = false;
		options.User.RequireUniqueEmail = true;
		options.SignIn.RequireConfirmedAccount = false;
		options.User.AllowedUserNameCharacters =
			"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
	})
	.AddErrorDescriber<FriendlyIdentityErrorDescriber>()
	.AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
	options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// User Seeding
using (var scope = app.Services.CreateScope()) {
	await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
	await UserSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	// Unhandled exceptions are logged, then the request is re-run against the friendly 500 page.
	// In Development the detailed developer exception page is shown instead.
	app.UseExceptionHandler("/Home/Error");

	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

app.Run();