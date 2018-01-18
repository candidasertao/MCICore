using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Atributos;
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
	[Produces("application/json")]
	[Route("api/[controller]")]
	public class InterjeicaoController : ControllerBase, IControllers<InterjeicaoModel>
	{
        const int SUBPAGINAID = 92;
        const int PAGINAID = 131;

        public int ClienteID { get { return int.Parse(User.FindFirst(a => a.Type == "clienteid").Value); } }

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Any())
					if (User.FindFirst(c => c.Type == ClaimTypes.GroupSid).Value != "5")
						return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return new Nullable<int>();
			}
		}

		readonly IRepository<InterjeicaoModel> repository = null;

		public InterjeicaoController(IRepository<InterjeicaoModel> repos)
		{
			repository = repos;
		}

		[HttpPut("add/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<InterjeicaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<InterjeicaoModel>() { Start = DateTime.Now, Itens = t.Count() };

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

		[HttpPost("update/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<InterjeicaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<InterjeicaoModel>() { Start = DateTime.Now, Itens = t.Count() };


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
		[HttpDelete("delete/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<InterjeicaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<InterjeicaoModel>() { Start = DateTime.Now, Itens = t.Count() };


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
			var b = new BaseEntityDTO<IEnumerable<InterjeicaoModel>>() { Start = DateTime.Now };

			try
			{
				b.Result = await repository.GetAll(new InterjeicaoModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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

			var b = new BaseEntityDTO<InterjeicaoModel>() { Start = DateTime.Now };

			try
			{
				b.Result = (await repository.FindById(new InterjeicaoModel() { Codigo = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID));
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

			var b = new BaseEntityDTO<IEnumerable<InterjeicaoModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = (await repository.Search(new InterjeicaoModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID));
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

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] InterjeicaoModel t)
		{
			throw new NotImplementedException();
		}
	}
}
