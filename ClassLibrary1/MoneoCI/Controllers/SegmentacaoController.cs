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
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	public class SegmentacaoController : ControllerBase, IControllers<SegmentacaoModel>
	{
		const int SUBPAGINAID = 86;
		const int PAGINAID = 120;

		public int ClienteID { get { return int.Parse(User.FindFirst(a => a.Type == "clienteid").Value); } }

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Any())
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return new Nullable<int>();
			}
		}
		readonly IRepository<SegmentacaoModel> repository = null;

		public SegmentacaoController(IRepository<SegmentacaoModel> repos)
		{
			repository = repos;
		}

		//segmentacao/add
		[HttpPut("add/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<SegmentacaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<SegmentacaoModel>() { Start = DateTime.Now, Itens = t.Count() };

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

                if (b.Error.Contains("IX_SEGMENTACAO"))
                    b.Error = $"Item já existente: {t.ElementAt(0).Nome}";

                res = BadRequest(b);
			}
			return res; ;
		}

		//segmentacao/update
		[HttpPost("update/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<SegmentacaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<SegmentacaoModel>() { Start = DateTime.Now, Itens = t.Count() };

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

				if (b.Error.Contains("IX_SEGMENTACAO"))
					b.Error = $"Item já existente: {t.ElementAt(0).Nome}";

				res = BadRequest(b);
			}
			return res;
		}

		//segmentacao/delete
		[HttpDelete("delete/")]
		[NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<SegmentacaoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<SegmentacaoModel>() { Start = DateTime.Now, Itens = t.Count() };

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

		//segmentacao/get
		[HttpGet("get/")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<SegmentacaoModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.GetAll(new SegmentacaoModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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


		//segmentacao/get/14
		[HttpGet("get/{id:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<SegmentacaoModel>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				b.Result = await repository.FindById(new SegmentacaoModel() { SegmentacaoID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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

		//segmentacao/get/search/Tipo
		[HttpGet("get/search/{s}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<SegmentacaoModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.Search(new SegmentacaoModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID);
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

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] SegmentacaoModel t)
		{
			throw new NotImplementedException();
		}
	}
}
