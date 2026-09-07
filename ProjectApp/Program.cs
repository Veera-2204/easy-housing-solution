using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Data;
using Microsoft.Extensions.Configuration;
using Serilog; 
using System;

namespace ProjectApp
{
    public class Program
    {
        /// <summary>
        /// The function is used to create and build the application by specifying the configuration, DbContext, Identity and running the code
        /// </summary>
        /// <param name="args"></param>
        public static void Main( string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() 
                .WriteTo.File(
                    path: Path.Combine("Logs", "log-.txt"),   
                    rollingInterval: RollingInterval.Day,             // Daily log rotation
                    retainedFileCountLimit: 7,                     // Keep logs for 7 days
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}") // Log format
                .CreateLogger();

            try
            {
                // Log that the application is starting
                Log.Information("Application starting up...");

                var builder = WebApplication.CreateBuilder(args);

                // Tell the app to use Serilog
                builder.Host.UseSerilog();

                // Load configuration from appsettings.json
                var configuration = builder.Configuration;
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                builder.Services.AddDbContext<AppsDbContext>(options =>
                    options.UseSqlServer(connectionString));

                builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<AppsDbContext>()
                    .AddDefaultTokenProviders();

                // Configure Identity options
                builder.Services.Configure<IdentityOptions>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 8;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;

                    options.User.RequireUniqueEmail = true;
                });

                // Configure cookie settings for authentication
                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();              // http site transport security : prevents man-in-the-middle attacks by strictly enforcing https
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                Log.Information("Application started successfully.");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}



/*
 builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppsDbContext>()  // Use AppsDbContext to store identity data
    .AddDefaultTokenProviders();     
*/

/* 
 Services like UseAuthentication() and UseAuthorization() are middleware services that are defined in a certain way which are part of the request pipleine.
They are defined after the requests pipeline
*/