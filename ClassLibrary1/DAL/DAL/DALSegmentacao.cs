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
	public class DALSegmentacao : IDal<SegmentacaoModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<SegmentacaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"INSERT INTO SEGMENTACAO(NOME, CLIENTEID) VALUES (@Nome,@ClienteID)", t.Select(a => new
					{
                        ClienteID = c,
						Nome = a.Nome.Trim()
                    }), transaction: tran,
					commandTimeout: Util.TIMEOUTEXECUTE);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t.ToList(), null, c, u, ModuloAtividadeEnumns.CONTRATANTE, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task AtualizaItensAsync(IEnumerable<SegmentacaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					string query = @"UPDATE SEGMENTACAO SET NOME=@Nome WHERE CODIGO=@SegmentacaoID AND CLIENTEID=@ClienteID";

					await conn.ExecuteAsync(query, t.Select(a => new
					{
						Nome = a.Nome.Trim(),
						SegmentacaoID = a.SegmentacaoID,
						ClienteID = c
					}), transaction: tran, commandTimeout: 888);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t.ToList(), null, c, u, ModuloAtividadeEnumns.CONTRATANTE, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<SegmentacaoModel> BuscarItemByIDAsync(SegmentacaoModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT [NOME], SEGMENTACAOID FROM SEGMENTACAO WHERE CODIGO=@SegmentacaoID AND CLIENTEID=@ClienteID";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("SegmentacaoID", t.SegmentacaoID, DbType.Int32, ParameterDirection.Input);


					var result = await conn.QuerySingleOrDefaultAsync<dynamic>(query, p);

					if (result != null)
					{
						return new SegmentacaoModel()
						{
							SegmentacaoID = t.SegmentacaoID,
							Nome = result.NOME,
							Cliente = new ClienteModel() { ClienteID = result.CLIENTEID },

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

		public async Task<IEnumerable<SegmentacaoModel>> BuscarItensAsync(SegmentacaoModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT [NOME],CODIGO SEGMENTACAOID FROM SEGMENTACAO WHERE SEGMENTACAO LIKE '%'+@Busca+'%' AND CLIENTEID=@ClienteID";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
					{
						return result.Select(a => new SegmentacaoModel()
						{
							Cliente = t.Cliente,
							Nome = a.NOME,
							SegmentacaoID = a.SEGMENTACAOID
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

		public async Task ExcluirItensAsync(IEnumerable<SegmentacaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync("DELETE FROM SEGMENTACAO WHERE CODIGO=@SegmentacaoID AND CLIENTEID=@ClienteID", t.Select(a => new { ClienteID = c, SegmentacaoID = a.SegmentacaoID }), commandTimeout: 888);

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t.ToList(), null, c, u, ModuloAtividadeEnumns.CONTRATANTE, TiposLogAtividadeEnums.EXCLUSAO);
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

		public Task ExcluirItensUpdateAsync(IEnumerable<SegmentacaoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<SegmentacaoModel>> ObterTodosAsync(SegmentacaoModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
                    string query = @"SELECT [NOME], CODIGO SEGMENTACAOID, C.CARTEIRA, C.CARTEIRAID,  C.VISIVEL FROM SEGMENTACAO S LEFT JOIN CARTEIRAS C ON S.CODIGO=C.SEGMENTACAOID AND C.ISEXCLUDED=0 WHERE S.CLIENTEID=@ClienteID ORDER BY NOME";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(query, p);

					if (result != null)
						return result
							.GroupBy(a=>new {Nome=a.NOME, SegmentacaoID=a.SEGMENTACAOID}, (a,b)=> new SegmentacaoModel()
							{
								Cliente=t.Cliente,
								Nome=a.Nome,
								SegmentacaoID=a.SegmentacaoID,
								Carteiras=b.Where(m => m.CARTEIRAID!=null).Select(m=>new CarteiraModel() { CarteiraID=m.CARTEIRAID, Carteira=m.CARTEIRA, Visivel=m.VISIVEL})

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

		public Task<IEnumerable<SegmentacaoModel>> ObterTodosPaginadoAsync(SegmentacaoModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
