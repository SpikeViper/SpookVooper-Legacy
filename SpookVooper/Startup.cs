using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Services;
using SpookVooper.Data.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Westwind.AspNetCore.Markdown;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Microsoft.AspNetCore.SignalR;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using AutoMapper;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Newtonsoft.Json;
using SpookVooper.Web;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Hubs;

namespace SpookVooper
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            if (!File.Exists("sv_config.json"))
            {
                Config config = new Config();
                string ser = JsonConvert.SerializeObject(config);
                File.WriteAllText("sv_config.json", ser);
                Secrets.config = config;
            }
            else
            {
                string ser = File.ReadAllText("sv_config.json");
                Config config = JsonConvert.DeserializeObject<Config>(ser);
                Secrets.config = config;
            }


            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {

                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(_ => true)
                        .AllowCredentials()
                        .WithOrigins("https://spookvooper.com",
                        "https://www.spookvooper.com",
                        "http://spookvooper.com",
                        "http://www.spookvooper.com",
                        "https://vooper.io",
                        "https://www.vooper.io",
                        "http://vooper.io",
                        "http://www.vooper.io",
                        "http://localhost:5000",
                        "https://localhost:5000",
                        "http://localhost:5001",
                        "https://localhost:5001");
                });
            });

            services.AddSignalR();

            services.AddDbContextPool<VooperContext>(options => {
                options.UseMySql(Secrets.DBstring, ServerVersion.FromString("8.0.20-mysql"), options => options.EnableRetryOnFailure().CharSet(CharSet.Utf8Mb4));
            });

            services.AddDbContextPool<NerdcraftContext>(options => {
                options.UseMySql(Secrets.NCDBString, ServerVersion.FromString("8.0.20-mysql"), options => options.EnableRetryOnFailure().CharSet(CharSet.Utf8Mb4));
            });

            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<VooperContext>()
                .AddDefaultTokenProviders();

            // Fix keyring issues
            services.AddDataProtection().SetApplicationName("SpookVooper").PersistKeysToFileSystem(new System.IO.DirectoryInfo(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Keys"));

            services.AddSingleton<IConnectionHandler, ConnectionHandler>();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(150);
                // If the LoginPath isn't set, ASP.NET Core defaults 
                // the path to /Account/Login.
                options.LoginPath = "/Account/Login";
                // If the AccessDeniedPath isn't set, ASP.NET Core defaults 
                // the path to /Account/AccessDenied.
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddAuthorization(config =>
            {
                config.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            });

            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.AddMarkdown(config =>
                        {
                // Create custom MarkdigPipeline 
                // using MarkDig; for extension methods
                config.ConfigureMarkdigPipeline = builder =>
                            {
                                builder.UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Default)
                                    .UsePipeTables()
                                    .UseGridTables()
                                    .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // Headers get id="name" 
                                    .UseAutoLinks() // URLs are parsed into anchors
                                    .UseAbbreviations()
                                    .UseYamlFrontMatter()
                                    .UseEmojiAndSmiley(true)
                                    .UseListExtras()
                                    .UseFigures()
                                    .UseTaskLists()
                                    .UseCustomContainers()
                                    .DisableHtml()   // renders HTML tags as text including script
                                    .UseGenericAttributes();
                            };
                        });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseFileServer();
            StaticFileOptions option = new StaticFileOptions();
            FileExtensionContentTypeProvider contentTypeProvider = (FileExtensionContentTypeProvider)option.ContentTypeProvider ??
            new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings.Add(".unityweb", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".mem", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".data", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".memgz", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".datagz", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".unity3dgz", "application/octet-stream");
            contentTypeProvider.Mappings.Add(".jsgz", "application/x-javascript; charset=UTF-8");
            option.ContentTypeProvider = contentTypeProvider;
            app.UseStaticFiles(option);

            if (env.IsDevelopment())
            {
                Console.WriteLine("///////////////////////////////////");
                Console.WriteLine("Application is in DEVELOPMENT mode.");
                Console.WriteLine("///////////////////////////////////");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHub<TransactionHub>("/transactionHub");
                endpoints.MapHub<ExchangeHub>("/ExchangeHub");
            });

            TransactionHub.Current = app.ApplicationServices.GetService<IHubContext<TransactionHub>>();
            ExchangeHub.Current = app.ApplicationServices.GetService<IHubContext<ExchangeHub>>();
        }
    }
}
