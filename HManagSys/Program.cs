using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Middleware;
using HManagSys.Services;
using HManagSys.Services.Implementations;
using HManagSys.Services.Interfaces;
using HospitalManagementSystem.Data.Repositories;
using HospitalManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données
builder.Services.AddDbContext<HospitalManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Configuration des repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHospitalCenterRepository, HospitalCenterRepository>();
builder.Services.AddScoped<IUserCenterAssignmentRepository, UserCenterAssignmentRepository>();


// Configuration des services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IApplicationLogger, ApplicationLogger>();
builder.Services.AddScoped<IAuditService, AuditService>();


// Services métier - Stock
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ITransferService, TransferService>();
// builder.Services.AddScoped<IStockService, StockService>(); // À ajouter plus tard


// Configuration de la session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(720); // 12 heures
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "HospitalSession";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configuration MVC avec support des TempData
builder.Services.AddControllersWithViews(options =>
{
    // Configuration globale si nécessaire
}).AddSessionStateTempDataProvider();

// Configuration du TempData pour les sessions
//builder.Services.AddSessionStateTempDataProvider();

// Configuration du HttpContextAccessor pour les services
builder.Services.AddHttpContextAccessor();

// Configuration du logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    // Ajouter Serilog si configuré
});

// Service d'arrière-plan pour le nettoyage des sessions
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

// Configuration du pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware de session (avant l'authentification)
app.UseSession();

// Middleware d'authentification personnalisé
app.UseSessionValidation();

// Configuration des routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Service d'arrière-plan pour le nettoyage périodique des sessions
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

                var cleanedCount = await authService.CleanExpiredSessionsAsync();

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Nettoyage automatique: {Count} sessions expirées supprimées", cleanedCount);
                }

                // Attendre 1 heure avant le prochain nettoyage
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage automatique des sessions");

                // En cas d'erreur, attendre 30 minutes avant de réessayer
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}

//using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
//using HManagSys.Data.DBContext;
//using HManagSys.Data.Repositories;
//using HManagSys.Data.Repositories.Interfaces;
//using HManagSys.Helpers;
//using HManagSys.Services;
//using HManagSys.Services.Interfaces;
//using HospitalManagementSystem.Data.Repositories;
//using HospitalManagementSystem.Services;
//using Microsoft.EntityFrameworkCore;
//using QuestPDF.Infrastructure;
//using Serilog;

//namespace HManagSys
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            builder.Services.AddHttpContextAccessor();
//            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
//            builder.Services.AddScoped<IUserRepository, UserRepository>();
//            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
//            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
//            builder.Services.AddScoped<IAuditService, AuditService>();
//            builder.Services.AddScoped<IHospitalCenterRepository, HospitalCenterRepository>();
//            builder.Services.AddScoped<IUserCenterAssignmentRepository, UserCenterAssignmentRepository>();
//            // Enregistrer le service de logging applicatif
//            builder.Services.AddScoped<IApplicationLogger, ApplicationLogger>();

//            builder.Services.AddAutoMapper(typeof(AutoMapperProfil));
//            QuestPDF.Settings.License = LicenseType.Community;

//            builder.Services.AddDbContext<HospitalManagementContext>(option =>
//            {
//                option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
//            }, ServiceLifetime.Scoped);


//            builder.Services.AddSession(options =>
//            {
//                options.IdleTimeout = TimeSpan.FromMinutes(720);
//                options.Cookie.Name = "HospitalSession";
//                options.Cookie.HttpOnly = true;
//                options.Cookie.IsEssential = true;
//            });

//            //// Configurer les logs système (fichiers) avec Serilog par exemple
//            //builder.Host.UseSerilog((context, config) =>
//            //{
//            //    config
//            //        .WriteTo.Console()
//            //        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
//            //        .ReadFrom.Configuration(context.Configuration);
//            //});

//            // Add services to the container.
//            builder.Services.AddControllersWithViews();

//            var app = builder.Build();

//            // Configure the HTTP request pipeline.
//            if (!app.Environment.IsDevelopment())
//            {
//                app.UseExceptionHandler("/Home/Error");
//                app.UseHsts();

//            }
//            app.UseHttpsRedirection();

//            app.UseStaticFiles();

//            app.UseRouting();

//            app.UseSession();

//            app.UseAuthorization();

//            app.MapControllerRoute(
//                name: "default",
//                pattern: "{controller=Home}/{action=Index}/{id?}");

//            app.Run();
//        }
//    }
//}
