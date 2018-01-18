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
	public class BlacklistController : ControllerBase, IControllers<BlackListModel>
	{
		const int SUBPAGINAID = 0;
		const int PAGINAID = 123;

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

		readonly IRepository<BlackListModel> repository = null;

		public BlacklistController(IRepository<BlackListModel> repos)
		{
			repository = repos;
		}

		//blacklist/add/
		[HttpPut("add/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<BlackListModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<BlackListModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				if (t.Count() > 1000)
					t = t.Take(1000);

				await repository.Add(t, ClienteID, UsuarioID);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;

				if (b.Error.Contains("IX_CELULAR_BLACKLIST"))
					b.Error = $"Item já existente: {t.ElementAt(0).Celular}";

				res = BadRequest(b);
			}
			return res;
		}

		//blacklist/update/
		[HttpPost("update/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<BlackListModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<BlackListModel>() { Start = DateTime.Now, Itens = t.Count() };


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

				if (b.Error.Contains("IX_CELULAR_BLACKLIST"))
					b.Error = $"Número já existente na blacklist: {t.ElementAt(0).Celular}";

				res = BadRequest(b);
			}
			return res;
		}

		//blacklist/delete
		[HttpDelete("delete/")]
		[NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<BlackListModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<BlackListModel>() { Start = DateTime.Now, Itens = t.Count() };


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

		//blacklist/get
		[HttpGet("get/")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<BlackListModel>>() { Start = DateTime.Now };

			try
			{
				b.Result = await repository.GetAll(new BlackListModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);


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

		//blacklist/get/14
		[HttpGet("get/{id:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<BlackListModel>() { Start = DateTime.Now };

			try
			{
				b.Result = await repository.FindById(new BlackListModel() { BlacklistID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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

		//blacklist/get/search/Ricardo
		
		[HttpGet("get/search/{s}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<BlackListModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.Search(new BlackListModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID);
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
		public async Task<IActionResult> GetAllPaginadoAsync([FromBody] BlackListModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<BlackListModel>>() { Start = DateTime.Now };

			try
			{
				if (t.Registros == 0)
					throw new Exception("o campo Registro precisa de valor maior que 0");

				t.Cliente = new ClienteModel() { ClienteID = ClienteID };

				b.Result = await repository.GetAllPaginado(t, UsuarioID);

				if (b.Result == null || !b.Result.Any())
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
	}
}
