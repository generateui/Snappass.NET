using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.SQLite;

namespace Snappass
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(6 * 31); // https://www.ncsc.nl/onderwerpen/verbindingsbeveiliging
                options.Preload = true;
                options.IncludeSubDomains = true;
            });
            services.AddScoped<IMemoryStore, SqliteStore>();
			services.AddSingleton<IDateTimeProvider, CurrentDateTimeProvider>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // required for MemoryStore
			services.AddScoped(sp =>
			{
				var databaseFilePath = @"database.sqlite";
				var connectionString = $@"Data Source={databaseFilePath};Version=3;";
				return new SQLiteConnection(connectionString);
			});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.Use(async (context, next) => {
                context.Response.Headers.Add("Content-Security-Policy","script-src 'self'; style-src 'self'; img-src 'self'");
                context.Response.Headers.Add("X-Xss-Protection", "1");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                await next();
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Share}/{action=Share}");
                endpoints.MapControllerRoute(
                    name: "password",
                    pattern: "Password/{key}", new { controller = "Password", action = "Preview" });
            });
        }
    }
}
