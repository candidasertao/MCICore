using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Threading;
using System.Threading.Tasks;


namespace MoneoCI.Controllers
{
	public class HomeController : Controller
	{
		readonly UserManager<IdentityUser> _userManager;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly RoleManager<IdentityRole> _roleManager;


		public HomeController(
			UserManager<IdentityUser> userManager, 
			SignInManager<IdentityUser> signInManager, 
			RoleManager<IdentityRole> roleManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Index()
		{


			//await _roleManager.CreateAsync(new IdentityRole() { Name = "OutroAs" });


			//var appuser = new IdentityUser() { UserName = "duartebeck", Email = "duartebeck@hotmail.com", LockoutEnabled=false, EmailConfirmed=true   };


			//	await _roleManager.DeleteAsync((await _roleManager.FindByNameAsync("OutroAs")));



			//await _userManager.DeleteAsync(await _userManager.FindByNameAsync("duartebeck"));

			//	var t = await _userManager.CreateAsync(appuser, "!UpX6j5Jm");

			////var can = await _signInManager.CanSignInAsync(appuser);
			//var retorno = (await _userManager.CheckPasswordAsync((await _userManager.FindByNameAsync("duartebeck")),"!UpX6j5Jm"));

			//await _signInManager.SignOutAsync();
			//await HttpContext.Authentication.SignOutAsync("CookieAuth");




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


			//await _signInManager.SignOutAsync();


			//var appuser = new IdentityUser() { UserName="duartebeck", Email="duartebeck@hotmail.com" };

			////_userManager.RemoveLoginAsync(appuser, )

			//var handler = new JwtSecurityTokenHandler();

			//ISecureDataFormat<AuthenticationTicket>

			//handler.CreateJwtSecurityToken(new SecurityTokenDescriptor() {Audience="teste", Expires=DateTime.Now.AddHours(1), SigningCredentials = new SigningCredentials(SecurityKey)

			////var ko = _userManager.FindByNameAsync("duartebeck");
			////ko.Wait();

			//var claim = new Claim(ClaimTypes.Email, "duartebeck@hotmail.com");


			////var ii = await _userManager.DeleteAsync(await _userManager.FindByNameAsync("duartebeck"));



			//var t = await _userManager.CreateAsync(appuser, "!UpX6j5Jm");
			//var k = await _userManager.AddClaimAsync(appuser, claim);

			//Defini uma data de expiração do Token
			//var expires = DateTime.UtcNow.AddMinutes(5);
			////Cria uma instancia da classe que gera o token
			//var handler = new JwtSecurityTokenHandler();
			////Criar as claims do usuário
			//var identity = new ClaimsIdentity(new GenericIdentity("duartebeck", "TokenAuth"), new[] {
			//	new Claim("UserId", "1", ClaimValueTypes.Integer),
			//	new Claim("ClienteID", "1", ClaimValueTypes.Integer),
			//	new Claim(ClaimTypes.Role, "Admin") });


			//// Gera as infos que iram constar token de segurança
			//var securityToken = handler.CreateToken(new SecurityTokenDescriptor()
			//{
			//	Issuer = _tokenOptions.Issuer,
			//	Audience = _tokenOptions.Audience,
			//	SigningCredentials = _tokenOptions.SigningCredentials,
			//	Subject = identity,
			//	Expires = expires,
			//	IssuedAt = DateTime.Now
			//});

			//// Escreve o token de segurança
			//var token = handler.WriteToken(securityToken);

			//// retorna o token com as informações desejadas.
			//var _return = new { authenticated = true, entityId = 1, token = token, tokenExpires = expires };

			//var k = handler.ReadJwtToken(token);

			HttpContext.Response.StatusCode = 400;
			await HttpContext.Response.WriteAsync("Bad request.");


			//var jj = await HttpContext.Authentication.GetTokenAsync("access_token");


			return View();
		}
		public IActionResult Error()
		{
			return View();
		}
		public IActionResult EmailSend()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EmailSend(EmailViewModel model)
		{
			


			Thread.Sleep(10000);

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			return  Ok("121");
		}
	}
}
