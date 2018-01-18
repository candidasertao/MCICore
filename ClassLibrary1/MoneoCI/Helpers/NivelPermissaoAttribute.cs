using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MoneoCI.Repository;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Atributos
{
	class NivelPermissaoAttribute : ActionFilterAttribute
	{
		public byte Permissao { get; set; }
		public int PaginaID { get; set; }
		public int SubPaginaID { get; set; }

		public NivelPermissaoAttribute(byte permissao)
		{
			this.Permissao = permissao;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			if (context.HttpContext.User.Identity.IsAuthenticated)
			{
				if (context.HttpContext.User.IsInRole("Usuario"))
				{
					var claim = context.HttpContext.User.FindFirst(ClaimTypes.GroupSid);

                    var permissao = (new GrupoUsuariosRepository().PermissaoPagina(int.Parse(claim.Value),
						SubPaginaID == 0 ? new Nullable<int>() : new Nullable<int>(SubPaginaID),
						PaginaID, int.Parse(context.HttpContext.User.FindFirst(a => a.Type == "clienteid").Value))).GetAwaiter().GetResult();
    
					if (claim != null)
					{
						if (permissao < Permissao)
							context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
					}
				}
			}
			else
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

			if (context.HttpContext.Response.StatusCode == 401)
				context.Result = new EmptyResult();
		}
	}
}
