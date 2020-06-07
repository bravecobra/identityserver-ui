// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServerSts.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;
using System.Reflection;
using IdentityServerSts.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace IdentityServerSts
{
    /// <summary>
    /// This is the summary of the startup class
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("IdentityDb")));
            services.AddDistributedMemoryCache();
            services.AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = false;
                })
                .AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
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
                    options.UserInteraction.LoginUrl = "/Account/Login";
                    options.UserInteraction.LogoutUrl = "/Account/Logout";
                    options.Authentication = new IdentityServer4.Configuration.AuthenticationOptions
                    {
                        CookieLifetime = TimeSpan.FromHours(10), // ID server cookie timeout set to 10 hours
                        CookieSlidingExpiration = true
                    };
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

            services.AddAuthentication(options =>
                {
                    // options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    // options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    // options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogle("Google", options =>
                {
                    //options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SaveTokens = true;

                    options.ClientId = _configuration.GetValue<string>("Google:ClientId");
                    options.ClientSecret = _configuration.GetValue<string>("Google:ClientSecret");
                })
            //    .AddOpenIdConnect("oidc", "Some Other OIDC", options =>
            //     {
            //         options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
            //         options.SignOutScheme = IdentityServerConstants.SignoutScheme;
            //         options.SaveTokens = true;
            //
            //         options.Authority = "<OIDC Authotiry URI>";
            //         options.ClientId = "<ClientID>";
            //         options.ClientSecret = "<ClientSecret>";
            //         options.ResponseType = "code";
            //
            //         options.Scope.Clear();
            //         options.Scope.Add("openid");
            //         options.CallbackPath = new PathString("/callback");
            //         options.ClaimsIssuer = "<ClaimIssuer>";
            //         // Saves tokens to the AuthenticationProperties
            //         options.SaveTokens = true;
            //
            //         options.Events = new OpenIdConnectEvents
            //         {
            //             // handle the logout redirection
            //             OnRedirectToIdentityProviderForSignOut = (context) =>
            //             {
            //                 var logoutUri =
            //                     $"<Logout URI>";
            //
            //                 var postLogoutUri = context.Properties.RedirectUri;
            //                 if (!string.IsNullOrEmpty(postLogoutUri))
            //                 {
            //                     if (postLogoutUri.StartsWith("/"))
            //                     {
            //                         // transform to absolute
            //                         var request = context.Request;
            //                         postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase +
            //                                         postLogoutUri;
            //                     }
            //
            //                     logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
            //                 }
            //
            //                 context.Response.Redirect(logoutUri);
            //                 context.HandleResponse();
            //
            //                 return Task.CompletedTask;
            //             }
            //         };
            //     }
            // )
            ;

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => Environment.IsProduction();
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
                options.SlidingExpiration = true;

            });
            services.AddSingleton<IEmailSender, EmailSender>();
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
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
