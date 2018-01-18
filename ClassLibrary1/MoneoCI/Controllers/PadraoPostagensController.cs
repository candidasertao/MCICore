using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Atributos;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	public class PadraoPostagensController : ControllerBase, IControllers<PadraoPostagensModel>
	{
        const int PAGINAID = 122;
        const int SUBPAGINAID = 0;

        public int ClienteID { get { return int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value); } }

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Count() > 0)
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return null;
			}
		}

		readonly IRepository<PadraoPostagensModel> repository = null;

		public PadraoPostagensController(IRepository<PadraoPostagensModel> repos)
		{
			repository = repos;
		}

		//padraopostagens/add/
		[HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<PadraoPostagensModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<PadraoPostagensModel>() { Start = DateTime.Now, Itens = t.Count() };
            
			try
			{
				var validator = new PadraoPostagensModelValidator();
				var result = await validator.ValidateAsync(t.ElementAt(0));


				if (!result.IsValid)
					throw new Exception(result.Errors.Select(a => a.ErrorMessage).Aggregate((a, k) => $"{a},{k}"));

				await repository.Add(t, ClienteID, UsuarioID);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;

				if (b.Error.Contains("IX_PADRAO_POSTAGENS"))
					b.Error = "Padrão já cadastrado";

				res = BadRequest(b);
			}
			return res; ;
		}

		//padraopostagens/add/
		[HttpPut("additem/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdiicionaItem([FromBody] IEnumerable<PadraoPostagensModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<PadraoPostagensModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				Regex regPadraoArquivo = new Regex("([0-9]{8}|[0-9]{6}).(txt|csv|TXT|CSV)", RegexOptions.Compiled);

				var padrao = t.ElementAt(0);
				padrao.Padrao = regPadraoArquivo.Replace(padrao.Padrao, string.Empty);

				b.Result = await new PadraoPostagensRepository().Adicionaitem(padrao, ClienteID, UsuarioID);
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

		//padraopostagens/update
		[HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<PadraoPostagensModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<PadraoPostagensModel>() { Start = DateTime.Now, Itens = t.Count() };

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
                
                if (b.Error.Contains("IX_PADRAO_POSTAGENS"))
                    b.Error = "Padrão já cadastrado";

                res = BadRequest(b);
			}
			return res;
		}

		//padraopostagens/delete
		[HttpDelete("delete/")]
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<PadraoPostagensModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<PadraoPostagensModel>() { Start = DateTime.Now, Itens = t.Count() };

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

		//padraopostagens/get
		[HttpGet("get/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<PadraoPostagensModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = (await repository.GetAll(new PadraoPostagensModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID));
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


        //padraopostagens/get/14

        [HttpGet("get/{id:int}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<PadraoPostagensModel>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				b.Result = await repository.FindById(new PadraoPostagensModel() { Codigo = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		//padraopostagens/get/search/Ricardo

		[HttpGet("get/search/{s}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<PadraoPostagensModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.Search(new PadraoPostagensModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID);

				if (!b.Result.Any() || b.Result == null)
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

		[HttpGet("carteiras/")]
		[NivelPermissao(1, PaginaID = 120, SubPaginaID = 85)]
		public async Task<IActionResult> Carteiras() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.CADASTRO });


		[HttpPost("get/p/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAllPaginadoAsync([FromBody] PadraoPostagensModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<PadraoPostagensModel>>() { Start = DateTime.Now };
			try
			{
				t.Cliente = new ClienteModel() { ClienteID = ClienteID };
				b.Result = await repository.GetAllPaginado(t, UsuarioID);

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
	}
}
