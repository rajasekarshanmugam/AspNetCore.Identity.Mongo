﻿using AspNetCore.Identity.Mongo;
using SampleSite.Blog;
using SampleSite.GridFs;
using SampleSite.Identity;
using SampleSite.Mailing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerSideAnalytics;
using ServerSideAnalytics.Mongo;

namespace SampleSite
{
    public class Startup
    {
        private string ConnectionString => Configuration.GetConnectionString("DefaultConnection");


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddIdentityMongoDbProvider<MaddalenaUser>(mongo =>
            {
                mongo.ConnectionString = ConnectionString;
            });

            var store = GetAnalyticStore(ConnectionString);
            services.AddSingleton<IAnalyticStore>(store);

            services.AddTransient<IEmailSender, EmailSender>();

            var gridFs = new GridFileSystem(ConnectionString, "gridFsTable");

            services.AddSingleton<IGridFileSystem>(gridFs);
            services.AddSingleton<IBlogService>(new MongoBlogService(ConnectionString));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public MongoAnalyticStore GetAnalyticStore(string connectionString)
        {
            var store = (new MongoAnalyticStore(connectionString));
            return store;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseServerSideAnalytics(GetAnalyticStore(ConnectionString));
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "search",
                    template: "search",
                    defaults: new { controller = "Blog", action = "Search" });

                routes.MapRoute(
                    name: "mynuget",
                    template: "mynuget",
                    defaults: new { controller = "Home", action = "MyNuget" });

                routes.MapRoute(
                    name: "read",
                    template: "read/{link}",
                    defaults: new { controller = "Blog", action = "Read" });

                routes.MapRoute(
                    name: "privacy",
                    template: "privacy",
                    defaults: new { controller = "Home", action = "Privacy" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}