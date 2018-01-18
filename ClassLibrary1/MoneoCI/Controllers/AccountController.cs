using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using MoneoCI.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MoneoCI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Models;
using MoneoCI.Repository;

namespace MoneoCI.Controllers
{
	public class Account
	{
		public string AccountNumber { get; set; }
		public decimal CurrentBalance { get; set; }
	}

	public class Transaction
	{
		[JsonProperty("transactionAmount")]
		public decimal transactionAmount { get; set; }
		[JsonProperty("transactionType")]
		public string transactionType { get; set; }

	}

	//[Produces("application/json")]
	[Route("api/[controller]")]
	public class AccountController : ControllerBase
	{
		readonly IMemoryCache _cache;
		readonly UserManager<IdentityUser> _userManager;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly IEmailSender _emailSender;
		readonly ISmsSender _smsSender;
		readonly ILogger _logger;
		readonly RoleManager<IdentityRole> _roleManager;

		public AccountController(
			UserManager<IdentityUser> userManager,
			SignInManager<IdentityUser> signInManager,
			IEmailSender emailSender,
			ISmsSender smsSender,
			ILoggerFactory loggerFactory,
			RoleManager<IdentityRole> roleManager,
			IMemoryCache cache
			)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_emailSender = emailSender;
			_smsSender = smsSender;
			_logger = loggerFactory.CreateLogger<AccountController>();
			_roleManager = roleManager;

			_cache = cache;
		}
		//[HttpGet]
		//[AllowAnonymous]
		//[Route("Gestor/")]
		//public JsonResultHelper  GestorGet()
		//{
		//	return null;
		//}

		[HttpGet]
		[AllowAnonymous]
		[Route("ListaCliente/")]
		public async Task<IActionResult> Get()
		{

			
			//var cts = new CancellationTokenSource();
			//_cache.Set(CacheKeys.DependentCTS, cts);

			//using (var entry = _cache.CreateEntry(CacheKeys.Parent))
			//{
			//	// expire this entry if the dependant entry expires.
			//	entry.Value = DateTime.Now;
			//	entry.RegisterPostEvictionCallback(DependentEvictionCallback, this);

			//	_cache.Set(CacheKeys.Child,
			//		DateTime.Now,
			//		new CancellationChangeToken(cts.Token));
			//}

			var user = await _userManager.FindByNameAsync("duartebeck");

			if (await _userManager.CheckPasswordAsync(user, "!UpX6j5Jm"))
			{
				List<Claim> userClaims = new List<Claim> { new Claim("userId", "23") };
				ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
				await HttpContext.Authentication.SignInAsync("CookieAuth", principal);
			}







			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Administrador" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Cliente" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Fornecedor" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "UsuarioLeitura" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "UsuarioEscrita" });


			//var appuser = new IdentityUser() { UserName = "duartebeck", Email = "duartebeck@hotmail.com", LockoutEnabled=false, EmailConfirmed=true   };


			//	await _roleManager.DeleteAsync((await _roleManager.FindByNameAsync("OutroAs")));



			//await _userManager.DeleteAsync(await _userManager.FindByNameAsync("duartebeck"));

			//	var t = await _userManager.CreateAsync(appuser, "!UpX6j5Jm");

			////var can = await _signInManager.CanSignInAsync(appuser);
			//var retorno = (await _userManager.CheckPasswordAsync((await _userManager.FindByNameAsync("duartebeck")),"!UpX6j5Jm"));





			//var kk = await HttpContext.Authentication.GetAuthenticateInfoAsync("CookieAuth");


			//var logado = _signInManager.IsSignedIn(HttpContext.User);


			//if (!logado)
			//{
			//	var res = await _signInManager.PasswordSignInAsync("duartebeck", "!UpX6j5Jm", true, false);



			//	if (res.Succeeded)
			//	{
			//		List<Claim> userClaims = new List<Claim> { new Claim("userId", "23") };
			//		ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
			//await HttpContext.Authentication.SignInAsync("CookieAuth", principal);
			//	}
			//}


			return Ok();
		}


		
		[Authorize]
		[HttpGet]
		[Route("InsertCliente/")]
		public async Task<IActionResult> InsertCliente()
		{
			await HttpContext.Authentication.SignOutAsync("CookieAuth");

			//			CreatedAtRoute()

			return Ok();
		}

		[Authorize(ActiveAuthenticationSchemes = "CookieAuth")]
		[HttpGet]
		[Route("EditCliente/")]
		
		public async Task<IActionResult> EditCliente()
		{

			var auth = await HttpContext.Authentication.GetAuthenticateInfoAsync("CookieAuth");

			var cl = auth.Principal.Claims;
			//HttpContext.Response.StatusCode = 400;
			//await HttpContext.Response.WriteAsync("Bad request.");

			return Ok();
		}

	


		[HttpGet]
		[ValidateAntiForgeryToken]
		[Authorize]
		//[Authorize(ActiveAuthenticationSchemes = "CookieAuth")]
		[Route("GetEmails/")]
		[ResponseCache]
		public IQueryable<EmailViewModel> GetEmails()
		{
			var emails = new List<EmailViewModel>() {
				new EmailViewModel() { Email = "duartebeck@hotmail.com", Nome = "Beck", Telefone = "71981273460" },
				new EmailViewModel() { Email = "duartebeck@gmail.com", Nome = "Beck", Telefone = "7121377412"  }}.AsQueryable();

			//HttpContext.Session.GetJson<string>();

			
			
			if (_cache.Get("teste") == null)
				_cache.Set("teste", emails);



			return _cache.Get("teste") as IQueryable<EmailViewModel>;
		}



		
	}
}