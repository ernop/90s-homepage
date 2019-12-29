using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace FusekiC
{
    public class Startup
    {
        public const string CookieScheme = "FusekiThESchemee32";
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var logger = new ConsoleLogger();

            services.AddSingleton(logger);
            services.AddAuthorization();
            services.AddAuthentication(CookieScheme) // Sets the default scheme to cookies
                .AddCookie(CookieScheme, options =>
                {
                    options.AccessDeniedPath = "/list";
                    options.LoginPath = "/account/login";
                });
            var renderer = new Renderer();
            var settings = Configuration.Get<Settings>();

            services.AddSingleton(settings);
            services.AddSingleton(renderer);

            var pc = new PublishConfiguration("temp", "wwwroot/css", "wwwroot/js", "../fusekiimages", settings.PublishTarget);
            services.AddSingleton(pc);

            services.AddMvc(oo => { oo.EnableEndpointRouting = false; });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            //TODO what does this do?
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "../fusekiimages")),
                RequestPath = "/images"
            });

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.UseMvc();

        }
    }
}
