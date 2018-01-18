using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MoneoCI.Services
{
	public class CustomJwtDataFormat : ISecureDataFormat<AuthenticationTicket>
	{
		private readonly string algorithm;
		private readonly TokenValidationParameters validationParameters;

		public CustomJwtDataFormat(string algorithm, TokenValidationParameters validationParameters)
		{
			
			this.algorithm = algorithm;
			this.validationParameters = validationParameters;
		}

		public AuthenticationTicket Unprotect(string protectedText)
			=> Unprotect(protectedText, null);

		public AuthenticationTicket Unprotect(string protectedText, string purpose)
		{
			var handler = new JwtSecurityTokenHandler();
			ClaimsPrincipal principal = null;
			SecurityToken validToken = null;

			try
			{
				principal = handler.ValidateToken(protectedText, this.validationParameters, out validToken);

				
				var validJwt = validToken as JwtSecurityToken;

				if (validJwt == null)
					throw new ArgumentException("Invalid JWT");

				if (!validJwt.Header.Alg.Equals(algorithm, StringComparison.Ordinal))
					throw new ArgumentException($"Algorithm must be '{algorithm}'");

				// Additional custom validation of JWT claims here (if any)
			}
			catch (SecurityTokenValidationException)
			{
				return null;
			}
			catch (ArgumentException)
			{
				return null;
			}

			// Validation passed. Return a valid AuthenticationTicket:
			return new AuthenticationTicket(principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties(), "CookieAuth");
		}


		// This ISecureDataFormat implementation is decode-only
		public string Protect(AuthenticationTicket data)
		{
			
			throw new NotImplementedException();
		}

		public string Protect(AuthenticationTicket data, string purpose)
		{

			

			var token = new JwtSecurityToken(
				validationParameters.ValidIssuer, 
				validationParameters.ValidAudience, 
				data.Principal.Claims,
				DateTime.Now,
				DateTime.Now.AddMinutes(30), 
									new SigningCredentials(
										new SymmetricSecurityKey(Encoding.ASCII.GetBytes("mysupersecret_secretkey!123")), 
										SecurityAlgorithms.HmacSha256));

			return new JwtSecurityTokenHandler().WriteToken(token);

			

		}
	}
}
