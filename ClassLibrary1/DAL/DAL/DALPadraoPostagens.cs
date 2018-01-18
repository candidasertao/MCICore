using Dapper;
using Microsoft.Extensions.Configuration;
using DAL;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class DALPadraoPostagens
    {


        public async Task AdicionarItensAsync(IEnumerable<PadraoPostagensModel> t, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    //var p = new DynamicParameters();
                    //p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
                    //p.Add("Codigo", t.Codigo, DbType.Int32, ParameterDirection.Input);


                    await conn.ExecuteAsync(@"INSERT INTO PADRAO_POSTAGENS(PADRAO,CARTEIRAID,CLIENTEID,TIPOCAMPANHAID, USUARIOID, LEIAUTEID) VALUES(@Padrao, @CarteiraID, @ClienteID, @TipoCampanhaID, @UsuarioID, @LeiauteID)", t.Select(a => new
                    {
                        CarteiraID = a.Carteira.CarteiraID,
                        Padrao = a.Padrao.Trim(),
                        LeiauteID = a.Leiaute.LeiauteID,
                        ClienteID = c,
                        TipoCampanhaID = a.TipoCampanha.TipoCampanhaID,
                        UsuarioID = u
                    }), transaction: tran, commandTimeout: 888);

                    tran.Commit();

                    try
                    {
#pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PADRAOENVIO, TiposLogAtividadeEnums.GRAVACAO);
#pragma warning restore 4014
                    }
                    catch { }

                    //return default(int);
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
        public async Task<PadraoPostagensModel> Adicionaitem(PadraoPostagensModel t, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("Padrao", t.Padrao.Trim(), DbType.String, ParameterDirection.Input);
                    p.Add("LeiauteID", t.Leiaute.LeiauteID, DbType.Int32, ParameterDirection.Input);
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("TipoCampanhaID", t.TipoCampanha.TipoCampanhaID, DbType.Int32, ParameterDirection.Input);
                    p.Add("CarteiraID", t.Carteira.CarteiraID, DbType.Int32, ParameterDirection.Input);
                    p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
                    p.Add("Codigo", DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync(@"INSERT INTO PADRAO_POSTAGENS(PADRAO,CARTEIRAID,CLIENTEID,TIPOCAMPANHAID, USUARIOID, LEIAUTEID) VALUES (@Padrao, @CarteiraID, @ClienteID, @TipoCampanhaID, @UsuarioID, @LeiauteID);SELECT @Codigo = SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    var result = await conn.QuerySingleOrDefaultAsync<dynamic>(@"SELECT PADRAO, P.CARTEIRAID, C.CARTEIRA, T.TIPOCAMPANHA, P.TIPOCAMPANHAID, C.HORALIMITE FROM PADRAO_POSTAGENS P 
										JOIN CARTEIRAS C ON P.CARTEIRAID = C.CARTEIRAID
										JOIN TIPOCAMPANHA T ON P.TIPOCAMPANHAID = T.CODIGO
										WHERE P.CODIGO = @Codigo AND P.CLIENTEID = @ClienteID", new { Codigo = p.Get<int>("Codigo"), ClienteID = c }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					if (result != null)
						t = new PadraoPostagensModel()
						{
							Padrao = result.PADRAO,
							Carteira = new CarteiraModel() { Carteira = result.CARTEIRA, CarteiraID = result.CARTEIRAID, HoraLimite = result.HORALIMITE },
                            TipoCampanha = new TipoCampanhaModel() { TipoCampanha = result.TIPOCAMPANHA, TipoCampanhaID = result.TIPOCAMPANHAID },
                            Codigo = p.Get<int>("Codigo")
                        };

                    tran.Commit();

                    try
                    {
#pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PADRAOENVIO, TiposLogAtividadeEnums.GRAVACAO);
#pragma warning restore 4014
                    }
                    catch { }

                    return t;
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


        public async Task AtualizaItensAsync(IEnumerable<PadraoPostagensModel> t, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    string query = @"UPDATE PADRAO_POSTAGENS SET PADRAO=@Padrao, CARTEIRAID=@CarteiraID, TIPOCAMPANHAID=@TipoCampanhaID, LEIAUTEID=@LeiauteID  WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID";

                    await conn.ExecuteAsync(query, t.Select(a => new
                    {
                        CarteiraID = a.Carteira.CarteiraID,
                        Padrao = a.Padrao.Trim(),
                        Codigo = a.Codigo,
                        ClienteID = c,
                        LeiauteID = a.Leiaute.LeiauteID,
                        TipoCampanhaID = a.TipoCampanha.TipoCampanhaID
                    }), transaction: tran, commandTimeout: 888);

                    tran.Commit();

                    try
                    {
#pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PADRAOENVIO, TiposLogAtividadeEnums.ATUALIZACAO);
#pragma warning restore 4014
                    }
                    catch { }

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

        public async Task<(IEnumerable<PadraoPostagensModel> _padroes, Dictionary<string, string> padroes)> PadroesToEnvio(Dictionary<string, string> padroes, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("Padroes", padroes.Select(a => a.Value.ToLower()).GroupBy(a => a, (a, b)=>a) .Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);

                    var result = await conn.QueryAsync(string.Format(@"SELECT PADRAO, P.CARTEIRAID, C.CARTEIRA, T.TIPOCAMPANHA, P.TIPOCAMPANHAID, C.VISIVEL, C.HORALIMITE, [LEIAUTEID], T.VISIVEL VISIVELTIPO, C.BLOQUEIOENVIO, C.DIASHIGIENIZACAO FROM PADRAO_POSTAGENS P {0}
										JOIN string_split(@Padroes, ',') S ON LOWER(P.PADRAO)=S.value
										JOIN CARTEIRAS C ON P.CARTEIRAID = C.CARTEIRAID
										JOIN TIPOCAMPANHA T ON P.TIPOCAMPANHAID = T.CODIGO
										WHERE P.CLIENTEID = @ClienteID AND C.ISEXCLUDED=0 AND T.ISEXCLUDED=0", u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON P.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty), p);

                    if (result != null)
                    {
                        var dados = result.Select(a => new PadraoPostagensModel()
                        {
                            Leiaute = new LeiauteModel() { LeiauteID = a.LEIAUTEID },
                            Padrao = a.PADRAO,
                            Carteira = new CarteiraModel() {
								Carteira = a.CARTEIRA,
								CarteiraID = a.CARTEIRAID,
								Visivel = a.VISIVEL,
								HoraLimite = a.HORALIMITE,
								BloqueioEnvio =a.BLOQUEIOENVIO??false,
								DiasHigienizacao =a.DIASHIGIENIZACAO,
								Higieniza=a.DIASHIGIENIZACAO==null
							},
                            TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA, TipoCampanhaID = a.TIPOCAMPANHAID, Visivel = a.VISIVELTIPO }
                        });

						

						return (dados, padroes.Join(dados, a => a.Value, b => b.Padrao, (a, b) => a).ToDictionary(a => a.Key, a => a.Value));
                    }


                    return (null, null);

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

        public async Task<PadraoPostagensModel> BuscarItemByIDAsync(PadraoPostagensModel t, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    string query = @"SELECT PADRAO, P.CARTEIRAID, C.CARTEIRA, T.TIPOCAMPANHA, P.TIPOCAMPANHAID FROM PADRAO_POSTAGENS P 
										JOIN CARTEIRAS C ON P.CARTEIRAID = C.CARTEIRAID
										JOIN TIPOCAMPANHA T ON P.TIPOCAMPANHAID = T.CODIGO
										WHERE P.CODIGO = @Codigo AND P.CLIENTEID = @ClienteID";

                    var p = new DynamicParameters();
                    p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
                    p.Add("Codigo", t.Codigo, DbType.Int32, ParameterDirection.Input);


                    var result = await conn.QuerySingleOrDefaultAsync<dynamic>(query, p);

                    if (result != null)
                        return new PadraoPostagensModel()
                        {
                            Padrao = result.PADRAO,
                            Carteira = new CarteiraModel() { Carteira = result.CARTEIRA, CarteiraID = result.CARTEIRAID },
                            Cliente = new ClienteModel() { ClienteID = result.CLIENTEID },
                            TipoCampanha = new TipoCampanhaModel() { TipoCampanha = result.TIPOCAMPANHA, TipoCampanhaID = result.TIPOCAMPANHAID },
                            Codigo = result.CODIGO
                        };

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

        public async Task<IEnumerable<PadraoPostagensModel>> BuscarItensAsync(PadraoPostagensModel t, string s, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    string query = "SELECT PADRAO, P.CARTEIRAID, C.CARTEIRA FROM PADRAO_POSTAGENS P JOIN CARTEIRAS C ON P.CARTEIRAID=C.CARTEIRAID WHERE (P.ARQUIVO LIKE '%'+@Busca+'%' OR C.CARTEIRA LIKE '%'+@Busca+'%') AND P.CLIENTEID=@ClienteID";

                    var p = new DynamicParameters();
                    p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
                    p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);



                    var result = await conn.QueryAsync(query, p);

                    if (result != null)
                    {
                        return result.Select(a => new PadraoPostagensModel()
                        {
                            Padrao = a.PADRAO,
                            Carteira = new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = a.CARTEIRAID },
                            Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
                            Codigo = a.CODIGO
                        });

                    }
                    else
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

        public async Task ExcluirItensAsync(IEnumerable<PadraoPostagensModel> t, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    await conn.ExecuteAsync("DELETE FROM PADRAO_POSTAGENS WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID", t.Select(a => new { ClienteID = c, Codigo = a.Codigo }), commandTimeout: 888);

                    try
                    {
#pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PADRAOENVIO, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
                    }
                    catch { }
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

        public Task ExcluirItensUpdateAsync(IEnumerable<PadraoPostagensModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }



        public async Task<IEnumerable<PadraoPostagensModel>> ObterTodos(PadraoPostagensModel t, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    string query = @"SELECT P.PADRAO, P.CARTEIRAID, C.CARTEIRA, T.TIPOCAMPANHA, P.TIPOCAMPANHAID, L.[NOME], P.CODIGO FROM PADRAO_POSTAGENS P 
										JOIN CARTEIRAS C ON P.CARTEIRAID = C.CARTEIRAID AND T.ISEXCLUDED=0
										JOIN TIPOCAMPANHA T ON P.TIPOCAMPANHAID = T.CODIGO AND T.ISEXCLUDED=0
										JOIN LAYOUT L ON P.LEIAUTEID=L.LEIAUTEID
										WHERE P.CLIENTEID = @ClienteID ORDER BY P.PADRAO";

                    var p = new DynamicParameters();
                    p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);


                    var result = await conn.QueryAsync(query, p);

                    if (result != null)
                        return result.Select(a => new PadraoPostagensModel()
                        {
                            Padrao = a.PADRAO,
                            Carteira = new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = a.CARTEIRAID },
                            Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
                            Codigo = a.CODIGO,
                            TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA, TipoCampanhaID = a.TIPOCAMPANHAID }
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

        public async Task<IEnumerable<PadraoPostagensModel>> ObterTodosPaginadoAsync(PadraoPostagensModel t, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    string query = @"SELECT P.PADRAO, P.CARTEIRAID, C.CARTEIRA, T.TIPOCAMPANHA, P.TIPOCAMPANHAID, L.[NOME], P.LEIAUTEID, P.CODIGO FROM PADRAO_POSTAGENS P 
										JOIN CARTEIRAS C ON P.CARTEIRAID = C.CARTEIRAID AND C.ISEXCLUDED=0
										JOIN TIPOCAMPANHA T ON P.TIPOCAMPANHAID = T.CODIGO AND T.ISEXCLUDED=0
										JOIN LAYOUT L ON P.LEIAUTEID=L.LEIAUTEID 
										WHERE P.CLIENTEID = @ClienteID ORDER BY P.PADRAO";

                    var p = new DynamicParameters();
                    p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
                    p.Add("Search", t.Search, DbType.String, ParameterDirection.Input);

                    if (t.PaginaAtual.HasValue)
                    {
                        if (t.PaginaAtual.Value == 0)
                            t.PaginaAtual = 1;
                    }
                    else
                        t.PaginaAtual = 1;


                    var result = await conn.QueryAsync(query, p);

                    if (result != null)
                        return result.Select(a => new PadraoPostagensModel()
                        {
                            Padrao = a.PADRAO,
                            Carteira = new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = a.CARTEIRAID },
                            Leiaute = new LeiauteModel() { LeiauteID = a.LEIAUTEID, Nome = a.NOME },
                            Codigo = a.CODIGO,
                            TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA, TipoCampanhaID = a.TIPOCAMPANHAID },
                            Registros = result.Count(),
                            Paginas = result.Count() / t.Registros
                        })
                        .Skip((t.PaginaAtual.Value - 1) * t.Registros)
                        .Take(t.Registros);

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
    }
}
