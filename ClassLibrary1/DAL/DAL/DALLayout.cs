using Dapper;
using DAL;
using DAL;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConecttaManagerData.DAL
{
	public class DALLeiaute : IDal<LeiauteModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<LeiauteModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var p = new DynamicParameters();

					int _leiauteID = 0;

					foreach (var item in t)
					{
						p = new DynamicParameters();
						p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
						p.Add("Nome", item.Nome, DbType.String, ParameterDirection.Input);
						p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
						p.Add("Visivel", item.Visivel, DbType.Boolean, ParameterDirection.Input);
						p.Add("Padrao", item.Padrao, DbType.Boolean, ParameterDirection.Input);
						p.Add("LeiauteID", DbType.Int32, direction: ParameterDirection.Output);


						await conn.ExecuteAsync(@"INSERT INTO LAYOUT (CLIENTEID, USUARIOID, DATA, VISIVEL, PADRAO, NOME) VALUES (@ClienteID, @UsuarioID, @Data, @Visivel, @Padrao, @Nome); SELECT @LeiauteID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

						_leiauteID = p.Get<int>("LeiauteID");

						await conn.ExecuteAsync("INSERT INTO LAYOUT_VARIAVEIS (LEIAUTEID, VARIAVEL, IDCOLUNA) VALUES (@LeiauteID, @Variavel, @IDColuna)", item.LeiauteVariaveis
							.Select(a => new
							{
								LeiauteID = _leiauteID,
								Variavel = a.Variavel.Trim().ToLower(),
								IDColuna = a.IDColuna
							}), transaction: tran,
						commandTimeout: Util.TIMEOUTEXECUTE);
					}

					var leiauteID = await conn.QuerySingleOrDefaultAsync<int>("SELECT COUNT(LEIAUTEID) FROM LAYOUT WHERE CLIENTEID=@ClienteID AND PADRAO=1", new { ClienteID = c }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					if (leiauteID == 0) //se não tiver nenhum registro como padrão, é definido o atual como PADRAO=1
						await conn.ExecuteAsync("UPDATE LAYOUT SET PADRAO=1 WHERE CLIENTEID=@ClienteID AND LEIAUTEID=@LeiauteID",
								new { ClienteID = c, LeiauteID = _leiauteID },
								transaction: tran,
								commandTimeout: Util.TIMEOUTEXECUTE);


					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.LEIAUTE, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task<IEnumerable<LeiauteModel>> CapturaLayoutEnvio(int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT LV.VARIAVEL, IDCOLUNA, L.NOME, L.LEIAUTEID, L.PADRAO FROM LAYOUT L JOIN LAYOUT_VARIAVEIS LV ON L.LEIAUTEID=LV.LEIAUTEID WHERE CLIENTEID=@ClienteID AND PADRAO=1";

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);


					var result = await conn.QueryAsync(query, p);

					if (result != null)
						return result.GroupBy(a => new LeiauteModel() { LeiauteID = (int)a.LEIAUTEID, Nome = a.NOME, Padrao = a.PADRAO },
							(a, b) => new LeiauteModel()
							{
								LeiauteID = a.LeiauteID,
								Padrao = a.Padrao,
								Nome = a.Nome,
								LeiauteVariaveis = b.Select(k => new LeiauteViariaveisModel()
								{
									IDColuna = k.IDCOLUNA,
									Variavel = k.VARIAVEL
								})

							}, new CompareObject<LeiauteModel>(
							(a, b) => a.LeiauteID == b.LeiauteID,
							i => i.LeiauteID.GetHashCode()));


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
		public async Task AtualizaItensAsync(IEnumerable<LeiauteModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var p = new DynamicParameters();
					foreach (var item in t)
					{
						p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
						p.Add("Nome", item.Nome, DbType.String, ParameterDirection.Input);
						p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
						p.Add("Visivel", item.Visivel, DbType.Boolean, ParameterDirection.Input);
						p.Add("Padrao", item.Padrao, DbType.Boolean, ParameterDirection.Input);
						p.Add("LeiauteID", item.LeiauteID, DbType.Int32, direction: ParameterDirection.Input);

						await conn.ExecuteAsync(@"UPDATE LAYOUT SET VISIVEL=@Visivel, PADRAO=@Padrao, NOME=@Nome WHERE CLIENTEID=@ClienteID AND LEIAUTEID=@LeiauteID", p, transaction: tran, commandTimeout: 888);

						await conn.ExecuteAsync("DELETE FROM LAYOUT_VARIAVEIS WHERE LEIAUTEID=@LeiauteID",
							new { LeiauteID = item.LeiauteID },
							transaction: tran,
							commandTimeout: Util.TIMEOUTEXECUTE);

						await conn.ExecuteAsync("INSERT INTO LAYOUT_VARIAVEIS (LEIAUTEID, VARIAVEL, IDCOLUNA) VALUES (@LeiauteID, @Variavel, @IDColuna)", item.LeiauteVariaveis
							.Where(a => !string.IsNullOrEmpty(a.Variavel.Trim()))
							.Select(a => new
							{
								LeiauteID = item.LeiauteID,
								Variavel = a.Variavel,
								IDColuna = a.IDColuna
							}), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);


						var leiauteID = await conn.QuerySingleOrDefaultAsync<int>("SELECT COUNT(LEIAUTEID)  FROM LAYOUT WHERE CLIENTEID=@ClienteID AND PADRAO=1", new { ClienteID = c }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

						if (leiauteID == 0) //se não tiver nenhum registro como padrão, é definido o atual como PADRAO=1
							await conn.ExecuteAsync("UPDATE LAYOUT SET PADRAO=1 WHERE CLIENTEID=@ClienteID AND LEIAUTEID=@LeiauteID",
									new { ClienteID = c, LeiauteID = item.LeiauteID },
									transaction: tran,
									commandTimeout: Util.TIMEOUTEXECUTE);
					}

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.LEIAUTE, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task DefiniPadraoAsync(LeiauteModel l, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("LeiauteID", l.LeiauteID, DbType.Int32, ParameterDirection.Input);

					await conn.ExecuteAsync(@"UPDATE LAYOUT SET PADRAO=0 WHERE CLIENTEID=@ClienteID", p, transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"UPDATE LAYOUT SET PADRAO=1 WHERE CLIENTEID=@ClienteID AND LEIAUTEID=@LeiauteID", p, transaction: tran, commandTimeout: 888);

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(l, null, c, u, ModuloAtividadeEnumns.LEIAUTE, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<LeiauteModel> BuscarItemByIDAsync(LeiauteModel t, int? u)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("LeiauteID", t.LeiauteID, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync("SELECT L.DATA, L.LEIAUTEID, L.NOME, L.PADRAO, L.VISIVEL, L.ISESPECIAL, LV.IDCOLUNA, LV.VARIAVEL, LV.CODIGO, L.ISESPECIAL FROM LAYOUT L JOIN LAYOUT_VARIAVEIS LV ON L.LEIAUTEID=LV.LEIAUTEID WHERE CLIENTEID=@ClienteID AND L.LEIAUTEID=@LeiauteID", p);

					if (result != null)
					{
						var dados = result.GroupBy(a => new LeiauteModel() { LeiauteID = (int)a.LEIAUTEID, Nome = a.NOME, Data = a.DATA, Padrao = a.PADRAO, Visivel = a.VISIVEL, IsEspecial = a.ISESPECIAL ?? false },
							(a, b) => new LeiauteModel()
							{
								LeiauteID = a.LeiauteID,
								Visivel = a.Visivel,
								Padrao = a.Padrao,
								Nome = a.Nome,
								Data = a.Data,
								IsEspecial = a.IsEspecial,
								LeiauteVariaveis = b.Select(k => new LeiauteViariaveisModel()
								{
									IDColuna = k.IDCOLUNA,
									Variavel = regIgnorar.IsMatch(k.VARIAVEL) ? regIgnorar.Replace(k.VARIAVEL, string.Empty) : k.VARIAVEL
								})
							}, new CompareObject<LeiauteModel>(
							(a, b) => a.LeiauteID == b.LeiauteID,
							i => i.LeiauteID.GetHashCode()));

						return dados.Select(a => new LeiauteModel()
						{
							LeiauteID = a.LeiauteID,
							Nome = a.Nome,
							Data = a.Data,
							Visivel = a.Visivel,
							Padrao = a.Padrao,
							LeiauteVariaveis = a.LeiauteVariaveis,
							IsEspecial = a.IsEspecial
						}).ElementAt(0);

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

		public Task<IEnumerable<LeiauteModel>> BuscarItensAsync(LeiauteModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task ExcluirItensAsync(IEnumerable<LeiauteModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("DELETE FROM LAYOUT WHERE LEIAUTEID=@LeiauteID AND CLIENTEID=@ClienteID", t.Select(a => new
					{
						ClienteID = c,
						LeiauteID = a.LeiauteID
					}), transaction: tran, commandTimeout: 888);

					var leiauteID = await conn.QuerySingleOrDefaultAsync<int>("SELECT COUNT(LEIAUTEID) FROM LAYOUT WHERE CLIENTEID=@ClienteID AND PADRAO=1", new { ClienteID = c }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					if (leiauteID == 0) //se não tiver nenhum registro como padrão, é definido algum como 1
						await conn.ExecuteAsync("UPDATE TOP (1) LAYOUT SET PADRAO=1 WHERE CLIENTEID=@ClienteID",
								new
								{
									ClienteID = c
								},
								transaction: tran,
								commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync("DELETE FROM PADRAO_POSTAGENS WHERE LEIAUTEID=@LeiauteID AND CLIENTEID=@ClienteID", t.Select(a => new
					{
						ClienteID = c,
						LeiauteID = a.LeiauteID
					}), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);


					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.LEIAUTE, TiposLogAtividadeEnums.EXCLUSAO);
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

		public Task ExcluirItensUpdateAsync(IEnumerable<LeiauteModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}
		public async Task<IEnumerable<LeiauteModel>> ListaLayouts(int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync<LeiauteModel>("SELECT NOME,LEIAUTEID, PADRAO, ISESPECIAL FROM LAYOUT L WHERE CLIENTEID=@ClienteID OR CLIENTEID IS NULL ORDER BY NOME", p);

					if (result != null)
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
		public async Task<IEnumerable<LeiauteModel>> ObterTodosAsync(LeiauteModel t, int? u)
		{
			var p = new DynamicParameters();
			p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
			p.Add("Search", t.Search, DbType.String, ParameterDirection.Input);


			var result = await DALGeneric.GenericReturnAsync<dynamic>("SELECT L.DATA, L.LEIAUTEID, L.NOME, L.PADRAO, L.VISIVEL, LV.IDCOLUNA, LV.VARIAVEL, LV.CODIGO, LV.INICIO, LV.TAMANHO, L.ISESPECIAL FROM LAYOUT L JOIN LAYOUT_VARIAVEIS LV ON L.LEIAUTEID=LV.LEIAUTEID WHERE CLIENTEID=@ClienteID OR CLIENTEID IS NULL ORDER BY NOME", d: p);

			if (result != null)
			{
				var dados = result.GroupBy(a => new LeiauteModel() { LeiauteID = (int)a.LEIAUTEID, Nome = a.NOME, Data = a.DATA, Padrao = a.PADRAO, Visivel = a.VISIVEL, IsEspecial = a.ISESPECIAL ?? false },
					(a, b) => new LeiauteModel()
					{
						LeiauteID = a.LeiauteID,
						Visivel = a.Visivel,
						Padrao = a.Padrao,
						Nome = a.Nome,
						Data = a.Data,
						IsEspecial = a.IsEspecial,
						LeiauteVariaveis = b.Select(k => new LeiauteViariaveisModel()
						{
							InicioLeitura = k.INICIO,
							QuantidadeCaracteres = k.TAMANHO,
							IDColuna = k.IDCOLUNA,
							Variavel = regIgnorar.IsMatch(k.VARIAVEL) ? regIgnorar.Replace(k.VARIAVEL, string.Empty) : k.VARIAVEL
						})
					}, new CompareObject<LeiauteModel>(
					(a, b) => a.LeiauteID == b.LeiauteID,
					i => i.LeiauteID.GetHashCode()));

				return dados.Select(a => new LeiauteModel()
				{
					LeiauteID = a.LeiauteID,
					Nome = a.Nome,
					Data = a.Data,
					Visivel = a.Visivel,
					Padrao = a.Padrao,
					LeiauteVariaveis = a.LeiauteVariaveis,
					IsEspecial = a.IsEspecial
				});

			}
			return null;
		}

		Regex regIgnorar = new Regex("#ignorar[0-9]+$", RegexOptions.Compiled);


		public async Task<IEnumerable<LeiauteModel>> ObterTodosPaginadoAsync(LeiauteModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					string query = string.Format(@"SELECT L.DATA, L.LEIAUTEID, L.NOME, L.PADRAO, L.VISIVEL, LV.IDCOLUNA, LV.VARIAVEL, LV.CODIGO, LV.TAMANHO, LV.INICIO, L.ISESPECIAL
                                    FROM LAYOUT L 
                                    JOIN LAYOUT_VARIAVEIS LV ON L.LEIAUTEID=LV.LEIAUTEID WHERE (CLIENTEID=@ClienteID OR CLIENTEID IS NULL) {0} ORDER BY NOME",
									!string.IsNullOrEmpty(t.Search) ? "AND (UPPER(CAST(L.NOME AS VARCHAR(MAX)) COLLATE SQL_LATIN1_GENERAL_CP1251_CS_AS) LIKE UPPER('%'+@Search+'%') OR UPPER(CAST(LV.VARIAVEL AS VARCHAR(MAX)) COLLATE SQL_LATIN1_GENERAL_CP1251_CS_AS) LIKE UPPER('%'+@Search+'%'))" : string.Empty);

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", t.Search.NoAcento(), DbType.String, ParameterDirection.Input);

					if (t.PaginaAtual.HasValue)
					{
						if (t.PaginaAtual.Value == 0)
							t.PaginaAtual = 1;
					}
					else
						t.PaginaAtual = 1;

					var result = await conn.QueryAsync(query, p);

					if (result != null)
					{
						var dados = result.GroupBy(a => new LeiauteModel() { LeiauteID = (int)a.LEIAUTEID, Nome = a.NOME, Data = a.DATA, Padrao = a.PADRAO, Visivel = a.VISIVEL, IsEspecial = a.ISESPECIAL??false },
							(a, b) => new LeiauteModel()
							{
								LeiauteID = a.LeiauteID,
								Visivel = a.Visivel,
								Padrao = a.Padrao,
								Nome = a.Nome,
								Data = a.Data,
								IsEspecial = a.IsEspecial,
								LeiauteVariaveis = b.Select(k => new LeiauteViariaveisModel()
								{
									IDColuna = k.IDCOLUNA,
									InicioLeitura = k.INICIO,
									QuantidadeCaracteres = k.TAMANHO,
									Variavel = regIgnorar.IsMatch(k.VARIAVEL) ? regIgnorar.Replace(k.VARIAVEL, " ") : k.VARIAVEL
								}).OrderBy(k => k.IDColuna)
							}, new CompareObject<LeiauteModel>(
							(a, b) => a.LeiauteID == b.LeiauteID,
							i => i.LeiauteID.GetHashCode()));

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
	}
}
