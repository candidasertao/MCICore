using IdentityServer4.Models;
using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.Data;
using System.Security.Claims;
using IdentityModel;
using Helpers;

namespace ConecttaManagerData.DAL
{
    public class DALIdentityServer : IDal<Client>
    {
        public List<Client> Clients { get; set; }
        public List<ApiResource> Apis { get; set; }

        public DALIdentityServer()
        {
            Clients = new List<Client>()
            {
                new Client()
                {
                    ClientId = "oauthClient",
                    ClientName = "Conectta",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                    ClientSecrets = new List<Secret> { new Secret("rv2b7000438dm".Sha256()) },
                    AllowedScopes= { "moneoci"},
                    AccessTokenType = AccessTokenType.Jwt
                    
                }
            };

            Apis = new List<ApiResource> {
            new ApiResource("moneoci","Custom API") {
                UserClaims = new List<string> {
                    ClaimTypes.Role,
                    JwtClaimTypes.Email,
                    "clienteid","usuarioid",
                    ClaimTypes.GroupSid}
            }};


        }

        public async Task AtualizaApiResoureTokenAsync(string token, int id)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {

                await conn.OpenAsync();

                try
                {
                    await conn.ExecuteAsync("UPDATE IDENTITYSERVER_CLIENTS_SCOPES SET TOKEN=@Token WHERE ID=@Id", new { Token = token, Id = id }, commandTimeout: 888);
                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();

                }
            }
        }

