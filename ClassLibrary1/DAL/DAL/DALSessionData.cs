using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.Data;
using FastMember;

namespace DAL
{
	public class DALSessionData : IDal<SessionDataModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<SessionDataModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(t.Select(m => new
						{
							Guid = m.Guid,
							Key = m.Key,
							Value = m.Value.Compress().GetAwaiter().GetResult(),
							Data = m.Data
						}),
						"Guid", "Key", "Value", "Data"))
						{
							bcp.DestinationTableName = "SESSION_ITENS";
							bcp.ColumnMappings.Add("Guid", "GUID");
							bcp.ColumnMappings.Add("Key", "KEY");
							bcp.ColumnMappings.Add("Value", "VALUE");
							bcp.ColumnMappings.Add("Data", "DATA");
							bcp.EnableStreaming = true;
							bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

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

		public async Task AtualizaItensAsync(IEnumerable<SessionDataModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					await conn.ExecuteAsync("UPDATE SESSION_ITENS SET [VALUE]=@Value WHERE GUID=@Guid AND [KEY]=@Key", t.Select( a => new SessionDataModel()
					{
						Guid = a.Guid,
						Key = a.Key,
						Value = a.Value.Compress().GetAwaiter().GetResult()
					}));
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

		public async Task<SessionDataModel> BuscarItemByIDAsync(SessionDataModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{ 
				var retorno =await conn.QuerySingleOrDefaultAsync<SessionDataModel>("SELECT [CODIGO], [VALUE], [DATA],[KEY]  FROM SESSION_ITENS WHERE [GUID]=@Guid AND [KEY]=@Key", t);

					if (retorno != null)
						retorno.Value = await retorno.Value.Decompress();

					return retorno;
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

		public Task<IEnumerable<SessionDataModel>> BuscarItensAsync(SessionDataModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task ExcluirItemByKeysAsync(IEnumerable<SessionDataModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("DELETE FROM SESSION_ITENS WHERE [GUID]=@Guid AND [KEY]=@Key", t, transaction: tran);

					tran.Commit();
				}
				catch (Exception err)
				{
					tran.Rollback();
					throw err;
				}
				finally
				{
					conn.Close();
				}
			}
		}

		public async Task ExcluirItensAsync(IEnumerable<SessionDataModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync("DELETE FROM SESSION_ITENS WHERE [GUID]=@Guid", t);
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

		public Task ExcluirItensUpdateAsync(IEnumerable<SessionDataModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<SessionDataModel>> ObterTodosAsyncByNumericKey(SessionDataModel t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var dados = await conn.QueryAsync<SessionDataModel>("SELECT [CODIGO], [VALUE], [DATA], [KEY]  FROM SESSION_ITENS WHERE [GUID]=@Guid AND ISNUMERIC([KEY])=1", t);

					if (dados.Any())
						dados = dados.Select(a => new SessionDataModel()
						{
							Guid = a.Guid,
							Key = a.Key,
							Value = a.Value.Decompress().GetAwaiter().GetResult(),
							Codigo=a.Codigo
						});

					return dados;
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
		public async Task<IEnumerable<SessionDataModel>> ObterTodosAsync(SessionDataModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var dados = await conn.QueryAsync<SessionDataModel>("SELECT [CODIGO], [VALUE], [DATA], [KEY]  FROM SESSION_ITENS WHERE [GUID]=@Guid", t);


					if (dados.Any())
						dados = dados.Select(a => new SessionDataModel()
						{
							Guid = a.Guid,
							Key = a.Key,
							Value = a.Value.Decompress().GetAwaiter().GetResult(),
							Codigo = a.Codigo
						});

					return dados;
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

		public Task<IEnumerable<SessionDataModel>> ObterTodosPaginadoAsync(SessionDataModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
