using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static IdentityServer4.IdentityServerConstants;

namespace MoneoCI
{

	public static class Config
	{
		public static IEnumerable<Client> GetClientes()
		{
			return new List<Client> { new Client()
			{
				ClientId = "oauthClient",
				ClientName = "Conectta",
				AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
				ClientSecrets = new List<Secret> { new Secret("rv2b7000438dm".Sha256()) },
				AllowedScopes= { "moneoci"},
				AccessTokenType = AccessTokenType.Jwt
				

			}
			,new Client() {
				ClientId = "api",
				ClientName = "Moneo",
				AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
				ClientSecrets = new List<Secret> { new Secret("ms6tgsoem2650".Sha256()) },
				AllowedScopes= { "fornecedor"},
				AccessTokenType = AccessTokenType.Reference,
				AccessTokenLifetime= int.MaxValue,
				//EnableLocalLogin=false,
				//LogoutSessionRequired=false,
				Claims =  new List<Claim>(){
					new Claim("fornecedorid", "23"),
					new Claim(ClaimTypes.Email, "23")

				}
			},new Client() {
				ClientId = "emailreset",
				ClientName = "Moneo",
				AllowedGrantTypes = GrantTypes.ImplicitAndClientCredentials,
				ClientSecrets = new List<Secret> { new Secret("ms6tgsoem2650".Sha256()) },
				AllowedScopes= { "brasilina", "ricardobeck","duartebeck"},
				AccessTokenType = AccessTokenType.Reference

			}
			};
		}

		public static IEnumerable<IdentityResource> GetIdentityResources()
		{
			var lista = new List<IdentityResource> {

			new IdentityResource {
				Name = "role",
				UserClaims = new List<string> { ClaimTypes.Role, ClaimTypes.Email }
			} };

			return lista;
		}

		//Claims =  new List<Claim>(){
		//			new Claim("login", "brasilina"),
		//			new Claim("email", "ricardo@conecttasoftwares.com.br")

		//		}

		public static IEnumerable<ApiResource> GetApiResources()
		{
			return new List<ApiResource> {
			new ApiResource("moneoci","Custom API") {
				UserClaims = new List<string> {
					ClaimTypes.Role,
					JwtClaimTypes.Email,
					"clienteid",
					"usuarioid",
					"fornecedorid",
					ClaimTypes.GroupSid}
			}
			
		};
		}

		public static List<TestUser> GetUsers()
		{
			return new List<TestUser> {
			new TestUser {
				SubjectId="2",
				Username = "scott",
				Password = "Ms6tgs1234",
				Claims = new List<Claim> {
					new Claim(JwtClaimTypes.Email, "scott@scottbrady91.com"),
					new Claim(ClaimTypes.Role, "AdminOnly"),
					new Claim("clienteid","1"),
					new Claim("usuarioid","23"),
					new Claim("pasta","FlexRR"),
				}
			}
		};
		}
	}
}
