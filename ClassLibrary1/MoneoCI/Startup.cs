
using FluentValidation.AspNetCore;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MoneoCI.Controllers;
using MoneoCI.Data;
using MoneoCI.Helpers;
using MoneoCI.Repository;
using MoneoCI.Services;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using IdentityModel;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace MoneoCI
{
	public class Startup
	{

		private RsaSecurityKey _key;
		private TokenAuthOptions _tokenOptions;

		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			//if (env.IsDevelopment())
			//builder.AddUserSecrets("");





			builder.AddEnvironmentVariables();
			Configuration = builder.Build();


		}

		public IConfigurationRoot Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{


			//services.AddDistributedRedisCache(options =>
			//{

			//	options.Configuration = "localhost";
			//	options.InstanceName = "SampleInstance";
			//});

			services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
			services.AddScoped<AngularAntiForgeryTokenAttribute>();
			services.AddScoped<IRepository<GestorModel>, GestorRepository>();
			services.AddScoped<IRepository<CarteiraModel>, CarteiraRepository>();
			services.AddScoped<IRepository<TipoCampanhaModel>, TipoCampanhaRepository>();
			services.AddScoped<IRepository<CampanhaModel>, CampanhaRepository>();
			services.AddScoped<IRepository<BlackListModel>, BlacklistRepository>();
			services.AddScoped<IRepository<UsuarioModel>, UsuarioRepository>();
			services.AddScoped<IRepository<PadraoPostagensModel>, PadraoPostagensRepository>();
			services.AddScoped<IRepository<SegmentacaoModel>, SegmentacaoRepository>();
			services.AddScoped<IRepository<FornecedorModel>, FornecedorRepository>();
			services.AddScoped<IRepository<GrupoUsuariosModel>, GrupoUsuariosRepository>();
			services.AddScoped<IRepository<ClienteModel>, ClienteRepository>();
			services.AddScoped<IRepository<RetornoModel>, RetornoRepository>();
			services.AddScoped<IRepository<LogAtividadeModel>, LogAtividadeRepository>();
			services.AddScoped<IRepository<InterjeicaoModel>, InterjeicaoRepository>();
			services.AddScoped<IRepository<ConsolidadoModel>, ConsolidadoRepository>();
			services.AddScoped<IRepository<SessionDataModel>, SessionDataRepository>();
			services.AddScoped<IRepository<LeiauteModel>, LeiauteRepository>();
			services.AddScoped<IRepository<UsersResetPasswordModel>, UsersResetPasswordRepository>();

			services.AddResponseCompression(options =>
			{
				options.Providers.Add<GzipCompressionProvider>();
			});
			services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

			services.AddDistributedMemoryCache();
			services.AddMemoryCache();
			services.AddSession(options =>
			{
				//options.CookieName = ".MoneoCI.Session";
				options.IdleTimeout = TimeSpan.FromMinutes(30);

			});

			//ClaimTypes

			services.AddIdentityServer()
						//.AddClientStore<ClientStores>()
						//.AddResourceStore<ResourceStores>()
						.AddInMemoryClients(Config.GetClientes())
						.AddInMemoryApiResources(Config.GetApiResources())
						.AddInMemoryIdentityResources(Config.GetIdentityResources())
						.AddInMemoryPersistedGrants()
						//.addasp
						.AddProfileService<IdentityWithAdditionalClaimsProfileService>();

						//.AddTemporarySigningCredential();

			//services.AddDbContext<ApplicationDbContext>(options =>
			//{
			//	options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
			//});



			services.AddIdentity<IdentityUser, IdentityRole>(options =>
			{

				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.SignIn.RequireConfirmedEmail = false;
				options.SignIn.RequireConfirmedPhoneNumber = false;
				options.User.RequireUniqueEmail = true;
				
				
				//options.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents
				//{
				//	OnRedirectToLogin = ctx =>
				//	{
				//		if (ctx.Request.Path.StartsWithSegments("/api"))
				//		{
				//			ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
				//			//ctx.Response.WriteAsync("{\"error\": " + ctx.Response.StatusCode + "}");
				//		}
				//		else
				//		{
				//			ctx.Response.Redirect(ctx.RedirectUri);
				//		}
				//		return Task.FromResult(0);
				//	}
				//};
				options.Lockout.MaxFailedAccessAttempts = 10;


			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			services.AddMvcCore()
				.AddJsonOptions(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
				.AddAuthorization()
				.AddJsonFormatters(option => option.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

			services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = "oidc";
			})
				.AddCookie(options =>
				{
					
					options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
					options.Cookie.Name = "mvcimplicit";
				});
			//services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
			//   .AddIdentityServerAuthentication(options =>
			//   {
			//	   options.Authority = Constants.Authority;
			//	   options.RequireHttpsMetadata = false;

			//	   options.ApiName = "api1";
			//	   options.ApiSecret = "secret";
			//   }).AddCookie(;



			services.AddMvc().AddFluentValidation();

			// Add application services.
			services.AddTransient<IEmailSender, AuthMessageSender>();
			services.AddTransient<ISmsSender, AuthMessageSender>();
		}
		ValueTask<int> Tarefa(int valor)
		{
			return new ValueTask<int>(0);
		}
		public RSAParameters GetRSAParameters()
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
			{
				try
				{
					return rsa.ExportParameters(true);
				}
				finally
				{
					rsa.PersistKeyInCsp = false;
				}
			}
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IAntiforgery antiforgery, IMemoryCache cache)
		{

			var supportedCultures = new[] { new CultureInfo("pt-BR") };

			app.UseRequestLocalization(new RequestLocalizationOptions() { DefaultRequestCulture = new RequestCulture("pt-BR"), SupportedCultures = supportedCultures, SupportedUICultures = supportedCultures });

#pragma warning disable 4014
			Util.CacheFactory<IEnumerable<PrefixoModel>>(cache, "prefixos", env);
			Util.CacheFactory<IEnumerable<PrefixoModel>>(cache, "nextel", env);
			Util.CacheFactory<HashSet<decimal>>(cache, "quarentena", env);
#pragma warning restore 4014

			app.UseSession();

			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();


			//if (env.IsDevelopment())
			//{
			app.UseDeveloperExceptionPage();
			
			app.UseBrowserLink();
			//}
			//else
			//	app.UseExceptionHandler("/Home/Error");




			app.UseResponseCompression();

			app.UseAuthentication();
			app.UseIdentityServer();

			app.Use(next => context =>
			{
				if (string.Equals(context.Request.Path.Value, "/", StringComparison.OrdinalIgnoreCase))
				{
					var tokens = antiforgery.GetAndStoreTokens(context);
					context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions() { HttpOnly = false });
				}
				return next(context);
			});

			///JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			
			
			//app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions()
			//{
			//	Authority = Util.Configuration["UrlIdentity"],
			//	EnableCaching = true,
			//	CacheDuration = TimeSpan.FromMinutes(10), // that's the default
			//	AutomaticAuthenticate = true,
			//	RoleClaimType = ClaimTypes.Role,
			//	AutomaticChallenge = true,
			//	RequireHttpsMetadata = false,
			//	SupportedTokens = SupportedTokens.Both,
			//	AllowedScopes = { "moneoci", "fornecedor" }
			//});

			app.UseDefaultFiles();
			app.UseStaticFiles();

			// app.UseMiddleware<TokenProviderMiddleware>(Options.Create(new TokenProviderOptions
			//{
			//    Audience = "ExampleAudience",
			//    Issuer = "ExampleIssuer",
			//    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256)
			//}));

			app.UseMvc();


		}
	}
}