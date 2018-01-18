using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Data.SqlClient;
using Dapper;
using System.Data;
using Helpers;
using DAL;

namespace Gestores
{
	public class DALGestores : IDal<GestorModel>
	{
		const int TIMEOUTEXECUTE = 888;

		IConfigurationRoot Configuration
		{
			get
			{
				return new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
					.Build();
			}
		}

		/// <summary>
		/// Adiciona os gestores junto com as carteiras, e-mails e telefones de contato
		/// </summary>
		/// <param name="gestores"></param>
		public async Task AdicionarItensAsync(IEnumerable<GestorModel> gestores, int c, int? u)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var g = gestores.ElementAt(0);
					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Nome", g.Nome.Trim(), DbType.String, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("GestorID", DbType.Int32, direction: ParameterDirection.Output);

					var carteiras = await conn.QueryAsync<CarteiraModel>("SELECT CARTEIRAID FROM CARTEIRAS WHERE CLIENTEID=@ClienteID AND VISIVEL=1", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					if (!carteiras.Any())
						throw new Exception("Sem carteiras na condição de ativas");


					await conn.QueryAsync<int>("INSERT INTO GESTOR(NOME,CLIENTEID,DATA) VALUES (@Nome,@ClienteID, @Data); SELECT @GestorID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

					g.GestorID = p.Get<int>("GestorID");


					var carteirasGestor = g.Carteiras.Join(carteiras, k => k.CarteiraID.Value, b => b.CarteiraID.Value, (k, b) => b);

					if (g.Carteiras.Any())
						if (!carteirasGestor.Any())
							throw new Exception("Sem carteiras para a associação com o gestor na condição de ativas");

					await conn.ExecuteAsync("INSERT INTO GESTOR_CARTEIRAS (CARTEIRAID,GESTORID) VALUES (@CarteiraID, @GestorID)", carteirasGestor.Select(a => new { CarteiraID = a.CarteiraID, GestorID = g.GestorID }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);
					await conn.ExecuteAsync("INSERT INTO GESTOR_EMAIL(GESTORID,EMAIL) VALUES (@GestorID, @Email)", g.Emails.Where(a => Util.RegexEmail.IsMatch(a)).Select(a => new { GestorID = g.GestorID, Email = a.Trim() }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);
					await conn.ExecuteAsync("INSERT INTO GESTOR_TELEFONES (GESTORID,TELEFONE) VALUES (@GestorID, @Telefone)", g.Telefones.Select(a => new { GestorID = g.GestorID, Telefone = a }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(gestores.ToList(), null, c, u, ModuloAtividadeEnumns.GESTOR, TiposLogAtividadeEnums.GRAVACAO);
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

		/// <summary>
		/// Atualiza um gestor da carteira junto com os novos telefones, e-mails e carteiras
		/// </summary>
		/// <param name="gestores"></param>
		public async Task AtualizaItensAsync(IEnumerable<GestorModel> gestores, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var g = gestores.ElementAt(0);
					var p = new DynamicParameters();

					p.Add("GestorID", g.GestorID, DbType.Int32, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Nome", g.Nome.Trim(), DbType.String, ParameterDirection.Input);


					var carteiraNaoVisivel = await conn.QueryAsync<CarteiraModel>("SELECT CARTEIRAID FROM CARTEIRAS WHERE CLIENTEID=@ClienteID AND VISIVEL=0", p, transaction: tran);


					g.Carteiras = g.Carteiras.Except(carteiraNaoVisivel, new CompareObject<CarteiraModel>((a, b) => a.CarteiraID == b.CarteiraID, i => i.CarteiraID.GetHashCode())).ToList();

					await conn.ExecuteAsync("UPDATE GESTOR SET NOME=@Nome WHERE GESTORID=@GestorID AND CLIENTEID=@ClienteID", p, transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync("DELETE GC FROM GESTOR_CARTEIRAS GC JOIN CARTEIRAS C ON GC.CARTEIRAID=C.CARTEIRAID WHERE GC.GESTORID=@GestorID AND VISIVEL=1", new { GestorID = g.GestorID }, transaction: tran, commandTimeout: TIMEOUTEXECUTE);
					await conn.ExecuteAsync("INSERT INTO GESTOR_CARTEIRAS (CARTEIRAID,GESTORID) VALUES (@CarteiraID, @GestorID)", g.Carteiras.Select(a => new { CarteiraID = a.CarteiraID, GestorID = g.GestorID }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);

					await conn.ExecuteAsync("DELETE FROM GESTOR_EMAIL WHERE GESTORID=@GestorID", new { GestorID = g.GestorID }, transaction: tran, commandTimeout: TIMEOUTEXECUTE);
					await conn.ExecuteAsync("INSERT INTO GESTOR_EMAIL(GESTORID,EMAIL) VALUES (@GestorID, @Email)", g.Emails.Where(a => Util.RegexEmail.IsMatch(a)).Select(a => new { GestorID = g.GestorID, Email = a.Trim() }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);

					await conn.ExecuteAsync("DELETE FROM GESTOR_TELEFONES WHERE GESTORID=@GestorID", new { GestorID = g.GestorID }, transaction: tran, commandTimeout: TIMEOUTEXECUTE);
					await conn.ExecuteAsync("INSERT INTO GESTOR_TELEFONES(GESTORID,TELEFONE) VALUES (@GestorID, @Telefone)", g.Telefones.Select(a => new { GestorID = g.GestorID, Telefone = a }), transaction: tran, commandTimeout: TIMEOUTEXECUTE);

					tran.Commit();

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(gestores, null, c, u, ModuloAtividadeEnumns.GESTOR, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<IEnumerable<GestorModel>> GestorByCarteiras(int c, IEnumerable<int> carteiras)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Carteiras", carteiras.Select(a => a.ToString()).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);

					var query = @"SELECT NOME, EMAIL, CARTEIRAID, GESTORID FROM GESTORESSMS G 
														JOIN string_split(@Carteiras,',') S ON G.CARTEIRAID=CAST(S.VALUE AS INT)
														WHERE CLIENTEID=@ClienteID GROUP BY NOME, EMAIL, CARTEIRA, CARTEIRAID, GESTORID";

					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.GroupBy(a => new { Nome = a.NOME, GestorID = a.GESTORID }, (a, b) => new GestorModel()
						{
							Nome = a.Nome,
							GestorID = a.GestorID,
							CarteiraList = b.Select(k => new CarteiraModel() { CarteiraID = (int)k.CARTEIRAID }),
							Emails = b.GroupBy(k => k.EMAIL, (k, n) => (string)k).ToList()
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
		public async Task<(IEnumerable<GestorModel>, Dictionary<string, string>)> GestoresPadraoEnvio(int c, int? u, Dictionary<string, string> padrao)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Padrao", padrao.Select(a => a.Key).Aggregate((a, b) => $"{a.ToUpper()},{b.ToUpper()}"), DbType.String, ParameterDirection.Input);

					var result = await conn.QueryAsync(@"SELECT G.NOME, G.EMAIL, G.CARTEIRA, G.TELEFONE, PP.CARTEIRAID, PP.TIPOCAMPANHAID, T.TIPOCAMPANHA, PP.PADRAO  FROM PADRAO_POSTAGENS PP 
														JOIN TIPOCAMPANHA T ON PP.TIPOCAMPANHAID=T.CODIGO
														JOIN string_split(@Padrao, ',') S ON PP.PADRAO=S.VALUE
														JOIN GESTORESSMS G ON PP.CARTEIRAID=G.CARTEIRAID AND PP.CLIENTEID=G.CLIENTEID WHERE PP.CLIENTEID=@ClienteID", p, commandTimeout: Util.TIMEOUTEXECUTE);

					if (result != null)
					{
						var dados = result.GroupBy(a => new
						{
							Nome = (string)a.NOME,
							ArquivoPadrao = (string)a.PADRAO
						},
								(a, b) => new GestorModel()
								{
									Nome = a.Nome,
									ArquivoPadrao = a.ArquivoPadrao,
									Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID }).ToList(),
									Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
									TipoCampanha = b.Where(k => k.TIPOCAMPANHA != null).GroupBy(k => k.TIPOCAMPANHA, (k, n) => new TipoCampanhaModel() { TipoCampanha = k.TIPOCAMPANHA, TipoCampanhaID = k.TIPOCAMPANHA }),
									Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
								});

						var _dic = new Dictionary<string, string>();
						foreach (var item in padrao.Join(dados, a => a.Key, b => b.ArquivoPadrao, (a, b) => new { Key = a.Key, Value = a.Value }))
							if (!_dic.ContainsKey(item.Key))
								_dic.Add(item.Key, item.Value);



						return (dados, _dic);


					}



					return (new GestorModel[] { }, null);

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
		public async Task<IEnumerable<GestorModel>> GestoresEmailEnvio(int c, int? carteiraid = null)
		{

			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);

					var query = @"SELECT EMAIL, NOME FROM [dbo].[GESTORESSMS] WHERE CLIENTEID=@ClienteID GROUP BY EMAIL, NOME";

					if (carteiraid.HasValue)
						query = @"SELECT EMAIL, NOME FROM [dbo].[GESTORESSMS] WHERE CLIENTEID=@ClienteID AND CARTEIRAID=@CarteiraID GROUP BY EMAIL, NOME";


					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null || result.Any())
						return result.GroupBy(a => (string)a.NOME, (a, b) => new GestorModel()
						{
							Nome = a,
							Emails = new List<string>(b.Select(k => (string)k.EMAIL))
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
		/// <summary>
		/// Busca gestores de acordo com o nome, email ou carteira
		/// </summary>
		/// <param name="g"></param>
		/// <returns>Retorno um ou mais gestores de acordo com o critério da busca</returns>
		public async Task<IEnumerable<GestorModel>> BuscarItensAsync(GestorModel g, string s, int? u)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", g.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input);

					var result = await conn.QueryAsync("SELECT NOME,CLIENTEID,TELEFONE,EMAIL,CARTEIRAID,GESTORID,CARTEIRA,VISIVEL,LIMITE FROM dbo.GESTORESSMS WHERE (NOME LIKE '%'+@Busca+'%' OR EMAIL LIKE'%'+@Busca+'%') AND CLIENTEID=@ClienteID", p, commandTimeout: 888);

					if (result != null)
						return result.GroupBy(a => new GestorModel() { Nome = (string)a.NOME, GestorID = (int)a.GESTORID, Cliente = new ClienteModel() { ClienteID = (int)a.CLIENTEID } },
						(a, b) => new GestorModel()
						{
							Nome = a.Nome,
							Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID }).ToList(),
							Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
							Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
							GestorID = a.GestorID,
							Cliente = a.Cliente
						},
						new CompareObject<GestorModel>(
						(a, b) => a.GestorID == b.GestorID &&
								a.Nome == b.Nome &&
								a.Cliente.ClienteID == b.Cliente.ClienteID,
						i => (i.GestorID.GetHashCode() ^
								i.Cliente.ClienteID.GetHashCode() ^
								i.Nome.GetHashCode())));


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

		/// <summary>
		/// Exclui itens
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public async Task ExcluirItensAsync(IEnumerable<GestorModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync("DELETE FROM GESTOR WHERE GESTORID=@GestorID AND CLIENTEID=@ClienteID", t.Select(a => new { GestorID = a.GestorID, ClienteID = c }), commandTimeout: 888);

                    try
                    {
                        #pragma warning disable 4014
                        new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.GESTOR, TiposLogAtividadeEnums.EXCLUSAO);
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

		/// <summary>
		/// Lista todos os dados de gestore com base no clienteid
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
		public async Task<IEnumerable<GestorModel>> ObterTodosAsync(GestorModel g, int? u)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", g.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = (await conn.QueryAsync("SELECT NOME,CLIENTEID,TELEFONE,EMAIL,CARTEIRAID,GESTORID,CARTEIRA,VISIVEL,LIMITE FROM dbo.GESTORESSMS WHERE CLIENTEID=@ClienteID ORDER BY NOME", p, commandTimeout: 888))
						.GroupBy(a => new GestorModel() { Nome = (string)a.NOME, GestorID = (int)a.GESTORID, Cliente = new ClienteModel() { ClienteID = (int)a.CLIENTEID } },
						(a, b) => new GestorModel()
						{
							Nome = a.Nome,
							Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID }).OrderBy(k => k.Carteira).ToList(),
							Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
							Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
							GestorID = a.GestorID,
							Cliente = a.Cliente
						},
						new CompareObject<GestorModel>(
						(a, b) => a.GestorID == b.GestorID &&
								a.Nome == b.Nome &&
								a.Cliente.ClienteID == b.Cliente.ClienteID,
						i => (i.GestorID.GetHashCode() ^
								i.Cliente.ClienteID.GetHashCode() ^
								i.Nome.GetHashCode())));


					return result;

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
		/// Busca um item pelo código (identity)
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
		public async Task<GestorModel> BuscarItemByIDAsync(GestorModel g, int? u)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var gestor = new GestorModel();
					var p = new DynamicParameters();
					p.Add("ClienteID", g.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("GestorID", g.GestorID, DbType.Int32, ParameterDirection.Input);

					var result = (await conn.QueryAsync("SELECT NOME, CLIENTEID,TELEFONE,EMAIL, CARTEIRAID, GESTORID, CARTEIRA, VISIVEL, LIMITE FROM GESTORESSMS WHERE GESTORID=@GestorID AND CLIENTEID=@ClienteID", p, commandTimeout: 888));

					if (result != null)
					{
						var r = result.GroupBy(a => new GestorModel() { Nome = (string)a.NOME, GestorID = (int)a.GESTORID, Cliente = new ClienteModel() { ClienteID = (int)a.CLIENTEID } },
						(a, b) => new GestorModel()
						{
							Nome = a.Nome,
							Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID }).ToList(),
							Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
							Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
							Cliente = a.Cliente,
							GestorID = a.GestorID
						}
						,
						new CompareObject<GestorModel>(
						(a, b) => a.GestorID == b.GestorID &&
								a.Nome == b.Nome &&
								a.Cliente.ClienteID == b.Cliente.ClienteID,
						i => (i.GestorID.GetHashCode() ^
								i.Cliente.ClienteID.GetHashCode() ^
								i.Nome.GetHashCode())));

						if (r.Count() > 0)
							gestor = r.ElementAt(0);

						return gestor;
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

		public async Task<IEnumerable<GestorModel>> GestorByCarteira(int carteiraid, int clienteid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);
					p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);


					var result = (await conn.QueryAsync("SELECT NOME,CLIENTEID,TELEFONE,EMAIL,CARTEIRAID,GESTORID,CARTEIRA,VISIVEL,LIMITE FROM dbo.GESTORESSMS WHERE CLIENTEID=@ClienteID AND CARTEIRAID=@CarteiraID AND VISIVEL=1 ORDER BY NOME", p, commandTimeout: 888))
						.GroupBy(a => new GestorModel()
						{
							Nome = (string)a.NOME,
							GestorID = (int)a.GESTORID,
							Cliente = new ClienteModel() { ClienteID = (int)a.CLIENTEID }
						},
						(a, b) => new GestorModel()
						{
							Nome = a.Nome,
							Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID }).ToList(),
							Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
							Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
							GestorID = a.GestorID,
							Cliente = a.Cliente
						},
						new CompareObject<GestorModel>(
						(a, b) => a.GestorID == b.GestorID &&
								a.Nome == b.Nome &&
								a.Cliente.ClienteID == b.Cliente.ClienteID,
						i => (i.GestorID.GetHashCode() ^
								i.Cliente.ClienteID.GetHashCode() ^
								i.Nome.GetHashCode())));

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

		public async Task<IEnumerable<GestorModel>> GestorByCarteiras(IEnumerable<int> carteiras, int clienteid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Carteiras", carteiras.Select(a=>a.ToString()).Aggregate((a,b)=>$"{a},{b}"), DbType.String, ParameterDirection.Input);
					p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);


					var result = await conn.QueryAsync("SELECT NOME,TELEFONE, EMAIL, CARTEIRAID, GESTORID FROM dbo.GESTORESSMS G JOIN string_split(@Carteiras,',') S ON G.CARTEIRAID=CAST(S.VALUE AS INT) WHERE CLIENTEID=@ClienteID AND VISIVEL=1 ORDER BY NOME", p, commandTimeout: 888);

					if (result == null || !result.Any())
						return null;

					return result.Select(a => new GestorModel() { Nome = a.NOME, CarteiraID = a.CARTEIRAID, Email = a.EMAIL, Celular = a.TELEFONE });

				
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


		public Task ExcluirItensUpdateAsync(IEnumerable<GestorModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<GestorModel>> ObterTodosPaginadoAsync(GestorModel t, int? u)
		{
			using (var conn = new SqlConnection(Configuration.GetConnectionString("DefaultConnection")))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Carteiras", t.CarteiraList.Any() ? t.CarteiraList.Select(a => a.CarteiraID.ToString()).Aggregate((a, b) => $"{a},{b}") : null, DbType.String, ParameterDirection.Input);
					p.Add("Search", t.Search, DbType.String, ParameterDirection.Input);

					var query = "SELECT NOME,CLIENTEID,TELEFONE,EMAIL,CARTEIRAID,GESTORID,CARTEIRA,VISIVEL,LIMITE FROM dbo.GESTORESSMS WHERE CLIENTEID=@ClienteID ORDER BY NOME";

					if (t.PaginaAtual.HasValue)
					{
						if (t.PaginaAtual.Value == 0)
							t.PaginaAtual = 1;
					}
					else
						t.PaginaAtual = 1;



					//if (t.CarteiraList.Any())
					//{
					//	query = @"SELECT NOME,CLIENTEID,TELEFONE,EMAIL,CARTEIRAID,GESTORID,CARTEIRA,VISIVEL,LIMITE FROM dbo.GESTORESSMS G 
					//			JOIN string_split(@Carteiras,',') S ON G.CARTEIRAID=CAST(S.VALUE AS INT)
					//			WHERE CLIENTEID=@ClienteID ORDER BY NOME";
					//}



					//if (!string.IsNullOrEmpty(t.Search))
					//	query = query.Insert(query.LastIndexOf("CLIENTEID=@ClienteID"), @"(NOME LIKE '%'+@Search+'%' OR EMAIL LIKE '%'+@Search+'%') AND ");

					var result = (await conn.QueryAsync(query, p, commandTimeout: 888))
						.GroupBy(a => new GestorModel() { Nome = (string)a.NOME, GestorID = (int)a.GESTORID, Cliente = new ClienteModel() { ClienteID = (int)a.CLIENTEID } },
						(a, b) => new GestorModel()
						{
							Nome = a.Nome,
							Carteiras = b.Where(k => k.CARTEIRA != null).GroupBy(k => new { k.CARTEIRA, k.CARTEIRAID, k.VISIVEL }, (m, n) => new CarteiraModel() { Carteira = m.CARTEIRA, CarteiraID = m.CARTEIRAID, Visivel=m.VISIVEL }).OrderBy(k => k.Carteira).ToList(),
							Emails = b.Where(k => k.EMAIL != null).GroupBy(k => k.EMAIL, (m, n) => (string)m).ToList(),
							Telefones = b.Where(k => k.TELEFONE != null).GroupBy(k => k.TELEFONE, (m, n) => (decimal)m).ToList(),
							GestorID = a.GestorID,
							Cliente = a.Cliente
						},
						new CompareObject<GestorModel>(
						(a, b) => a.GestorID == b.GestorID,
						i => (i.GestorID.GetHashCode() ^
								i.Cliente.ClienteID.GetHashCode() ^
								i.Nome.GetHashCode())));


					return result.Select(a => new GestorModel()
					{
						Nome = a.Nome,
						Carteiras = a.Carteiras,
						Emails = a.Emails,
						Telefones = a.Telefones,
						GestorID = a.GestorID,
						Cliente = a.Cliente
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
	}
}
