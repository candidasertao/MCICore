
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4.Endpoints;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MoneoCI.Data;
using MoneoCI.Repository;
using MoneoCI.Services;
using Helpers;
using Atributos;
using DTO;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize]
	public class LoginController : ControllerBase
	{
		readonly IMemoryCache _cache;

		//private readonly OpenIddictApplicationManager<OpenIddictApplication> _applicationManager;
		readonly ApplicationDbContext _applicationDbContext;
		readonly IOptions<IdentityOptions> _identityOptions;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly UserManager<IdentityUser> _userManager;
		readonly RoleManager<IdentityRole> _roleManager;
		readonly IRepository<UsersResetPasswordModel> repository = null;

		public int? UsuarioID
		{
			get
			{

				if (User.Identity.IsAuthenticated)
				{
					var result = User.Claims.Where(a => a.Type == "usuarioid");
					if (result.Count() > 0)
						return new Nullable<int>(int.Parse(result.ElementAt(0).Value));
				}

				return new Nullable<int>();
			}
		}

		public int ClienteID
		{
			get
			{

				if (User.Identity.IsAuthenticated)
					return int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value);

				return 0;
			}
		}


		public LoginController(
			//OpenIddictApplicationManager<OpenIddictApplication> applicationManager, 
			ApplicationDbContext applicationDbContext,
			 IOptions<IdentityOptions> identityOptions,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager,
			IMemoryCache cache,
			IRepository<UsersResetPasswordModel> repos
			)
		{
			//_applicationManager = applicationManager;
			_applicationDbContext = applicationDbContext;
			_identityOptions = identityOptions;
			_signInManager = signInManager;
			_userManager = userManager;
			_roleManager = roleManager;
			_cache = cache;
			repository = repos;
		}
        
		[HttpGet("bearer/")]
		public async Task<IActionResult> Bearer()
		{

			var client = new DiscoveryClient(Util.Configuration["UrlIdentity"]);
			client.Policy.RequireHttps = false;
			var disco = await client.GetAsync();

			// request token
			var tokenClient = new TokenClient(disco.TokenEndpoint, "oauthClient", "supers", AuthenticationStyle.BasicAuthentication);
			var tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync("scott", "Ms6tgs1234", "api2");

			var clienteHttp = new HttpClient();
			clienteHttp.SetBearerToken(tokenResponse.AccessToken);
			var response = await clienteHttp.GetAsync("http://conecttaoffice:12201/identity/");

			return Ok(response);
		}

        [AllowAnonymous]
		[HttpGet("regrava")]
		public async Task<IActionResult> Regrava()
		{
			try
			{
                
				var d = await _userManager.DeleteAsync(await _userManager.FindByNameAsync("0759196000126"));
                
				IdentityUser user;

				List<dynamic> _fornecedores = new List<dynamic>() { };

				var listaFornecedores = await new FornecedorRepository().ListaFornecedoresCadastro();

				foreach (var item in listaFornecedores.Where(a => a.CPFCNPJ == "0759196000126"))
				{
					var senha = "3DrpHIDg";

					//checando existência de usuário
					var _userExistentLogin = await _userManager.FindByNameAsync(item.CPFCNPJ);
					var _userExistentEmail = await _userManager.FindByEmailAsync(item.EMAIL);

					if (_userExistentEmail == null && _userExistentEmail == null)
					{

						//montagem do fornecedor
						user = new IdentityUser { UserName = item.CPFCNPJ, Email = item.EMAIL, LockoutEnabled = true, EmailConfirmed = true };


						//criando o usuário
							var criacaoUser = await _userManager.CreateAsync(user, senha);

						if (criacaoUser.Succeeded)
						{
							user = await _userManager.FindByNameAsync(item.CPFCNPJ);

							await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Fornecedor"),
					new Claim("fornecedorid", item.FORNECEDORID.ToString())});

							await _userManager.AddToRoleAsync(user, "Fornecedor");

							
						}
					}
					else
						_fornecedores.Add(item);
				}

				return Ok(_fornecedores);
			}
			catch (Exception)
			{

				throw;
			}


		}

        [HttpGet("addclaim/{usuario}")]
		public async Task<IActionResult> AddClaim(string usuario)
		{
			//checando existência de usuário
			var _userExistent = await _userManager.FindByNameAsync(usuario);

			var result = await _userManager.AddClaimsAsync(_userExistent, new List<Claim> {
					new Claim(ClaimTypes.GroupSid, "4")
					});


			return Ok();


		}

		[HttpGet("gravaavulso/")]
		public async Task<IActionResult> GravaAvulso()
		{
			try
			{
				ContatoModel[] contato = { new ContatoModel() { Celular = 71981273460, Email = "ricardo@conecttasoftwares.com.br" } };

				var cliente = new ClienteModel() { Contatos = contato, CNPJ = "07459196000126" };

				if (string.IsNullOrEmpty(cliente.Contatos.ElementAt(0).Email))
					throw new Exception($"Cliente sem e-mail informado pra cadastro");

				var senha = Uteis.GeraSenha();

				//checando existência de usuário
				var _userExistent = await _userManager.FindByNameAsync(cliente.CNPJ);

				if (_userExistent != null)
					throw new Exception($"usuário {cliente.CNPJ} já existente");

				//montagem do usuário
				var user = new IdentityUser { UserName = cliente.CNPJ, Email = cliente.Contatos.ElementAt(0).Email, LockoutEnabled = true, EmailConfirmed = true };
				var clienteid = 1;

				//criando o usuário
				var criacaoUser = await _userManager.CreateAsync(user, Uteis.GeraSenha());


				if (criacaoUser.Succeeded)
				{
					user = await _userManager.FindByNameAsync(cliente.CNPJ);

					//adicionando as claims
					var _resultClaim = await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Cliente"),
					new Claim("clienteid", clienteid.ToString())
					});

					//adicionando o usuário a role
					await _userManager.AddToRoleAsync(user, "Cliente");
				}
				else
					throw new Exception(criacaoUser.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));


			}
			catch (Exception err)
			{
				throw err;
			}
			return Ok();
		}

		[HttpGet("set/carteira/{id:int}/arquivo/{file}")]
		public async Task<IActionResult> GravaArquivo(int id, string file)
		{

			var usuario = new UsuarioModel() { LoginUser = "brasilina", Email = "brasilinaa@terra.com.br" };

			var senha = Uteis.GeraSenha();

			//checando existência de usuário
			var _userExistent = await _userManager.FindByNameAsync(usuario.LoginUser);

			if (_userExistent != null)
				throw new Exception($"usuário {usuario.LoginUser} já existente");

			//montagem do usuário
			var user = new IdentityUser { UserName = usuario.LoginUser, Email = usuario.Email, LockoutEnabled = true, };
			var usuarioid = 2;

			//criando o usuário
			var criacaoUser = await _userManager.CreateAsync(user, Uteis.GeraSenha());

			if (criacaoUser.Succeeded)
			{
				user = await _userManager.FindByNameAsync(usuario.LoginUser);

				//adicionando as claims
				var _resultClaim = await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Usuario"),
					new Claim("clienteid", "1"),
					new Claim("usuarioid", usuarioid.ToString())});

				//adicionando o usuário a role
				await _userManager.AddToRoleAsync(user, "Usuario");
			}


			return Ok(new LoginViewModel() { Username = "teste", Password = "1234" });

		}

		[HttpGet("get/token/{audience}/{id}")]
		public IActionResult TesteCache(string audience, string id)
		{
			string guidapi = Guid.NewGuid().ToString();

			List<Claim> claim = new List<Claim>() { };
			switch (audience)
			{
				
				case "RatinhoAPI":
					claim.Add(new Claim("ratinho", guidapi));
					break;
				case "FornecedorAPI":
					claim.Add(new Claim("fornecedorid", id));
					break;
				case "ClientAPI":
					claim.Add(new Claim("clienteid", "1"));
					claim.Add(new Claim("usuarioid", "2"));
					break;
			}

			if (audience == "FornecedorAPI")
				new FornecedorRepository().AtualizaAPIKey(new FornecedorModel()
				{
					FornecedorID = int.Parse(id),
					ApiKey = guidapi
				});


			claim.Add(new Claim(JwtRegisteredClaimNames.Jti, guidapi));

			var _options = new TokenProviderOptions
			{
				Audience = audience,
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Util.GetKeyEncodingToken)), SecurityAlgorithms.HmacSha256)
			};

			// Create the JWT and write it to a string
			var _jwt = new JwtSecurityToken(
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claim,
				notBefore: DateTime.Now,
				//expires: DateTime.Now.Add(_options.Expiration),
				signingCredentials: _options.SigningCredentials);
			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(_jwt);
			return Ok(new { Token = encodedJwt });
		}
		
		[HttpPost("fornecedor")]
		public async Task<IActionResult> LogarFornecedor([FromBody] EmailViewModel c)
		{


			var client = new DiscoveryClient(Util.Configuration["UrlIdentity"]);
			client.Policy.RequireHttps = false;
			var disco = await client.GetAsync();
			var tokenClient = new TokenClient(disco.TokenEndpoint, "emailreset", "ms6tgsoem2650");

			var tokenResponse = await tokenClient.RequestClientCredentialsAsync();

			var introspectionClient = new IntrospectionClient(disco.IntrospectionEndpoint, "brasilina", "reset");

			var response = await introspectionClient.SendAsync(new IntrospectionRequest { Token = tokenResponse.AccessToken });
			var isActice = response.IsActive;
			var claims = response.Claims;
			return Ok(tokenResponse);
		}

		[AllowAnonymous]
		[HttpPost("token/")]
		public async Task<IActionResult> TokenAuthentication([FromBody] LoginViewModel m)
		{
			IActionResult res = null;
			var _b = new BaseEntityDTO<LoginViewModel>() { Start = DateTime.Now, Itens = 1 };
			try
			{
				var _urp = new UsersResetPasswordModel() { Token = m.Token };

				var t = await new UsersResetPasswordRepository().FindById(_urp, null);

				if (t == null)
					throw new Exception($"Link expirado para redefinição de senha");

				var existUser = await _userManager.FindByNameAsync(t.LoginUser);

				if (existUser == null)
					throw new Exception($"Usuário não encontrado");

				string tokenPassword = await _userManager.GeneratePasswordResetTokenAsync(existUser);

				await _userManager.ResetAccessFailedCountAsync(existUser);

				string senha = Uteis.GeraSenha();

				var result = await _userManager.ResetPasswordAsync(existUser, tokenPassword, senha);

				if (!result.Succeeded)
					throw new Exception("Houve um erro ao tentar zerar a senha");

				result = await _userManager.ChangePasswordAsync(existUser, senha, m.Password);

				if (!result.Succeeded)
					throw new Exception(result.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));

				await repository.Update(new UsersResetPasswordModel[] { new UsersResetPasswordModel() {
						Token= m.Token,
						SenhaTrocada=true,
						LoginUser=t.LoginUser
					 } }, 0, null);

				m.Username = existUser.UserName;

				_b.Result = await AutenticaLogin(m, existUser);

				_b.End = DateTime.Now;
				_b.Itens = 1;
				res = Ok(_b);

			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);

			}

			return res;
		}

		async Task<LoginViewModel> AutenticaLogin(LoginViewModel l, IdentityUser existUser)
		{
			var client = new DiscoveryClient(Util.Configuration["UrlIdentity"]);
			client.Policy.RequireHttps = false;
			var disco = await client.GetAsync();

			var tokenClient = new TokenClient(disco.TokenEndpoint, "oauthClient", "rv2b7000438dm");
			var tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync(existUser.UserName, l.Password, "moneoci");

			if (tokenResponse.Exception != null)
				throw tokenResponse.Exception;

			if (!string.IsNullOrEmpty(tokenResponse.Error))
				throw new Exception(tokenResponse.Error);

			var role = await _userManager.GetRolesAsync(existUser);

			if (!role.Any())
				throw new Exception($"Sem Role associada ao usuário {l.Username}");

			switch (role[0])
			{
				case "Usuario":
					var user = await new UsuarioRepository().UsuarioByLoginUser(new UsuarioModel() { LoginUser = l.Username });

					var dados = await new GrupoUsuariosRepository().PaginasPermissao(new GrupoUsuariosModel() { GrupoUsuarioID = user.GrupoUsuario.GrupoUsuarioID }, user.Cliente.ClienteID);

					l.Nome = user.Nome;
					l.Organizacao = user.Cliente.Nome;
					l.AdmPerfil = user.AdmPerfil;
					l.ID = user.UsuarioID.ToString();
					if (dados != null)
						l.GrupoUsuarioPages = new
						{
							grupo = dados.Nome,
							grupousuarioid = dados.GrupoUsuarioID,
							paginas = dados.GrupoUserPaginas.Select(k => new
							{
								pagina = k.Pagina.Pagina,
								tipoacesso = k.TipoAcesso,
								paginaid = k.Pagina.PaginaID,
								subpaginaid = k.Pagina.SubPagina.SubPaginaID
							})
						};

					break;
				case "Cliente":
					var cliente = await new ClienteRepository().ClienteLogin(new ClienteModel() { CNPJ = l.Username });
					if (cliente == null)
						throw new Exception("Cliente não autorizado");

					l.Nome = cliente.Nome;
					l.ID = cliente.ClienteID.ToString();
					break;

				case "Fornecedor":
					var fornecedor = await new FornecedorRepository().FornecedorByLogin(new FornecedorModel() { CPFCNPJ = l.Username });
					if (fornecedor == null)
						throw new Exception("Fornecedor não autorizado");

					l.Nome = fornecedor.Nome;
					l.ID = fornecedor.FornecedorID.ToString();
					break;

				case "AdminOnly":
					l.Nome = "Ricardo Beck";
					l.ID = "89898";
					break;

			}
			l.Guid = Guid.NewGuid().ToString();
			l.Token = tokenResponse.AccessToken;
			l.Role = role[0];
			l.Password = null;

			return l;
		}

		[AllowAnonymous]
		[HttpPost("logar/")]
		public async Task<IActionResult> Logar([FromBody] LoginViewModel l)
		{
			IActionResult res = null;

			var _b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now, Itens = 1 };
			var existUser = await _userManager.FindByNameAsync(l.Username);

			try
			{
				if (existUser != null)
				{
					if (!existUser.LockoutEnabled)
						throw new Exception("Login inativo no sistema");

					var checkSenha = await _userManager.CheckPasswordAsync(existUser, l.Password);

					if (checkSenha)
					{
						var result = await AutenticaLogin(l, existUser);
						_b.Result = result;
						_b.End = DateTime.Now;
						_b.Itens = 1;
						res = Ok(_b);

					}
					else
						res = BadRequest(new { Itens = 1, _b.Start, End = DateTime.Now, Result = "Problemas com a senha informada" });
				}
				else
					res = BadRequest(new { Itens = 1,  _b.Start, End = DateTime.Now, Result = "Usuário não encontrado com a senha informada" });
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}

			return res;

		}

		[AllowAnonymous]
		[HttpPost("logout/")]
		public async Task<IActionResult> Logout([FromBody] LoginViewModel l)
		{
			IActionResult res = null;

			var _b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now, Itens = 1 };

			try
			{
                if (User.FindFirst(a => a.Type == "fornecedorid") == null)
                {
                    var x = HttpContext.Request.Headers["Guid"];
				    await new SessionDataRepository().Remove(new SessionDataModel[] { new SessionDataModel { Guid = x } }, ClienteID, null);
                }

				_b.End = DateTime.Now;
				_b.Itens = 1;
				res = Ok(_b);
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}

			return res;
		}

		[HttpPost("trocasenha")]
		public async Task<IActionResult> TrocarSenha([FromBody] LoginViewModel l)
		{
			IActionResult res = null;
			var _b = new BaseEntityDTO<string>() { Start = DateTime.Now };

			try
			{
				var existUser = await _userManager.FindByNameAsync(l.Username);
				if (existUser == null)
					throw new Exception($"{l.Username} não encontrado");


				var result = await _userManager.ChangePasswordAsync(existUser, l.Password, l.NewPassword);

				if (!result.Succeeded)
					throw new Exception(result.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));

				_b.Result = "Senha trocada com sucesso";
				_b.End = DateTime.Now;
				_b.Itens = 1;
				res = Ok(_b);
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}

			return res;
		}

		[AllowAnonymous]
		[HttpPost("resetpassword")]
		public async Task<IActionResult> ResetPassword([FromBody] LoginViewModel l)
		{
			IActionResult res = null;
			var _b = new BaseEntityDTO<string>() { Start = DateTime.Now };

			try
			{
				var existUser = await _userManager.FindByEmailAsync(l.Email);

				if (existUser == null)
					throw new Exception($"{l.Email} não encontrado");

				if (!existUser.LockoutEnabled)
					throw new Exception("Usuário inativo no sistema");


				l.Username = existUser.UserName;
				//existUser.Email = "ricardo@conecttasoftwares.com.br";

				//tokenResponse.AccessToken
				string senha = Uteis.GeraSenha();
				var guid = Guid.NewGuid().ToString();


				await repository.Add(new UsersResetPasswordModel[] { new UsersResetPasswordModel() {
						Token= guid,
						SenhaTrocada=false,
						LoginUser=l.Username
					 } }, ClienteID, UsuarioID);


				// existUser.Email = "hugocalheira@gmail.com";

				await Util.SendEmailAsync(
					new EmailViewModel[] { new EmailViewModel(existUser.Email) },
					"Senha zerada",
					Emails.RedefinicaoSenha($"{Util.Configuration["UrlIdentity"]}redefine-senha/{guid}"),
					true,
					TipoEmail.RESETSENHA);

				_b.Result = $"Email encaminhado pra {l.Email} com o link pra reset da senha";
				_b.End = DateTime.Now;
				_b.Itens = 1;
				res = Ok(_b);
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}

			return res;
		}


		//[ValidateAntiForgeryToken]
		public async Task<IActionResult> Get()
		{
			var _b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			var existUser = await _userManager.FindByNameAsync("ricardobeck");
			//if (existUser != null)
			//{
			//	await _userManager.DeleteAsync(existUser);
			//	existUser = null;
			//}

			//await HttpContext.Authentication.SignOutAsync(null);

			if (User.Identity.IsAuthenticated)
				await _signInManager.SignOutAsync();

			//var appuser = new IdentityUser() { UserName = "alice@wonderland.com", Email = "alice@wonderland.com", LockoutEnabled = false, EmailConfirmed = true };

			//await _userManager.CreateAsync(appuser, request.Password);







			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Cliente" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Fornecedor" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Usuario" });
			//await _roleManager.CreateAsync(new IdentityRole() { Name = "Gestor" });

			//var item = new IdentityRole() { Name = "AdminOnly" };

			//var _res = await _roleManager.AddClaimAsync(await _roleManager.FindByNameAsync("AdminOnly"), new Claim("permission", "view"));


			//Claims = new List<Claim> {
			//					new Claim(JwtClaimTypes.Email, "scott@scottbrady91.com"),
			//					new Claim(JwtClaimTypes.Role, "AdminOnly") }


			//var existUser = await _userManager.FindByNameAsync("ricardobeck");

			//if(existUser!=null)
			//	await _userManager.DeleteAsync(existUser);



			if (existUser == null)
			{
				var user = new IdentityUser
				{
					UserName = "ricardobeck",
					Email = "duartebeck@hotmail.com"

				};

				var result = await _userManager.CreateAsync(user, "Ms6tgs1234");

				//ClaimTypes.
				//	user = await _userManager.FindByNameAsync("ricardobeck");
				//List<Claim> userClaims = new List<Claim> { new Claim("userId", "23") };

				var _resultClaim = await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "AdminOnly"),
					new Claim("clienteid", "1"),
					new Claim("usuarioid", "23"),
					new Claim("pasta", "FlexRR")
			});
				var _resultRole = await _userManager.AddToRoleAsync(user, "AdminOnly");

				existUser = await _userManager.FindByNameAsync("ricardobeck");
			}

			//	await _roleManager.CreateAsync(new IdentityRole() { Name = "AdminOnly" });

			//	



			var checkPass = await _userManager.CheckPasswordAsync(existUser, "Ms6tgs1234");



			if (checkPass)
			{
				//var claims = await _userManager.GetClaimsAsync(existUser);

				//var res = await _signInManager.PasswordSignInAsync(existUser.UserName, "Ms6tgs1234", true, false);




				//if (res.Succeeded)
				//{

				var principal = await _signInManager.CreateUserPrincipalAsync(existUser);

				//principal.AddIdentity(new ClaimsIdentity(new List<Claim> {
				//	new Claim("userId", "23"),
				//new Claim("clienteid", "1")}));

				var client = new DiscoveryClient("http://moneoci-dev.us-east-1.elasticbeanstalk.com");
				client.Policy.RequireHttps = false;
				var disco = await client.GetAsync();

				// request token
				var tokenClient = new TokenClient(disco.TokenEndpoint, "oauthClient", "supers", AuthenticationStyle.BasicAuthentication);
				//var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api2");
				var tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync(existUser.UserName, "Ms6tgs1234", "api2");



				IActionResult res = null;
				try
				{
					_b.Result = tokenResponse;
					_b.End = DateTime.Now;
					_b.Itens = 1;
					res = Ok(_b);
				}
				catch (Exception err)
				{
					_b.End = DateTime.Now;
					_b.Error = (err.InnerException ?? err).Message;
					res = BadRequest(_b);
				}
				return Ok(_b);



				//var clienteHttp = new HttpClient();
				//clienteHttp.SetBearerToken(tokenResponse.AccessToken);
				//var response = await clienteHttp.GetAsync("http://conecttaoffice:12201/identity/");



				//ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(existUser
				//	.Claims
				//	.Select(a=>new Claim(a.ClaimType, a.ClaimValue))));


				//await _signInManager.SignInAsync(existUser, isPersistent: true);

				//await HttpContext.Authentication.SignInAsync("CookieAuth", principal);

				//response.EnsureSuccessStatusCode();
				//return Ok(response.StatusCode);
				//}
			}



			return Ok();
		}

		[HttpGet("get/info")]
		public async Task<IActionResult> GetInfo()
		{
			IActionResult res = null;
			var _b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
                var x = User.Claims.Where(k => k.Type.EndsWith("role")).ElementAt(0);
                
                switch (x.Value)
                {
                    case "Fornecedor":

                        var fornecedorid = int.Parse(User.FindFirst(a => a.Type == "fornecedorid").Value);

                        var f = await new FornecedorRepository().FindById(new FornecedorModel { FornecedorID = fornecedorid }, null);
                                                
                        _b.Result = new {
                            tipo = "Fornecedor",
                            nome = f.Nome,
                            cpfcnpj = f.CPFCNPJ,
                            username = f.CPFCNPJ,
                            endereco = f.Endereco,
                            numero = f.Numero,
                            complemento = f.Complemento,
                            bairro = f.Bairro,
                            cidade = f.Cidade,
                            cep = f.CEP,
                            uf = f.UF,
                            contatos = f.Contatos.Select(a => new { telefone = a.Celular, email = a.Email })
                        };
                        break;

                    case "Cliente":

                        var clienteid = int.Parse(User.FindFirst(a => a.Type == "clienteid").Value);

                        var c = await new ClienteRepository().FindById(new ClienteModel { ClienteID = clienteid }, null);                        
                
                        _b.Result = new {
                            tipo = "Cliente",
                            nome = c.Nome,
                            cnpj = c.CNPJ,
                            username = c.CNPJ,
                            endereco = c.Endereco,
                            numero = c.Numero,
                            complemento = c.Complemento,
                            bairro = c.Bairro,
                            cep = c.CEP,
                            cidade = c.Cidade,
                            uf = c.UF,
                            contatos = c.Contatos.Select(a => new { telefone = a.Celular, email = a.Email})
                        };

                        break;

                    case "Usuario":

                        var usuarioid = int.Parse(User.FindFirst(a => a.Type == "usuarioid").Value);

                        var u = await new UsuarioRepository().DadosCadastrais(usuarioid);

                        _b.Result = u;
                        break;

                    case "AdminOnly":

                        break;
                }

                _b.End = DateTime.Now;
				_b.Itens = 1;
				res = Ok(_b);
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}
			return Ok(_b);
		}

        [HttpPost("update/info")]
        public async Task<IActionResult> UpdateInfo([FromBody] object o)
        {
            IActionResult res = null;
            var _b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                var x = User.Claims.Where(k => k.Type.EndsWith("role")).ElementAt(0);

                switch (x.Value)
                {
                    case "Fornecedor":

                        var fornecedorid = int.Parse(User.FindFirst(a => a.Type == "fornecedorid").Value);

                        var f = JsonConvert.DeserializeObject<FornecedorModel>(o.ToString());

                        await new FornecedorRepository().Update(new FornecedorModel[] { f }, fornecedorid, null);
                        
                        break;

                    case "Cliente":

                        var clienteid = int.Parse(User.FindFirst(a => a.Type == "clienteid").Value);

                        var c = JsonConvert.DeserializeObject<ClienteModel> (o.ToString());

                        await new ClienteRepository().Update(new ClienteModel[] { c }, clienteid, null);
                        
                        break;

                    case "Usuario":
                        
                        break;

                    case "AdminOnly":

                        break;
                }

                _b.End = DateTime.Now;
                _b.Itens = 1;
                res = Ok(_b);
            }
            catch (Exception err)
            {
                _b.End = DateTime.Now;
                _b.Error = (err.InnerException ?? err).Message;
                res = BadRequest(_b);
            }
            return Ok(_b);
        }
    }
}
