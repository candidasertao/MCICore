using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
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
	public class MonitoriaController:ControllerBase,IControllers<MonitoriaModel>
	{
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

		

		public Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<MonitoriaModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<MonitoriaModel> t)
		{


			throw new NotImplementedException();
		}

		public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<MonitoriaModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAll()
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetByIDAsync(int id)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> Search(string s)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] MonitoriaModel t)
		{
			throw new NotImplementedException();
		}
	}
}
