using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
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
	[Authorize(Roles = "Cliente,AdminOnly, Usuario")]
	public class TipoCampanhaController : ControllerBase, IControllers<TipoCampanhaModel>
	{
        const int PAGINAID = 121;
        const int SUBPAGINAID = 0;

        public int ClienteID
        {
            get {
				return int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value);
			}
        }

		public int? UsuarioID{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Count() > 0)
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return null;
			}
		}

		readonly IRepository<TipoCampanhaModel> repository = null;

		public TipoCampanhaController(IRepository<TipoCampanhaModel> repos)
		{
            
            repository = repos;
		}

		//tipocampanha/add
		[HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<TipoCampanhaModel> t)
		{
           

            IActionResult res = null;
			var b = new BaseEntityDTO<TipoCampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };


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

				if (b.Error.Contains("IX_TIPOCAMPANHA"))
					b.Error = $"Tipo de Campanha: {t.ElementAt(0).TipoCampanha} já existente";

				res = BadRequest(b);
			}
			return res; 
		}

		//tipocampanha/update
		[HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<TipoCampanhaModel> t)
		{

			IActionResult res = null;
			var b = new BaseEntityDTO<TipoCampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };


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

		//tipocampanha/delete
		[HttpDelete("delete/")]
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<TipoCampanhaModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<TipoCampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };

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

			var b = new BaseEntityDTO<IEnumerable<TipoCampanhaModel>>() { Start = DateTime.Now};

			try
			{
				b.Result = (await repository.GetAll(new TipoCampanhaModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID));
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

		[HttpGet("getparaenvios/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetParaEnvios()
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<TipoCampanhaModel>>() { Start = DateTime.Now };

			try
			{
				
				b.Result = await new TipoCampanhaRepository().ObterTodosParaEnvioSMS(new TipoCampanhaModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } });

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

		//tipocampanha/get/14
		[HttpGet("get/{id:int}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetByIDAsync(int id)
		{

			IActionResult res = null;

			var b = new BaseEntityDTO<TipoCampanhaModel>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				b.Result = await repository.FindById(new TipoCampanhaModel() { TipoCampanhaID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
				if (b.Result == null)
					return NoContent();

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

		//tipocampanha/get/search/Tipo
		[HttpGet("get/search/{s}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async  Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<TipoCampanhaModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.Search(new TipoCampanhaModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID);

				if (b.Result == null || b.Result.Any())
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

		[HttpPost("get/p/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAllPaginadoAsync([FromBody] TipoCampanhaModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<TipoCampanhaModel>>() { Start = DateTime.Now };
			try
			{
				if (t.Registros == 0)
					throw new Exception("o campo Registro precisa de valor maior que 0");

				t.Cliente = new ClienteModel() { ClienteID = ClienteID };

				b.Result = await repository.GetAllPaginado(t, UsuarioID);

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
