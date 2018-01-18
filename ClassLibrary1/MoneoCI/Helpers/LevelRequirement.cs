using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Helpers
{
	public class LevelRequirement : IAuthorizationRequirement
	{
		public LevelRequirement(byte _level)
		{
			minlevel = _level;
			
		}

		public byte minlevel { get; set; }
	}
	public class LevelHandler : AuthorizationHandler<LevelRequirement>
	{
		
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LevelRequirement requirement)
		{

			
			if (!context.User.HasClaim(c => c.Type == ClaimTypes.GroupSid))
				return Task.CompletedTask;



			if (Convert.ToByte(context.User.FindFirst(c => c.Type == ClaimTypes.GroupSid).Value) >= requirement.minlevel)
				context.Succeed(requirement);


			return Task.CompletedTask;
		}
	}
}
