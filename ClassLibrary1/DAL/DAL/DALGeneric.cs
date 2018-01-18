using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	internal static class DALGeneric
	{
		/// <summary>
		/// Retorna um enumerable de um objeto T específico
		/// </summary>
		/// <typeparam name="T">tiop o parâmetro</typeparam>
		/// <param name="s">query</param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static async Task<IEnumerable<T>> GenericReturnAsync<T>(string s, DynamicParameters d = null, object param = null, CommandType commandtype = CommandType.Text, int commandtimeout = 888)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					return param == null && d == null ? await conn.QueryAsync<T>(s, commandType: commandtype, commandTimeout: commandtimeout) :
																		await conn.QueryAsync<T>(s, param ?? d, commandType: commandtype, commandTimeout: commandtimeout);

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

		public static async Task<T> GenericReturnSingleOrDefaultAsyn<T>(string query, DynamicParameters d = null, object param = null, CommandType commandtype = CommandType.Text, int commandtimeout = 888)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					return param == null && d == null ? await conn.QuerySingleOrDefaultAsync<T>(query, commandType: commandtype, commandTimeout: commandtimeout) :
																		await conn.QuerySingleOrDefaultAsync<T>(query, param ?? d, commandType: commandtype, commandTimeout: commandtimeout);

					
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
		/// <summary>
		/// Execute genérico
		/// </summary>
		/// <param name="s">query pra execução</param>
		/// <param name="d">parâmetros pra execução</param>
		/// <returns> Int32 quantidade de registros afetados</returns>
		public static async Task<int> GenericExecuteAsync(string s, DynamicParameters d = null, object param = null, CommandType commandtype = CommandType.Text, int commandtimeout = 888, bool hastransaction = false)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				SqlTransaction transaction = null;

				try
				{
					int retornos = 0;

					if (hastransaction)
					{
						transaction = conn.BeginTransaction();
						retornos = param == null && d == null ? await conn.ExecuteAsync(s, commandType: commandtype, commandTimeout: commandtimeout, transaction: transaction) :
																	await conn.ExecuteAsync(s, param ?? d, commandType: commandtype, commandTimeout: commandtimeout, transaction: transaction);
						transaction.Commit();
					}
					else
						retornos = param == null && d == null ? await conn.ExecuteAsync(s, commandType: commandtype, commandTimeout: commandtimeout) :
																		await conn.ExecuteAsync(s, param ?? d, commandType: commandtype, commandTimeout: commandtimeout);

					return retornos;

				}
				catch (Exception err)
				{
					if (hastransaction) transaction.Rollback();

					throw err;
				}
				finally
				{
					if (hastransaction) transaction.Dispose();

					conn.Close();
				}
			}
		}
	}
}
