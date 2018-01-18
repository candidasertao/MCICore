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
	public class DALBlacklist : IDal<BlackListModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<BlackListModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{

					t = t.Except(await conn.QueryAsync<BlackListModel>("SELECT CELULAR FROM CELULAR_BLACKLIST WHERE CLIENTEID=@ClienteID", new { ClienteID = c }, transaction: tran),
										new CompareObject<BlackListModel>((a, b) => a.Celular == b.Celular, i => i.Celular.GetHashCode()));

					if (!t.Any())
						throw new Exception("Número(s) já cadastrado(s) na base");

					await conn.ExecuteAsync(@"INSERT INTO [dbo].[CELULAR_BLACKLIST]([CELULAR],[DATA],[CLIENTEID])VALUES(@Celular, @Data, @ClienteID)", t.Select(a => new
					{
						Celular = a.Celular,
						Data = DateTime.Now,
						ClienteID = c
					}), transaction: tran, commandTimeout: 888);

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t.ToList(), null, c, u, ModuloAtividadeEnumns.BLACKLIST, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task AtualizaItensAsync(IEnumerable<BlackListModel> t, int c, int? u)
		{
			await DALGeneric.GenericExecuteAsync(@"UPDATE CELULAR_BLACKLIST SET CELULAR=@Celular WHERE BLACKLISTID=@BlacklistID AND ClienteID=@ClienteID",
				param: t.Select(a => new
				{
					BlacklistID = a.BlacklistID,
					Celular = a.Celular,
					ClienteID = c
				}),
				hastransaction: true);

			try
			{
#pragma warning disable 4014
				new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.BLACKLIST, TiposLogAtividadeEnums.ATUALIZACAO);
#pragma warning restore 4014
			}
			catch { }


		}

		public async Task<BlackListModel> BuscarItemByIDAsync(BlackListModel t, int? u)
		{
			var p = new DynamicParameters();
			p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
			p.Add("BlacklistID", t.BlacklistID, DbType.Int32, ParameterDirection.Input);

			var result = await DALGeneric.GenericReturnSingleOrDefaultAsyn<dynamic>(@"SELECT CELULAR, DATA, BLACKLISTID FROM CELULAR_BLACKLIST WHERE CLIENTEID=@ClienteID AND BLACKLISTID=@BlacklistID", d: p);

			if (result != null)
			{
				return new BlackListModel()
				{
					BlacklistID = t.BlacklistID,
					Celular = result.CELULAR,
					Cliente = new ClienteModel() { ClienteID = result.CLIENTEID },

				};
			}
			else
				return null;


		}

		public async Task<IEnumerable<BlackListModel>> BuscarItensAsync(BlackListModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT CELULAR, DATA, BLACKLISTID FROM CELULAR_BLACKLIST WHERE CLIENTEID=@ClienteID AND CAST(CELULAR AS VARCHAR(12)) LIKE '%'+@Busca+'%'  ORDER BY DATA DESC";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
					{
						return result.Select(a => new BlackListModel()
						{
							Cliente = t.Cliente,
							Celular = a.CELULAR,
							Data = a.DATA,
							BlacklistID = a.BLACKLIST
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

		public async Task ExcluirItensAsync(IEnumerable<BlackListModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					await conn.ExecuteAsync("DELETE FROM CELULAR_BLACKLIST WHERE BLACKLISTID=@BlacklistID AND CLIENTEID=@ClienteID", t.Select(a => new { ClienteID = c, BlacklistID = a.BlacklistID }), commandTimeout: 888);

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.BLACKLIST, TiposLogAtividadeEnums.EXCLUSAO);
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

		public Task ExcluirItensUpdateAsync(IEnumerable<BlackListModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<BlackListModel>> ObterTodosAsync(BlackListModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					string query = "SELECT CELULAR, DATA, BLACKLISTID FROM CELULAR_BLACKLIST WHERE CLIENTEID=@ClienteID ORDER BY DATA DESC";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result.Any())
						return result.Select(a => new BlackListModel()
						{
							Cliente = t.Cliente,
							Celular = a.CELULAR,
							Data = a.DATA,
							BlacklistID = a.BLACKLISTID
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

		public async Task<IEnumerable<BlackListModel>> ObterTodosPaginadoAsync(BlackListModel t, int? u)
		{


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

			var result = await DALGeneric.GenericReturnAsync<dynamic>(string.Format("SELECT CELULAR, DATA, BLACKLISTID FROM CELULAR_BLACKLIST WHERE CLIENTEID=@ClienteID {0} ORDER BY DATA DESC", !string.IsNullOrEmpty(t.Search) ? "AND CAST(CELULAR AS VARCHAR(11)) LIKE '%'+@Search+'%'" : string.Empty), 
				d: p);

			if (result.Any())
			{
				var dados = result.Select(a => new BlackListModel()
				{
					Cliente = t.Cliente,
					Celular = a.CELULAR,
					Data = a.DATA,
					BlacklistID = a.BLACKLISTID,
					Registros = result.Count(),
					Paginas = result.Count() / t.Registros
				})
				.Skip((t.PaginaAtual.Value - 1) * t.Registros)
				.Take(t.Registros);

				return dados;
			}
			return null;
		}
	}
}
