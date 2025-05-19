using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;

namespace secNET
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SecNETContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "secNET",
                        ValidAudience = "secNET",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                            Configuration.GetSection("JwtSettings:SecretKey").Value))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Path.StartsWithSegments("/Login") || context.Request.Path.StartsWithSegments("/BranchSelection"))
                            {
                                context.NoResult();
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AtLeastTier1", policy => policy.RequireRole("Tier1", "Tier2", "Tier3"));
                options.AddPolicy("AtLeastTier2", policy => policy.RequireRole("Tier2", "Tier3"));
                options.AddPolicy("AtLeastTier3", policy => policy.RequireRole("Tier3"));
            });

            services.AddRazorPages()
                .AddNewtonsoftJson();
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
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24 * 7;
                    ctx.Context.Response.Headers["Cache-Control"] = $"public, max-age={durationInSeconds}";
                }
            });

            app.UseSession();

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/Login") && !context.Request.Path.StartsWithSegments("/BranchSelection"))
                {
                    var token = context.Request.Cookies["jwtToken"];
                    if (!string.IsNullOrEmpty(token) && !context.Request.Headers.ContainsKey("Authorization"))
                    {
                        context.Request.Headers.Add("Authorization", "Bearer " + token);
                    }
                }
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/Login") ||
                    context.Request.Path.StartsWithSegments("/BranchSelection") ||
                    context.Request.Path.StartsWithSegments("/css") ||
                    context.Request.Path.StartsWithSegments("/js") ||
                    context.Request.Path.StartsWithSegments("/lib"))
                {
                    await next();
                    return;
                }

                if (!context.User.Identity.IsAuthenticated)
                {
                    context.Response.Redirect("/Login");
                    return;
                }

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}