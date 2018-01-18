using IdentityModel;
using IdentityModel.Client;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MoneoCI.Data;
using MoneoCI.Repository;
using Helpers;
using Atributos;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	public class UsuariosController : ControllerBase, IControllers<UsuarioModel>
	{
		const int SUBPAGINAID = 83;
		const int PAGINAID = 118;


		readonly IRepository<UsersResetPasswordModel> _usersPasswordRepo = null;
		readonly IRepository<UsuarioModel> repository = null;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly UserManager<IdentityUser> _userManager;
		readonly RoleManager<IdentityRole> _roleManager;

		public UsuariosController(
			IRepository<UsuarioModel> repos,
			ApplicationDbContext applicationDbContext,
			 IOptions<IdentityOptions> identityOptions,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager,
			IRepository<UsersResetPasswordModel> usersrepo
			)
		{

			repository = repos;
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
			_usersPasswordRepo = usersrepo;
		}

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Count() > 0)
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return new Nullable<int>();
			}
		}

		public int ClienteID
		{
			get
			{
				//var cliente = User.Claims.Where(a => a.ValueType == "clienteid");

				return int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value);
			}
		}

		//usuario/add
		[HttpPut("add/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<UsuarioModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now, Itens = t.Count() };
			int usuarioid = 0;
			try
			{
				var usuario = t.ElementAt(0);
				var validator = new UsuarioModelValidator();
				var result = await validator.ValidateAsync(t.ElementAt(0));


				if (!result.IsValid)
					throw new Exception(result.Errors.Select(a => a.ErrorMessage).Aggregate((a, k) => $"{a},{k}"));

				if (!Util.RegexEmail.IsMatch(usuario.Email))
					throw new Exception("Email inválido");




				var senha = Uteis.GeraSenha();

				//checando existência de usuário
				var _userExistent = await _userManager.FindByNameAsync(usuario.LoginUser);

				if (_userExistent != null)
				{
					//await _userManager.DeleteAsync(_userExistent);
					throw new Exception($"usuário {usuario.LoginUser} já existente");

				}

				_userExistent = await _userManager.FindByEmailAsync(usuario.Email);


				if (_userExistent != null)
					throw new Exception($"usuário com e-mail {usuario.Email} já cadastrado no sistema");


				//montagem do usuário
				var user = new IdentityUser { UserName = usuario.LoginUser, Email = usuario.Email, LockoutEnabled = true, PhoneNumber = usuario.Telefone.ToString(), EmailConfirmed = true };

				usuarioid = await new UsuarioRepository().Add(usuario, ClienteID, UsuarioID);

				//criando o usuário
				var criacaoUser = await _userManager.CreateAsync(user, Uteis.GeraSenha());

				if (criacaoUser.Succeeded)
				{

					user = await _userManager.FindByNameAsync(usuario.LoginUser);

					await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Usuario"),
					new Claim(ClaimTypes.GroupSid, usuario.GrupoUsuario.GrupoUsuarioID.ToString()),
					new Claim("clienteid", ClienteID.ToString()),
					new Claim("usuarioid", usuarioid.ToString())});

					//adicionando o usuário a role
					await _userManager.AddToRoleAsync(user, "Usuario");

					var guid = Guid.NewGuid().ToString();

					await _usersPasswordRepo.Add(new UsersResetPasswordModel[] { new UsersResetPasswordModel() {
						Token= guid,
						SenhaTrocada=false,
						LoginUser=usuario.LoginUser
					 } }, ClienteID, UsuarioID);

					//encaminhando um e-mail ao usuário após o cadastro
					await Util.SendEmailAsync(
						new EmailViewModel[] { new EmailViewModel(user.Email, usuario.Nome) },
						"Novo Usuário Moneo",
						Emails.NovoUsuario(usuario.LoginUser, $"{Util.Configuration["UrlIdentity"]}redefine-senha/{guid}"),
						true,
						TipoEmail.NOVOCADASTRO);
				}
				else
					throw new Exception(criacaoUser.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));



				b.Result = $"usuário {user.UserName} gravado com sucesso";
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				if (usuarioid > 0)
					await new UsuarioRepository().ExcluirItensAsyncAfected(new UsuarioModel[] { new UsuarioModel() { UsuarioID = usuarioid } }, ClienteID, UsuarioID).ContinueWith(async (k) =>
					{
						if (!k.IsFaulted)
						{
							if (k.Result > 0)
							{
								var _user = await _userManager.FindByNameAsync(t.ElementAt(0).LoginUser);
								if (_user != null)
									await _userManager.DeleteAsync(_user);
							}
						}
					});
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;

                if (b.Error.Contains("CK_GRUPOUSUARIOS_SALDO"))
                    b.Error = "Valor da cota maior do que a permitida pelo grupo";

				res = BadRequest(b);
			}
			return res;
		}

		//usuarios/add
		[HttpPost("update/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<UsuarioModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<UsuarioModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				IdentityUser user;

				foreach (var a in t)
				{

					var email = await new UsuarioRepository().BuscaByEmail(a, ClienteID);

					if (a.Email != email) //usuario fez a mudança do e-mail
					{

						if (!Util.RegexEmail.IsMatch(a.Email))
							throw new Exception($"Email informado {a.Email} inválido");


						user = await _userManager.FindByEmailAsync(a.Email);
						if (user != null)
							throw new Exception($"Email informado {a.Email} já existente no sistema");

					}

					user = await _userManager.FindByNameAsync(a.LoginUser);

					if (user != null)
					{
						user.LockoutEnabled = a.Ativo;

						var claims = await _userManager.GetClaimsAsync(user);

						var claimold = claims.SingleOrDefault(k => k.Type == ClaimTypes.GroupSid);

						var result = await _userManager.ReplaceClaimAsync(user,
							claimold,
							new Claim(ClaimTypes.GroupSid, a.GrupoUsuario.GrupoUsuarioID.ToString()));

						if (!user.Email.Equals(a.Email))
						{
							string token = await _userManager.GenerateChangeEmailTokenAsync(user, a.Email);
							await _userManager.ChangeEmailAsync(user, a.Email, token);
						}

						await repository.Update((new UsuarioModel[] { a }).ToList(), ClienteID, UsuarioID);
					}
				}

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;

				var excecao = err.InnerException ?? err;
                if (excecao.Message.Contains("'IX_USUARIOS_EMAIL'"))
                    b.Error = $"Usuário com e-mail {t.ElementAt(0)} já exstente no sistema";
                else if (excecao.Message.Contains("CK_GRUPOUSUARIOS_SALDO"))
                    b.Error = "Valor da cota fornecida, é maior do que a cota do grupo";

                else
                    b.Error = err.Message;
				res = BadRequest(b);
			}
			return res;
		}

		//usuarios/delete
		[HttpDelete("delete/")]
		[NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<UsuarioModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{

				foreach (var usuario in t)
				{
					//checando existência de usuário
					var _u = await _userManager.FindByNameAsync(usuario.LoginUser);


					if (_u != null)
					{
						var _exlusao = await _userManager.DeleteAsync(_u);
						if (!_exlusao.Succeeded)
							throw new Exception(_exlusao.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));


					}


                    await repository.Remove((new UsuarioModel[] { usuario }).ToList(), ClienteID, UsuarioID);

				}

				b.Result = $"{t.ElementAt(0).LoginUser} excluído com sucesso";

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpGet("get/perfil")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetPerfis()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<GrupoUsuariosModel>>() { Start = DateTime.Now };

			try
			{
				var dados = await new GrupoUsuariosRepository().ListaGrupos(ClienteID);



				if (dados == null || !dados.Any())
					return NoContent();

				b.Result = dados;

				b.End = DateTime.Now;
				b.Itens = dados.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		//usuarios/delete
		[HttpGet("get/")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<UsuarioModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.GetAll(new UsuarioModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);

				if (!b.Result.Any() || b.Result == null)
					return NoContent();

				b.End = DateTime.Now;
				b.Itens = b.Result.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		//usuarios/delete
		[HttpGet("get/c/{carteiraid:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAllByCarteiraID(int carteiraid)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<UsuarioModel>>() { Start = DateTime.Now };
			try
			{


				b.Result = await new UsuarioRepository().ObterTodosByCarteiraID(new UsuarioModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, CarteiraID = carteiraid });

				if (!b.Result.Any() || b.Result == null)
					return NoContent();

				b.End = DateTime.Now;
				b.Itens = b.Result.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpGet("get/{id:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<UsuarioModel>() { Start = DateTime.Now };

			try
			{
				b.Result = await repository.FindById(new UsuarioModel() { UsuarioID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, null);
				if (b.Result == null)
					return NoContent();

				b.Itens = 1;
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpPost("alterasaldo/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AlteraSaldo([FromBody] IEnumerable<UsuarioModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				var dados = await new UsuarioRepository().RenovaSaldoUsuario(t.ElementAt(0), ClienteID);

				if (dados < 0)
					throw new Exception("Houve um erro na atualização de saldo do usuário");

				b.Result = $"Usuário atualizado com sucesso";
				b.Itens = t.Count();

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpGet("carteiras/")]
		[NivelPermissao(1, PaginaID = 120, SubPaginaID = 85)]
		public async Task<IActionResult> Carteiras() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.CADASTRO });

		[HttpGet("get/search/{s}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<UsuarioModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = (await repository.Search(new UsuarioModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID));
				b.Itens = b.Result.Count();
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpPost("get/p/")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAllPaginadoAsync([FromBody] UsuarioModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<UsuarioModel>>() { Start = DateTime.Now };

			try
			{
				if (t.Registros == 0)
					throw new Exception("o campo Registros precisa de valor maior que 0");

				t.Cliente = new ClienteModel() { ClienteID = ClienteID };

				b.Result = await repository.GetAllPaginado(t, UsuarioID);

				if (b.Result == null || !b.Result.Any())
					return NoContent();

				if (t.CarteiraList.Any())
				{
					var users = new List<UsuarioModel>() { };
					foreach (var item in b.Result)
					{
						if (item.Carteiras.Join(t.CarteiraList.Select(a => a.CarteiraID), a => a.CarteiraID.Value, k => k.Value, (a, k) => a).Any())
							users.Add(item);
					}

					if (!users.Any())
						return NoContent();

					foreach (var item in users)
					{
						item.Registros = users.Count;
						item.Paginas = item.Paginas / users.Count;
					}

					b.Result = users;
				}

				if (t.PaginaAtual.HasValue)
				{
					if (t.PaginaAtual.Value == 0)
						t.PaginaAtual = 1;
				}
				else
					t.PaginaAtual = 1;


				b.Result = b.Result
						.Skip((t.PaginaAtual.Value - 1) * t.Registros)
						.Take(t.Registros);

				b.End = DateTime.Now;
				b.Itens = b.Result.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpGet("get/carteiras/{id:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> BuscaCarteirasByUsuarioID(int id)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CarteiraModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await new UsuarioRepository().BuscaCarteirasByUsuarioID(new UsuarioModel() { UsuarioID = id }, ClienteID);

				if (b.Result == null || !b.Result.Any())
					return NoContent();



				b.Itens = b.Result.Count();


				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}


	}
}
