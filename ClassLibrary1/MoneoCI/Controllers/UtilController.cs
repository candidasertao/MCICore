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
using System.Threading;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize]

	public class UtilController: ControllerBase
	{
		[HttpGet("horario/")]
		public IActionResult GetAll()
		{
			return Ok(DateTime.Now);
		}

		public async Task<int> IsPermission(int p, int s)
		{

					var claim = User.FindFirst(ClaimTypes.GroupSid);
					return await new GrupoUsuariosRepository().PermissaoPagina(int.Parse(claim.Value),
						s == 0 ? new Nullable<int>() : new Nullable<int>(s),
						p, int.Parse(User.FindFirst(a => a.Type == "clienteid").Value));

		}

		
		public async Task<IActionResult> GenericCall<T>(int ClienteID, int? UsuarioID, IRepository<T> repo, T t) where T: class
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<T>> { Start = DateTime.Now };

			try
			{
				b.Result = await repo.GetAll(t, UsuarioID);

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
