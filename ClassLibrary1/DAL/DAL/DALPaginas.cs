using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALPaginas : IDal<PaginaModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<PaginaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"INSERT INTO [dbo].[PAGINAS]([PAGINA],[URL],[GRUPOID])VALUES(@Pagina, @Url @GrupoID)", t.Select(a => new
					{
						CilenteID = c
						
					}), transaction: tran,
					commandTimeout: 888);

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

		public Task AtualizaItensAsync(IEnumerable<PaginaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<PaginaModel> BuscarItemByIDAsync(PaginaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<PaginaModel>> BuscarItensAsync(PaginaModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<PaginaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<PaginaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<PaginaModel>> ObterTodosAsync(PaginaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<PaginaModel>> ObterTodosPaginadoAsync(PaginaModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
