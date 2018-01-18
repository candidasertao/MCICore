using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Authorize]
	[Produces("application/json")]
	[Route("api/[controller]")]
	public class LogAtividadeController : ControllerBase, IControllers<LogAtividadeModel>
	{
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

		readonly IRepository<LogAtividadeModel> repository = null;

		public LogAtividadeController(IRepository<LogAtividadeModel> repos)
		{
			repository = repos;
		}


		[HttpPut("add/")]
		public Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<LogAtividadeModel> t)
		{
			throw new NotImplementedException();
		}
		[HttpPost("update/")]
		public Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<LogAtividadeModel> t)
		{
			throw new NotImplementedException();
		}

		[HttpDelete("delete/")]
		public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<LogAtividadeModel> t)
		{
			throw new NotImplementedException();
		}

		[HttpGet("get/")]
		public Task<IActionResult> GetAll()
		{
			throw new NotImplementedException();
		}

		[HttpGet("get/{id:int}")]
		public Task<IActionResult> GetByIDAsync(int id)
		{
			throw new NotImplementedException();
		}

		[HttpGet("get/search/{s}")]
		public Task<IActionResult> Search(string s)
		{
			throw new NotImplementedException();
		}

		[HttpGet("dashboard/")]
		public async Task<IActionResult> DashBoard(string s)
		{

			IActionResult res = null;

			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var dados = await new LogAtividadeRepository().DashBoard(ClienteID, UsuarioID);

                b.Result = dados.OrderByDescending(o => o.Data).Select(a => new
                {
                    Descricao = a.Descricao,
                    Data = a.Data,
                    Usuario = a.Usuario.Nome,
                    Carteira = a.Carteira.Carteira,
                    Cliente = a.Cliente.Nome,
                    Modulo = a.Modulo.EnumDescription(),
                    Tipo = a.Tipo.EnumDescription()
                });
                
				b.Itens = dados.Count();
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

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] LogAtividadeModel t)
		{
			throw new NotImplementedException();
		}
	}
}
