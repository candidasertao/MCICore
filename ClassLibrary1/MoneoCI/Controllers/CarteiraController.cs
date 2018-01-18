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
	[Authorize(Roles ="Usuario,Cliente,AdminOnly")]
	public class CarteiraController : ControllerBase, IControllers<CarteiraModel>
	{
        const int SUBPAGINAID = 85;
        const int PAGINAID = 120;

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


		readonly IRepository<CarteiraModel> repository = null;

		public CarteiraController(IRepository<CarteiraModel> repos)
		{
			
			repository = repos;
		}


		//carteira/add/
		[HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<CarteiraModel> t)
		{
			
			IActionResult res = null;
			var b = new BaseEntityDTO<CarteiraModel>() { Start = DateTime.Now, Itens = t.Count() };
			try
			{

				var validator = new CarteiraModellValidator();
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

				if (b.Error.Contains("IX_CARTEIRA_carteira"))
					b.Error = $"Carteira já existente: {t.ElementAt(0).Carteira}";

				res = BadRequest(b);
			}
			return res;
		}


		//carteira/update
		[HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<CarteiraModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<CarteiraModel>() { Start = DateTime.Now, Itens = t.Count() };

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
                
                if (b.Error.Contains("IX_CARTEIRA_carteira"))
                    b.Error = $"Carteira já existente: {t.ElementAt(0).Carteira}";

                res = BadRequest(b);
			}
			return res;

		}


        //carteira/delete
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        [HttpDelete("delete/")]
		public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<CarteiraModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<CarteiraModel>() { Start = DateTime.Now, Itens = t.Count() };

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
		public async Task<IActionResult> GetAll() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.CADASTRO });

		[HttpGet("get/{id:int}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetByIDAsync(int id)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<CarteiraModel>() { Start = DateTime.Now };

			try
			{
				b.Result = (await repository.FindById(new CarteiraModel() { CarteiraID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID));
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

		//carteira/get/search/Ricardo
		[HttpGet("get/search/{s}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<CarteiraModel>>() { Start = DateTime.Now};
			try
			{
				b.Result = await repository.Search(new CarteiraModel() {Cliente = new ClienteModel() { ClienteID = ClienteID } , OrigemChamada= OrigemChamadaEnums.CADASTRO}, s, UsuarioID);
				b.Itens = b.Result.Count();
				b.End = DateTime.Now;
				res= Ok(b);
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
		public async Task<IActionResult> GetAllPaginadoAsync([FromBody] CarteiraModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CarteiraModel>>() { Start = DateTime.Now };

			try
			{
				if (t.Registros == 0)
					throw new Exception("o campo Registros precisa de valor maior que 0");

				t.OrigemChamada = OrigemChamadaEnums.CADASTRO;

				t.Cliente = new ClienteModel() { ClienteID = ClienteID };

				b.Result = await repository.GetAllPaginado(t, null);

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
