using Dapper;
using FastMember;
using Microsoft.EntityFrameworkCore;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALPrefixo
	{
		public async Task AdicionarItens(IEnumerable<PrefixoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("TRUNCATE TABLE HELPER.dbo.PREFIXO", transaction:tran);


					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					using (var reader = ObjectReader.Create(t.Select(a => new
					{
						a.Prefixo,
						OperadoraID = (byte)a.Operadora
					}), "Prefixo", "OperadoraID"))
					{

						bcp.DestinationTableName = "HELPER.dbo.PREFIXO";
						bcp.ColumnMappings.Add("Prefixo", "PREFIXO");
						bcp.ColumnMappings.Add("OperadoraID", "OPERADORAID");
						
						await bcp.WriteToServerAsync(reader);
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

		public Task AtualizaItens(IEnumerable<PrefixoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<PrefixoModel> BuscarItemByID(PrefixoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<PrefixoModel>> BuscarItens(PrefixoModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<int> ExcluirTodos()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					return await conn.ExecuteAsync("TRUNCATE TABLE PREFIXOS");
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


		public Task ExcluirItens(IEnumerable<PrefixoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<PrefixoModel>> ObterTodos() => await DALGeneric.GenericReturnAsync<PrefixoModel>("SELECT PREFIXO, OPERADORAID OPERADORA FROM HELPER.dbo.PREFIXO");
	}
}
