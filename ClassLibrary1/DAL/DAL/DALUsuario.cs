using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace DAL
{
	public class DALUsuario : IDal<UsuarioModel>
	{
		/// <summary>
		/// Atualiza o saldo do usuário
		/// </summary>
		/// <param name="t"></param>
		/// <param name="c"></param>
		/// <param name="quantidade"></param>
		/// <param name="tran"></param>
		/// <param name="conn"></param>
		/// <returns></returns>
		public async Task<int> AlteraSaldoUsuario(UsuarioModel t, int c, int quantidade, SqlTransaction tran = null, SqlConnection conn = null)
		{
			var p = new DynamicParameters();
			p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
			p.Add("UsuarioID", t.UsuarioID, DbType.Int32, ParameterDirection.Input);
			p.Add("Quantidade", quantidade, DbType.Int32, ParameterDirection.Input);


			string query = @"UPDATE U SET SALDO=U.COTA FROM USUARIOS U JOIN [dbo].[GRUPOUSUARIOS] G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID WHERE U.USUARIOID=@UsuarioID AND G.SALDOCOMPARTILHADO=0 AND U.SALDOILIMITADO=0 AND U.CLIENTEID=@ClienteID";


			if (tran != null && conn != null)
				return await conn.ExecuteAsync(query, p, transaction: tran);
			else
			{
				using (var connection = new SqlConnection(Util.ConnString))
				{
					try
					{
						await connection.OpenAsync();
						return await connection.ExecuteAsync(query, p, commandTimeout: 888);
					}
					catch (Exception err)
					{

						throw err;
					}
					finally
					{
						connection.Close();
					}
				}
			}
		}

		public async ValueTask<string> BuscaByEmail(UsuarioModel u, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				var p = new DynamicParameters();
				p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
				p.Add("UsuarioID", u.UsuarioID, DbType.Int32, ParameterDirection.Input);

				await conn.OpenAsync();

				try
				{
					return await conn.QuerySingleOrDefaultAsync<string>(@"SELECT EMAIL FROM USUARIOS WHERE USUARIOID=@UsuarioID", p, commandTimeout: Util.TIMEOUTEXECUTE);

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

		public async ValueTask<int> RenovaSaldoUsuarioAsync(UsuarioModel u, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				var p = new DynamicParameters();
				p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
				p.Add("UsuarioID", u.UsuarioID, DbType.Int32, ParameterDirection.Input);

				await conn.OpenAsync();
				try
				{
					return await conn.ExecuteAsync(@"UPDATE U SET SALDO=U.COTA FROM USUARIOS U
													JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
													 WHERE USUARIOID=@UsuarioID AND U.CLIENTEID=@ClienteID AND SALDOILIMITADO=0 AND G.SALDOCOMPARTILHADO=0", p, commandTimeout: Util.TIMEOUTEXECUTE);



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


		public async Task<int> AlteraSaldoUsuarioEnvio(UsuarioModel u, int c, int quantidade, SqlTransaction tran = null, SqlConnection conn = null)
		{
			var p = new DynamicParameters();
			p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
			p.Add("UsuarioID", u.UsuarioID, DbType.Int32, ParameterDirection.Input);
			p.Add("Quantidade", quantidade, DbType.Int32, ParameterDirection.Input);

			int retorno = 0;

			if (tran != null && conn != null)
			{
				retorno = await conn.ExecuteAsync(@"UPDATE G SET SALDO=G.SALDO-(@Quantidade) FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID AND U.CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND SALDOCOMPARTILHADO=1 AND U.SALDOILIMITADO=0", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

				if (retorno == 0)
					retorno = await conn.ExecuteAsync(@"UPDATE U SET SALDO=SALDO-(@Quantidade) FROM USUARIOS U WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND SALDOILIMITADO=0", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

			}
			else
			{
				using (var connection = new SqlConnection(Util.ConnString))
				{
					await connection.OpenAsync();
					SqlTransaction transaction = conn.BeginTransaction();
					try
					{
						retorno = await connection.ExecuteAsync(@"UPDATE G SET  SALDO=G.SALDO-(@Quantidade) FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID AND U.CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND SALDOCOMPARTILHADO=1", p, transaction: transaction, commandTimeout: Util.TIMEOUTEXECUTE);

						if (retorno == 0)
							retorno = await connection.ExecuteAsync(@"UPDATE U SET SALDO=SALDO-(@Quantidade) FROM USUARIOS U WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND SALDOILIMITADO=0", p, transaction: transaction, commandTimeout: Util.TIMEOUTEXECUTE);

						transaction.Commit();

					}
					catch (Exception err)
					{

						throw err;
					}
					finally
					{
						transaction.Dispose();
						connection.Close();
					}
				}
			}
			return retorno;
		}

		public async Task<(int, bool)> SaldoUsuario(int c, int u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);



					var dados = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT IIF(G.SALDOCOMPARTILHADO=1, G.SALDO, U.SALDO) SALDO, SALDOILIMITADO FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID WHERE USUARIOID=@UsuarioID AND U.CLIENTEID=@ClienteID AND ATIVO=1", p);


					if (dados == null)
						return (-1, false);

					return ((int)dados.SALDO, (bool)dados.SALDOILIMITADO);

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

		public async Task<int> AddUser(UsuarioModel t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					if (t.SaldoCotaIlimitado)
						t.Cota = 0;

					//var saldocompartilhado = await conn.QuerySingleOrDefaultAsync<bool>("SELECT SALDOCOMPARTILHADO FROM GRUPOUSUARIOS WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND USUARIOID=@UsuarioID", new { ClienteID = c, GrupoUsuarioID = t.GrupoUsuario.GrupoUsuarioID }, transaction: tran);

					//if (saldocompartilhado)
					//	t.Cota = 0;



					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("LoginUser", t.LoginUser, DbType.String, ParameterDirection.Input);
					p.Add("Nome", t.Nome.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("SaldoIlimitado", t.SaldoCotaIlimitado, DbType.Boolean, ParameterDirection.Input);
					p.Add("Email", t.Email.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("Telefone", t.Telefone, DbType.Decimal, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("Ativo", t.Ativo, DbType.Boolean, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", t.GrupoUsuario.GrupoUsuarioID, DbType.Int32, ParameterDirection.Input);
					p.Add("AdmPerfil", t.AdmPerfil, DbType.Boolean, ParameterDirection.Input);
					p.Add("UsuarioID", DbType.Int32, direction: ParameterDirection.Output);

					var registrosAfectados = 0;

					if (!t.SaldoCotaIlimitado)//se não for ilimitado, tenta abater do grupo, caso seja saldo compartilhado
						registrosAfectados = await conn.ExecuteAsync(@"UPDATE G SET SALDO=SALDO-@Cota FROM GRUPOUSUARIOS G WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID AND SALDOCOMPARTILHADO=0 AND ISEXCLUDED=0", new { ClienteID = c, GrupoUsuarioID = t.GrupoUsuario.GrupoUsuarioID, Cota = t.Cota }, transaction: tran, commandTimeout: 888);

					if (registrosAfectados == 0)
					{
						t.Cota = 0;
						t.Saldo = 0;
					}

					p.Add("Saldo", t.Cota, DbType.Int32, ParameterDirection.Input);
					p.Add("Cota", t.Cota, DbType.Int32, ParameterDirection.Input);

					await conn.QueryAsync<int>(@"INSERT INTO[dbo].[USUARIOS]([NOME],[LOGINUSER],[CLIENTEID],[SALDO],[SALDOILIMITADO],[EMAIL],[TELEFONE],[DATA],[ATIVO], [GRUPOUSUARIOID], COTA, ADMPERFIL, ISEXCLUDED) VALUES 
																			(@Nome, @LoginUser, @ClienteID, @Saldo, @SaldoIlimitado, @Email, @Telefone, @Data, @Ativo, @GrupoUsuarioID, @Cota, @AdmPerfil, 0);
												SELECT @UsuarioID = SCOPE_IDENTITY()",
												p, transaction: tran, commandTimeout: 888);


					await conn.ExecuteAsync(@"INSERT INTO USUARIOS_CARTEIRA (USUARIOID, CARTEIRAID) VALUES (@UsuarioID, @CarteiraID)",
											t.Carteiras.Select(l => new
											{
												UsuarioID = p.Get<int>("UsuarioID"),
												CarteiraID = l.CarteiraID
											}), transaction: tran, commandTimeout: 888);

					tran.Commit();

					int usuarioID = p.Get<int>("UsuarioID");


					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.GRAVACAO);
#pragma warning restore 4014
					}
					catch { }

					return usuarioID;
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

		public Task AdicionarItensAsync(IEnumerable<UsuarioModel> t, int c, int? u)
		{


			throw new NotImplementedException();
		}

		public async Task AtualizaItensAsync(IEnumerable<UsuarioModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var _user = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT SALDOILIMITADO, U.GRUPOUSUARIOID, U.COTA, U.SALDO, G.SALDOCOMPARTILHADO, U.USUARIOID FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID WHERE USUARIOID=@UsuarioID AND U.CLIENTEID=@ClienteID AND U.ISEXCLUDED=0",
						new { UsuarioID = t.ElementAt(0).UsuarioID, ClienteID = c }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);





					UsuarioModel _userNew = t.ElementAt(0);
					UsuarioModel _userOld = new UsuarioModel()
					{
						UsuarioID = _user.USUARIOID,
						SaldoCotaIlimitado = _user.SALDOILIMITADO,
						Cota = _user.COTA,
						Saldo = _user.SALDO,
						GrupoUsuario = new GrupoUsuariosModel()
						{
							GrupoUsuarioID = _user.GRUPOUSUARIOID,
							SaldoCompartilhado = _user.SALDOCOMPARTILHADO
						}
					};


					if (_userNew.SaldoCotaIlimitado && !_userOld.SaldoCotaIlimitado)//zera o saldo e cota do usuário e o devolve ao grupo dele
					{
						await conn.ExecuteAsync("UPDATE G SET SALDO=IIF(G.SALDO+@Saldo>G.COTA, G.COTA,G.SALDO+@Saldo) FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID AND SALDOCOMPARTILHADO=0", new
						{
							ClienteID = c,
							GrupoUsuarioID = _userOld.GrupoUsuario.GrupoUsuarioID,
							Saldo = _userOld.Cota
						}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

						_userNew.Cota = 0;
						_userNew.Saldo = 0;

					}
					else if (!_userNew.SaldoCotaIlimitado && _userOld.SaldoCotaIlimitado)//abate o valor da cota para o usuário
					{
						var grupousuarioid = _userNew.GrupoUsuario.GrupoUsuarioID;

						if (_userNew.GrupoUsuario.GrupoUsuarioID != _userOld.GrupoUsuario.GrupoUsuarioID)//se houve troca de grupo, o saldo a ser devolvido deve ser mandando para o grupo anterior
							grupousuarioid = _userOld.GrupoUsuario.GrupoUsuarioID;



						var _p = new DynamicParameters();
						_p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
						_p.Add("GrupoUsuarioID", grupousuarioid, DbType.Int32, ParameterDirection.Input);
						_p.Add("Cota", _userNew.Cota, DbType.Int32, ParameterDirection.Input);




						var retorno = await conn.ExecuteAsync(@"UPDATE G SET SALDO=G.SALDO-@Cota FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND SALDOCOMPARTILHADO=0 AND CLIENTEID=@ClienteID", _p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE); //se tiver registros afetados, indica que houve o débito correto do saldo da cota do grupo, caso contrário, é compartilhado e deve assim zerar o saldo do cliente

						if (retorno > 0)
							_userNew.Saldo = _userNew.Cota.HasValue ? _userNew.Cota.Value : 0;
						else
						{
							_userNew.Saldo = 0;
							_userNew.Cota = 0;
						}
					}
					else if (_userNew.SaldoCotaIlimitado == _userOld.SaldoCotaIlimitado) //se não foi alterado o saldo ilimitado faz os ajustes de cota e saldo
					{

						if (!_userNew.SaldoCotaIlimitado)
						{


							if (_userNew.GrupoUsuario.GrupoUsuarioID != _userOld.GrupoUsuario.GrupoUsuarioID)//houve troca de grupo, deve ser validado o valor da cota, pois caso o cliente deixe o valor da  cota anterior
							{

								//devolve o valor anterior pra o grupo antigo
								await conn.ExecuteAsync(@"UPDATE G SET SALDO=IIF(G.SALDO+@Cota>G.COTA, G.COTA, G.SALDO+@Cota) FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND SALDOCOMPARTILHADO=0 AND CLIENTEID=@ClienteID",
									 new
									 {
										 ClienteID = c,
										 GrupoUsuarioID = _userOld.GrupoUsuario.GrupoUsuarioID,
										 Cota = _userOld.Cota
									 }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);



								//acrescenta o valor novo da cota para o grupo novo
								var retorno = await conn.ExecuteAsync(@"UPDATE G SET SALDO=G.SALDO-@Cota FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND SALDOCOMPARTILHADO=0 AND CLIENTEID=@ClienteID",
										new
										{
											ClienteID = c,
											GrupoUsuarioID = _userNew.GrupoUsuario.GrupoUsuarioID,
											Cota = _userNew.Cota
										}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

								// se o novo grupo possui saldo compartilhado, a cota do usuário fica zerada desde q o  resultado do update tenha surtido efeito
								if (retorno == 0)
									_userNew.Cota = 0;
							}
							else
							{
								var saldocompartilhado = await conn.QuerySingleAsync<bool>("SELECT SALDOCOMPARTILHADO FROM GRUPOUSUARIOS WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID", new { ClienteID = c, GrupoUsuarioID = _userNew.GrupoUsuario.GrupoUsuarioID }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

								if (!saldocompartilhado)
								{
									if (_userOld.Cota != _userNew.Cota) //houve troca de valor de cota, ajusta o valor do grupo
									{
										var grupousuarioid = _userNew.GrupoUsuario.GrupoUsuarioID;

										string q = null;

										if (_userNew.Cota < _userOld.Cota)//o novo valor da COTA for maior do q o antigo, 
											q = "UPDATE G SET SALDO=G.SALDO+@Saldo FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID AND SALDOCOMPARTILHADO=0";
										else
											q = "UPDATE G SET SALDO=G.SALDO-@Saldo FROM GRUPOUSUARIOS G WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID AND SALDOCOMPARTILHADO=0";

										var retorno = await conn.ExecuteAsync(q, new
										{
											ClienteID = c,
											GrupoUsuarioID = grupousuarioid,
											Saldo = _userNew.Cota > _userOld.Cota ? _userNew.Cota - _userOld.Cota : _userOld.Cota - _userNew.Cota
										}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

									}
								}
								else
								{
									_userNew.Cota = 0;
								}
							}
						}
					}

					_userNew.Saldo = _userNew.Cota.Value;


					//if (!_userNew.SaldoCotaIlimitado)
					//{
					//    //identitifica se novo grupo, há cota disponível pra o valor apresentado na cota do usuário
					//    var cotas = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT SUM(U.COTA) COTAUSUARIOS, G.COTA COTAGRUPO   FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID WHERE U.CLIENTEID=@ClienteID AND U.GRUPOUSUARIOID=@GrupoUsuarioID GROUP BY U.GRUPOUSUARIOID, G.COTA",
					//        new
					//        {
					//            ClienteID = c,
					//            GrupoUsuarioID = _userNew.GrupoUsuario.GrupoUsuarioID
					//        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					//    var cotaRestante = (int)cotas.COTAGRUPO - (((int)cotas.COTAUSUARIOS - _userOld.Cota) + _userNew.Cota);

					//    if (cotaRestante < 0)
					//        throw new Exception($"Não há cota disponível no novo grupo selecionado. Cota restante do grupo {cotaRestante}");
					//}

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Nome", _userNew.Nome.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("Saldo", _userNew.Saldo, DbType.Int32, ParameterDirection.Input);
					p.Add("SaldoIlimitado", _userNew.SaldoCotaIlimitado, DbType.Boolean, ParameterDirection.Input);
					p.Add("Email", _userNew.Email.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("Telefone", _userNew.Telefone, DbType.Decimal, ParameterDirection.Input);
					p.Add("Ativo", _userNew.Ativo, DbType.Boolean, ParameterDirection.Input);
					p.Add("Cota", _userNew.Cota, DbType.Int32, ParameterDirection.Input);
					p.Add("AdmPerfil", _userNew.AdmPerfil, DbType.Boolean, ParameterDirection.Input);
					p.Add("UsuarioID", _userNew.UsuarioID, DbType.Int32, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", _userNew.GrupoUsuario.GrupoUsuarioID, DbType.Int32, ParameterDirection.Input);


					await conn.ExecuteAsync(@"UPDATE U SET 
													NOME=@Nome,
													SALDOILIMITADO=@SaldoIlimitado, 
													EMAIL=@Email, 
													TELEFONE=@Telefone, 
													ATIVO=@Ativo, 
													GRUPOUSUARIOID=@GrupoUsuarioID, 
													COTA=@Cota,
													SALDO=@Saldo,
													ADMPERFIL=@AdmPerfil FROM USUARIOS U
													WHERE USUARIOID=@UsuarioID AND U.CLIENTEID=@ClienteID", p, transaction: tran, commandTimeout: 888);


					await conn.ExecuteAsync(@"DELETE FROM USUARIOS_CARTEIRA WHERE USUARIOID=@UsuarioID", new { UsuarioID = _userNew.UsuarioID }, transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"INSERT INTO USUARIOS_CARTEIRA (USUARIOID, CARTEIRAID) VALUES (@UsuarioID, @CarteiraID)",
											_userNew.Carteiras.Select(l => new
											{
												UsuarioID = t.ElementAt(0).UsuarioID,
												CarteiraID = l.CarteiraID
											}), transaction: tran, commandTimeout: 888);


					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.ATUALIZACAO);
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




		public async Task<UsuarioModel> BuscarItemByIDAsync(UsuarioModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", t.UsuarioID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync<dynamic>(@"SELECT U.NOME, LOGINUSER, U.SALDO, SALDOILIMITADO,EMAIL, TELEFONE, DATA, ATIVO, G.NOME GRUPO, U.USUARIOID, G.GRUPOUSUARIOID, U.COTA, ADMPERFIL, UC.CARTEIRAID, G.COTA COTAGRUPO, G.SALDO SALDOGRUPO, G.SALDOCOMPARTILHADO  FROM USUARIOS U 
							JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
							LEFT JOIN USUARIOS_CARTEIRA UC ON U.USUARIOID=UC.USUARIOID
							WHERE U.USUARIOID=@UsuarioID AND U.CLIENTEID=@ClienteID", p);

					if (result != null)
						return result.GroupBy(a => new UsuarioModel()
						{
							AdmPerfil = a.ADMPERFIL,
							Ativo = a.ATIVO,
							Nome = a.NOME,
							LoginUser = a.LOGINUSER,
							Saldo = a.SALDO,
							Cliente = t.Cliente,
							Data = a.DATA,
							Email = a.EMAIL,
							Telefone = a.TELEFONE,
							UsuarioID = a.USUARIOID,
							SaldoCotaIlimitado = a.SALDOILIMITADO,
							Cota = a.COTA,
							GrupoUsuario = new GrupoUsuariosModel() { Nome = a.GRUPO, GrupoUsuarioID = a.GRUPOUSUARIOID, Cota = a.COTAGRUPO, Saldo = a.SALDOGRUPO, SaldoCompartilhado = a.SALDOCOMPARTILHADO }
						}, (a, b) => new UsuarioModel()
						{
							QuantidadeCarteiras = b.Count(m => m.CARTEIRAID != null),
							AdmPerfil = a.AdmPerfil,
							Ativo = a.Ativo,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = t.Cliente,
							Data = a.Data,
							Email = a.Email,
							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario,
							Carteiras = b.Any(m => m.CARTEIRAID != null) ? new List<CarteiraModel>(b.Select(m => new CarteiraModel() { CarteiraID = m.CARTEIRAID })) : null
						}
						, new CompareObject<UsuarioModel>((a, b) => a.UsuarioID == b.UsuarioID, i => (i.UsuarioID.GetHashCode()))
						).ElementAt(0);

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

		public async Task<IEnumerable<UsuarioModel>> BuscarItensAsync(UsuarioModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT U.NOME, LOGINUSER, U.SALDO, SALDOILIMITADO,EMAIL, TELEFONE, DATA, ATIVO, G.NOME GRUPO, U.USUARIOID, G.GRUPOUSUARIOID, U.COTA, ADMPERFIL, UC.CARTEIRAID, G.COTA COTAGRUPO, G.SALDO SALDOGRUPO, G.SALDOCOMPARTILHADO  FROM USUARIOS U 
							JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
							LEFT JOIN USUARIOS_CARTEIRA UC ON U.USUARIOID=UC.USUARIOID
							WHERE U.CLIENTEID=@ClienteID AND (U.NOME LIKE '%'+@Busca+'%' OR U.EMAIL LIKE '%'+@Busca+'%')", p, commandTimeout: 888);

					if (result != null)
						return result.GroupBy(a => new UsuarioModel()
						{
							AdmPerfil = a.ADMPERFIL,
							Ativo = a.ATIVO,
							Nome = a.NOME,
							LoginUser = a.LOGINUSER,
							Saldo = a.SALDO,
							Cliente = t.Cliente,
							Data = a.DATA,
							Email = a.EMAIL,
							Telefone = a.TELEFONE,
							UsuarioID = a.USUARIOID,
							SaldoCotaIlimitado = a.SALDOILIMITADO,
							Cota = a.COTA,
							QuantidadeCarteiras = a.CARTEIRAID == null ?? 0,
							GrupoUsuario = new GrupoUsuariosModel() { Nome = a.GRUPO, GrupoUsuarioID = a.GRUPOUSUARIOID, Cota = a.COTAGRUPO, Saldo = a.SALDOGRUPO, SaldoCompartilhado = a.SALDOCOMPARTILHADO }
						}, (a, b) => new UsuarioModel()
						{
							QuantidadeCarteiras = b.Count(m => m.CARTEIRAID != null),
							AdmPerfil = a.AdmPerfil,
							Ativo = a.Ativo,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = t.Cliente,
							Data = a.Data,
							Email = a.Email,
							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario,
							Carteiras = b.Any(m => m.CARTEIRAID != null) ? new List<CarteiraModel>(b.Select(m => new CarteiraModel() { CarteiraID = m.CARTEIRAID })) : null
						}
						, new CompareObject<UsuarioModel>((a, b) => a.UsuarioID == b.UsuarioID, i => (i.UsuarioID.GetHashCode())))
						;

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
		public async Task ExcluirItens(UsuarioModel t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync("DELETE FROM USUARIOS WHERE USUARIOID=@UsuarioID AND CLIENTEID=@ClienteID", new { UsuarioID = t.UsuarioID, ClienteID = c }, commandTimeout: 888);

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.EXCLUSAO);
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

		public async Task<int> ExcluirItensAsyncAfected(IEnumerable<UsuarioModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();


				try
				{
					int retorno = await conn.ExecuteAsync("DELETE FROM USUARIOS WHERE USUARIOID=@UsuarioID AND CLIENTEID=@ClienteID", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: 888);


					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
					}
					catch { }

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


		public async Task ExcluirItensUpdateAsync(IEnumerable<UsuarioModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("UPDATE USUARIOS SET ISEXCLUDED=1 WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);

					await conn.ExecuteAsync("DELETE FROM USUARIOS_CARTEIRA WHERE USUARIOID=@UsuarioID", t.Select(a => new { UsuarioID = a.UsuarioID }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);

					await conn.ExecuteAsync(@"UPDATE G SET SALDO=IIF(G.SALDO+U.SALDO>G.COTA, G.COTA,G.SALDO+U.SALDO) FROM GRUPOUSUARIOS G JOIN USUARIOS U ON G.GRUPOUSUARIOID=U.GRUPOUSUARIOID WHERE 
		                                    G.SALDOCOMPARTILHADO=0 AND 
		                                    U.SALDOILIMITADO=0 AND G.CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);




					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.EXCLUSAO);
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

		public async Task ExcluirItensAsync(IEnumerable<UsuarioModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				SqlTransaction tran = conn.BeginTransaction();


				try
				{
					await conn.ExecuteAsync(@"UPDATE G SET SALDO=IIF(G.SALDO+U.SALDO>G.COTA, G.COTA,G.SALDO+U.SALDO) FROM GRUPOUSUARIOS G JOIN USUARIOS U ON G.GRUPOUSUARIOID=U.GRUPOUSUARIOID WHERE 
		                                    G.SALDOCOMPARTILHADO=0 AND 
		                                    U.SALDOILIMITADO=0 AND G.CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);

					await conn.ExecuteAsync(@"If EXISTS (SELECT 1 FROM CAMPANHAS WHERE USUARIOID=@UsuarioID AND CLIENTEID=@ClienteID) RAISERROR('FK_CAMPANHAS_USUARIOS', 16, 1);", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);

					await conn.ExecuteAsync("DELETE FROM USUARIOS WHERE USUARIOID=@UsuarioID AND CLIENTEID=@ClienteID", t.Select(a => new { UsuarioID = a.UsuarioID, ClienteID = c }), commandTimeout: Util.TIMEOUTEXECUTE, transaction: tran);


					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.USUARIO, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
					}
					catch { }

					tran.Commit();

				}
				catch (Exception err)
				{
					tran.Rollback();

					var msg = (err.InnerException ?? err).Message;

					if (msg.Contains("FK_CAMPANHAS_USUARIOS") ||
						msg.Contains("FK_CAMPANHAS_CONSOLIDADOS_USUARIO") ||
						msg.Contains("FK_USERS_RESETPASSWORD_USERS"))
						await ExcluirItensUpdateAsync(t, c, u);
					else
						throw err;

				}
				finally
				{
					tran.Dispose();
					conn.Close();

				}
			}
		}

		public async Task<UsuarioModel> UsuarioByLoginName(UsuarioModel t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("LoginUser", t.LoginUser, DbType.String, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT U.NOME, U.USUARIOID, U.CLIENTEID, U.GRUPOUSUARIOID, U.ADMPERFIL, C.NOME AS CLIENTE FROM USUARIOS U (NOLOCK)
                                                            INNER JOIN CLIENTES C (NOLOCK) ON U.CLIENTEID = C.CLIENTEID
                                                            WHERE LOGINUSER=@LoginUser AND U.ISEXCLUDED=0", p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new UsuarioModel()
						{
							UsuarioID = a.USUARIOID,
							AdmPerfil = a.ADMPERFIL,
							Nome = a.NOME,
							Cliente = new ClienteModel() { ClienteID = a.CLIENTEID, Nome = a.CLIENTE },
							GrupoUsuario = new GrupoUsuariosModel() { GrupoUsuarioID = a.GRUPOUSUARIOID }
						}).ElementAt(0);

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


		public async Task<IEnumerable<CarteiraModel>> BuscaCarteirasByUsuarioID(UsuarioModel t, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("UsuarioID", t.UsuarioID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT C.CARTEIRA, UC.CARTEIRAID, C.VISIVEL FROM USUARIOS_CARTEIRA UC JOIN CARTEIRAS C ON UC.CARTEIRAID=C.CARTEIRAID WHERE UC.USUARIOID=@UsuarioID ORDER BY C.CARTEIRA", p, commandTimeout: 888);

					if (result != null)
						return result.Select(l => new CarteiraModel() { Carteira = l.CARTEIRA, CarteiraID = l.CARTEIRAID, Visivel = l.VISIVEL });



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

		public async Task<IEnumerable<UsuarioModel>> ObterTodosByCarteiraID(UsuarioModel t)
		{


			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", t.CarteiraID.Value, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT U.NOME, LOGINUSER, U.SALDO, SALDOILIMITADO,EMAIL, TELEFONE, U.DATA, ATIVO, G.NOME GRUPO, U.USUARIOID, G.GRUPOUSUARIOID, U.COTA, ADMPERFIL, UC.CARTEIRAID, G.COTA COTAGRUPO, G.SALDO SALDOGRUPO, G.SALDOCOMPARTILHADO, UC.CARTEIRAID, C.CARTEIRA, C.VISIVEL   FROM USUARIOS U 
							JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
							JOIN USUARIOS_CARTEIRA UC ON U.USUARIOID=UC.USUARIOID
							JOIN CARTEIRAS C ON UC.CARTEIRAID=C.CARTEIRAID
							WHERE U.CLIENTEID=@ClienteID AND UC.CARTEIRAID=@CarteiraID AND ISEXCLUDED=0 ORDER BY U.NOME", p, commandTimeout: 888);

					if (result != null)
						return result.GroupBy(a => new UsuarioModel()
						{
							AdmPerfil = a.ADMPERFIL,
							Ativo = a.ATIVO,
							Nome = a.NOME,
							LoginUser = a.LOGINUSER,
							Saldo = a.SALDO,
							Cliente = t.Cliente,
							Data = a.DATA,
							Email = a.EMAIL,
							Telefone = a.TELEFONE,
							UsuarioID = a.USUARIOID,
							SaldoCotaIlimitado = a.SALDOILIMITADO,
							Carteiras = new List<CarteiraModel>() { new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = t.CarteiraID.Value, Visivel = a.VISIVEL } },
							Cota = a.COTA,
							GrupoUsuario = new GrupoUsuariosModel() { Nome = a.GRUPO, GrupoUsuarioID = a.GRUPOUSUARIOID, Cota = a.COTAGRUPO, Saldo = a.SALDOGRUPO, SaldoCompartilhado = a.SALDOCOMPARTILHADO }
						}, (a, b) => new UsuarioModel()
						{
							QuantidadeCarteiras = b.Count(m => m.CARTEIRAID != null),
							AdmPerfil = a.AdmPerfil,
							Ativo = a.Ativo,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = t.Cliente,
							Data = a.Data,
							Email = a.Email,

							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Carteiras = a.Carteiras,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario
						}
						, new CompareObject<UsuarioModel>((a, b) => a.UsuarioID == b.UsuarioID, i => (i.UsuarioID.GetHashCode()))
						);



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

		public async Task<IEnumerable<UsuarioModel>> RegravaTodosUsuarios()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					var result = await conn.QueryAsync(@"SELECT USUARIOID, CLIENTEID, GRUPOUSUARIOID, LOGINUSER FROM USUARIOS", commandTimeout: 888);

					if (result != null)
						return result.Select(a => new UsuarioModel()
						{
							LoginUser = a.LOGINUSER,
							UsuarioID = a.USUARIOID,
							Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
							GrupoUsuario = new GrupoUsuariosModel() { GrupoUsuarioID = a.GRUPOUSUARIOID }
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

		public async Task<IEnumerable<UsuarioModel>> ObterTodosAsync(UsuarioModel t, int? u)
		{


			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT U.NOME, LOGINUSER, U.SALDO, SALDOILIMITADO,EMAIL, TELEFONE, DATA, ATIVO, G.NOME GRUPO, U.USUARIOID, G.GRUPOUSUARIOID, U.COTA, ADMPERFIL, UC.CARTEIRAID, G.COTA COTAGRUPO, G.SALDO SALDOGRUPO, G.SALDOCOMPARTILHADO  FROM USUARIOS U 
							JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
							LEFT JOIN USUARIOS_CARTEIRA UC ON U.USUARIOID=UC.USUARIOID
							WHERE U.CLIENTEID=@ClienteID ORDER BY U.NOME", p, commandTimeout: 888);

					if (result != null)
					{
						return result.GroupBy(a => new UsuarioModel()
						{
							AdmPerfil = a.ADMPERFIL,
							Ativo = a.ATIVO,
							Nome = a.NOME,
							LoginUser = a.LOGINUSER,
							Saldo = a.SALDO,
							Cliente = t.Cliente,
							Data = a.DATA,
							Email = a.EMAIL,
							Telefone = a.TELEFONE,
							UsuarioID = a.USUARIOID,
							SaldoCotaIlimitado = a.SALDOILIMITADO,
							Cota = a.COTA,
							GrupoUsuario = new GrupoUsuariosModel() { Nome = a.GRUPO, GrupoUsuarioID = a.GRUPOUSUARIOID, Cota = a.COTAGRUPO, Saldo = a.SALDOGRUPO, SaldoCompartilhado = a.SALDOCOMPARTILHADO }
						}, (a, b) => new UsuarioModel()
						{
							QuantidadeCarteiras = b.Count(m => m.CARTEIRAID != null),
							AdmPerfil = a.AdmPerfil,
							Ativo = a.Ativo,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = t.Cliente,
							Data = a.Data,
							Email = a.Email,
							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario
						}
						, new CompareObject<UsuarioModel>((a, b) => a.UsuarioID == b.UsuarioID, i => (i.UsuarioID.GetHashCode()))
						);


					}



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


		public async Task<IEnumerable<UsuarioModel>> ObterTodosPaginadoAsync(UsuarioModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var query = @"SELECT U.NOME, LOGINUSER, U.SALDO, SALDOILIMITADO,EMAIL, TELEFONE, DATA, ATIVO, U.USUARIOID, U.GRUPOUSUARIOID, U.COTA, ADMPERFIL, UC.CARTEIRAID, G.SALDO SALDOGRUPO, G.COTA COTAGRUPO, SALDOCOMPARTILHADO, G.NOME GRUPO FROM USUARIOS U 
							JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID
							LEFT JOIN USUARIOS_CARTEIRA UC ON U.USUARIOID=UC.USUARIOID
							WHERE U.CLIENTEID=@ClienteID AND U.ISEXCLUDED=0 ORDER BY U.NOME";

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



					if (!string.IsNullOrEmpty(t.Search))
						query = query.Insert(query.LastIndexOf("U.CLIENTEID=@ClienteID"), @"(U.NOME LIKE '%'+@Search+'%' OR LOGINUSER LIKE '%'+@Search+'%' OR EMAIL LIKE '%'+@Search+'%' OR G.NOME LIKE '%'+@Search+'%') AND ");

					if (t.GrupoUsuario != null)
					{
						if (t.GrupoUsuario.GrupoUsuarioID > 0)
						{
							p.Add("GrupoUsuarioID", t.GrupoUsuario.GrupoUsuarioID, DbType.Int32, ParameterDirection.Input);
							query = query.Insert(query.LastIndexOf("U.CLIENTEID=@ClienteID"), @"G.GRUPOUSUARIOID=@GrupoUsuarioID AND ");
						}
					}




					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
					{
						var dados = result.GroupBy(a => new UsuarioModel()
						{
							AdmPerfil = a.ADMPERFIL,
							Ativo = a.ATIVO,
							Nome = a.NOME,
							LoginUser = a.LOGINUSER,
							Saldo = a.SALDO,
							Cliente = t.Cliente,
							Data = a.DATA,
							Email = a.EMAIL,
							Telefone = a.TELEFONE,
							UsuarioID = a.USUARIOID,
							SaldoCotaIlimitado = a.SALDOILIMITADO,
							Cota = a.COTA,
							GrupoUsuario = new GrupoUsuariosModel()
							{
								Nome = a.GRUPO,
								GrupoUsuarioID = a.GRUPOUSUARIOID,
								Cota = a.COTAGRUPO,
								Saldo = a.SALDOGRUPO,
								SaldoCompartilhado = a.SALDOCOMPARTILHADO
							}

						}, (a, b) => new UsuarioModel()
						{
							QuantidadeCarteiras = b.Count(m => m.CARTEIRAID != null),
							AdmPerfil = a.AdmPerfil,
							Ativo = a.Ativo,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = t.Cliente,
							Data = a.Data,
							Email = a.Email,
							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario,
							Carteiras = b.Any(k => k.CARTEIRAID != null) ? b.Select(m => new CarteiraModel() { CarteiraID = m.CARTEIRAID }).ToList() : new CarteiraModel[] { }.ToList()
						}
						, new CompareObject<UsuarioModel>((a, b) => a.UsuarioID == b.UsuarioID, i => (i.UsuarioID.GetHashCode()))
						);

						return dados.Select(a => new UsuarioModel()
						{
							Ativo = a.Ativo,
							QuantidadeCarteiras = a.QuantidadeCarteiras,
							AdmPerfil = a.AdmPerfil,
							Nome = a.Nome,
							LoginUser = a.LoginUser,
							Saldo = a.Saldo,
							Cliente = a.Cliente,
							Data = a.Data,
							Email = a.Email,
							Telefone = a.Telefone,
							UsuarioID = a.UsuarioID,
							Carteiras = a.Carteiras,
							SaldoCotaIlimitado = a.SaldoCotaIlimitado,
							Cota = a.Cota,
							GrupoUsuario = a.GrupoUsuario,
							Registros = dados.Count(),
							Paginas = dados.Count() / t.Registros
						});
					}



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
        
        public async Task<dynamic> DadosCadastrais(int c)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("UsuarioID", c, DbType.Int32, ParameterDirection.Input);

                    var query = @"SELECT U.NOME, U.EMAIL, IIF(G.SALDOCOMPARTILHADO = 0, G.COTA, U.COTA) AS COTA, IIF(G.SALDOCOMPARTILHADO = 0, G.SALDO, U.SALDO) AS SALDO
                                , U.ADMPERFIL AS ADMINISTRADOR, G.NOME PERFIL, U.SALDOILIMITADO AS ILIMITADO, G.GRUPOUSUARIOID, U.USUARIOID, U.LOGINUSER
                                FROM USUARIOS U (NOLOCK)
                                JOIN GRUPOUSUARIOS G (NOLOCK) ON G.GRUPOUSUARIOID = U.GRUPOUSUARIOID
                                WHERE U.USUARIOID = @UsuarioID";

                    var dados = await conn.QuerySingleAsync<dynamic>(query, p);

                    var retorno = new
                    {
                        usuarioid = dados.USUARIOID,
                        grupousuarioid = dados.GRUPOUSUARIOID,
                        username = dados.LOGINUSER,
                        nome = dados.NOME,
                        email = dados.EMAIL,
                        cota = dados.COTA,
                        saldo = dados.SALDO,
                        administrador = dados.ADMINISTRADOR,
                        perfil = dados.PERFIL,
                        ilimitado = dados.ILIMITADO,
                        imagem = ""
                    };


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
    }
}
