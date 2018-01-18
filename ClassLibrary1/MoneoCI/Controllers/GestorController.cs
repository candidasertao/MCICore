using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Models;
using MoneoCI.Repository;
using DTO;
using MoneoCI.Validate;
using System.Text.RegularExpressions;
using Atributos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Cliente,AdminOnly,Usuario")]
	public class GestorController : ControllerBase, IControllers<GestorModel>
	{
		const int PAGINAID = 119;
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

		readonly IHostingEnvironment _hostingEnvironment;
		readonly IMemoryCache _cache;
		readonly IRepository<GestorModel> repository = null;

		public GestorController(IRepository<GestorModel> repos, IHostingEnvironment env, IMemoryCache cache)
		{
			_cache = cache;
			_hostingEnvironment = env;
			repository = repos;
		}

		[HttpGet("carteiras/")]
		[NivelPermissao(1, PaginaID = 120, SubPaginaID = 85)]
		public async Task<IActionResult> Carteiras() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.CADASTRO });


		//gestor/get/14
		[HttpGet("get/{id:int}")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]

		public async Task<IActionResult> GetByIDAsync(int id)
		{

			IActionResult res = null;

			var b = new BaseEntityDTO<GestorModel>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				b.Result = (await repository.FindById(new GestorModel() { GestorID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID));
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

		//gestor/get
		[HttpGet("get/")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]

		public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<GestorModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = await repository.GetAll(new GestorModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
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
		//gestor/get
		[HttpGet("get/carteira/{carteiraid:int}")]
		public async Task<IActionResult> GetByCarteira(int carteiraid)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<GestorModel>>() { Start = DateTime.Now };
			try
			{
				var gestor = new List<GestorModel>() { };
				var dados = await new GestorRepository().GetAll(new GestorModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);

				foreach (var item in dados)
					if (item.Carteiras.Any(k => k.CarteiraID == carteiraid))
						gestor.Add(item);

				b.Result = gestor;
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

		//gestor/get/search/Ricardo 
		[HttpGet("get/search/{s}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> Search(string s)
		{
			IActionResult res = null;

			var b = new BaseEntityDTO<IEnumerable<GestorModel>>() { Start = DateTime.Now };
			try
			{
				b.Result = (await repository.Search(new GestorModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, s, UsuarioID));
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

        //gestor/add
        [HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<GestorModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GestorModel>() { Start = DateTime.Now, Itens = t.Count() };
			try
			{

				var prefixos = await Util.CacheFactory<IEnumerable<PrefixoModel>>(_cache, "prefixos", _hostingEnvironment);

				if (prefixos == null || !prefixos.Any())
					throw new Exception("Não foi possível carregar a lista de prefixos");


				foreach (var item in t)
				{
					if (item.Telefones.Any())
						item.Telefones = item.Telefones.Join(prefixos, a => a.ToPrefixo(), m => m.Prefixo, (a, m) => a).ToList();

					if (item.Emails.Any())
						item.Emails = item.Emails.Where(a => Util.RegexEmail.IsMatch(a)).ToList();

					if (!item.Emails.Any())
						throw new Exception("Não existe um e-mail válido");
				}


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

		//gestor/update
		[HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<GestorModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GestorModel>() { Start = DateTime.Now, Itens = t.Count() };


			try
			{

				var prefixos = await Util.CacheFactory<IEnumerable<PrefixoModel>>(_cache, "prefixos", _hostingEnvironment);

				if (prefixos == null || !prefixos.Any())
					throw new Exception("Não foi possível carregar a lista de prefixos");


				foreach (var item in t)
				{
					if (item.Telefones.Any())
						item.Telefones = item.Telefones.Join(prefixos, a => a.ToPrefixo(), m => m.Prefixo, (a, m) => a).ToList();

					if (item.Emails.Any())
						item.Emails = item.Emails.Where(a => Util.RegexEmail.IsMatch(a)).ToList();

					if (!item.Emails.Any())
						throw new Exception("Não existe um e-mail válido");
				}

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

		//gestor/delete
		[HttpDelete("delete/")]
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<GestorModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<GestorModel>() { Start = DateTime.Now, Itens = t.Count() };


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

		[HttpPost("get/p/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAllPaginadoAsync([FromBody] GestorModel t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<GestorModel>>() { Start = DateTime.Now };
			try
			{
				if (t.Registros == 0)
					throw new Exception("o campo Registro precisa de valor maior que 0");

				t.Cliente = new ClienteModel() { ClienteID = ClienteID };

				b.Result = (await repository.GetAllPaginado(t, UsuarioID)).ToList();

				if (b.Result == null || !b.Result.Any())
					return NoContent();

				t.Search = t.Search.Trim();

				List<GestorModel> gestor = new List<GestorModel>() { };

				if (t.CarteiraList.Any())
					foreach (var item in b.Result)
					{
						foreach (var carteira in item.Carteiras)
							if (t.CarteiraList.Where(a => a.CarteiraID.Value == carteira.CarteiraID.Value).Any())
								gestor.Add(item);
					}
				else if (!string.IsNullOrEmpty(t.Search))
				{
					foreach (var item in b.Result)
					{
						if (Regex.IsMatch(item.Nome,t.Search, RegexOptions.IgnoreCase) || 
							item.Emails.Any(a=> Regex.IsMatch(a, t.Search, RegexOptions.IgnoreCase)) || 
							item.Carteiras.Any(a=>Regex.IsMatch(a.Carteira, t.Search, RegexOptions.IgnoreCase))
							)
							gestor.Add(item);
						
						

						
					}
				}
				if (gestor.Any())
					b.Result = gestor;

				

				foreach (var item in b.Result)
				{
					item.Registros = b.Result.Count();
					item.Paginas = b.Result.Count() / t.Registros;
				}

				b.Result = b.Result.Skip(((t.PaginaAtual??1)-1) * t.Registros)
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
	}
}