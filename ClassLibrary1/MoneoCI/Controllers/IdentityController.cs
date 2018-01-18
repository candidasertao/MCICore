using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public class IdentityController : ControllerBase
	{

		[HttpGet]
		//[Authorize("2")]
		public IActionResult Get()
		{
			
			return Ok(from c in User.Claims select new { c.Type, c.Value });
		}
	}
}
