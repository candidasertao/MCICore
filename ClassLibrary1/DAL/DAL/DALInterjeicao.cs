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
	public class DALInterjeicao : IDal<InterjeicaoModel>
	{


		public async Task AdicionarItensAsync(IEnumerable<InterjeicaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"INSERT INTO RETORNO_INTERJEICAO (CLIENTEID, INTERJEICAO, USUARIOID, CLASSIFICACAO) VALUES (@ClienteID, @Interjeicao, @UsuarioID, @Classificacao)", t.Select(a => new
					{
						ClienteID = c,
						Interjeicao = a.Interjeicao,
						UsuarioID = u,
						Classificacao = (byte)a.Classificacao
					}), transaction: tran, commandTimeout: 888);

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

		public async Task AtualizaItensAsync(IEnumerable<InterjeicaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					string query = @"UPDATE RETORNO_INTERJEICAO SET INTERJEICAO=@Interjeicao, CLASSIFICACAO=@Classificacao WHERE CODIGO=@Codigo AND ClienteID=@ClienteID";

					if (u.HasValue)
						query = @"UPDATE RETORNO_INTERJEICAO SET INTERJEICAO=@Interjeicao, CLASSIFICACAO=@Classificacao WHERE CODIGO=@Codigo AND ClienteID=@ClienteID AND USUARIOID=@UsuarioID";

					var r = await conn.ExecuteAsync(query, t.Select(a => new
					{
						ClienteID = c,
						Interjeicao = a.Interjeicao,
						Classificacao = (byte)a.Classificacao,
						Codigo = a.Codigo,
						UsuarioID = u
					}), transaction: tran, commandTimeout: 888);

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

		public async Task<InterjeicaoModel> BuscarItemByIDAsync(InterjeicaoModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT [CODIGO],[INTERJEICAO],[CLASSIFICACAO] FROM [dbo].[RETORNO_INTERJEICAO] WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Codigo", t.Codigo, DbType.Int32, ParameterDirection.Input);

					if (u.HasValue)
					{
						p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
						query = "SELECT [INTERJEICAO],[CLASSIFICACAO] FROM [dbo].[RETORNO_INTERJEICAO] WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID";
					}


					var result = await conn.QuerySingleOrDefaultAsync<dynamic>(query, p);

					if (result != null)
						return new InterjeicaoModel()
						{
							Cliente = new ClienteModel() { ClienteID = result.CLIENTEID },
							Usuario = u.HasValue ? new UsuarioModel() { UsuarioID = u.Value } : null,
							Interjeicao = result.INTERJEICAO,
							Codigo = t.Codigo,
							Classificacao = ((ClassificaoRetornoEnums)Enum.Parse(typeof(ClassificaoRetornoEnums), result.CLASSIFICACAO.ToString()))
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

		public Task<IEnumerable<InterjeicaoModel>> BuscarItensAsync(InterjeicaoModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task ExcluirItensAsync(IEnumerable<InterjeicaoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{

					string query = "DELETE FROM [RETORNO_INTERJEICAO] WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID";

					if (u.HasValue)
						query = "DELETE FROM [RETORNO_INTERJEICAO] WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID";
					
					await conn.ExecuteAsync(query, t.Select(a => new { ClienteID = c, Codigo = a.Codigo, UsuarioID = u }), commandTimeout: 888);
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

		public Task ExcluirItensUpdateAsync(IEnumerable<InterjeicaoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<InterjeicaoModel>> ObterTodosAsync(InterjeicaoModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					string query = "SELECT [CODIGO],[INTERJEICAO],[CLASSIFICACAO] FROM [dbo].[RETORNO_INTERJEICAO] WHERE CLIENTEID=@ClienteID";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);


					if (u.HasValue)
					{
						query = "SELECT [CODIGO],[INTERJEICAO],[CLASSIFICACAO] FROM [dbo].[RETORNO_INTERJEICAO] WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID";
						p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
					}

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
						return result.Select(a => new InterjeicaoModel()
						{
							Cliente = new ClienteModel() { ClienteID = t.Cliente.ClienteID },
							Interjeicao = a.INTERJEICAO,
							Codigo = a.CODIGO,
							Classificacao = ((ClassificaoRetornoEnums)Enum.Parse(typeof(ClassificaoRetornoEnums), a.CLASSIFICACAO.ToString()))
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

		public Task<IEnumerable<InterjeicaoModel>> ObterTodosPaginadoAsync(InterjeicaoModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
