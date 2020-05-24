// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Reflection;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServerAspNetIdentity.Models;
using IdentityServerSts.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServerSts
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("IdentityDb")));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddControllersWithViews();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
            });

            var builder = services.AddIdentityServer(options =>
                {
                    options.IssuerUri = _configuration.GetValue<string>("IdentityServer:IssuerUri");
                    options.PublicOrigin = _configuration.GetValue<string>("IdentityServer:PublicOrigin");
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddAspNetIdentity<ApplicationUser>()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(_configuration.GetConnectionString("ConfigurationDb"),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(_configuration.GetConnectionString("PersistedGrantDb"),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                });

            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                var list = new List<string>();
                _configuration.GetSection("Auth:AllowCORS").Bind(list);
                options.AddPolicy("default", policy =>
                {
                    foreach (var allowedCors in list)
                    {
                        policy.WithOrigins(allowedCors)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }

                });
            });

            services.AddAuthentication()
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = _configuration.GetValue<string>("Google:ClientId");
                    options.ClientSecret = _configuration.GetValue<string>("Google:ClientSecret");
                });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders();
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("default");

            app.UseIdentityServer();

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
