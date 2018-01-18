using IdentityServer4.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Helpers
{
	using Data;
	using IdentityServer4;
	using IdentityServer4.Extensions;
	using IdentityServer4.Models;
	using Microsoft.AspNetCore.Identity;
	using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
	using Models;

	public class IdentityWithAdditionalClaimsProfileService : IProfileService
	{
		readonly IUserClaimsPrincipalFactory<IdentityUser> _claimsFactory;
		readonly UserManager<IdentityUser> _userManager;

		public IdentityWithAdditionalClaimsProfileService(
			UserManager<IdentityUser> userManager, 
			IUserClaimsPrincipalFactory<IdentityUser> claimsFactory)
		{
			_userManager = userManager;
			_claimsFactory = claimsFactory;
		}


		public async Task GetProfileDataAsync(ProfileDataRequestContext context)
		{

			var sub = context.Subject.GetSubjectId();
			var user = await _userManager.FindByIdAsync(sub);
			var principal = await _claimsFactory.CreateAsync(user);

			throw new NotImplementedException();
		}

		public async Task IsActiveAsync(IsActiveContext context)
		{
			var sub = context.Subject.GetSubjectId();
			var user = await _userManager.FindByIdAsync(sub);
			context.IsActive = user != null;
		}
	}
}
