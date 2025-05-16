using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using HManagSys.Data.Repositories;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Services.Interfaces;
using HospitalManagementSystem.Services;
using Serilog;

namespace HManagSys
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            // Enregistrer le service de logging applicatif
            builder.Services.AddScoped<IApplicationLogger, ApplicationLogger>();

            // Configurer les logs système (fichiers) avec Serilog par exemple
            builder.Host.UseSerilog((context, config) =>
            {
                config
                    .WriteTo.Console()
                    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
                    .ReadFrom.Configuration(context.Configuration);
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
