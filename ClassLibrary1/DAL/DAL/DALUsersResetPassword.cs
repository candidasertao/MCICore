using Dapper;
using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConecttaManagerData.DAL
{
	public class DALUsersResetPassword : IDal<UsersResetPasswordModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<UsersResetPasswordModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{

					var users = t.ElementAt(0);
					var clienteusuario = await ClienteUsuarioAsync(c, u, users.LoginUser, conn, tran);
					int clienteID = clienteusuario.ClienteID;
					int? usuarioID = clienteusuario.UsuarioID;

					if (clienteID == 0)
						throw new Exception("Cliente não localizado com o login informado");


					var p = new DynamicParameters();
					p.Add("ClienteID", clienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", usuarioID, DbType.Int32, ParameterDirection.Input);
					p.Add("Token", users.Token, DbType.String, ParameterDirection.Input);
					p.Add("SenhaTrocada", users.SenhaTrocada, DbType.Boolean, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);

					await conn.ExecuteAsync(@"INSERT INTO [dbo].[USERS_RESETPASSWORD] (CLIENTEID, USUARIOID, DATA, TOKEN, SENHATROCADA) VALUES (@ClienteID, @UsuarioID, @Data, @Token, @SenhaTrocada);", p, transaction: tran, commandTimeout: 888);

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

		async Task<(int ClienteID, int? UsuarioID)> ClienteUsuarioAsync(int clienteid, int? usuarioid, string loginuser, SqlConnection conn, SqlTransaction tran)
		{
			int clienteID = 0;
			int? usuarioID = usuarioid;
			if (!string.IsNullOrEmpty(loginuser))
			{
				var retorno = await conn.QueryAsync("SELECT CLIENTEID, USUARIOID FROM USUARIOS WHERE LOGINUSER=@Login", new { Login = loginuser }, transaction: tran);
				if (retorno != null && retorno.Count() > 0)
				{
					clienteID = (int)retorno.ElementAt(0).CLIENTEID;
					usuarioID = retorno.ElementAt(0).USUARIOID;
				}
				else //retorno  = null
				{
					retorno = await conn.QueryAsync("SELECT CLIENTEID FROM CLIENTES WHERE CNPJ=@CNPJ", new { CNPJ = loginuser }, transaction: tran);

					if (retorno != null)
						clienteID = retorno.ElementAt(0).CLIENTEID;
				}
			}
			else
				clienteID = clienteid;

			return (clienteID, usuarioID);
		}

		public async Task AtualizaItensAsync(IEnumerable<UsersResetPasswordModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var user = t.ElementAt(0);
					var clienteusuario = await ClienteUsuarioAsync(c, u, user.LoginUser, conn, tran);

					if (clienteusuario.ClienteID == 0)
						throw new Exception("Cliente não localizado com o login informado");

					var p = new DynamicParameters();
					p.Add("Token", user.Token, DbType.String, ParameterDirection.Input);
					p.Add("SenhaTrocada", user.SenhaTrocada, DbType.Boolean, ParameterDirection.Input);

					var affected = await conn.ExecuteAsync(
															@"UPDATE [dbo].[USERS_RESETPASSWORD] SET SENHATROCADA=@SenhaTrocada WHERE TOKEN=@Token;"
															, p, transaction: tran, commandTimeout: 888);

					if (affected == 0)
						throw new Exception("Token Inválido");

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

		public async Task<UsersResetPasswordModel> BuscarItemByIDAsync(UsersResetPasswordModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT COALESCE(U.LOGINUSER,C.[CNPJ]) LOGINUSER
                                    FROM [dbo].[USERS_RESETPASSWORD] R
                                    LEFT JOIN [dbo].[USUARIOS] U ON R.USUARIOID = U.USUARIOID
									LEFT JOIN CLIENTES C ON R.CLIENTEID=C.CLIENTEID
                                    WHERE R.SENHATROCADA = 0
                                    AND DATEDIFF(HOUR, R.DATA, GETDATE()) <= 48
                                    AND TOKEN = @Token";

					var p = new DynamicParameters();
					p.Add("Token", t.Token, DbType.String, ParameterDirection.Input);
					p.Add("Hoje", DateTime.Now, DbType.DateTime, ParameterDirection.Input);

					var result = await conn.QuerySingleOrDefaultAsync<string>(query, p);

					if (result != null)
						return new UsersResetPasswordModel()
						{
							LoginUser = result
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

		public Task<IEnumerable<UsersResetPasswordModel>> BuscarItensAsync(UsersResetPasswordModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<UsersResetPasswordModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<UsersResetPasswordModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<UsersResetPasswordModel>> ObterTodosAsync(UsersResetPasswordModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<UsersResetPasswordModel>> ObterTodosPaginadoAsync(UsersResetPasswordModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
