using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Atributos;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	public class GrupoUsuariosController : ControllerBase, IControllers<GrupoUsuariosModel>
	{
        const int SUBPAGINAID = 84;
        const int PAGINAID = 118;

        readonly SignInManager<IdentityUser> _signInManager;
		readonly UserManager<IdentityUser> _userManager;
		readonly RoleManager<IdentityRole> _roleManager;

		public int ClienteID { get { return int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value); } }

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

		readonly IRepository<GrupoUsuariosModel> repository = null;

		public GrupoUsuariosController(
			IRepository<GrupoUsuariosModel> repos,
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

		[HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<GrupoUsuariosModel> t)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<GrupoUsuariosModel>() { Start = DateTime.Now, Itens = t.Count() };
			try
			{
				await repository.Add(t, ClienteID, UsuarioID);
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
		//[NivelPermissao(Permissao = 2)]
		[HttpPost("update/atualizapermissao/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItem([FromBody] IEnumerable<GrupoUsuarioPaginas> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GrupoUsuariosModel>() { Start = DateTime.Now, Itens = t.Count() };


			try
			{

				//var usuarios = await new UsuarioRepository
				//foreach (var usuario in t)
				//{
				//	//checando existência de usuário
				//	var _u = await _userManager.FindByNameAsync(usuario.LoginUser);


				//	if (_u != null)
				//	{
				//		var _exlusao = await _userManager.DeleteAsync(_u);
				//		if (!_exlusao.Succeeded)
				//			throw new Exception(_exlusao.Errors.Select(a => a.Description).Aggregate((n, o) => $"Ocorreram os seguintes erros: {n},{o}"));
				//		else
				//			await new UsuarioRepository().Remove(usuario, ClienteID, null);
				//	}
				//}

				await new GrupoUsuariosRepository().AtualizaPermissaoPaginaAsync(t, ClienteID);

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
		[HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<GrupoUsuariosModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GrupoUsuariosModel>() { Start = DateTime.Now, Itens = t.Count() };


			try
			{
				await repository.Update(t, ClienteID, UsuarioID);
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
        public async Task<IActionResult> AtualizaSaldoGrupo([FromBody] IEnumerable<GrupoUsuariosModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now, Itens = t.Count() };
			try
			{
				await new GrupoUsuariosRepository().AtualizaSaldoGrupo(t.ElementAt(0), ClienteID, UsuarioID);

				b.Result = "Saldo do grupo atualizado com sucesso";
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
		[HttpGet("get/permissoes/{id:int}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> PaginasByGrupoUsuario(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
				var dados = await new GrupoUsuariosRepository().PaginasPermissao(new GrupoUsuariosModel() { GrupoUsuarioID = id }, ClienteID);
				b.Result = new
				{
					grupo = dados.Nome,
					grupousuarioid = dados.GrupoUsuarioID,
					paginas = dados.GrupoUserPaginas.Select(k => new
					{
						grupousuariopaginaid = k.GrupoUsuarioPaginaID,
						pagina = k.Pagina.Pagina,
						tipoacesso = k.TipoAcesso
					})
				};

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

		[HttpDelete("delete/")]
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<GrupoUsuariosModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GrupoUsuariosModel>() { Start = DateTime.Now, Itens = t.Count() };


			try
			{
				await repository.Remove(t, ClienteID, UsuarioID);
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

		[HttpGet("get/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
				var dados = await repository.GetAll(new GrupoUsuariosModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);

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

		

		[HttpGet("get/paginas")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetPaginas()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<PaginaModel>>() { Start = DateTime.Now };

			try
			{
				b.Result = await new GrupoUsuariosRepository().RetornaPaginas(ClienteID); ;
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

			var b = new BaseEntityDTO<GrupoUsuariosModel>() { Start = DateTime.Now };

			try
			{
				b.Result = await repository.FindById(new GrupoUsuariosModel() { GrupoUsuarioID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
				if (b.Result != null)
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

		[HttpGet("get/search/{s}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<GrupoUsuariosModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.Search(new GrupoUsuariosModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID);
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

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] GrupoUsuariosModel t)
		{
			throw new NotImplementedException();
		}
	}
}
