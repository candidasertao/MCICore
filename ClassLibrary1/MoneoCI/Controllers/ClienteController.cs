using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Helpers;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	[Route("api/[controller]")]
	public class ClienteController : ControllerBase, IControllers<ClienteModel>
	{
		readonly IRepository<ClienteModel> repository = null;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly UserManager<IdentityUser> _userManager;
		readonly RoleManager<IdentityRole> _roleManager;

		public ClienteController(
			IRepository<ClienteModel> repos,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager
			)
		{



			repository = repos;
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
		}


		//cliente/add
		[HttpPut("add/")]
		[AllowAnonymous]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<ClienteModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now, Itens = t.Count() };

			var cliente = t.ElementAt(0);

			try
			{


				if (cliente.Contatos == null || !cliente.Contatos.Any())
				{
					if (Util.RegexEmail.IsMatch(cliente.Email))
						cliente.Contatos = new ContatoModel[] { new ContatoModel() { Celular = cliente.Telefone, Email = cliente.Email } };
					else
						throw new Exception("E-mail inválido");
				}


				if(!Util.ValidaCnpj(cliente.CNPJ))
					throw new Exception("CNPJ inválido");


				foreach (var item in cliente.Contatos)
					if (!string.IsNullOrEmpty(item.Email))
					{
						if (!Util.RegexEmail.IsMatch(item.Email))
							throw new Exception($"Email {item.Email} com formato inváldo");

						if (_userManager.FindByEmailAsync(item.Email).GetAwaiter().GetResult() != null)
								throw new Exception($"Email {item.Email} já existente no sistema");
					}


				var senha = Uteis.GeraSenha();

				//checando existência de usuário
				var _userExistent = await _userManager.FindByNameAsync(cliente.CNPJ);

				if (_userExistent != null) throw new Exception($"usuário com login: {cliente.CNPJ} já existente");

				

				//montagem do usuário
				var user = new IdentityUser { UserName = cliente.CNPJ, Email = cliente.Contatos.Where(a=>!string.IsNullOrEmpty(a.Email)).ElementAt(0).Email, LockoutEnabled = true, EmailConfirmed = true };
				cliente.ClienteID = await new ClienteRepository().AddCliente(t);

				//criando o usuário
				var criacaoUser = await _userManager.CreateAsync(user, Uteis.GeraSenha());


				if (criacaoUser.Succeeded)
				{
					user = await _userManager.FindByNameAsync(cliente.CNPJ);

					var claims = new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Cliente"),
					new Claim("clienteid", cliente.ClienteID.ToString())
					};

					//adicionando as claims
					var _resultClaim = await _userManager.AddClaimsAsync(user, claims);



					//adicionando o usuário a role
					await _userManager.AddToRoleAsync(user, "Cliente");

					await Util.SendEmailAsync(new EmailViewModel[] { new EmailViewModel(user.Email) }, "Novo Cadastro Moneo", Emails.NovoCadastro(), true, TipoEmail.NOVOCADASTRO);
				}
				else
				{
					//DuplicateEmail
					if (criacaoUser.Errors.Contains(new IdentityError() { Code = "DuplicateEmail" }))
						throw new Exception($"E-mail {user.Email} já existente no sistema");

					throw new Exception(criacaoUser.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));
				}

				b.Result = $"Cliente cadastrado com sucesso. Senha gerada: {senha}";
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;


				if (b.Error.Contains("IX_EMAIL_CLIENTES_EMAIL_UNIQUE"))
					b.Error = "Um ou mais e-mails informados, já existente no sistema";

				if (b.Error.Contains("IX_CLIENTES"))
					b.Error = $"Cliente com o CNPJ: {t.ElementAt(0).CNPJ} já existente na base";


				//await new ClienteRepository().Remove(new ClienteModel[] { new ClienteModel() { ClienteID = cliente.ClienteID } }, 0, null);


				res = BadRequest(b);
			}
			return res;
		}




		//cliente/update
		[HttpPost("update/")]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<ClienteModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<ClienteModel>() { Start = DateTime.Now, Itens = t.Count() };


			try
			{
				await repository.Update(t, 0, null);
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

		public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<ClienteModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAll()
		{
			throw new NotImplementedException();
		}

		//cliente/get/14
		[HttpGet("get/{id:int}")]
		public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<ClienteModel>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				b.Result = await repository.FindById(new ClienteModel() { ClienteID = id }, null);
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

        [HttpGet("get/InfoEnvio")]
        public async Task<IActionResult> GetInfoEnvio()
        {          
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                var ClienteID = User.Identity.IsAuthenticated ? int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value) : 0;

                var UsuarioID = 0;

                var clainusuario = User.Claims.Where(a => a.Type == "usuarioid");

                if (clainusuario.Any())
                {
                    UsuarioID = User.Identity.IsAuthenticated ? int.Parse(clainusuario.ElementAt(0).Value) : 0;
                }

                var dados = await new ClienteRepository().GetInfoEnvio(ClienteID, UsuarioID);

                if (dados == null)
                    return NoContent();

                b.Result = dados;

                b.End = DateTime.Now;
                b.Itens = 1;
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

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> Search(string s)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] ClienteModel t)
		{
			throw new NotImplementedException();
		}
	}
}
