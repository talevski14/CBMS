using Entity.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Implementation;
using Repository.Implementation.Integration;
using Repository.Interface;
using Repository.Interface.Integration;
using Service;
using Service.Implementation;
using Service.Implementation.Integration;
using Service.Interface;
using Service.Interface.Integration;
using Stripe;
using System.Globalization;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var connectionStringForIntegration = builder.Configuration.GetConnectionString("IntegrationConnection") ?? throw new InvalidOperationException("Connection string 'IntegrationConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<IntegrationDbContext>(options => options.UseSqlServer(connectionStringForIntegration));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<CbmsUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

var cultureInfo = new CultureInfo("mk-MK");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
builder.Services.AddScoped(typeof(IOrderRepository), typeof(OrderRepository));
builder.Services.AddScoped(typeof(IIntegrationRepository<>), typeof(IntegrationRepository<>));

builder.Services.AddTransient<IBartenderService, BartenderService>();
builder.Services.AddTransient<IProductsService, ProductsService>();
builder.Services.AddTransient<IWaiterService, WaiterService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IIntegrationCategoriesService, IntegrationCategoriesService>();
builder.Services.AddTransient<IIntegrationProductsService, IntegrationProductsService>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<CbmsUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the database is created and seed the roles
        context.Database.Migrate();
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Products/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "Products",
    pattern: "{controller=Products}/{action=Index}/{id?}"
    );

app.MapRazorPages();

app.Run();