        public async Task<(string clienteid, string senha, int id)> AdicionaApiResource(Client c, ApiResource api)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {


                    var codigoclienteid = await conn.QuerySingleOrDefaultAsync<int>(@"SELECT CODIGO FROM [dbo].[IDENTITYSERVER_CLIENTS] WHERE CLIENTEID=@ClientId", new { ClientId = c.ClientId }, transaction: tran, commandTimeout: 888);

                    var p = new DynamicParameters();
                    p.Add("ApiSecret", api.ApiSecrets.ElementAt(0).Value, DbType.String, ParameterDirection.Input);
                    p.Add("Api", api.Name, DbType.String, ParameterDirection.Input);
                    p.Add("DisplayName", api.DisplayName, DbType.String, ParameterDirection.Input);
                    p.Add("Codigo", codigoclienteid, DbType.Int32, ParameterDirection.Input);
                    p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
                    p.Add("Id", DbType.Int32, direction: ParameterDirection.Output);


					await conn.ExecuteAsync("DELETE FROM IDENTITYSERVER_CLIENTS_SCOPES WHERE API=@Api", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    await conn.QueryAsync<int>("INSERT INTO IDENTITYSERVER_CLIENTS_SCOPES (APISECRET, API,DISPLAYNAME, CODIGO, DATA) VALUES (@ApiSecret, @Api, @DisplayName, @Codigo, @Data); SELECT @Id=SCOPE_IDENTITY();",
                        p,
                        transaction: tran,
                        commandTimeout: Util.TIMEOUTEXECUTE);

                    int id = p.Get<int>("Id");

                    tran.Commit();

                    return (api.Name,
                            api.ApiSecrets.ElementAt(0).Value, 
							id
                            );
                }
                catch (Exception err)
                {
                    tran.Rollback();
                    throw err;

                }
                finally
                {
                    tran.Dispose();
                    conn.Close();
                }
            }
        }

        public Task AdicionarItensAsync(IEnumerable<Client> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task AtualizaItensAsync(IEnumerable<Client> t, int c, int? u)
        {
            throw new NotImplementedException();
        }
        public async Task RemoveToken(string token)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    await conn.ExecuteAsync(@"DELETE FROM IDENTITYSERVER_CLIENTS_SCOPES WHERE TOKEN=@Token", new { Token = token }, commandTimeout: 888);

                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
        public async Task<ApiResource> GetApiResourceByToken(string token)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();


                try
                {
                    var result = await conn.QueryAsync(@"SELECT APISECRET, API FROM IDENTITYSERVER_CLIENTS_SCOPES WHERE TOKEN=@Token AND ISRESETED=0", new { Token = token }, commandTimeout: 888);

                    if (result.Any())
                        return result.Select(a => new ApiResource() { ApiSecrets = new List<Secret>() { new Secret((string)a.APISECRET) }, Name = (string)a.API }).ElementAt(0);



                    return null;


                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public async Task<IEnumerable<ApiResource>> GetAllResources()
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var result = await conn.QueryAsync(@"SELECT API, DISPLAYNAME, APISECRET FROM IDENTITYSERVER_CLIENTS C 
								JOIN IDENTITYSERVER_CLIENTS_SCOPES ICS ON C.CODIGO=ICS.CODIGO 
								", commandTimeout: 888);

                    if (result.Any())
                    {
                        Apis.AddRange(result.Select(a => new ApiResource(a.API, a.DISPLAYNAME)
                        {
                            ApiSecrets = { new Secret(((string)a.APISECRET).Sha256()) }
                        }));
                    }



                    return Apis;

                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(string scopeNames)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", scopeNames, DbType.String, ParameterDirection.Input);

                    var query = @"SELECT API, DISPLAYNAME, APISECRET FROM IDENTITYSERVER_CLIENTS C 
								JOIN IDENTITYSERVER_CLIENTS_SCOPES ICS ON C.CODIGO=ICS.CODIGO 
								WHERE C.CLIENTEID=ClienteID";


                    var result = await conn.QueryAsync(query, p, commandTimeout: 888);

                    if (result != null)
                        return result.Select(a => new ApiResource(a.API, a.DISPLAYNAME)
                        {
                            ApiSecrets = { new Secret(((string)a.APISECRET).Sha256()) }
                        });


                    return null;

                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
        IEnumerable<string> RetornaGranTypes(string g)
        {
            var _grantype = new string[] { }.AsEnumerable();

            switch (g)
            {
                case "ImplicitAndClientCredentials":
                    _grantype = GrantTypes.ImplicitAndClientCredentials;
                    break;

                case "ResourceOwnerPasswordAndClientCredentials":
                    _grantype = GrantTypes.ResourceOwnerPasswordAndClientCredentials;
                    break;

            }

            return _grantype;
        }
        public async Task<Client> BuscarItemByIDAsync(Client t, int? u)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {


                    if (Clients.Where(k => k.ClientId == t.ClientId).Any())
                        return Clients.Where(k => k.ClientId == t.ClientId).ElementAt(0);

                    var p = new DynamicParameters();
                    p.Add("ClienteID", t.ClientId, DbType.String, ParameterDirection.Input);

                    var query = @"SELECT [SECRET], CLIENTEID, TOKENTYPE, GRANTYPES, TOKENLIFE, API, DISPLAYNAME, ICC.TYPE, ICC.VALUE, C.SECRET FROM IDENTITYSERVER_CLIENTS C 
									JOIN IDENTITYSERVER_CLIENTS_SCOPES ICS ON C.CODIGO=ICS.CODIGO 
									JOIN [IDENTITYSERVER_CLIENTS_CLAIM] ICC ON C.CODIGO=ICC.CODIGO
									WHERE CLIENTEID=@ClienteID";





                    var result = await conn.QueryAsync(query, p, commandTimeout: 888);



                    if (result.Any())
                    {

                        var dados = result.Select(a => new
                        {
                            ClienteID = (string)a.CLIENTEID,
                            GranTypes = RetornaGranTypes((string)a.GRANTYPES),
                            AllowedScopes = new List<string>() { (string)a.API },
                            Claims = new List<Claim>() { new Claim(a.TYPE, a.VALUE) },
                            ClientSecrets = new Secret[] { new Secret(((string)a.SECRET).Sha256()) },
                            AccessTokenType = ((AccessTokenType)Enum.Parse(typeof(AccessTokenType), a.TOKENTYPE.ToString()))

                        });

                        var _result = dados.GroupBy(a => new Client()
                        {
                            ClientId = a.ClienteID,
                            AccessTokenType = a.AccessTokenType,
                            AllowedScopes = a.AllowedScopes,
                            ClientSecrets = a.ClientSecrets
                        },
                            (a, b) => new Client()
                            {
                                ClientId = a.ClientId,
                                AllowedGrantTypes = a.AllowedGrantTypes,
                                AccessTokenType = a.AccessTokenType,
                                AllowedScopes = a.AllowedScopes,
                                Claims = b.SelectMany(k => k.Claims).ToList(),
                                ClientSecrets = a.ClientSecrets


                            },
                            new CompareObject<Client>((a, m) => a.ClientId == m.ClientId, i => i.ClientId.GetHashCode()));

                        if (!Clients.Where(k => k.ClientId == t.ClientId).Any())
                            Clients.Add(_result.ElementAt(0));

                        return Clients.Where(k => k.ClientId == t.ClientId).ElementAt(0);
                    }


                    return null;

                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public Task<IEnumerable<Client>> BuscarItensAsync(Client t, string s, int? u)
        {
            throw new NotImplementedException();
        }

        public Task ExcluirItensAsync(IEnumerable<Client> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task ExcluirItensUpdateAsync(IEnumerable<Client> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> ObterTodosAsync(Client t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> ObterTodosPaginadoAsync(Client t, int? u)
        {
            throw new NotImplementedException();
        }
    }
}
