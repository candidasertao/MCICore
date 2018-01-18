using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace MoneoCI.Controllers
{

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public  class AngularAntiForgeryTokenAttribute: ActionFilterAttribute
	{
		private const string CookieName = "XSRF-TOKEN";
		private readonly IAntiforgery antiforgery;

		public AngularAntiForgeryTokenAttribute(IAntiforgery antiforgery)
		{
			this.antiforgery = antiforgery;
		}

		public override void OnResultExecuting(ResultExecutingContext context)
		{
			base.OnResultExecuting(context);

			if (!context.Cancel)
			{
				var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);

				context.HttpContext.Response.Cookies.Append(
					CookieName,
					tokens.RequestToken,
					new CookieOptions { HttpOnly = false });
			}
		}

	}
}
