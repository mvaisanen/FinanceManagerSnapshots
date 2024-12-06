using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using FinanceManager.Server.Database;
using FinanceManager.Server.Database.Domain.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Actors;
using Server.Auth.Entities;
using Server.Auth.Helpers;
using Server.Auth.Services;
using Server.Database;
using Server.Services;

namespace Server
{
    public class Startup
    {
        private string _authConnStr;
        private string _dataConnStr;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _dataConnStr = configuration["DataConnStr"];
            _authConnStr = configuration["AuthConnStr"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }

            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost3000",
                    builder =>
                    {
                        builder.WithOrigins(new[] { "http://localhost:3000", "http://localhost:53573", "http://localhost:56918",
                                "http://localhost:64842", "http://localhost:64845", $"http://{localIP}:8006", "http://localhost:8006",
                                "https://localhost:3001"})
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders("Token-Expired"); //Need this for the token-expired header to not get dropped
                    });
            });

            services.AddControllers();

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2); ;
            //services.AddMvcCore().AddApiExplorer();

            services.AddTransient<GenTypescriptSdk, GenTypescriptSdk>();

            services.AddSingleton<IJwtHandler, JwtHandler>();
            services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

            //services.TryAddTransient<IHttpContextAccessor, HttpContextAccessor>(); //Needed If we need to access the context outside controller

            var jwtSection = Configuration.GetSection("jwt");
            var jwtOptions = new JwtOptions();
            jwtSection.Bind(jwtOptions);

            services.AddAuthentication(opts =>
            {               
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;
                cfg.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    //RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    ClockSkew = TimeSpan.Zero,
                };
                cfg.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            //Another option would be to just send 401 Unauthorized with some content like "Token expired"
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                };
            });

            services.Configure<JwtOptions>(jwtSection);

            services.AddAuthorization(opts =>
            {
                var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
                defaultAuthorizationPolicyBuilder =
                    defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
                opts.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
            });

            services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(_authConnStr));

            // add identity
            services.AddIdentityCore<AppUser>(opts =>
            {
                opts.Password.RequireDigit = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>();


            //Normal database with all the business logic, no auth stuff
            services.AddDbContext<FinanceManagerContext>(options =>
                options.UseSqlServer(_dataConnStr));

            //services.AddTransient<FinanceManagerSeeder>();
            services.AddTransient<FinanceManagerDemoSeeder>();

            Akka.Configuration.Config akkaConfig = @"akka.loglevel = DEBUG
                    akka.loggers=[""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]";
            services.AddSingleton(_ => ActorSystem.Create("FMSystem", akkaConfig));
            /*services.AddSingleton(sp =>
            {
                var bootstrap = BootstrapSetup.Create().WithConfig(akkaConfig);
                var di = DependencyResolverSetup.Create(sp);
                var actorSystemSetup = bootstrap.And(di);
                return ActorSystem.Create("FMSystem", actorSystemSetup);
            });*/

            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<FxService>();
            services.AddTransient<PortfolioService>();
            services.AddTransient<DividendService>();
            services.AddTransient<StockService>();
            services.AddTransient<ScreenerService>();

            //Akka Actor Registrations. These need a better system, or rather, most Actors can be replaced with normal service classes (ongoing work)
            services.AddSingleton<WatchlistActorProvider>(provider =>
            {
                var actorSystem = provider.GetService<ActorSystem>();
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var watchlistActor = actorSystem.ActorOf(Props.Create(() => new WatchlistActor(serviceScopeFactory)));
                return () => watchlistActor;
            });//HMMMM.....
            services.AddSingleton<StockActorProvider>(provider =>
            {
                var actorSystem = provider.GetService<ActorSystem>();
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var stockActor = actorSystem.ActorOf(Props.Create(() => new StockActor(serviceScopeFactory)));
                return () => stockActor;
            });//HMMMM.....
            services.AddSingleton<DividendActorProvider>(provider =>
            {
                var actorSystem = provider.GetService<ActorSystem>();
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var dividendActor = actorSystem.ActorOf(Props.Create(() => new DividendsActor(serviceScopeFactory)));
                return () => dividendActor;
            });//HMMMM.....
            services.AddSingleton<SchedulerActorProvider>(provider =>
            {
                var actorSystem = provider.GetService<ActorSystem>();
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var stock = provider.GetService<StockActorProvider>();
                var schedulerActor = actorSystem.ActorOf(Props.Create(() => new SchedulerActor(serviceScopeFactory, stock)));
                return () => schedulerActor;
            });//HMMMM.....           
            services.AddSingleton<ScreenerActorProvider>(provider =>
            {
                var actorSystem = provider.GetService<ActorSystem>();
                var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();
                var screenerActor = actorSystem.ActorOf(Props.Create(() => new ScreenerActor(serviceScopeFactory)));
                return () => screenerActor;
            });//HMMMM.....           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            lifetime.ApplicationStarted.Register(() =>
            {
                var system = app.ApplicationServices.GetRequiredService<ActorSystem>(); //Start the actor system
                var wl = app.ApplicationServices.GetRequiredService<WatchlistActorProvider>().Invoke();
                var stock = app.ApplicationServices.GetRequiredService<StockActorProvider>().Invoke();
                var scheduler = app.ApplicationServices.GetRequiredService<SchedulerActorProvider>().Invoke();
                var dividend = app.ApplicationServices.GetRequiredService<DividendActorProvider>().Invoke();
 
                //Startup seeding of stock - REMOVE for production use!
                //var file = "U.S.DividendChampions.xlsx";
                //stock.Tell(Messages.StockMessage.NewUpdateUsCCC(file));

            });
            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });
            app.UseCors("AllowLocalhost3000");


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection(); //Tämä tuli Core 2.1:ssä, vois olla kannattava!

            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseMiddleware<ErrorHandlerMiddleware>();

            // app.UseMiddleware<TokenManagerMiddleware>();

            UseDatabases();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                if (env.IsDevelopment())
                {
                    //scope.ServiceProvider.GetRequiredService<GenTypescriptSdk>().Generate();
                }
                if (Configuration["WipeDatabase"] != null && bool.Parse(Configuration["WipeDatabase"]))
                {
                    var seeder = scope.ServiceProvider.GetService<FinanceManagerDemoSeeder>(); //Switch this to demo seeder for demo usage
                    seeder.Seed().Wait();
                }
            }

            //app.UseMvc();

            app.UseRouting();
            app.UseAuthentication();           
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }

        protected virtual void UseDatabases()
        {
            var opts = new DbContextOptionsBuilder<AuthDbContext>();
            opts.UseSqlServer(_authConnStr);
            using (var client = new AuthDbContext(opts.Options))
            {
                if (Configuration["WipeDatabase"] != null && bool.Parse(Configuration["WipeDatabase"]))
                {
                    client.Database.EnsureDeleted();
                    client.Database.EnsureCreated();
                }
            }

            var fmOpts = new DbContextOptionsBuilder<FinanceManagerContext>();
            fmOpts.EnableDetailedErrors(true);
            fmOpts.UseSqlServer(_dataConnStr);
            using (var client = new FinanceManagerContext(fmOpts.Options))
            {
                if (Configuration["WipeDatabase"] != null && bool.Parse(Configuration["WipeDatabase"]))
                {
                    client.Database.EnsureDeleted();
                    client.Database.EnsureCreated();
                }
                else
                {
                    client.Database.Migrate();
                }
            }
        }
    }
}
