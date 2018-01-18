using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALTipoCampanha : IDal<TipoCampanhaModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<TipoCampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"INSERT INTO TIPOCAMPANHA(TIPOCAMPANHA, CLIENTEID, VISIVEL) VALUES(@TipoCampanha, @ClienteID, @Visivel)", t.Select(a => new
					{
						ClienteID = c,
						TipoCampanha = a.TipoCampanha.Trim(),
						Visivel = a.Visivel
					}), transaction: tran,
					commandTimeout: 888);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.TIPOCAMPANHA, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task AtualizaItensAsync(IEnumerable<TipoCampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					string query = @"UPDATE dbo.TIPOCAMPANHA SET TIPOCAMPANHA=@TipoCampanha, VISIVEL=@Visivel WHERE CODIGO=@TipoCampanhaID AND CLIENTEID=@ClienteID";

					await conn.ExecuteAsync(query, t.Select(a => new
					{
						TipoCampanhaID = a.TipoCampanhaID,
						TipoCampanha = a.TipoCampanha.Trim(),
						ClienteID = c,
						Visivel = a.Visivel
					}), transaction: tran, commandTimeout: 888);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.TIPOCAMPANHA, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<TipoCampanhaModel> BuscarItemByIDAsync(TipoCampanhaModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT TIPOCAMPANHA FROM TIPOCAMPANHA WHERE CLIENTEID=@ClienteID AND TIPOCAMPANHAID=@TipoCampanhaID AND ISEXCLUDED=0";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("TipoCampanhaID", t.TipoCampanhaID, DbType.Int32, ParameterDirection.Input);


					var result = await conn.QuerySingleOrDefaultAsync<dynamic>(query, p);

					if (result != null)
					{
						return new TipoCampanhaModel()
						{
							TipoCampanha = result.TIPOCAMPANHA,
							Cliente = new ClienteModel() { ClienteID = result.CLIENTEID },
							TipoCampanhaID = t.TipoCampanhaID
						};
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

		public async Task<IEnumerable<TipoCampanhaModel>> BuscarItensAsync(TipoCampanhaModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT TIPOCAMPANHA, CODIGO, VISIVEL FROM TIPOCAMPANHA WHERE CLIENTEID=@ClienteID AND TIPOCAMPANHA LIKE '%'+@Busca+'%' AND ISEXCLUDED=0 ORDER BY TIPOCAMPANHA";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);



					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
					{
						return result.Select(a => new TipoCampanhaModel()
						{
							Cliente = t.Cliente,
							TipoCampanha = a.TIPOCAMPANHA,
							TipoCampanhaID = a.CODIGO,
							Visivel = a.VISIVEL
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

		public async Task ExcluirItensAsync(IEnumerable<TipoCampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();
                var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("UPDATE TIPOCAMPANHA SET ISEXCLUDED=1 WHERE CODIGO=@TipoCampanhaID AND CLIENTEID=@ClienteID AND ISEXCLUDED=0", t.Select(a => new { ClienteID = c, TipoCampanhaID = a.TipoCampanhaID }), commandTimeout: 888, transaction:tran);
                    
                    await conn.ExecuteAsync("DELETE FROM PADRAO_POSTAGENS WHERE TIPOCAMPANHAID=@TipoCampanhaID", t.Select(a => new
                    {
                        TipoCampanhaID = a.TipoCampanhaID
                    }), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    try
                    {
#pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.TIPOCAMPANHA, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
                    }
                    catch { }

                    tran.Commit();
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

		public async Task<IEnumerable<TipoCampanhaModel>> ObterTodosParaEnvioSMS(TipoCampanhaModel t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					string query = "SELECT TIPOCAMPANHA, CODIGO, VISIVEL FROM TIPOCAMPANHA WHERE VISIVEL=1 AND CLIENTEID=@ClienteID AND ISEXCLUDED=0 ORDER BY TIPOCAMPANHA";
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(query, p);

					if (result != null)
						return result.Select(a => new TipoCampanhaModel()
						{
							Cliente = t.Cliente,
							TipoCampanha = a.TIPOCAMPANHA,
							TipoCampanhaID = a.CODIGO,
							Visivel=a.VISIVEL
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


		public async Task<IEnumerable<TipoCampanhaModel>> ObterTodosAsync(TipoCampanhaModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					string query = "SELECT TIPOCAMPANHA, CODIGO, VISIVEL FROM TIPOCAMPANHA WHERE CLIENTEID=@ClienteID AND ISEXCLUDED=0 AND VISIVEL=1 ORDER BY TIPOCAMPANHA";
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(query, p);

					if (result != null)
					{
						return result.Select(a => new TipoCampanhaModel()
						{
							Cliente = t.Cliente,
							TipoCampanha = a.TIPOCAMPANHA,
							TipoCampanhaID = a.CODIGO,
							Visivel = a.VISIVEL
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

		public Task ExcluirItensUpdateAsync(IEnumerable<TipoCampanhaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<TipoCampanhaModel>> ObterTodosPaginadoAsync(TipoCampanhaModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", t.Search, DbType.String, ParameterDirection.Input);

					string query = "SELECT TIPOCAMPANHA, CODIGO, VISIVEL FROM TIPOCAMPANHA WHERE CLIENTEID=@ClienteID AND ISEXCLUDED=0 ORDER BY TIPOCAMPANHA";

					if (t.PaginaAtual.HasValue)
					{
						if (t.PaginaAtual.Value == 0)
							t.PaginaAtual = 1;
					}
					else
						t.PaginaAtual = 1;



					if (!string.IsNullOrEmpty(t.Search))
						query = query.Insert(query.LastIndexOf("CLIENTEID=@ClienteID"), @"(TIPOCAMPANHA LIKE '%'+@Search+'%') AND ");

					var result = await conn.QueryAsync(query, p);

					if (result != null)
						return result.Select(a => new TipoCampanhaModel()
						{
							Cliente = t.Cliente,
							TipoCampanha = a.TIPOCAMPANHA,
							TipoCampanhaID = a.CODIGO,
							Registros = result.Count(),
							Visivel=a.VISIVEL,
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
