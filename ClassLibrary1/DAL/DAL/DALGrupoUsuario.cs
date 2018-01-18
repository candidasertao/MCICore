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
	public class DALGrupoUsuario : IDal<GrupoUsuariosModel>
	{

		public async Task AdicionarItensAsync(IEnumerable<GrupoUsuariosModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var grupo = t.ElementAt(0);
					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Cota", grupo.Cota, DbType.String, ParameterDirection.Input);
					p.Add("Saldo", grupo.Cota, DbType.Int32, ParameterDirection.Input);
					p.Add("SaldoCompartilhado", grupo.SaldoCompartilhado, DbType.Int32, ParameterDirection.Input);
					p.Add("Nome", grupo.Nome.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", DbType.Int32, direction: ParameterDirection.Output);

					await conn.ExecuteAsync(@"INSERT INTO [dbo].[GRUPOUSUARIOS]([COTA],[SALDO],[CLIENTEID],[SALDOCOMPARTILHADO],[NOME]) VALUES (@Cota, @Saldo, @ClienteID, @SaldoCompartilhado, @Nome);SELECT @GrupoUsuarioID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

					var grupousuarioid = p.Get<int>("GrupoUsuarioID");

					await conn.ExecuteAsync(@"INSERT INTO GRUPOUSUARIO_PAGINAS (PAGINAID, GRUPOUSUARIOID, PERMISSAO, SUBPAGINAID) VALUES (@PaginaID, @GrupoUsuarioID, @PermissaoID, @SubPaginaID)",
						grupo.GrupoUserPaginas.Select(l => new
						{
							GrupoUsuarioID = grupousuarioid,
							PaginaID = l.Pagina.PaginaID,
							PermissaoID = (byte)l.TipoAcesso,
							SubPaginaID = l.Pagina.SubPagina == null ? new Nullable<int>() : l.Pagina.SubPagina.SubPaginaID == 0 ? new Nullable<int>() : l.Pagina.SubPagina.SubPaginaID
						})
						,
						transaction: tran,
						commandTimeout: Util.TIMEOUTEXECUTE);


					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.GRAVACAO);
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

		public async ValueTask<byte> PermissaoPaginaAsync(int grupousuarioid, int? subpaginaid, int paginaid, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", grupousuarioid, DbType.Int32, ParameterDirection.Input);
					p.Add("PaginaID", paginaid, DbType.Int32, ParameterDirection.Input);
					p.Add("SubpaginaID", subpaginaid, DbType.Int32, ParameterDirection.Input);

					var query = @"SELECT PERMISSAO FROM GRUPOUSUARIO_PAGINAS WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND PAGINAID=@PaginaID";

					if (subpaginaid.HasValue && subpaginaid.Value > 0)
						query += " AND SUBPAGINAID=@SubpaginaID";
					else
						query += " AND SUBPAGINAID IS NULL";

					var r = await conn.QuerySingleAsync<byte>(query, p);

					return r;

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

		public async Task AtualizaSaldoGrupo(GrupoUsuariosModel t, int c, int? u)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var registros = await conn.ExecuteAsync(@"UPDATE G SET SALDO=COTA  FROM GRUPOUSUARIOS G WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID AND SALDOCOMPARTILHADO=1", new { ClienteID = c, GrupoUsuarioID = t.GrupoUsuarioID }, commandTimeout: Util.TIMEOUTEXECUTE);

					if (registros == 0)
						await conn.ExecuteAsync(@"UPDATE U SET SALDO=U.COTA FROM USUARIOS U JOIN GRUPOUSUARIOS G ON U.GRUPOUSUARIOID=G.GRUPOUSUARIOID WHERE U.GRUPOUSUARIOID=@GrupoUsuarioID AND G.CLIENTEID=@ClienteID AND SALDOCOMPARTILHADO=0 AND U.ISEXCLUDED=0", new { ClienteID = c, GrupoUsuarioID = t.GrupoUsuarioID }, commandTimeout: Util.TIMEOUTEXECUTE);


					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task AtualizaPermissaoPaginaAsync(IEnumerable<GrupoUsuarioPaginas> t, int c)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();
				try
				{
					await conn.ExecuteAsync(@"UPDATE GP SET PERMISSAO=@Permissao FROM GRUPOUSUARIO_PAGINAS GP JOIN GRUPOUSUARIOS GU ON GP.GRUPOUSUARIOID=GU.GRUPOUSUARIOID  WHERE GRUPOUSUARIOPAGINAID=@GrupoUsuarioPaginaID AND PAGINAID=@PaginaID AND CLIENTEID=@ClienteID", t.Select(a => new
					{
						ClienteID = c,
						Permissao = (byte)a.TipoAcesso,
						GrupoUsuarioPaginaID = a.GrupoUsuarioPaginaID,
						PaginaID = a.Pagina.PaginaID
					}), transaction: tran,
					commandTimeout: Util.TIMEOUTEXECUTE);

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, null, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task AtualizaItensAsync(IEnumerable<GrupoUsuariosModel> t, int c, int? u)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{

					foreach (var item in t)
					{


						//capturo o saldo, cota e saldocompartilhado do usuáriopra efeito de comparação
						var grupoUsuario = await conn.QuerySingleOrDefaultAsync<GrupoUsuariosModel>("SELECT SALDOCOMPARTILHADO, COTA, SALDO FROM GRUPOUSUARIOS WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID",
							new
							{
								ClienteID = c,
								GrupoUsuarioID = item.GrupoUsuarioID
							},
							transaction: tran,
							commandTimeout: Util.TIMEOUTEXECUTE);

						//caso o saldocompartilhado novo tenha sido diferente do atual
						if (grupoUsuario.SaldoCompartilhado != item.SaldoCompartilhado)
						{
							await conn.ExecuteAsync("UPDATE USUARIOS SET COTA=0, SALDO=0 WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID AND ISEXCLUDED=0", new
							{
								GrupoUsuarioID = item.GrupoUsuarioID,
								ClienteID = c
							},
								transaction: tran,
								commandTimeout: Util.TIMEOUTEXECUTE);
						}

						var saldoUsuarios = await conn.QuerySingleAsync<int?>("SELECT SUM(COTA) FROM USUARIOS U WHERE U.GRUPOUSUARIOID=@GrupoUsuarioID", new { GrupoUsuarioID = item.GrupoUsuarioID }, transaction: tran);


						var saldo = 0;
						if (!item.SaldoCompartilhado)
						{
							saldo = saldoUsuarios ?? 0;

							if (item.Cota > saldo || item.Cota== saldo)
								item.Saldo = item.Cota - saldo;
							else 
								throw new Exception("Valor da cota é menor do que o saldo de usuários");
						}
						else
							item.Saldo = item.Cota;
						

					
					


						await conn.ExecuteAsync("UPDATE GRUPOUSUARIOS SET NOME=@Nome, COTA=@Cota, SALDO=@Saldo, SALDOCOMPARTILHADO=@SaldoCompartilhado WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID",
							new
							{
								ClienteID = c,
								Nome = item.Nome.Trim(),
								Cota = item.Cota,
								Saldo = item.Saldo,
								SaldoCompartilhado = item.SaldoCompartilhado,
								GrupoUsuarioID = item.GrupoUsuarioID
							}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

						await conn.ExecuteAsync("UPDATE GRUPOUSUARIO_PAGINAS SET PERMISSAO=@Permissao WHERE GRUPOUSUARIOPAGINAID=@GrupoUsuarioPaginaID", item.GrupoUserPaginas.Select(a => new
						{
							Permissao = (byte)a.TipoAcesso,
							GrupoUsuarioPaginaID = a.GrupoUsuarioPaginaID
						}), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
					}

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<GrupoUsuariosModel> BuscarItemByIDAsync(GrupoUsuariosModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", t.GrupoUsuarioID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT [COTA],[SALDO],[CLIENTEID],[SALDOCOMPARTILHADO],[NOME],GRUPO, PAGINA, SUBPAGINA, GP.[GRUPOUSUARIOPAGINAID], P.PAGINAID, S.SUBPAGINAID, PERMISSAO
																FROM GRUPOUSUARIOS G 
																JOIN [GRUPOUSUARIO_PAGINAS] GP ON G.GRUPOUSUARIOID=GP.GRUPOUSUARIOID
																JOIN PAGINAS P ON GP.PAGINAID=P.PAGINAID
																JOIN GRUPOPAGINAS GA ON P.GRUPOID=GA.GRUPOID
																LEFT JOIN SUBPAGINAS S ON GP.SUBPAGINAID=S.SUBPAGINAID
																WHERE G.GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID", p);

					if (result != null)
						return result.GroupBy(a => new { Cota = a.COTA, GrupoUsuarioID = t.GrupoUsuarioID, Nome = a.NOME, Saldo = a.SALDO, Cliente = t.Cliente, SaldoCompartilhado = a.SALDOCOMPARTILHADO },
							   (a, b) => new GrupoUsuariosModel()
							   {
								   Cota = a.Cota,
								   GrupoUsuarioID = t.GrupoUsuarioID,
								   Nome = a.Nome,
								   Saldo = a.Saldo,
								   Cliente = t.Cliente,
								   SaldoCompartilhado = a.SaldoCompartilhado,
								   GrupoUserPaginas = b.Select(k => new GrupoUsuarioPaginas()
								   {
									   Pagina = new PaginaModel() { Pagina = MontaNomePagina(k.PAGINA, k.SUBPAGINA, k.GRUPO), PaginaID = k.PAGINAID },
									   GrupoUsuarioPaginaID = (int)k.GRUPOUSUARIOPAGINAID,
									   TipoAcesso = ((TipoAcessoSistemaEnums)Enum.Parse(typeof(TipoAcessoSistemaEnums), ((byte)k.PERMISSAO).ToString()))
								   })
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

		public async Task<IEnumerable<GrupoUsuariosModel>> BuscarItensAsync(GrupoUsuariosModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);

					var result = await conn.QueryAsync(@"SELECT [COTA],[SALDO],[CLIENTEID],[SALDOCOMPARTILHADO],[NOME], GRUPOUSUARIOID FROM GRUPOUSUARIOS G WHERE NOME LIKE '%'+@Busca+'%' AND CLIENTEID=@ClienteID", p);

					if (result.Any())
						return result.Select(a => new GrupoUsuariosModel()
						{
							Nome = a.NOME,
							Cliente = t.Cliente,
							Cota = a.COTA,
							Saldo = a.SALDO,
							SaldoCompartilhado = a.SALDOCOMPARTILHADO,
							GrupoUsuarioID = a.GRUPOUSUARIOID
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

		public async Task<IEnumerable<GrupoUsuariosModel>> ListaGrupos(int c)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync<GrupoUsuariosModel>("SELECT NOME, GRUPOUSUARIOID FROM GRUPOUSUARIOS WHERE CLIENTEID=@ClienteID AND ISEXCLUDED=0 ORDER BY NOME", p);

					if (result.Any())
						return result;

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

		public async Task<IEnumerable<GrupoUsuariosModel>> ObterTodosAsync(GrupoUsuariosModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);


					var usuarios = (await conn.QueryAsync("SELECT SALDOILIMITADO, GRUPOUSUARIOID, COTA FROM USUARIOS WHERE CLIENTEID=@ClienteID", new { ClienteID = t.Cliente.ClienteID })).ToList();


					var query = @"SELECT G.NOME, GUP.PERMISSAO, P.PAGINA, P.PAGINAID, GR.GRUPO, GR.GRUPOID, G.GRUPOUSUARIOID, G.SALDO, G.SALDOCOMPARTILHADO, G.COTA, S.SUBPAGINA,S.SUBPAGINAID, GRUPOUSUARIOPAGINAID, COUNT(U.USUARIOID) QUANTIDADEUSUARIO, SUM(ISNULL(U.COTA, 0)) COTASUSUARIO, SUM(ISNULL(U.SALDO,0)) SALDOUSUARIOS FROM GRUPOUSUARIOS G
									LEFT JOIN USUARIOS U ON G.GRUPOUSUARIOID=U.GRUPOUSUARIOID AND U.ISEXCLUDED=0
									JOIN GRUPOUSUARIO_PAGINAS GUP ON G.GRUPOUSUARIOID=GUP.GRUPOUSUARIOID
									JOIN PAGINAS P ON GUP.PAGINAID=P.PAGINAID
									JOIN GRUPOPAGINAS GR ON P.GRUPOID=GR.GRUPOID
									LEFT JOIN SUBPAGINAS S ON GUP.SUBPAGINAID=S.SUBPAGINAID
									WHERE G.CLIENTEID=@ClienteID AND G.ISEXCLUDED=0
									GROUP BY G.NOME, GUP.PERMISSAO, P.PAGINA, P.PAGINAID, GR.GRUPO, GR.GRUPOID, G.GRUPOUSUARIOID, G.SALDO, G.SALDOCOMPARTILHADO, G.COTA, S.SUBPAGINA, S.SUBPAGINAID, GRUPOUSUARIOPAGINAID ORDER BY G.NOME";

					var result = await conn.QueryAsync(query, p);

					if (result.Any())
						return result
							.GroupBy(a => new
							{
								Nome = a.NOME,
								Saldo = a.SALDO,
								SaldoCompartilhado = a.SALDOCOMPARTILHADO,
								GrupoUsuarioID = a.GRUPOUSUARIOID,
								Cota = a.COTA,
								QuantidadeUsuario = a.QUANTIDADEUSUARIO
							}, (a, b) => new GrupoUsuariosModel()
							{
								Nome = a.Nome,
								Cota = a.Cota,
								Saldo = a.Saldo,
								SaldoCompartilhado = a.SaldoCompartilhado,
								GrupoUsuarioID = a.GrupoUsuarioID,
								GrupoUserPaginas = b.GroupBy(k => new { k.GRUPO, k.PAGINA, k.PAGINAID, k.PERMISSAO, k.GRUPOUSUARIOPAGINAID, k.SUBPAGINA }, (l, m) => new GrupoUsuarioPaginas()
								{
									Pagina = new PaginaModel() { Pagina = MontaNomePagina(l.PAGINA, l.SUBPAGINA, l.GRUPO), PaginaID = l.PAGINAID },
									GrupoUsuarioPaginaID = (int)l.GRUPOUSUARIOPAGINAID,
									TipoAcesso = ((TipoAcessoSistemaEnums)Enum.Parse(typeof(TipoAcessoSistemaEnums), ((byte)l.PERMISSAO).ToString()))
								}),
								QuantidadeUsuarios = a.QuantidadeUsuario
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
		public async Task<IEnumerable<PaginaModel>> RetornaPaginas(int clienteid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{


					var result = await conn.QueryAsync(@"SELECT P.PAGINAID, SUBPAGINAID, G.GRUPOID, G.GRUPO, P.PAGINA, S.SUBPAGINA FROM PAGINAS P 
										JOIN GRUPOPAGINAS G ON P.GRUPOID=G.GRUPOID
										LEFT JOIN SUBPAGINAS S ON P.PAGINAID=S.PAGINAID");

					return result.Select(l => new PaginaModel()
					{
						PaginaID = l.PAGINAID,
						Pagina = MontaNomePagina(l.PAGINA, l.SUBPAGINA, l.GRUPO),
						GrupoPagina = new GrupoPaginasModel() { Grupo = l.GRUPO, GrupoID = l.GRUPOID },
						SubPagina = l.SUBPAGINA != null ? new SubPaginaModel() { SubPagina = l.SUBPAGINA, SubPaginaID = l.SUBPAGINAID } : null
					});
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
		public async Task<GrupoUsuariosModel> PaginasPermissao(GrupoUsuariosModel g, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("GrupoUsuarioID", g.GrupoUsuarioID, DbType.Int32, ParameterDirection.Input);

					var query = @"SELECT GU.NOME, GRUPO, PAGINA, SUBPAGINA, GP.PERMISSAO, GP.GRUPOUSUARIOID, GP.GRUPOUSUARIOPAGINAID, GP.PAGINAID, GP.SUBPAGINAID  FROM [GRUPOUSUARIO_PAGINAS] GP 
										JOIN PAGINAS P ON GP.PAGINAID=P.PAGINAID 
										JOIN GRUPOPAGINAS G ON P.GRUPOID=G.GRUPOID
										JOIN GRUPOUSUARIOS GU ON GP.GRUPOUSUARIOID=GU.GRUPOUSUARIOID
										LEFT JOIN SUBPAGINAS S ON GP.SUBPAGINAID=S.SUBPAGINAID
										WHERE GP.GRUPOUSUARIOID=@GrupoUsuarioID AND GU.CLIENTEID=@ClienteID
										GROUP BY GRUPO, PAGINA, SUBPAGINA,GP.PERMISSAO, GP.GRUPOUSUARIOID, GU.NOME, GP.GRUPOUSUARIOPAGINAID, GP.PAGINAID, GP.SUBPAGINAID";

					var result = await conn.QueryAsync(query, p);

					if (result.Any())
					{
						var dados = result
							.GroupBy(a => new
							{
								Nome = a.NOME,
								GrupoUsuarioID = a.GRUPOUSUARIOID,
							}, (a, b) => new GrupoUsuariosModel()
							{
								Nome = a.Nome,
								GrupoUsuarioID = a.GrupoUsuarioID,
								GrupoUserPaginas = b.GroupBy(k => new { k.SUBPAGINA, k.PAGINA, k.PERMISSAO, k.GRUPO, k.GRUPOUSUARIOPAGINAID, k.PAGINAID, k.SUBPAGINAID }, (l, m) => new GrupoUsuarioPaginas()
								{
									GrupoUsuarioPaginaID = l.GRUPOUSUARIOPAGINAID,
									Pagina = new PaginaModel() { Pagina = MontaNomePagina(l.PAGINA, l.SUBPAGINA, l.GRUPO), PaginaID = l.PAGINAID, SubPagina = new SubPaginaModel() { SubPaginaID = l.SUBPAGINAID } },
									TipoAcesso = ((TipoAcessoSistemaEnums)Enum.Parse(typeof(TipoAcessoSistemaEnums), ((byte)l.PERMISSAO).ToString()))
								})
							}).ElementAt(0);

						return dados;

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
		string MontaNomePagina(string pagina, string subpagina, string grupo)
		{
			if (!string.IsNullOrEmpty(subpagina))
				subpagina = $":{subpagina}";

			return $"{grupo}/{pagina}{subpagina}";
		}
		public async Task ExcluirItensAsync(IEnumerable<GrupoUsuariosModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync("DELETE FROM GRUPOUSUARIOS WHERE GRUPOUSUARIOID=@GrupoUsuarioID AND CLIENTEID=@ClienteID", t.Select(a => new { ClienteID = c, GrupoUsuarioID = a.GrupoUsuarioID }), commandTimeout: Util.TIMEOUTEXECUTE);


					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
					}
					catch { }
				}
				catch (Exception err)
				{
					if (err.Message.Contains("FK_USUARIOS_GRUPOUSUARIOS"))
						await ExcluirItensUpdateAsync(t, c, u);
					else
						throw err;

				}
				finally
				{
					conn.Close();

				}
			}
		}




		public async Task ExcluirItensUpdateAsync(IEnumerable<GrupoUsuariosModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					var query = "UPDATE GRUPOUSUARIOS SET ISEXCLUDED=1 WHERE CLIENTEID=@ClienteID AND GRUPOUSUARIOID=@GrupoUsuarioID";
					await conn.ExecuteAsync(query, t.Select(a => new { ClienteID = c, GrupoUsuarioID = a.GrupoUsuarioID }), commandTimeout: 888);

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.PERFIL, TiposLogAtividadeEnums.EXCLUSAO);
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

		public Task<IEnumerable<GrupoUsuariosModel>> ObterTodosPaginadoAsync(GrupoUsuariosModel t, int? u)
		{
			throw new NotImplementedException();
		}


	}
}
