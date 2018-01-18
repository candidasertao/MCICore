using Amazon.S3.Transfer;
using Dapper;
using FastMember;
using Gestores;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.MonitoriaModel;

namespace DAL
{
	public class DALCampanha : IDal<CampanhaModel>
	{


		/// <summary>
		/// Metódo que retorna um detalhado com os campos necessários pra qualquer
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public async Task<IEnumerable<CampanhaModel>> DetalhadoGenerico(CampanhaModel c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", c.DataInicial.Value, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", c.DataFinal.Value, DbType.DateTime, ParameterDirection.Input);
					p.Add("StatusEnvio", (byte)c.StatusEnvio, DbType.Byte, ParameterDirection.Input);
					p.Add("UsuarioID", c.Usuario != null ? (object)c.Usuario.UsuarioID : null, DbType.Int32, ParameterDirection.Input);
					p.Add("Carteiras", c.CarteiraList.Any() ? c.CarteiraList.Select(a => a.CarteiraID.ToString()).Aggregate((a, b) => $"{a},{b}") : null, DbType.String, ParameterDirection.Input);


					string query = string.Format(@"IF(@Carteiras IS NOT NULL)
										WITH ENVIADAS AS
										(
										SELECT TEXTO, CELULAR, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, STATUSREPORT, TIPOCAMPANHAID, IDCLIENTE, OPERADORAID, DDD FROM CAMPANHAS C
										WHERE C.CLIENTEID=@ClienteID AND DATAENVIAR BETWEEN @DataInicial AND @DataFinal
										)
											SELECT TEXTO, CELULAR, STATUSENVIO, STATUSREPORT, CAT.CARTEIRA, C.CARTEIRAID, CA.ARQUIVO, C.ARQUIVOID, T.TIPOCAMPANHA, C.TIPOCAMPANHAID, NOME FORNECEDOR, C.FORNECEDORID,  IDCLIENTE, OPERADORAID, U.REGIAO, U.UF, C.DATAENVIAR,C.DDD FROM ENVIADAS C {0}
											JOIN string_split(@Carteiras, ',') TS ON C.CARTEIRAID=CAST(TS.VALUE AS INT)
											JOIN CARTEIRAS CAT ON CAST(TS.VALUE AS INT)=CAT.CARTEIRAID
											JOIN UFDDD U ON C.DDD=U.DDD
											JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
											LEFT JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
											LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID
										ELSE
										WITH ENVIADAS AS
										(
										SELECT TEXTO, CELULAR, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, STATUSREPORT, TIPOCAMPANHAID, IDCLIENTE, OPERADORAID, DDD FROM CAMPANHAS C
										WHERE C.CLIENTEID=@ClienteID AND DATAENVIAR BETWEEN @DataInicial AND @DataFinal
										)
										SELECT TEXTO, CELULAR, STATUSENVIO, STATUSREPORT, CAT.CARTEIRA, C.CARTEIRAID, CA.ARQUIVO, C.ARQUIVOID, T.TIPOCAMPANHA, C.TIPOCAMPANHAID, NOME FORNECEDOR, C.FORNECEDORID,  IDCLIENTE, OPERADORAID, U.REGIAO, U.UF, C.DATAENVIAR,C.DDD FROM ENVIADAS C {0}
											JOIN CARTEIRAS CAT ON C.CARTEIRAID=CAT.CARTEIRAID
											JOIN UFDDD U ON C.DDD=U.DDD
											JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
											LEFT JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
											LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID", c.Usuario != null ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);


					if (c.StatusEnvio < 10)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "STATUSENVIO=@StatusEnvio AND ");


					var r = await conn.ExecuteReaderAsync(query, p, commandTimeout: 8888, commandType: CommandType.Text);

					var campanhas = new List<CampanhaModel>() { };


					while (r.Read())
					{
						var campanha = new CampanhaModel();

						try
						{

							campanha.IDCliente = !r.IsDBNull(12) ? r.GetString(12) : string.Empty;
							campanha.Texto = r.GetString(0);
							campanha.Celular = r.GetDecimal(1);
							campanha.Report = RetornaReport(r.GetByte(2), r.IsDBNull(3) ? new Nullable<byte>() : new Nullable<byte>(r.GetByte(3)));
							campanha.StatusEnvio = r.GetByte(2);
							campanha.DataEnviar = r.GetDateTime(16);
							campanha.Fornecedor = new FornecedorModel() { FornecedorID = r.GetInt32(11), Nome = r.GetString(10) };
							campanha.Regiao = r.GetString(14);
							campanha.DDD = r.GetByte(17);
							campanha.Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), r.GetByte(13).ToString()));
							campanha.TipoCampanha = !r.IsDBNull(9) ? new TipoCampanhaModel() { TipoCampanha = r.GetInt32(9) > 0 ? r.GetString(8) : string.Empty, TipoCampanhaID = r.GetInt32(9) } : null;
							campanha.Arquivo = !r.IsDBNull(7) ? new ArquivoCampanhaModel() { Arquivo = r.GetString(6), ArquivoID = r.GetInt32(7) } : null;
							campanha.Carteira = new CarteiraModel() { Carteira = r.GetString(4), CarteiraID = r.GetInt32(5) };
							campanha.UF = r.GetString(15);
							campanhas.Add(campanha);
						}
						catch (Exception)
						{

							throw;
						}
					}


					if (campanhas.Any())
						return campanhas;

					return new CampanhaModel[] { };

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

		public async Task<int> GetClienteIDByArquivo(string arquivo)
		{
			//SELECT CLIENTEID FROM REQUISICAO_RELATORIO WHERE ARQUIVO = @Arquivo


			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Arquivo", arquivo, DbType.String, ParameterDirection.Input);

					var result = await conn.QueryFirstOrDefaultAsync<int>("SELECT CLIENTEID FROM REQUISICAO_RELATORIO WHERE ARQUIVO=@Arquivo", p);

					if (result > 0)
						return result;

					return 0;
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
		public async Task AddRejeitadas(IEnumerable<CampanhaModel> t)
		{
			using (var conn = new SqlConnection("Data Source=ec2-34-237-139-184.compute-1.amazonaws.com;Initial Catalog=MONEOSI;User Id=moneo;Password=rv2b7000438dm;"))
			{
				await conn.OpenAsync();

				var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("TRUNCATE TABLE REJEITADAS_ANTERIORES", transaction: tran);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(t.Select(m => new
						{
							Numero = m.Celular,
							Data = m.Data
						}),
						"Numero", "Data"))
						{
							bcp.DestinationTableName = "REJEITADAS_ANTERIORES";
							bcp.ColumnMappings.Add("Numero", "CELULAR");
							bcp.ColumnMappings.Add("Data", "DATA");
							bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
							bcp.EnableStreaming = true;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

					Console.WriteLine("Concluído");
					tran.Commit();
				}
				catch (Exception)
				{
					tran.Rollback();
					throw;
				}
				finally
				{
					tran.Dispose();
					conn.Close();
				}
			}
		}

		public async Task<CampanhaModel> DadosIOPeople(CampanhaModel c, int f)
		{
			var p = new DynamicParameters();
			p.Add("CampanhaID", c.CampanhaID, DbType.Int32, ParameterDirection.Input);
			p.Add("FornecedorID", f, DbType.Int32, ParameterDirection.Input);
			return await DALGeneric.GenericReturnSingleOrDefaultAsyn<CampanhaModel>("SELECT DATAENVIAR, CELULAR, TEXTO, CA.ARQUIVO FILENAME FROM CAMPANHAS C JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID WHERE CAMPANHAID=@CampanhaID AND FORNECEDORID=@FornecedorID", d: p);

		}
		public async Task AnaliticoRetorno(DateTime data)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataDia", data, DbType.Date, ParameterDirection.Input);

					var dados = await conn.QueryAsync<ConsolidadoModel>(@"SELECT C.CARTEIRA, SUM(QUANTIDADE) RECEBIDAS, C.CLIENTEID, HORA, RC.CARTEIRAID FROM [RETORNO_CONSOLIDADO] RC JOIN CARTEIRAS C ON RC.CARTEIRAID = C.CARTEIRAID WHERE DATADIA = @DataDia GROUP BY RC.CARTEIRAID, C.CLIENTEID,C.CARTEIRA, HORA ORDER BY C.CARTEIRA, RC.CARTEIRAID", p, commandTimeout: Util.TIMEOUTEXECUTE);

					var sb = new StringBuilder();
					IEnumerable<int> carteiras = new int[] { };
					IEnumerable<GestorModel> gestores = new GestorModel[] { };

					if (dados != null)
						foreach (var item in dados.GroupBy(a => a.ClienteID, (a, b) => new { ClienteID = a, Consolidados = b }).ToList())
						{

							carteiras = item.Consolidados.GroupBy(k => new { CarteiraID = k.CarteiraID }, (l, m) => l.CarteiraID.Value).ToList();
							gestores = await new DALGestores().GestorByCarteiras(item.ClienteID.Value, carteiras);

							foreach (var carteiraid in carteiras)
							{
								var emails = new List<EmailViewModel>() { };

								foreach (var gestor in gestores)
									if (gestor.CarteiraList.Where(a => a.CarteiraID.Value == carteiraid).Any())
										emails.AddRange(gestor.Emails.Select(a => new EmailViewModel() { Nome = gestor.Nome, Email = a }));

								var conteudoEmail = await Emails.AnaliticoRetorno(item.Consolidados.Where(k => k.CarteiraID.Value == carteiraid));

								await Util.SendEmailAsync(emails,
									$"Relatório analítico de retorno do dia: {data.ToShortDateTime()}",
									conteudoEmail.Item1,
									true,
									TipoEmail.ANALITICORETORNO,
									conteudoEmail.Item2);
							}
						}
				}
				catch (Exception)
				{

					throw;
				}
				finally
				{
					conn.Close();
				}

			}
		}

		public async Task RelatorioAnalitico(DateTime data)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataDia", data, DbType.Date, ParameterDirection.Input);
					p.Add("Carteiras", data, DbType.String, ParameterDirection.Input);

					var dados = await conn.QueryAsync<ConsolidadoModel>(@"SELECT CC.CARTEIRAID, CA.ARQUIVO, C.CARTEIRA, SUM(ENVIADA) ENVIADOS, SUM(ENTREGUE) ENTREGUES, SUM(EXCLUIDA) EXCLUIDAS, SUM(ERRO) ERROS, SUM(SUSPENSA) SUSPENSOS, SUM(CANCELADA) CANCELADA, SUM(EXPIRADA) EXPIRADAS, CC.CLIENTEID, CC.DATAENVIAR  FROM CAMPANHAS_CONSOLIDADO CC 
														JOIN CARTEIRAS C ON CC.CARTEIRAID=C.CARTEIRAID 
														LEFT JOIN [dbo].[CAMPANHAS_ARQUIVOS] CA ON CC.ARQUIVOID=CA.ARQUIVOID
														WHERE DATADIA=@DataDia 
														GROUP BY CC.CARTEIRAID, C.CARTEIRA, CA.ARQUIVO, CC.ARQUIVOID, CC.DATAENVIAR, CC.CLIENTEID ORDER BY DATAENVIAR, CC.CARTEIRAID;", p, commandTimeout: Util.TIMEOUTEXECUTE);

					var sb = new StringBuilder();
					IEnumerable<int> carteiras = new int[] { };
					IEnumerable<GestorModel> gestores = new GestorModel[] { };
					if (dados != null)
					{
						foreach (var item in dados.GroupBy(a => a.ClienteID, (a, b) => new { ClienteID = a, Consolidados = b }).ToList())
						{

							carteiras = item.Consolidados.GroupBy(k => new { CarteiraID = k.CarteiraID }, (l, m) => l.CarteiraID.Value).ToList();
							gestores = await new DALGestores().GestorByCarteiras(item.ClienteID.Value, carteiras);

							foreach (var carteiraid in carteiras)
							{
								var emails = new List<EmailViewModel>() { };

								foreach (var gestor in gestores)
									if (gestor.CarteiraList.Where(a => a.CarteiraID.Value == carteiraid).Any())
										emails.AddRange(gestor.Emails.Select(a => new EmailViewModel() { Nome = gestor.Nome, Email = a }));

								await Util.SendEmailAsync(
									emails,
									$"Relatório de Entrega do dia {data.ToShortDateTime()}",
									Emails.AnaliticoConsolidado(item.Consolidados.Where(k => k.CarteiraID.Value == carteiraid)),
									true,
									TipoEmail.RELATORIOANALITICO);
							}
						}
					}
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



		public async Task<IEnumerable<StatusQuantidade>> QuantidadeByStatus(int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					var _statusquantidade = new List<StatusQuantidade>() { };


					string query = @"SELECT STATUSENVIO, COUNT(CAMPANHAID) QUANTIDADE FROM CAMPANHAS WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID GROUP BY STATUSENVIO";

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);



					if (u.HasValue)
						query = @"SELECT STATUSENVIO, COUNT(CAMPANHAID) QUANTIDADE FROM CAMPANHAS C JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID GROUP BY STATUSENVIO";


					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
						return new List<StatusQuantidade>() {
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 0)? result.Where(a => a.STATUSENVIO == 0).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.AGENDADOS.ToString() },
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 1)?result.Where(a => a.STATUSENVIO == 1).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.ENVIANDO.ToString() },
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 2)? result.Where(a => a.STATUSENVIO == 2).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.ENVIADOS.ToString() },
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 3)? result.Where(a => a.STATUSENVIO == 3).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.ERROS.ToString() },
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 4)? result.Where(a => a.STATUSENVIO == 4).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.SUSPENSOS.ToString() },
							new StatusQuantidade() { Quantidade =result.Any(a => a.STATUSENVIO == 5)? result.Where(a => a.STATUSENVIO == 5).ElementAt(0).QUANTIDADE:0, Status = StatusEnvioEnums.CANCELADOS.ToString() }};

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

		public async Task<(IEnumerable<CampanhaModel>, IEnumerable<CampanhaModel>)> DashBoard(int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT SUM(QUANTIDADE) QUANTIDADE, DATEPART(HOUR,DATAENVIAR) DATAENVIAR FROM FORNECEDOR_CAMPANHAS WHERE DATAENVIAR BETWEEN @DataDia AND DATEADD(MINUTE, 1439, @DataDia) AND CLIENTEID=@ClienteID GROUP BY DATEPART(HOUR,DATAENVIAR);
									SELECT COUNT(CAMPANHAID) QUANTIDADE, STATUSENVIO, FORNECEDORID, IIF(DATAENVIOFORNECEDOR IS NULL, DATAENVIAR, DATAENVIOFORNECEDOR) AS DATAENVIAR FROM CAMPANHAS WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID GROUP BY STATUSENVIO, FORNECEDORID, DATAENVIAR, DATAENVIOFORNECEDOR";

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					if (u.HasValue)
						query = @"SELECT SUM(QUANTIDADE) QUANTIDADE, DATEPART(HOUR,DATAENVIAR) DATAENVIAR FROM FORNECEDOR_CAMPANHAS WHERE DATAENVIAR BETWEEN @DataDia AND DATEADD(MINUTE, 1439, @DataDia) AND CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID GROUP BY DATEPART(HOUR,DATAENVIAR);
								SELECT COUNT(CAMPANHAID) QUANTIDADE, STATUSENVIO, FORNECEDORID, IIF(DATAENVIOFORNECEDOR IS NULL, DATAENVIAR, DATAENVIOFORNECEDOR) AS DATAENVIAR FROM CAMPANHAS WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID GROUP BY STATUSENVIO, FORNECEDORID, DATAENVIAR";


					var result = await conn.QueryMultipleAsync(query, p);
					//var f = await new DALFornecedor().FornecedoresTelaEnvio(c);

					if (result != null)
					{
						var dados1 = await result.ReadAsync();
						var dados2 = await result.ReadAsync();

						var r = (dados1.Select(a => new CampanhaModel()
						{
							Quantidade = a.QUANTIDADE,
							Hora = a.DATAENVIAR
						}), dados2.Select(a => new CampanhaModel()
						{
							Quantidade = a.QUANTIDADE,
							StatusEnvio = a.STATUSENVIO,
							DataEnviar = a.DATAENVIAR,
							Fornecedor = new FornecedorModel() { FornecedorID = a.FORNECEDORID }
						}));

						return r;
					}

					return (null, null);


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
		#region FILECARDS

		public async Task<int> InsertFileCards(string arquivo, string guid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{


					var p = new DynamicParameters();
					p.Add("Guid", guid, DbType.String, ParameterDirection.Input);
					p.Add("Arquivo", arquivo, DbType.String, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("Codigo", DbType.Int32, direction: ParameterDirection.Output);

					await conn.ExecuteAsync("INSERT INTO [FILECARDS] (GUID, DATA, ARQUIVO) VALUES (@Guid, @Data, @Arquivo);SELECT @Codigo=SCOPE_IDENTITY()", p);

					return p.Get<int>("Codigo");
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

		public async Task ExcludeFileCards(string guid, int? codigo = null)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Guid", guid, DbType.String, ParameterDirection.Input);
					p.Add("Codigo", codigo, DbType.Int32, ParameterDirection.Input);

					var query = "DELETE FROM FILECARDS WHERE GUID=@Guid";

					if (codigo.HasValue)
						query = "DELETE FROM FILECARDS WHERE GUID=@Guid AND CODIGO=@Codigo";


					await conn.ExecuteAsync(query, p);
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

		#endregion




		public async Task<int> RemoveSession(string sessionid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("SessionID", sessionid, DbType.String, ParameterDirection.Input);
					return await conn.ExecuteAsync("DELETE FROM [SESSION] WHERE SESSIONID=@SessionID", p);

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
		public async Task<int> InsereSession(string sessionid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("SessionID", sessionid, DbType.String, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("ID", DbType.Int32, direction: ParameterDirection.Output);

					await conn.ExecuteAsync("INSERT INTO [SESSION] (SESSIONID, DATA) VALUES (@SessionID, @Data); SELECT @ID=SCOPE_IDENTITY();", p);



					return p.Get<int>("ID");

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
		public async Task<IEnumerable<int>> RetornaIDHashSession(string sessionid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("SessionID", sessionid, DbType.String, ParameterDirection.Input);

					return await conn.QueryAsync<int>("SELECT CODIGO FROM [SESSION] WHERE SESSIONID=@SessionID", p);

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

		public async Task<int> AtualizaCampanhaPendente(CampanhaModel c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("CampanhaID", c.CampanhaID, DbType.Int64, ParameterDirection.Input);
					p.Add("StatusEnvio", c.StatusEnvio, DbType.Byte, ParameterDirection.Input);
					p.Add("DataEnvioFornecedor", c.DataEnviar, DbType.DateTime, ParameterDirection.Input);

					return await conn.ExecuteAsync("UPDATE CAMPANHAS SET STATUSENVIO=@StatusEnvio, DATAENVIOFORNECEDOR=@DataEnvioFornecedor WHERE CAMPANHAID=@CampanhaID", p);
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
		public async Task<int> CampanhasConsolidadas()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();


				try
				{
					var p = new DynamicParameters();
					p.Add("DataEnviarInicial", DateTime.Now.Date, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataEnviarFinal", DateTime.Now.DateTimeMinuteInterval(), DbType.DateTime, ParameterDirection.Input);

					return await conn.ExecuteAsync("UPDATE CAMPANHAS SET STATUSENVIO=1 WHERE DATAENVIAR BETWEEN @DataEnviarInicial AND @DataEnviarFinal AND STATUSENVIO=0", p);
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


		public async Task<int> GeraConsolidados()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();


				try
				{
					var p = new DynamicParameters();
					p.Add("DataInicial", DateTime.Now.Date.AddDays(-1), DbType.Date, ParameterDirection.Input);
					p.Add("@DataFinal", DateTime.Now.Date.AddDays(-1), DbType.Date, ParameterDirection.Input);

					return await conn.ExecuteAsync("CONSOLIDADO_CAMPANHAS_AGENDADA", p, commandType: CommandType.StoredProcedure);
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
		public async Task<int> AtualizaCampanhasAgendadas()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataEnviarInicial", DateTime.Now.Date, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataEnviarFinal", DateTime.Now.DateTimeMinuteInterval(), DbType.DateTime, ParameterDirection.Input);

					return await conn.ExecuteAsync("UPDATE CAMPANHAS SET STATUSENVIO=1 WHERE DATAENVIAR BETWEEN @DataEnviarInicial AND @DataEnviarFinal AND STATUSENVIO=0", p, commandTimeout: 8888);
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

		public async Task UpdateFilaCampanha(IEnumerable<CampanhaModel> l)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{
					if (l.Any())
					{
						var erros = l.Where(k => k.StatusEnvio == 3);

						await conn.ExecuteAsync(@"CREATE TABLE #CAMPANHAS(
													    CAMPANHAID BIGINT NOT NULL,
                                                        DATAENVIOFORNECEDOR DATETIME NOT NULL,
													    STATUSENVIO TINYINT NOT NULL);

												    CREATE STATISTICS s_DATAENVIOFORNECEDOR ON #CAMPANHAS (DATAENVIOFORNECEDOR);
												    CREATE STATISTICS s_STATUSENVIO ON #CAMPANHAS (STATUSENVIO);
                                                    ", transaction: tran, commandTimeout: 888);

						await conn.ExecuteAsync(@"UPDATE STATISTICS #CAMPANHAS", transaction: tran, commandTimeout: 888);

						using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
						{

							using (var reader = ObjectReader.Create(l.AsParallel().Select(m => new
							{
								CampanhaID = m.CampanhaID,
								DataEnvioFornecedor = m.DataEnviar,
								StatusEnvio = m.StatusEnvio
							}),
							"CampanhaID", "DataEnvioFornecedor", "StatusEnvio"))
							{
								bcp.DestinationTableName = "#CAMPANHAS";
								bcp.ColumnMappings.Add("CampanhaID", "CAMPANHAID");
								bcp.ColumnMappings.Add("DataEnvioFornecedor", "DATAENVIOFORNECEDOR");
								bcp.ColumnMappings.Add("StatusEnvio", "STATUSENVIO");
								bcp.BulkCopyTimeout = 8888;
								bcp.EnableStreaming = true;

								await bcp.WriteToServerAsync(reader);
								bcp.Close();
							}
						}

						await conn.ExecuteAsync(@"UPDATE C SET C.STATUSENVIO = T.STATUSENVIO, C.DATAENVIOFORNECEDOR = T.DATAENVIOFORNECEDOR
                                                FROM CAMPANHAS C 
                                                INNER JOIN #CAMPANHAS T ON T.CAMPANHAID = C.CAMPANHAID", transaction: tran, commandTimeout: 888);

						await conn.ExecuteAsync(@"DROP TABLE #CAMPANHAS", transaction: tran, commandTimeout: 888);

						if (erros.Any())
						{
							using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
							{

								using (var reader = ObjectReader.Create(erros.AsParallel().Select(m => new
								{
									CampanhaID = m.CampanhaID,
									TipoErroApi = m.TipoErroApi
								}),
								"CampanhaID", "TipoErroApi"))
								{
									bcp.DestinationTableName = "CAMPANHA_ERRO_API";
									bcp.ColumnMappings.Add("CampanhaID", "CAMPANHAID");
									bcp.ColumnMappings.Add("TipoErroApi", "TIPOERROAPI");
									bcp.BulkCopyTimeout = 8888;
									bcp.EnableStreaming = true;

									await bcp.WriteToServerAsync(reader);
									bcp.Close();
								}
							}
						}

						tran.Commit();
					}
				}
				catch (Exception err)
				{
					throw err;
				}
				finally
				{
					tran.Dispose();
					conn.Close();
				}
			}
		}

		public async Task<IEnumerable<CampanhaModel>> CampanhasPendentes()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

					var result = await conn.QueryAsync(new CommandDefinition(@"WITH ENVIADAS AS(
                                                                                SELECT FORNECEDORID, CELULAR, TEXTO, CAMPANHAID, CLIENTEID, STATUSENVIO, ROW_NUMBER() OVER(PARTITION BY FORNECEDORID, CLIENTEID ORDER BY DATAENVIAR ASC) ROWNUMBER
                                                                                FROM CAMPANHAS WITH(NOLOCK) WHERE DATADIA=@DATADIA AND STATUSENVIO=1)
                                                                                SELECT E.CLIENTEID ,E.FORNECEDORID ,E.CELULAR ,E.TEXTO ,E.CAMPANHAID, FC.USUARIO, FC.SENHA, FC.TOKEN, E.STATUSENVIO
                                                                                FROM ENVIADAS E
                                                                                JOIN FORNECEDOR_CLIENTE FC (NOLOCK) ON FC.FORNECEDORID = E.FORNECEDORID
	                                                                                AND FC.CLIENTEID = E.CLIENTEID AND FC.STATUSFORNECEDOR IN (0, 1)
                                                                                WHERE ROWNUMBER <= (FC.ENVIOACADA5MIN / ((60 * 5) / 15)) "
																			, p, commandTimeout: Util.TIMEOUTEXECUTE));

					if (result != null || result.Any())
					{
						var r = result.Select(a => new CampanhaModel()
						{
							CampanhaID = a.CAMPANHAID,
							Celular = a.CELULAR,
							Texto = a.TEXTO,
							StatusEnvio = a.STATUSENVIO,
							Cliente = new ClienteModel { ClienteID = a.CLIENTEID },
							Fornecedor = new FornecedorModel() { FornecedorID = a.FORNECEDORID, Login = new LoginViewModel { Username = a.USUARIO, Password = a.SENHA } }
						});

						return r;
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

		public async Task<IEnumerable<CampanhaModel>> Filtragem(IEnumerable<CampanhaModel> c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{
					var p = new DynamicParameters();
					p.Add("Data", DateTime.Now.AddDays(-60), DbType.Date, ParameterDirection.Input);


					await conn.ExecuteAsync(@"CREATE TABLE #TMP_CELLS
										(
											CODIGO	INT PRIMARY KEY	IDENTITY (1,1), 
											CELULAR	NUMERIC(11,0)
										);
										CREATE NONCLUSTERED INDEX [ix_TEMP] ON #TMP_CELLS (CELULAR);
										CREATE STATISTICS s_CELULAR ON #TMP_CELLS (CELULAR);", transaction: tran);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
					{
						using (var reader = ObjectReader.Create(c.Select(m => new { Celular = m.Celular }), "Celular"))
						{
							bcp.EnableStreaming = true;
							bcp.DestinationTableName = "#TMP_CELLS";
							bcp.ColumnMappings.Add("Celular", "CELULAR");
							bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
							bcp.EnableStreaming = true;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

					var celulares = await conn.QueryAsync<CampanhaModel>("SELECT RA.CELULAR FROM REJEITADAS_ANTERIORES RA WITH(INDEX([IX_REJEITADAS_ANTERIORES_CELULAR_Includes])) JOIN #TMP_CELLS T ON RA.CELULAR=T.CELULAR WHERE DATA>=@Data", p,
						transaction: tran,
						commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync(@"DROP TABLE #TMP_CELLS", transaction: tran);

					tran.Commit();

					return celulares.AsParallel().GroupBy(a => a.Celular, (a, b) => new CampanhaModel() { Celular = a });

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
		public async Task<IEnumerable<CampanhaModel>> HigienizaCarteira(IEnumerable<CampanhaModel> c, int carteiraid, int diashigieniza, int cliente, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{

					var p = new DynamicParameters();
					p.Add("ClienteID", cliente, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", DateTime.Now.Date.AddDays(-diashigieniza), DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", DateTime.Now.Date.AddDays(+diashigieniza), DbType.Date, ParameterDirection.Input);
					p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);

					await conn.ExecuteAsync(@"CREATE TABLE #TMP_CELLS
										(
											CODIGO	INT PRIMARY KEY	IDENTITY (1,1), 
											CELULAR	NUMERIC(12,0)
										);
										CREATE NONCLUSTERED INDEX [ix_TEMP] ON #TMP_CELLS (CELULAR)", transaction: tran);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
					{
						using (var reader = ObjectReader.Create(c.Select(m => new { Celular = m.Celular }), "Celular"))
						{
							bcp.EnableStreaming = true;
							bcp.DestinationTableName = "#TMP_CELLS";
							bcp.ColumnMappings.Add("Celular", "CELULAR");
							bcp.BulkCopyTimeout = 888;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

					var celulares = await conn.QueryAsync("SELECT C.CELULAR FROM CAMPANHAS C JOIN #TMP_CELLS TC ON C.CELULAR=TC.CELULAR WHERE CARTEIRAID=@CarteiraID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND CLIENTEID=@ClienteID AND STATUSENVIO IN(0,1,2) GROUP BY C.CELULAR", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync(@"DROP TABLE #TMP_CELLS", transaction: tran);

					tran.Commit();

					if (celulares.Any())
						return celulares.Select(a => new CampanhaModel() { Celular = a.CELULAR });


					return new CampanhaModel[] { };

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

		//public async Task CampanhasAgendadas()
		//{
		//	using (var conn = new SqlConnection(Util.ConnString))
		//	{
		//		await conn.OpenAsync();

		//		try
		//		{
		//			var p = new DynamicParameters();
		//			p.Add("DataInicial", DateTime.Now, DbType.Date, ParameterDirection.Input);
		//			p.Add("DataFinal", a, DbType.String, ParameterDirection.Input);

		//			return await conn.QuerySingleOrDefaultAsync<bool>("SELECT CAST(COUNT(ARQUIVOID) AS BIT) RETORNO FROM CAMPANHAS_ARQUIVOS WHERE ARQUIVO=@Arquivo AND CLIENTEID=@ClienteID", p);

		//		}
		//		catch (Exception err)
		//		{
		//			throw err;
		//		}
		//		finally
		//		{
		//			conn.Close();
		//		}
		//	}
		//}
		public async Task<IEnumerable<string>> ArquivoExistente(IEnumerable<string> a, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Arquivo", a.Aggregate((k, m) => $"{k},{m}"), DbType.String, ParameterDirection.Input);

					return await conn.QueryAsync<string>("SELECT CA.ARQUIVO RETORNO FROM CAMPANHAS_ARQUIVOS CA JOIN string_split(@Arquivo, ',') C ON CA.ARQUIVO=C.VALUE WHERE CLIENTEID=@ClienteID", p);



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
		async Task RelatorioDetalhado(CampanhaRequisicaoRelatorioModel ca, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					var campanhas = await DetalhadoGenerico(new CampanhaModel() { DataInicial = ca.DataInicial, DataFinal = ca.DataFinal.Value.AddMinutes(1439), CarteiraList = ca.CarteiraList, Usuario = u.HasValue ? new UsuarioModel() { UsuarioID = u.Value } : null, Cliente = new ClienteModel() { ClienteID = c }, StatusEnvio = 10 });

					if (campanhas.Any())
					{
						var _b = new List<byte[]>() { };
						_b.Add(Util.EncoderDefaultFiles.GetBytes("IDCLIENTE;CELULAR;TEXTO;DATAENVIAR;DATAREPORT;STATUSREPORT;OPERADORA;CARTEIRA;ARQUIVO;TIPOCAMPANHA;REGIAO;DDD;UF\r\n"));
						_b.AddRange(campanhas.Select(a => Util.EncoderDefaultFiles.GetBytes(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}\r\n", a.IDCliente, a.Celular, a.Texto, a.DataEnviar, a.DataReport, a.Report, a.Operadora, a.Carteira.Carteira, a.Arquivo != null ? a.Arquivo.Arquivo : string.Empty, a.TipoCampanha != null ? a.TipoCampanha.TipoCampanha : string.Empty, a.Regiao, a.DDD, a.UF))));

						using (var memOriginal = new MemoryStream(_b.AsParallel().SelectMany(a => a).ToArray()))
						{
							using (var mem = new MemoryStream()) //stream final para o zip
							{
								using (var arquivo = new ZipArchive(mem, ZipArchiveMode.Create, true))
								{
									var _file = arquivo.CreateEntry("carteiras.csv", CompressionLevel.Optimal);
									using (var entryFile = _file.Open())
									{
										memOriginal.Position = 0L;
										var _byte = await ToArrayByte(memOriginal);
										await entryFile.WriteAsync(_byte, 0, _byte.Count());
									}
								}
								mem.Seek(0, SeekOrigin.Begin);


								await Util.UploadAamazon(mem.ToArray(), $"{c}/{ca.Arquivo}", "moneoup");
								await conn.ExecuteAsync("UPDATE REQUISICAO_RELATORIO SET STATUSRELATORIO=1, TAMANHO=@Tamanho WHERE REQUISICAOID=@RequisicaoID", new { RequisicaoID = ca.RequisicaoID, Tamanho = (decimal)mem.ToArray().LongCount() / 1024 });
								await Util.SendEmailAsync(ca.Emails.Select(k => new EmailViewModel() { Email = k }), $"Relatório Detalhado {ca.DataInicial.Value.ToString("dd/MM/yyyy")} a {ca.DataFinal.Value.ToString("dd/MM/yyyy")}",
										Emails.EmailRequisicaoCarteira($"{Util.Configuration["UrlIdentity"]}/api/campanha/r/d/{ca.Arquivo}"),
										true,
										TipoEmail.REQUISICAOCARTEIRA);
							}
						}
					}
					else
					{
						await conn.ExecuteAsync("UPDATE REQUISICAO_RELATORIO SET STATUSRELATORIO=2 WHERE REQUISICAOID=@RequisicaoID", new { RequisicaoID = ca.RequisicaoID });
						return;
					}



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

		public async static Task<byte[]> ToArrayByte(Stream s)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream m = new MemoryStream())
			{
				int read;
				while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
					await m.WriteAsync(buffer, 0, read);

				return m.ToArray();
			}
		}

		string RetornaReport(byte statusenvio, byte? statusreport)
		{
			string retorno = null;
			if (statusenvio == 2)
			{
				if (statusreport.HasValue)
					retorno = ((ReportDeliveryEnums)Enum.Parse(typeof(ReportDeliveryEnums), statusreport.Value.ToString())).ToString();
				else
					retorno = StatusEnvioEnums.ENVIADOS.ToString();
			}
			else
				retorno = ((StatusEnvioEnums)Enum.Parse(typeof(StatusEnvioEnums), statusenvio.ToString())).ToString();
			return retorno;
		}

		public async Task<IEnumerable<CampanhaRequisicaoRelatorioModel>> ListarRequisicaoRelatorio(int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT RR.DATAINICIAL, RR.DATAFINAL, RR.DATA, RRC.CARTEIRAID, RRE.EMAIL, C.CARTEIRA, RR.REQUISICAOID, TIPORELATORIO, STATUSRELATORIO, ARQUIVO, TAMANHO FROM [dbo].[REQUISICAO_RELATORIO] RR 
									 JOIN [dbo].[REQUISICAO_RELATORIO_EMAILS] RRE ON RR.REQUISICAOID=RRE.REQUISICAOID
									LEFT JOIN [dbo].[REQUISICAO_RELATORIO_CARTEIRAS] RRC ON RR.REQUISICAOID=RRC.REQUISICAOID
									LEFT JOIN CARTEIRAS C ON RRC.CARTEIRAID=C.CARTEIRAID 
									WHERE RR.CLIENTEID=@ClienteID ORDER BY RR.DATA DESC";



					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					if (u.HasValue)
					{
						query = @"
									SELECT RR.DATAINICIAL, RR.DATAFINAL, RR.DATA, RRC.CARTEIRAID, RRE.EMAIL, C.CARTEIRA, RR.REQUISICAOID, TIPORELATORIO, STATUSRELATORIO, ARQUIVO, TAMANHO FROM [dbo].[REQUISICAO_RELATORIO] RR 
									 JOIN [dbo].[REQUISICAO_RELATORIO_EMAILS] RRE ON RR.REQUISICAOID=RRE.REQUISICAOID
									LEFT JOIN [dbo].[REQUISICAO_RELATORIO_CARTEIRAS] RRC ON RR.REQUISICAOID=RRC.REQUISICAOID
									LEFT JOIN CARTEIRAS C ON RRC.CARTEIRAID=C.CARTEIRAID
									WHERE RR.CLIENTEID=@ClienteID AND RR.USUARIOID=@UsuarioID ORDER BY RR.DATA DESC";

					}

					var result = await conn.QueryAsync(query, p);

					if (result != null)
					{
						var dados = result.Select(a => new
						{
							Email = a.EMAIL,
							Carteira = a.CARTEIRA,
							RequisicaoID = a.REQUISICAOID,
							DataFinal = a.DATAFINAL,
							DataInicial = a.DATAINICIAL,
							CarteiraID = a.CARTEIRAID,
							Data = a.DATA,
							StatusRelatorio = (byte)a.STATUSRELATORIO,
							TipoRelatorio = (byte)a.TIPORELATORIO,
							Arquivo = a.ARQUIVO,
							Tamanho = a.TAMANHO
						});

						return dados.GroupBy(a => new { RequisicaoID = a.RequisicaoID, TipoRelatorio = a.TipoRelatorio, Data = a.Data, DataInicial = a.DataInicial, DataFinal = a.DataFinal, StatusRelatorio = a.StatusRelatorio, Arquivo = a.Arquivo, Tamanho = a.Tamanho }, (a, b) => new CampanhaRequisicaoRelatorioModel()
						{
							RequisicaoID = a.RequisicaoID,
							DataInicial = a.DataInicial,
							DataFinal = a.DataFinal,
							StatusRelatorio = ((StatusRelatorioEnum)Enum.Parse(typeof(StatusRelatorioEnum), a.StatusRelatorio.ToString())),
							Data = a.Data,
							TipoRelatorio = ((TipoRelatorioEnum)Enum.Parse(typeof(TipoRelatorioEnum), a.TipoRelatorio.ToString())),
							Carteiras = b.Any(k => !string.IsNullOrEmpty(k.Carteira)) ? b.GroupBy(k => new { Carteira = k.Carteira, CarteiraID = k.CarteiraID }, (k, l) => new CarteiraModel() { Carteira = k.Carteira, CarteiraID = k.CarteiraID }) : new CarteiraModel[] { },
							Emails = b.GroupBy(k => new { Email = k.Email }, (k, l) => Convert.ToString(k)),
							Arquivo = a.Arquivo,
							Tamanho = a.Tamanho
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


		public async Task CadastraRequsicao(CampanhaRequisicaoRelatorioModel ca, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", ca.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", ca.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("TipoRelatorio", (byte)ca.TipoRelatorio, DbType.Byte, ParameterDirection.Input);
					p.Add("Arquivo", ca.Arquivo, DbType.String, ParameterDirection.Input);
					p.Add("StatusRelatorio", (byte)ca.StatusRelatorio, DbType.Byte, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("RequisicaoID", dbType: DbType.Int32, direction: ParameterDirection.Output);

					await conn.ExecuteAsync("INSERT INTO [dbo].[REQUISICAO_RELATORIO]([CLIENTEID],[USUARIOID],[DATA],[DATAINICIAL],[DATAFINAL], [TIPORELATORIO], ARQUIVO, STATUSRELATORIO) VALUES (@ClienteID, @UsuarioID,@Data, @DataInicial, @DataFinal, @TipoRelatorio, @Arquivo,@StatusRelatorio); SELECT @RequisicaoID=SCOPE_IDENTITY(); ", p, transaction: tran, commandTimeout: 888);

					var requisicaoID = p.Get<int>("RequisicaoID");

					await conn.ExecuteAsync("INSERT INTO REQUISICAO_RELATORIO_EMAILS (EMAIL, REQUISICAOID) VALUES (@Email, @RequisicaoID)", ca.Emails.Select(a => new { Email = a, RequisicaoID = requisicaoID }), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					if (ca.CarteiraList.Any())
						await conn.ExecuteAsync("INSERT INTO REQUISICAO_RELATORIO_CARTEIRAS (REQUISICAOID, CARTEIRAID) VALUES (@RequisicaoID, @CarteiraID)", ca.CarteiraList.Select(a => new { CarteiraID = a.CarteiraID, RequisicaoID = requisicaoID }), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					tran.Commit();

					ca.RequisicaoID = requisicaoID;


#pragma warning disable 4014
					RelatorioDetalhado(ca, c, u);
#pragma warning restore 4014

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

		public async Task<MonitoriaModel> MonitoriaActions(MonitoriaModel monitoria, byte statusenvio, int c, int? u, int? carteiraid = null)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = string.Format(@"WITH ENVIADAS AS 
												 (
												 SELECT CAMPANHAID, ARQUIVOID, CARTEIRAID, FORNECEDORID, DATAENVIAR, DATEDIFF(SECOND, DATAENVIAR, ISNULL([DATAENVIOFORNECEDOR], DATAENVIAR)) ATRASO FROM CAMPANHAS C WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=@StatusEnvio {1}
												 )
												 SELECT COUNT(CAMPANHAID) QUANTIDADE, E.ARQUIVOID, E.CARTEIRAID, E.FORNECEDORID, CARTEIRA, ARQUIVO, HORALIMITE, DATAENVIAR, F.NOME, DISTRIBUICAO, CAPACIDADEENVIO, STATUSFORNECEDOR, STATUSOPERACIONAL, ATRASO FROM ENVIADAS E  {0}
												 JOIN CARTEIRAS C ON E.CARTEIRAID=C.CARTEIRAID
												 JOIN CAMPANHAS_ARQUIVOS CA ON E.ARQUIVOID=CA.ARQUIVOID
												 JOIN FORNECEDOR_CLIENTE FC ON E.FORNECEDORID=FC.FORNECEDORID AND FC.CLIENTEID=@ClienteID
												 JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID
												 LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
												 GROUP BY E.ARQUIVOID, E.CARTEIRAID, E.FORNECEDORID, CARTEIRA, ARQUIVO, HORALIMITE,  DATAENVIAR, F.NOME, DISTRIBUICAO, CAPACIDADEENVIO, STATUSFORNECEDOR, STATUSOPERACIONAL, ATRASO",
												 u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON E.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty,
												 carteiraid.HasValue ? "AND E.CARTEIRAID=@CarteiraID" : string.Empty
												 );


					var dataIn = DateTime.Now.AddDays(-1);

					if (statusenvio == 0)
						dataIn = DateTime.Now.Date;

					var dataOut = DateTime.Now.Date;


					if (statusenvio == 0)
						dataOut = dataOut.AddDays(1);


					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", monitoria.DataInicial.HasValue ? monitoria.DataInicial.Value : dataIn, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", monitoria.DataFinal.HasValue ? monitoria.DataFinal.Value : dataOut, DbType.Date, ParameterDirection.Input);
					p.Add("StatusEnvio", statusenvio, DbType.Byte, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(query, p);

					string usuario = null;

					if (!u.HasValue)
						usuario = (await conn.QuerySingleOrDefaultAsync<ClienteModel>("SELECT NOME FROM CLIENTES WHERE CLIENTEID=@ClienteID", new { ClienteID = c })).Nome;

					if (result != null)
					{
						var dados = result.Select(a => new
						{
							Carteira = a.CARTEIRA.ToString(),
							Arquivo = a.ARQUIVO,
							DataEnviar = a.DATAENVIAR,
							Quantidade = a.QUANTIDADE,
							DataCadastro = a.DATAENVIAR,
							Usuario = u.HasValue ? a.NOME : usuario,
							ArquivoID = (int)a.ARQUIVOID,
							CarteiraID = a.CARTEIRAID,
							HoraLimite = a.HORALIMITE,
							FornecedorID = a.FORNECEDORID,
							DataDia = Convert.ToDateTime(a.DATAENVIAR).Date,
							Fornecedor = a.NOME,
							Distribuicao = a.DISTRIBUICAO,
							Capacidade = a.CAPACIDADEENVIO,
							StatusFornecedor = a.STATUSFORNECEDOR,
							StatusOPeracional = a.STATUSOPERACIONAL,
							Atraso = a.ATRASO,
							Nome = a.NOME
						});

						return new MonitoriaModel()
						{
							Cliente = new ClienteModel() { ClienteID = c },
							TotalArquivos = dados.GroupBy(a => a.Arquivo).Count(),
							TotalRegistros = dados.Sum(m => m.Quantidade),
							CartArquivosDia = dados
							.GroupBy(a => new { Carteira = a.Carteira, CarteiraID = a.CarteiraID, HoraLimite = a.HoraLimite, DataDia = a.DataDia },
							(a, b) => new CarteiraArquivos()
							{
								DataDia = a.DataDia,
								HoraLimite = a.HoraLimite,
								Carteira = a.Carteira,
								CarteiraID = (int)a.CarteiraID,
								Arquivos = b.GroupBy(k => new { k.Arquivo, k.ArquivoID }, (m, n) => new Arquivos()
								{
									FornecedoresMin = n.GroupBy(k => k.FornecedorID, (k, o) => new FornecedorMinModel()
									{
										FornecedorID = k,
										Nome = o.ElementAt(0).Nome,
										EntregaTime = TimeSpan.FromSeconds(o.ElementAt(0).Atraso),
										StatusOperacional = ((StatusOperacionalFornecedorEnum)Enum.Parse(typeof(StatusOperacionalFornecedorEnum), ((byte)o.ElementAt(0).StatusOPeracional).ToString())),
										Distribuicao = Math.Round((decimal)o.Sum(option => option.Quantidade) / (decimal)n.Sum(option => option.Quantidade) * 100, 0)
									}),
									Quantidade = n.Sum(option => option.Quantidade),
									Arquivo = m.Arquivo,
									ArquivoID = m.ArquivoID,
									DataCadastro = n.ElementAt(0).DataCadastro,
									Usuario = n.ElementAt(0).Usuario == null ? usuario : n.ElementAt(0).Usuario.ToString(),
									Lotes = n
									.OrderBy(option => option.DataEnviar)
									.GroupBy(option => new { DataEnviar = option.DataEnviar },
									(j, l) => new CampanhaGridLotesModel()
									{
										DataEnviar = j.DataEnviar,
										Data = Convert.ToDateTime(j.DataEnviar).ToString("dd/MM/yyyy"),
										Hora = Convert.ToDateTime(j.DataEnviar).ToString("HH:mm"),
										Intervalos = n.Count(),
										Quantidade = l.Sum(op => op.Quantidade),
										Status = statusenvio == 4 ? "SUSPENSOS" : "AGENDADOS"
									})
									.Select((j, i) => new CampanhaGridLotesModel() { Fornecedores = j.Fornecedores, DataEnviar = j.DataEnviar, Data = j.Data, Hora = j.Hora, Intervalos = j.Intervalos, Quantidade = j.Quantidade, StatusLista = j.StatusLista, Status = j.Status, Lote = i + 1 })
								})
								.OrderBy(op => op.Arquivo)
								.ToList()
							}).Select(m => new CarteiraArquivos() { HoraLimite = m.HoraLimite, Carteira = m.Carteira, CarteiraID = m.CarteiraID, Arquivos = m.Arquivos, DataDia = m.DataDia })
							.OrderBy(option => option.DataDia).ThenBy(a => a.Carteira)
							.GroupBy(a => a.DataDia, (a, b) => new { DataDia = a, Arquivos = b })
							.Select(a => new ArquivosDia()
							{
								DataDia = a.DataDia,
								CartAqruivos = a.Arquivos
							})
						};

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

		public async Task<MonitoriaModel> MonitoriaHoje(int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = string.Format(@"WITH ENVIADAS AS 
									 (
									 SELECT CAMPANHAID, ARQUIVOID, CARTEIRAID, FORNECEDORID, DATAENVIAR, DATEDIFF(SECOND, DATAENVIAR, ISNULL([DATAENVIOFORNECEDOR], DATAENVIAR)) ATRASO, STATUSENVIO FROM CAMPANHAS C WHERE C.CLIENTEID=@ClienteID AND DATADIA=@DataDia
									 )
									 SELECT COUNT(CAMPANHAID) QUANTIDADE, E.ARQUIVOID, E.CARTEIRAID, E.FORNECEDORID, CARTEIRA, ARQUIVO, HORALIMITE, DATAENVIAR, F.NOME, DISTRIBUICAO, CAPACIDADEENVIO, STATUSFORNECEDOR, STATUSOPERACIONAL, AVG(CAST(ATRASO AS BIGINT)) ATRASO, STATUSENVIO, CA.DATA, U.NOME USUARIO FROM ENVIADAS E  {0}
									 JOIN CARTEIRAS C ON E.CARTEIRAID=C.CARTEIRAID AND C.ISEXCLUDED=0
									 JOIN CAMPANHAS_ARQUIVOS CA ON E.ARQUIVOID=CA.ARQUIVOID
									 JOIN FORNECEDOR_CLIENTE FC ON E.FORNECEDORID=FC.FORNECEDORID AND FC.CLIENTEID=@ClienteID
									 JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID
									 LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
									 GROUP BY E.ARQUIVOID, E.CARTEIRAID, E.FORNECEDORID, CARTEIRA, ARQUIVO, HORALIMITE,  DATAENVIAR, F.NOME, DISTRIBUICAO, CAPACIDADEENVIO, STATUSFORNECEDOR, STATUSOPERACIONAL, STATUSENVIO, CA.DATA, U.NOME",
									 u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON E.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					var result = await conn.QueryAsync(query, p);

					string usuario = null;

					if (!u.HasValue)
						usuario = (await conn.QuerySingleOrDefaultAsync<ClienteModel>("SELECT NOME FROM CLIENTES WHERE CLIENTEID=@ClienteID", new { ClienteID = c })).Nome;

					if (result != null)
					{
						var dados = result.Select(a => new
						{
							Carteira = a.CARTEIRA.ToString(),
							Arquivo = a.ARQUIVO,
							DataEnviar = a.DATAENVIAR,
							Quantidade = a.QUANTIDADE,
							DataCadastro = Convert.ToDateTime(a.DATAENVIAR).Date,
							Usuario = a.USUARIO ?? usuario,
							ArquivoID = (int)a.ARQUIVOID,
							CarteiraID = a.CARTEIRAID,
							HoraLimite = a.HORALIMITE,
							FornecedorID = a.FORNECEDORID,
							DataDia = Convert.ToDateTime(a.DATAENVIAR).Date,
							Fornecedor = a.NOME,
							Distribuicao = a.DISTRIBUICAO,
							Capacidade = a.CAPACIDADEENVIO,
							StatusFornecedor = a.STATUSFORNECEDOR,
							StatusOperacional = a.STATUSOPERACIONAL,
							Nome = a.NOME,
							Data = a.DATA,
							Atraso = (int)a.ATRASO,
							StatusEnvio = a.STATUSENVIO
						});


						var carteiraarq = dados.GroupBy(a => new { Carteira = a.Carteira, CarteiraID = a.CarteiraID, HoraLimite = a.HoraLimite },
							(a, b) => new CarteiraArquivos()
							{
								HoraLimite = a.HoraLimite == null ? TimeSpan.FromHours(22) : ((TimeSpan)a.HoraLimite).TotalHours == 0 ? TimeSpan.FromHours(22) : a.HoraLimite,
								Carteira = a.Carteira,
								CarteiraID = (int)a.CarteiraID,
								Arquivos = b.GroupBy(k => new { k.Arquivo, k.DataCadastro, k.ArquivoID }, (m, n) => new Arquivos()
								{
									FornecedoresMin = n.GroupBy(k => k.FornecedorID, (k, o) => new FornecedorMinModel()
									{
										FornecedorID = k,
										EntregaTime = TimeSpan.FromSeconds(o.Where(option => option.StatusEnvio == 2).Sum(option => option.Atraso)),
										Capacidade = o.ElementAt(0).Capacidade,
										StatusOperacional = ((StatusOperacionalFornecedorEnum)Enum.Parse(typeof(StatusOperacionalFornecedorEnum), ((byte)o.ElementAt(0).StatusOperacional).ToString())),
										Agendados = o.Where(option => option.StatusEnvio == 0).Sum(option => option.Quantidade),
										Entregues = o.Where(option => option.StatusEnvio == 2).Sum(option => option.Quantidade),
										Nome = o.ElementAt(0).Nome,
										Distribuicao = Math.Round((decimal)o.Sum(option => option.Quantidade) / (decimal)n.Sum(option => option.Quantidade) * 100, 0)
									}),
									Quantidade = n.Sum(option => option.Quantidade),
									Arquivo = m.Arquivo,
									ArquivoID = m.ArquivoID,
									DataCadastro = m.DataCadastro,
									Usuario = n.ElementAt(0).Usuario,
									Lotes = n
									.OrderBy(option => option.DataEnviar)
									.GroupBy(option => new { DataEnviar = option.DataEnviar },
									(j, l) => new CampanhaGridLotesModel()
									{

										DataEnviar = j.DataEnviar,
										Data = Convert.ToDateTime(j.DataEnviar).ToString("dd/MM/yyyy"),
										Hora = Convert.ToDateTime(j.DataEnviar).ToString("HH:mm"),
										Intervalos = n.Count(),
										Quantidade = l.Sum(option => option.Quantidade),
										StatusLista = new List<StatusValor>() {
											new StatusValor(StatusEnvioEnums.AGENDADOS.ToString(), l.Where(option=>option.StatusEnvio==0).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIANDO.ToString(), l.Where(option=>option.StatusEnvio==1).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIADOS.ToString(), l.Where(option=>option.StatusEnvio==2).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ERROS.ToString(), l.Where(option=>option.StatusEnvio==3).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.SUSPENSOS.ToString(), l.Where(option=>option.StatusEnvio==4).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.CANCELADOS.ToString(), l.Where(option=>option.StatusEnvio==5).Sum(option=>option.Quantidade))
										},
										Status = new List<StatusValor>() {
											new StatusValor(StatusEnvioEnums.AGENDADOS.ToString(), l.Where(option=>option.StatusEnvio==0).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIANDO.ToString(), l.Where(option=>option.StatusEnvio==1).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIADOS.ToString(), l.Where(option=>option.StatusEnvio==2).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ERROS.ToString(), l.Where(option=>option.StatusEnvio==3).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.SUSPENSOS.ToString(), l.Where(option=>option.StatusEnvio==4).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.CANCELADOS.ToString(), l.Where(option=>option.StatusEnvio==5).Sum(option=>option.Quantidade))
										}.OrderBy(option => option.Quantidade).Last().Status
									})
									.Select((j, i) => new CampanhaGridLotesModel()
									{
										DataEnviar = j.DataEnviar,
										Data = j.Data,
										Hora = j.Hora,
										Intervalos = j.Intervalos,
										Quantidade = j.Quantidade,
										StatusLista = j.StatusLista,
										Status = j.Status,
										Lote = i + 1
									})
								}).ToList()
							}).Select(m => new CarteiraArquivos() { HoraLimite = m.HoraLimite, Carteira = m.Carteira, CarteiraID = m.CarteiraID, Arquivos = m.Arquivos.OrderBy(option => option.Arquivo).ToList() })
							.OrderBy(option => option.Carteira)
							.ToList();

						return new MonitoriaModel()
						{
							CartArquivos = carteiraarq,
							Cliente = new ClienteModel() { ClienteID = c },
							TotalArquivos = dados.GroupBy(a => a.Arquivo).Count(),
							TotalRegistros = dados.Sum(m => m.Quantidade)

						};

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


		public async Task<IEnumerable<CampanhaGridLotesModel>> RetornaLotes(MonitoriaModel m, int carteiraid, int arquivoid, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT DATAENVIAR, STATUSENVIO, FORNECEDORID, COUNT(CAMPANHAID) QUANTIDADE FROM CAMPANHAS WHERE CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND CLIENTEID=@ClienteID AND DATADIA=@DataInicial GROUP BY DATAENVIAR, STATUSENVIO, FORNECEDORID";

					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", m.DataInicial.HasValue ? m.DataInicial.Value.Date : DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);
					p.Add("ArquivoID", arquivoid, DbType.Int32, ParameterDirection.Input);


					if (u.HasValue)
					{
						query = @"SELECT DATAENVIAR, STATUSENVIO, FORNECEDORID, COUNT(CAMPANHAID) QUANTIDADE FROM CAMPANHAS WHERE CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND CLIENTEID=@ClienteID AND DATADIA=@DataInicial GROUP BY DATAENVIAR, STATUSENVIO, FORNECEDORID";
						p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
					}


					var result = await conn.QueryAsync<dynamic>(query, p);
					if (result != null)
					{
						var dados = result.Select(a => new
						{
							DataEnviar = a.DATAENVIAR,
							FornecedorID = Convert.ToInt32(a.FORNECEDORID),
							StatusEnvio = Convert.ToByte(a.STATUSENVIO),
							Quantidade = Convert.ToInt32(a.QUANTIDADE)
						});

						int intervalos = dados.GroupBy(a => a.DataEnviar).Count();

						return dados.GroupBy(a => a.DataEnviar, (a, l) => new CampanhaGridLotesModel
						{
							DataEnviar = Convert.ToDateTime(a),
							Data = Convert.ToDateTime(a).ToString("dd/MM/yyyy"),
							Hora = Convert.ToDateTime(a).ToString("HH:mm"),
							Intervalos = intervalos,
							Quantidade = l.Sum(option => option.Quantidade),
							StatusLista = new List<StatusValor>() {
											new StatusValor(StatusEnvioEnums.AGENDADOS.ToString(), l.Where(option=>option.StatusEnvio==0).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIANDO.ToString(), l.Where(option=>option.StatusEnvio==1).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIADOS.ToString(), l.Where(option=>option.StatusEnvio==2).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ERROS.ToString(), l.Where(option=>option.StatusEnvio==3).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.SUSPENSOS.ToString(), l.Where(option=>option.StatusEnvio==4).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.CANCELADOS.ToString(), l.Where(option=>option.StatusEnvio==5).Sum(option=>option.Quantidade))
										},
							Status = new List<StatusValor>() {
											new StatusValor(StatusEnvioEnums.AGENDADOS.ToString(), l.Where(option=>option.StatusEnvio==0).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIANDO.ToString(), l.Where(option=>option.StatusEnvio==1).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ENVIADOS.ToString(), l.Where(option=>option.StatusEnvio==2).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.ERROS.ToString(), l.Where(option=>option.StatusEnvio==3).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.SUSPENSOS.ToString(), l.Where(option=>option.StatusEnvio==4).Sum(option=>option.Quantidade)),
											new StatusValor(StatusEnvioEnums.CANCELADOS.ToString(), l.Where(option=>option.StatusEnvio==5).Sum(option=>option.Quantidade))
										}.OrderBy(option => option.Quantidade).Last().Status
						})
						.Select((a, i) => new CampanhaGridLotesModel()
						{
							DataEnviar = a.DataEnviar,
							Intervalos = a.Intervalos,
							Quantidade = a.Quantidade,
							Data = a.Data,
							Hora = a.Hora,
							Status = a.Status,
							StatusLista = a.StatusLista,
							Lote = i + 1
						})
						.OrderBy(a => a.DataEnviar);

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



		public async Task<IEnumerable<RetornoModel>> RetornoClientes(ConsolidadoModel co, int c, int? u)
		{

			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, CODIGO FROM CAMPANHA_RETORNO CR 
									LEFT JOIN CAMPANHAS C  ON CR.CAMPANHAID=C.CAMPANHAID
									LEFT JOIN CAMPANHAS_ARQUIVOS CA  ON C.ARQUIVOID=CA.ARQUIVOID
									WHERE CR.DATARETORNO BETWEEN @DataInicial  AND @DataFinal   AND  C.CLIENTEID=@ClienteID
									GROUP BY CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, C.CAMPANHAID, CODIGO";

					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.DateTime, ParameterDirection.Input);

					if (u.HasValue)
					{
						query = @"SELECT CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, CODIGO FROM CAMPANHA_RETORNO CR 
									LEFT JOIN CAMPANHAS C  ON CR.CAMPANHAID=C.CAMPANHAID
									LEFT JOIN CAMPANHAS_ARQUIVOS CA  ON C.ARQUIVOID=CA.ARQUIVOID
									WHERE CR.DATARETORNO BETWEEN @DataInicial  AND DataFinal   AND  C.CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID
									GROUP BY CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, C.CAMPANHAID, CODIGO";

						p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
					}


					var result = await conn.QueryAsync<dynamic>(query, p);
					if (result != null)
						return result.Select(a => new RetornoModel()
						{
							RetornoCliente = a.RETORNO,
							IDCliente = a.IDCLIENTE,
							Celular = a.CELULAR,
							Texto = a.TEXTO,
							Arquivo = a.ARQUIVO,
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

		/// <summary>
		/// Suspende, Cancela e Reativa campanhas
		/// </summary>
		/// <param name="campanhas">lista contendo o grid de dataenviar</param>
		/// <param name="arquivoid">id do arquivo pra executar</param>
		/// <param name="carteiraid">id da carteira pra executar</param>
		/// <param name="statusenvio">status a ser modificado</param>
		/// <param name="c">id do cliente</param>
		/// <param name="u">id do usuário</param>
		/// <returns></returns>
		public async Task<int> ActionsCampanha(IEnumerable<CampanhaGridLotesModel> campanhas, int arquivoid, int carteiraid, byte statusenvio, int c, int? u, ActionCamp camp)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				string query = null;

				switch (camp)
				{
					case ActionCamp.CANCELAR:
						query = "UPDATE CAMPANHAS  SET STATUSENVIO=@StatusEnvio WHERE DATAENVIAR=@DataEnviar AND CLIENTEID=@ClienteID AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID AND STATUSENVIO IN(0,4,1)";
						break;
					case ActionCamp.SUSPENDER:
						query = "UPDATE CAMPANHAS  SET STATUSENVIO=@StatusEnvio WHERE DATAENVIAR=@DataEnviar AND CLIENTEID=@ClienteID AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID AND STATUSENVIO IN(0,1)";
						break;
					case ActionCamp.REAGENDAR:
						query = "UPDATE CAMPANHAS SET DATAENVIAR=@DataEnviar, DATADIA=CAST(@DataEnviar AS DATE), STATUSENVIO=IIF(@DataEnviar>@DataAtual,0,1) WHERE DATAENVIAR=@DataEnviarOld AND CLIENTEID=@ClienteID AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID AND STATUSENVIO IN(0,4,1)";
						break;
					default:
						break;
				}

				try
				{
					int retornocommits = 0;

					var sb = new StringBuilder();
					foreach (var item in campanhas.GroupBy(a => a.DataEnviar, (a, b) => a))
						sb.AppendFormat("{0:yyyy-MM-dd HH:mm},", item);

					if (camp == ActionCamp.REAGENDAR)
					{
						retornocommits = await conn.ExecuteAsync(query, campanhas.Select(a => new
						{
							DataEnviar = a.DataEnviar,
							ArquivoID = arquivoid,
							CarteiraID = carteiraid,
							ClienteID = c,
							DataEnviarOld = a.DataEnviarOld,
							DataAtual = DateTime.Now,
						}), transaction: tran, commandTimeout: 888);
					}
					else if (camp == ActionCamp.REATIVAR)
					{
						var p = new DynamicParameters();
						p.Add("ArquivoID", arquivoid, DbType.Int32, ParameterDirection.Input);
						p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
						p.Add("CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);
						p.Add("DataEnviar", sb.ToString().TrimEnd(','), DbType.String, ParameterDirection.Input);

						query = @"SELECT COUNT(CAMPANHAID) FROM CAMPANHAS C JOIN string_split(@DataEnviar,',') T ON C.DATAENVIAR=CAST(T.value AS SMALLDATETIME) WHERE CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND CLIENTEID=@ClienteID AND STATUSENVIO=4";



						var registros = await conn.QuerySingleAsync<int>(query, p, transaction: tran, commandTimeout: 888);

						var carteira = await new DALCarteira().BuscarItemByIDAsync(new CarteiraModel() { CarteiraID = carteiraid, Cliente = new ClienteModel() { ClienteID = c } }, u);
						//
						if (carteira.Limite < carteira.ConsumoPeriodo)
							throw new Exception($"Carteira {carteira.Carteira} sem limite disponível");

						if ((carteira.Limite - carteira.ConsumoPeriodo) < registros)
							throw new Exception($"Saldo indisponível para enviar {registros.ToString("N0")} registro(s)");

						retornocommits = await conn.ExecuteAsync(@"UPDATE CAMPANHAS  SET STATUSENVIO=IIF(@DataEnviarNova>@DataAtual,0,1), DATAENVIAR=@DataEnviarNova, DATADIA=@DataEnviarNova WHERE DATAENVIAR=@DataEnviar AND CLIENTEID=@ClienteID AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID AND STATUSENVIO=4",
																							campanhas.Select(a => new
																							{
																								DataEnviar = a.DataEnviar,
																								ArquivoID = arquivoid,
																								CarteiraID = carteiraid,
																								ClienteID = c,
																								DataAtual = DateTime.Now,
																								DataEnviarNova = a.DataEnviar.Date < DateTime.Now.Date ? DateTime.Now.DateTimeMinuteInterval().AddMinutes(5) : a.DataEnviar
																							}), transaction: tran, commandTimeout: 888);
					}
					else
					{
						retornocommits = await conn.ExecuteAsync(query, campanhas.Select(a => new
						{
							DataEnviar = a.DataEnviar,
							ArquivoID = arquivoid,
							CarteiraID = carteiraid,
							ClienteID = c,
							StatusEnvio = statusenvio,
							DataAtual = DateTime.Now,
							DataEnviarNova = a.DataEnviar.Date < DateTime.Now.Date ? DateTime.Now.DateTimeMinuteInterval() : a.DataEnviar
						}), transaction: tran, commandTimeout: 888);
					}

					var data = campanhas.Select(a => a.DataEnviar.ToString("yyyy-MM-dd HH:mm")).Aggregate((a, b) => $"{a},{b}");


					await conn.ExecuteAsync("DELETE FROM FORNECEDOR_CAMPANHAS WHERE CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND CLIENTEID=@ClienteID AND CAST(DATAENVIAR AS DATE)=@DataDia", new
					{
						DataDia = campanhas.ElementAt(0).DataEnviar.Date,
						CarteiraID = carteiraid,
						ArquivoID = arquivoid,
						ClienteID = c,
					}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);


					await conn.ExecuteAsync(@"DECLARE @TMP TABLE(QUANTIDADE INT, CARTEIRAID INT, ARQUIVOID INT, FORNECEDORID INT, DATAENVIAR SMALLDATETIME, CLIENTEID INT, STATUSENVIO TINYINT, USUARIOID INT);
												WITH ENVIADAS AS(SELECT CAMPANHAID, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, USUARIOID FROM CAMPANHAS WHERE CLIENTEID=@ClienteID AND DATADIA=@DataDia AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID)
												INSERT @TMP												
												SELECT COUNT(CAMPANHAID) QUANTIDADE, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, @ClienteID, STATUSENVIO, USUARIOID  FROM ENVIADAS GROUP BY CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, USUARIOID
												INSERT FORNECEDOR_CAMPANHAS
												SELECT DATAENVIAR, FORNECEDORID, CLIENTEID, USUARIOID,CARTEIRAID, ARQUIVOID, IIF(T.STATUSENVIO=4 OR T.STATUSENVIO=5,0, T.QUANTIDADE) FROM @TMP T", new
					{
						DataDia = campanhas.ElementAt(0).DataEnviar.Date,
						CarteiraID = carteiraid,
						ArquivoID = arquivoid,
						ClienteID = c,
					}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);


					tran.Commit();

					return retornocommits;

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



		public async Task<int> ActionsLoteCampanha(byte statusenvio, byte statusenvioold, int c, int? u, ActionCamp camp)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				string query = null;

				switch (camp)
				{
					case ActionCamp.CANCELAR:
					case ActionCamp.SUSPENDER:
						query = "UPDATE CAMPANHAS  SET STATUSENVIO=@StatusEnvio WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND STATUSENVIO=@StatusEnvioOld";
						if (u.HasValue)
							query = "UPDATE C SET STATUSENVIO=@StatusEnvio FROM CAMPANHAS C JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND STATUSENVIO=@StatusEnvioOld";
						break;
					case ActionCamp.REATIVAR:
						query = "UPDATE CAMPANHAS  SET STATUSENVIO=IIF(DATAENVIAR>@DataDia,0,1) WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND STATUSENVIO=4";
						if (u.HasValue)
							query = "UPDATE C  SET STATUSENVIO=IIF(DATAENVIAR>@DataDia,0,1) FROM CAMPANHAS C JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND STATUSENVIO=4";
						break;
					default:
						break;
				}

				try
				{
					int retornocommits = 0;

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("StatusEnvio", statusenvio, DbType.Byte, ParameterDirection.Input);
					p.Add("StatusEnvioOld", statusenvioold, DbType.Byte, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					retornocommits = await conn.ExecuteAsync(query, p, transaction: tran, commandTimeout: 8889);

					await conn.ExecuteAsync(@"DECLARE @TMP TABLE(QUANTIDADE INT, CARTEIRAID INT, ARQUIVOID INT, FORNECEDORID INT, DATAENVIAR SMALLDATETIME, CLIENTEID INT, STATUSENVIO TINYINT);
												WITH ENVIADAS AS(SELECT CAMPANHAID, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO FROM CAMPANHAS WHERE CLIENTEID=@ClienteID AND DATADIA=@DataDia)
												INSERT @TMP
												SELECT COUNT(CAMPANHAID) QUANTIDADE, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, @ClienteID, STATUSENVIO  FROM ENVIADAS GROUP BY CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO
												UPDATE FC SET QUANTIDADE=IIF(T.STATUSENVIO=4 OR T.STATUSENVIO=5,0, T.QUANTIDADE) FROM FORNECEDOR_CAMPANHAS FC JOIN @TMP T ON FC.ARQUIVOID=T.ARQUIVOID AND FC.CARTEIRAID=T.CARTEIRAID AND FC.CLIENTEID=T.CLIENTEID AND FC.FORNECEDORID=T.FORNECEDORID AND FC.DATAENVIAR=T.DATAENVIAR", new
					{
						DataDia = DateTime.Now.Date,
						ClienteID = c,
					}, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					tran.Commit();

					return retornocommits;

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


		public async Task EnviarSMSApi(IEnumerable<CampanhaModel> t, IEnumerable<CampanhaModel> i, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					//var IsClientPosPago = t.ElementAt(0).Cliente.PosPago;

					//if (!IsClientPosPago)
					//	await conn.ExecuteAsync(@"UPDATE C SET SALDO=SALDO-@Quantidade FROM CLIENTES C WHERE CLIENTEID=@ClienteID",
					//		new { Quantidade = t.Count(), ClienteID = c },
					//		transaction: tran,
					//		commandTimeout: 888);
					if (u.HasValue)
					{
						var affected = await new DALUsuario().AlteraSaldoUsuarioEnvio(new UsuarioModel() { UsuarioID = u.Value }, c, t.Count(), tran, conn);
						if (affected < 0)
							throw new Exception("Houve um erro no abatimento do saldo do usuário");
					}




					await conn.ExecuteAsync(@"CREATE TABLE #CAMPANHAS(
													CAMPANHAID bigint NOT NULL PRIMARY KEY IDENTITY,
													CARTEIRAID int NOT NULL,
													DATAENVIAR smalldatetime NOT NULL,
													DATA datetime NOT NULL,
													OPERADORAID tinyint NOT NULL,
													CLIENTEID int NOT NULL,
													USUARIOID int NULL,
													FORNECEDORID int NOT NULL,
													IDCLIENTE varchar(100) NULL,
													STATUSENVIO tinyint NOT NULL,
													TIPOCAMPANHAID int NULL,
													TIPOSMS tinyint NOT NULL,
													CELULAR numeric(12, 0) NOT NULL,
													TEXTO varchar(320) NOT NULL,
													DATADIA date NOT NULL);

												CREATE STATISTICS s_CARTEIRAID ON #CAMPANHAS (CARTEIRAID);
												CREATE STATISTICS s_DATAENVIAR ON #CAMPANHAS (DATAENVIAR);
												CREATE STATISTICS s_CELULAR ON #CAMPANHAS (CELULAR);
												CREATE STATISTICS s_DATA ON #CAMPANHAS (DATA);
												CREATE STATISTICS s_OPERADORAID ON #CAMPANHAS (OPERADORAID);
												CREATE STATISTICS s_CLIENTEID ON #CAMPANHAS (CLIENTEID);
												CREATE STATISTICS s_USUARIOID ON #CAMPANHAS (USUARIOID);
												CREATE STATISTICS s_FORNECEDORID ON #CAMPANHAS (FORNECEDORID);
												CREATE STATISTICS s_IDCLIENTE ON #CAMPANHAS (IDCLIENTE);
												CREATE STATISTICS s_STATUSENVIO ON #CAMPANHAS (STATUSENVIO);
												CREATE STATISTICS s_TIPOCAMPANHAID ON #CAMPANHAS (TIPOCAMPANHAID);
												CREATE STATISTICS s_TIPOSMS ON #CAMPANHAS (TIPOSMS);
												CREATE STATISTICS s_TEXTO ON #CAMPANHAS (TEXTO);
												CREATE STATISTICS s_DATADIA ON #CAMPANHAS (DATADIA);", transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"UPDATE STATISTICS #CAMPANHAS", transaction: tran, commandTimeout: 888);


					#region validos
					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{

						using (var reader = ObjectReader.Create(t.AsParallel().Select(m => new
						{
							CarteiraID = m.Carteira.CarteiraID,
							DataEnviar = m.DataEnviar,
							Data = m.Data,
							OperadoraID = (byte)m.Operadora,
							ClienteID = c,
							UsuarioID = u,
							FornecedorID = m.Fornecedor.FornecedorID,
							IDCliente = m.IDCliente,
							StatusEnvio = m.StatusEnvio,
							TipoCampanhaID = m.TipoCampanha == null ? null : (object)m.TipoCampanha.TipoCampanhaID,
							TipoSMS = m.TipoSMS,
							Celular = m.Celular,
							Texto = m.Texto,
							DataDia = m.DataEnviar
						}),
						"CarteiraID", "DataEnviar", "Data", "OperadoraID", "ClienteID", "UsuarioID", "FornecedorID", "IDCliente", "StatusEnvio", "TipoCampanhaID", "TipoSMS", "Celular", "Texto", "DataDia"))
						{
							bcp.DestinationTableName = "#CAMPANHAS";
							bcp.ColumnMappings.Add("CarteiraID", "CARTEIRAID");
							bcp.ColumnMappings.Add("DataEnviar", "DATAENVIAR");
							bcp.ColumnMappings.Add("Data", "DATA");
							bcp.ColumnMappings.Add("OperadoraID", "OPERADORAID");
							bcp.ColumnMappings.Add("ClienteID", "CLIENTEID");
							bcp.ColumnMappings.Add("UsuarioID", "USUARIOID");
							bcp.ColumnMappings.Add("FornecedorID", "FORNECEDORID");
							bcp.ColumnMappings.Add("IDCliente", "IDCLIENTE");
							bcp.ColumnMappings.Add("StatusEnvio", "STATUSENVIO");
							bcp.ColumnMappings.Add("TipoCampanhaID", "TIPOCAMPANHAID");
							bcp.ColumnMappings.Add("TipoSMS", "TIPOSMS");
							bcp.ColumnMappings.Add("Celular", "CELULAR");
							bcp.ColumnMappings.Add("Texto", "TEXTO");
							bcp.ColumnMappings.Add("DataDia", "DATADIA");
							bcp.BulkCopyTimeout = 8888;
							bcp.EnableStreaming = true;

							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}
					#endregion

					#region Inválidos
					if (i.Any())
						using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
						{
							using (var reader = ObjectReader.Create(i.Select(m => new
							{
								CarteiraID = m.Carteira.CarteiraID,
								DataEnviar = m.DataEnviar,
								ClienteID = c,
								UsuarioID = u,
								IDCliente = m.IDCliente,
								Celular = m.Celular,
								Texto = m.Texto.Length <= 300 ? m.Texto : m.Texto.Substring(0, 299),
								TipoInvalido = (byte)m.TipoInvalido
							}),
							"CarteiraID", "DataEnviar", "ClienteID", "UsuarioID", "IDCliente", "Celular", "Texto", "TipoInvalido"))
							{
								bcp.DestinationTableName = "CELULARES_INVALIDOS";
								bcp.ColumnMappings.Add("CarteiraID", "CARTEIRAID");
								bcp.ColumnMappings.Add("DataEnviar", "DATAENVIAR");
								bcp.ColumnMappings.Add("ClienteID", "CLIENTEID");
								bcp.ColumnMappings.Add("UsuarioID", "USUARIOID");
								bcp.ColumnMappings.Add("IDCliente", "IDCLIENTE");
								bcp.ColumnMappings.Add("Celular", "CELULAR");
								bcp.ColumnMappings.Add("Texto", "MENSAGEM");
								bcp.ColumnMappings.Add("TipoInvalido", "TIPOINVALIDO");
								bcp.EnableStreaming = true;
								bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
								await bcp.WriteToServerAsync(reader);
								bcp.Close();
							}
						}
					#endregion


					await conn.ExecuteAsync(@"UPDATE C SET OPERADORAID=P.OPERADORAID FROM #CAMPANHAS C JOIN PORTABILIDADE P ON C.CELULAR=P.NUMERO", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync(@"INSERT INTO CAMPANHAS (CARTEIRAID, DATAENVIAR, DATA, OPERADORAID, CLIENTEID, USUARIOID, FORNECEDORID, IDCLIENTE, STATUSENVIO, TIPOCAMPANHAID, TIPOSMS, CELULAR, DATADIA,TEXTO)
																SELECT CARTEIRAID, DATAENVIAR, DATA, OPERADORAID, CLIENTEID, USUARIOID, FORNECEDORID, IDCLIENTE, STATUSENVIO, TIPOCAMPANHAID, TIPOSMS, CELULAR, DATADIA, TEXTO FROM #CAMPANHAS", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);


					await conn.ExecuteAsync(@"INSERT INTO FORNECEDOR_CAMPANHAS SELECT DATAENVIAR, FORNECEDORID, CLIENTEID, USUARIOID, CARTEIRAID, NULL, COUNT(CAMPANHAID) FROM #CAMPANHAS GROUP BY DATAENVIAR, FORNECEDORID, CLIENTEID, USUARIOID, CARTEIRAID", transaction: tran, commandTimeout: 888);


					await conn.ExecuteAsync(@"DROP TABLE #CAMPANHAS", transaction: tran, commandTimeout: 888);

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

		public async Task AdicionaCampanhaAsync(List<CampanhaModel> validos, List<CampanhaModel> invalidos, int c, int? u)
		{
			var camps = new List<dynamic>() { };
			var campsInvalido = new List<dynamic>() { };


			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var arquivozip = validos.GroupBy(a => a.ArquivoZip, (a, b) => new { ArquivoZip = a, Campanhas = b }).Where(a => !string.IsNullOrEmpty(a.ArquivoZip));
					var arquivos = validos.Where(a => a.TipoInvalido == TiposInvalidosEnums.VALIDO).GroupBy(a => a.Arquivo.Arquivo, (a, b) => new { Arquivo = a, Campanhas = b }).Where(a => !string.IsNullOrEmpty(a.Arquivo));

					if (arquivozip.Any())
					{

						int arquivoID = 0;

						foreach (var a in arquivozip.Where(k => !string.IsNullOrEmpty(k.ArquivoZip)))
						{
							var p = new DynamicParameters();
							p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
							p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
							p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
							p.Add("Arquivo", a.ArquivoZip, DbType.String, ParameterDirection.Input);
							p.Add("ArquivoID", DbType.Int32, direction: ParameterDirection.Output);

							await conn.ExecuteAsync("INSERT INTO [dbo].[CAMPANHAS_ARQUIVOS]([ARQUIVO],[CLIENTEID],[USUARIOID],[DATA]) VALUES(@Arquivo, @ClienteID, @UsuarioID, @Data);SELECT @ArquivoID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

							arquivoID = p.Get<int>("ArquivoID");

							camps.AddRange(a.Campanhas.Where(m => m.TipoInvalido == TiposInvalidosEnums.VALIDO).Select(m => new
							{
								CarteiraID = m.Carteira.CarteiraID,
								DataEnviar = m.DataEnviar,
								Data = m.Data,
								OperadoraID = (byte)m.Operadora,
								ClienteID = c,
								UsuarioID = u,
								FornecedorID = m.Fornecedor.FornecedorID,
								IDCliente = m.IDCliente,
								StatusEnvio = m.StatusEnvio,
								TipoCampanhaID = m.TipoCampanha.TipoCampanhaID,
								TipoSMS = m.TipoSMS,
								Celular = m.Celular,
								Texto = m.Texto,
								ArquivoID = arquivoID,
								DataDia = m.DataEnviar.Date
							}));
						}

						await conn.ExecuteAsync("INSERT INTO [dbo].[CAMPANHAS_ARQUIVOS_FILESZIP] (ARQUIVO, ARQUIVOID) VALUES (@Arquivo, @ArquivoID)", arquivos.Select(k => new { Arquivo = k.Arquivo, ArquivoID = arquivoID }), transaction: tran);
					}
					else if (arquivos.Any())
					{
						foreach (var a in arquivos)
						{
							var p = new DynamicParameters();
							p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
							p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
							p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
							p.Add("Arquivo", a.Arquivo, DbType.String, ParameterDirection.Input);
							p.Add("ArquivoID", DbType.Int32, direction: ParameterDirection.Output);


							await conn.ExecuteAsync("INSERT INTO [dbo].[CAMPANHAS_ARQUIVOS]([ARQUIVO],[CLIENTEID],[USUARIOID],[DATA]) VALUES(@Arquivo, @ClienteID, @UsuarioID, @Data);SELECT @ArquivoID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

							int arquivoID = p.Get<int>("ArquivoID");

							camps.AddRange(a.Campanhas.Where(m => m.TipoInvalido == TiposInvalidosEnums.VALIDO).Select(m => new
							{
								CarteiraID = m.Carteira.CarteiraID,
								DataEnviar = m.DataEnviar,
								Data = m.Data,
								OperadoraID = (byte)m.Operadora,
								ClienteID = c,
								UsuarioID = u,
								FornecedorID = m.Fornecedor.FornecedorID,
								IDCliente = m.IDCliente,
								StatusEnvio = m.StatusEnvio,
								TipoCampanhaID = m.TipoCampanha.TipoCampanhaID,
								TipoSMS = m.TipoSMS,
								Celular = m.Celular,
								Texto = m.Texto,
								ArquivoID = arquivoID,
								DataDia = m.DataEnviar.Date
							}));
						}
					}
					else //não houve arquivo
						camps.AddRange(validos.Select(m => new
						{
							CarteiraID = m.Carteira.CarteiraID,
							DataEnviar = m.DataEnviar,
							Data = m.Data,
							OperadoraID = (byte)m.Operadora,
							ClienteID = c,
							UsuarioID = u,
							FornecedorID = m.Fornecedor.FornecedorID,
							IDCliente = m.IDCliente,
							StatusEnvio = m.StatusEnvio,
							TipoCampanhaID = m.TipoCampanha.TipoCampanhaID,
							TipoSMS = m.TipoSMS,
							Celular = m.Celular,
							Texto = m.Texto,
							ArquivoID = DBNull.Value,
							DataDia = m.DataEnviar.Date
						}));


					//var IsClientPosPago = t.ElementAt(0).Cliente.PosPago;

					//if (!IsClientPosPago)
					//	await conn.ExecuteAsync(@"UPDATE C SET SALDO=SALDO-@Quantidade FROM CLIENTES C WHERE CLIENTEID=@ClienteID",
					//		new { Quantidade = t.Count(), ClienteID = c },
					//		transaction: tran,
					//		commandTimeout: 888);

					if (u.HasValue)
					{
						var affected = await new DALUsuario().AlteraSaldoUsuarioEnvio(new UsuarioModel() { UsuarioID = u.Value }, c, validos.Count, tran, conn);
						if (affected < 0)
							throw new Exception("Houve um erro no abatimento do saldo do usuário");
					}



					await conn.ExecuteAsync(@"CREATE TABLE #CAMPANHAS(
													CAMPANHAID bigint NOT NULL PRIMARY KEY IDENTITY,
													CARTEIRAID int NOT NULL,
													DATAENVIAR smalldatetime NOT NULL,
													DATA datetime NOT NULL,
													OPERADORAID tinyint NOT NULL,
													CLIENTEID int NOT NULL,
													USUARIOID int NULL,
													FORNECEDORID int NOT NULL,
													IDCLIENTE varchar(100) NULL,
													STATUSENVIO tinyint NOT NULL,
													TIPOCAMPANHAID int NULL,
													TIPOSMS tinyint NOT NULL,
													CELULAR numeric(12, 0) NOT NULL,
													TEXTO varchar(320) NOT NULL,
													ARQUIVOID int NULL,
													DATADIA date NOT NULL);

												CREATE STATISTICS s_CARTEIRAID ON #CAMPANHAS (CARTEIRAID);
												CREATE STATISTICS s_DATAENVIAR ON #CAMPANHAS (DATAENVIAR);
												CREATE STATISTICS s_CELULAR ON #CAMPANHAS (CELULAR);
												CREATE STATISTICS s_DATA ON #CAMPANHAS (DATA);
												CREATE STATISTICS s_OPERADORAID ON #CAMPANHAS (OPERADORAID);
												CREATE STATISTICS s_CLIENTEID ON #CAMPANHAS (CLIENTEID);
												CREATE STATISTICS s_USUARIOID ON #CAMPANHAS (USUARIOID);
												CREATE STATISTICS s_FORNECEDORID ON #CAMPANHAS (FORNECEDORID);
												CREATE STATISTICS s_IDCLIENTE ON #CAMPANHAS (IDCLIENTE);
												CREATE STATISTICS s_STATUSENVIO ON #CAMPANHAS (STATUSENVIO);
												CREATE STATISTICS s_TIPOCAMPANHAID ON #CAMPANHAS (TIPOCAMPANHAID);
												CREATE STATISTICS s_TIPOSMS ON #CAMPANHAS (TIPOSMS);
												CREATE STATISTICS s_TEXTO ON #CAMPANHAS (TEXTO);
												CREATE STATISTICS s_ARQUIVOID ON #CAMPANHAS (ARQUIVOID);
												CREATE STATISTICS s_DATADIA ON #CAMPANHAS (DATADIA);", transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"UPDATE STATISTICS #CAMPANHAS", transaction: tran, commandTimeout: 888);


					#region validos
					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{

						using (var reader = ObjectReader.Create(camps.AsParallel().Select(m => new
						{
							CarteiraID = m.CarteiraID,
							DataEnviar = m.DataEnviar,
							Data = m.Data,
							OperadoraID = m.OperadoraID,
							ClienteID = m.ClienteID,
							UsuarioID = m.UsuarioID,
							FornecedorID = m.FornecedorID,
							IDCliente = m.IDCliente,
							StatusEnvio = m.StatusEnvio,
							TipoCampanhaID = m.TipoCampanhaID,
							TipoSMS = m.TipoSMS,
							Celular = m.Celular,
							Texto = m.Texto,
							ArquivoID = m.ArquivoID,
							DataDia = m.DataEnviar
						}),
						"CarteiraID", "DataEnviar", "Data", "OperadoraID", "ClienteID", "UsuarioID", "FornecedorID", "IDCliente", "StatusEnvio", "TipoCampanhaID", "TipoSMS", "Celular", "Texto", "ArquivoID", "DataDia"))
						{
							bcp.DestinationTableName = "#CAMPANHAS";
							bcp.ColumnMappings.Add("CarteiraID", "CARTEIRAID");
							bcp.ColumnMappings.Add("DataEnviar", "DATAENVIAR");
							bcp.ColumnMappings.Add("Data", "DATA");
							bcp.ColumnMappings.Add("OperadoraID", "OPERADORAID");
							bcp.ColumnMappings.Add("ClienteID", "CLIENTEID");
							bcp.ColumnMappings.Add("UsuarioID", "USUARIOID");
							bcp.ColumnMappings.Add("FornecedorID", "FORNECEDORID");
							bcp.ColumnMappings.Add("IDCliente", "IDCLIENTE");
							bcp.ColumnMappings.Add("StatusEnvio", "STATUSENVIO");
							bcp.ColumnMappings.Add("TipoCampanhaID", "TIPOCAMPANHAID");
							bcp.ColumnMappings.Add("TipoSMS", "TIPOSMS");
							bcp.ColumnMappings.Add("Celular", "CELULAR");
							bcp.ColumnMappings.Add("Texto", "TEXTO");
							bcp.ColumnMappings.Add("ArquivoID", "ARQUIVOID");
							bcp.ColumnMappings.Add("DataDia", "DATADIA");
							bcp.BulkCopyTimeout = 8888;
							bcp.EnableStreaming = true;

							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}
					#endregion

					#region Inválidos
					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(invalidos.Where(m => m.TipoInvalido != TiposInvalidosEnums.LEIAUTEINVALIDO).Select(m => new
						{
							CarteiraID = m.Carteira.CarteiraID,
							DataEnviar = m.DataEnviar,
							ClienteID = c,
							UsuarioID = u,
							IDCliente = m.IDCliente,
							Celular = m.Celular,
							Texto = m.Texto.Length <= 300 ? m.Texto : m.Texto.Substring(0, 299),
							Arquivo = m.Arquivo.Arquivo,
							TipoInvalido = (byte)m.TipoInvalido
						}),
						"CarteiraID", "DataEnviar", "ClienteID", "UsuarioID", "IDCliente", "Celular", "Texto", "Arquivo", "TipoInvalido"))
						{
							bcp.DestinationTableName = "CELULARES_INVALIDOS";
							bcp.ColumnMappings.Add("CarteiraID", "CARTEIRAID");
							bcp.ColumnMappings.Add("DataEnviar", "DATAENVIAR");
							bcp.ColumnMappings.Add("ClienteID", "CLIENTEID");
							bcp.ColumnMappings.Add("UsuarioID", "USUARIOID");
							bcp.ColumnMappings.Add("IDCliente", "IDCLIENTE");
							bcp.ColumnMappings.Add("Celular", "CELULAR");
							bcp.ColumnMappings.Add("Texto", "MENSAGEM");
							bcp.ColumnMappings.Add("Arquivo", "ARQUIVO");
							bcp.ColumnMappings.Add("TipoInvalido", "TIPOINVALIDO");
							bcp.EnableStreaming = true;
							bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}
					#endregion

					if (invalidos.Any(k => k.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO))
					{
						//var _inv = invalidos.Where(k => k.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).ToList();

						var arquivoForaPadrao = invalidos.Where(k => k.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).GroupBy(k => new
						{
							Arquivo = k.Arquivo.Arquivo,
							CarteiraID = k.Carteira.CarteiraID,
							DataDia = k.DataDia,
							ClienteID = c,
							UsuarioID = u
						}, (k, l) => new CampanhaModel()
						{
							Arquivo = new ArquivoCampanhaModel() { Arquivo = k.Arquivo },
							CarteiraID = k.CarteiraID,
							Cliente = new ClienteModel() { ClienteID = c },
							Usuario = u.HasValue ? new UsuarioModel() { UsuarioID = u.Value } : null,
							Quantidade = l.Count(),
							DataDia = k.DataDia,
							TipoInvalido = TiposInvalidosEnums.LEIAUTEINVALIDO
						}).ToList();

						await conn.ExecuteAsync(@"INSERT INTO [dbo].[CELULARES_INVALIDOS_CONSOLIDADO] (TIPOINVALIDO, CLIENTEID, USUARIOID, QUANTIDADE, CARTEIRAID, ARQUIVO, DATADIA) VALUES (@TIPOINVALIDO, @CLIENTEID, @USUARIOID, @QUANTIDADE, @CARTEIRAID, @ARQUIVO, @DATADIA)", arquivoForaPadrao.Select(k => new
						{
							TipoInvalido = (byte)k.TipoInvalido,
							ClienteID = c,
							UsuarioID = u,
							Arquivo = k.Arquivo.Arquivo,
							CarteiraID = k.CarteiraID,
							Quantidade = k.Quantidade,
							DataDia = k.DataDia
						}), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
					}

					await conn.ExecuteAsync(@"INSERT INTO FORNECEDOR_CAMPANHAS SELECT DATAENVIAR, FORNECEDORID, CLIENTEID, USUARIOID, CARTEIRAID, ARQUIVOID, COUNT(CAMPANHAID) 
												FROM #CAMPANHAS GROUP BY DATAENVIAR, FORNECEDORID, CLIENTEID, USUARIOID, CARTEIRAID, ARQUIVOID", transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"UPDATE C SET OPERADORAID=P.OPERADORAID FROM #CAMPANHAS C JOIN HELPER.dbo.PORTABILIDADE P ON C.CELULAR=P.NUMERO", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync(@"INSERT INTO CAMPANHAS (CARTEIRAID, DATAENVIAR, DATA, OPERADORAID, CLIENTEID, USUARIOID, FORNECEDORID, IDCLIENTE, STATUSENVIO, TIPOCAMPANHAID, TIPOSMS, CELULAR, ARQUIVOID, DATADIA,TEXTO)
																SELECT CARTEIRAID, DATAENVIAR, DATA, OPERADORAID, CLIENTEID, USUARIOID, FORNECEDORID, IDCLIENTE, STATUSENVIO, TIPOCAMPANHAID, TIPOSMS, CELULAR, ARQUIVOID, DATADIA, TEXTO FROM #CAMPANHAS", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					await conn.ExecuteAsync(@"DROP TABLE #CAMPANHAS", transaction: tran, commandTimeout: 888);

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

		public Task AdicionarItensAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{

			throw new NotImplementedException();


		}

		public async Task CampanhasLoteNovo(IEnumerable<CampanhaModel> t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{

					await conn.ExecuteAsync(@"CREATE TABLE #CAMPANHAS(
													CODIGO INT IDENTITY(1,1),
													CAMPANHAID bigint,
													STATUSENVIO tinyint,
													DATAENVIOFORNECEDOR DATETIME);", transaction: tran);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(t.Select(m => new
						{
							CampanhaID = m.CampanhaID,
							StatusEnvio = m.StatusEnvio,
							DataEnviar = m.DataEnviar
						}),
						"CampanhaID", "StatusEnvio", "DataEnviar"))
						{
							bcp.DestinationTableName = "#CAMPANHAS";
							bcp.ColumnMappings.Add("CampanhaID", "CAMPANHAID");
							bcp.ColumnMappings.Add("StatusEnvio", "STATUSENVIO");
							bcp.ColumnMappings.Add("DataEnviar", "DATAENVIOFORNECEDOR");
							bcp.BulkCopyTimeout = 888;

							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

					await conn.ExecuteAsync("UPDATE C SET STATUSENVIO=CT.STATUSENVIO, DATAENVIOFORNECEDOR=CT.DATAENVIOFORNECEDOR FROM CAMPANHAS C JOIN #CAMPANHAS CT ON C.CAMPANHAID=CT.CAMPANHAID WHERE C.STATUSENVIO=1", transaction: tran, commandTimeout: 888);

					await conn.ExecuteAsync(@"DROP TABLE #CAMPANHAS", transaction: tran, commandTimeout: 888);

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


		/// <summary>
		/// Atualiza campanha em lote para reagendamento, suspensão e reativação
		/// </summary>
		/// <param name="t">lisa de campanhas</param>
		/// <param name="c">clienteid</param>
		/// <param name="u">usuaroid</param>
		/// <returns></returns>
		public async Task AtualizaItensAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("UPDATE CAMPANHAS SET DATAENVIAR=@DataEnviar, STATUSENVIO=@StatusEnvio WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND CARTEIRAID=@CarteiraID AND DATAENVIAR=@DataEnviarOriginal AND STATUSENVIO=@StatusEnvioOriginal AND ARQUIVOID=@ArquivoID",
						t.Select(a => new
						{
							ClienteID = c,
							UsuarioID = u,
							StatusEnvio = a.StatusEnvio,
							StatusEnvioOriginal = a.StatusEnvioOriginal,
							CarteiraID = a.Carteira.CarteiraID,
							DataEnviar = a.DataEnviarOriginal,
							DataEnviarOriginal = a.DataEnviarOriginal,
							ArquivoID = a.Arquivo.ArquivoID
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

		/// <summary>
		/// Após o envio da mensagem ao fornecedor, ele informa uma identifcação única contendo o ID dele
		/// </summary>
		/// <param name="t">lista de campanhas</param>
		/// <returns></returns>
		public async Task<int> AtualizaItensCampanhaEnviada(IEnumerable<CampanhaModel> t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				var tran = conn.BeginTransaction();

				try
				{
					var affecteds = await conn.ExecuteAsync("UPDATE CAMPANHAS SET STATUSENVIO=@StatusEnvio WHERE CAMPANHAID=@CampanhaID AND FORNECEDORID=@FornecedorID AND STATUSENVIO=1",
						t.Select(a => new
						{
							StatusEnvio = a.StatusEnvio,
							CampanhaID = a.CampanhaID,
							FornecedorID = a.Fornecedor.FornecedorID,
						}),
						transaction: tran,
						commandTimeout: 888);

					tran.Commit();

					return affecteds;
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
		/// atualiza o report e data da campanha
		/// </summary>
		/// <param name="t">lista de campanhas</param>
		/// <returns></returns>
		public async Task<int> AtualizaItensStatusReport(IEnumerable<CampanhaModel> t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					int itensafected = 0;
					itensafected = await conn.ExecuteAsync("UPDATE CAMPANHAS SET STATUSREPORT=@StatusReport, DATAREPORT=@DataReport WHERE CAMPANHAID=@CampanhaID AND FORNECEDORID=@FornecedorID",
						t.Select(a => new
						{
							StatusReport = (byte)a.StatusReport,
							DataReport = a.DataReport,
							CampanhaID = a.CampanhaID,
							FornecedorID = a.FornecedorID
						}), transaction: tran, commandTimeout: 888);

					tran.Commit();

					return itensafected;
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
		/// Troca um fornecedor pelo lote, carteira e arquivo
		/// </summary>
		/// <param name="t"></param>
		/// <param name="c"></param>
		/// <param name="u"></param>
		/// <returns></returns>
		public async Task AtualizaItensTrocaFornecedor(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("UPDATE CAMPANHAS SET FORNECEDORID=@FornecedorID WHERE DATAENVIAR=@DataEnviar AND CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID",
						t.Select(a => new
						{
							ClienteID = c,
							UsuarioID = u,
							CarteiraID = a.Carteira.CarteiraID,
							DataEnviar = a.DataEnviar,
							FornecedorID = a.Fornecedor.FornecedorID,
							ArquivoID = a.Arquivo.ArquivoID
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




		public Task<CampanhaModel> BuscarItemByIDAsync(CampanhaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaModel>> BuscarItensAsync(CampanhaModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<CampanhaModel>> DownloadCelularesInvalidos(ConsolidadoModel co, int c, int? u, byte tipoinvalido)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataEnviar, DbType.Date, ParameterDirection.Input);
					p.Add("Arquivo", co.Arquivo, DbType.String, ParameterDirection.Input);
					p.Add("TipoInvalido", tipoinvalido, DbType.Byte, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID, DbType.Int32, ParameterDirection.Input);

					string query = string.Format(@"SELECT CELULAR, IDCLIENTE, MENSAGEM, C.CARTEIRA, CI.TIPOINVALIDO, DATAENVIAR FROM CELULARES_INVALIDOS CI {0}
													JOIN CARTEIRAS C ON CI.CARTEIRAID=C.CARTEIRAID WHERE {1} CI.CLIENTEID=@ClienteID AND CAST(DATAENVIAR AS DATE)=@DataInicial AND CI.CARTEIRAID=@CarteiraID AND TIPOINVALIDO=@TipoInvalido",
													u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON CI.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty,
													!string.IsNullOrEmpty(co.Arquivo) ? "ARQUIVO=@Arquivo AND" : "ARQUIVO IS NULL AND");







					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new CampanhaModel()
						{
							Celular = a.CELULAR,
							IDCliente = a.IDCLIENTE,
							DataEnviar = a.DATAENVIAR,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							Texto = a.MENSAGEM,
							TipoInvalido = ((TiposInvalidosEnums)Enum.Parse(typeof(TiposInvalidosEnums), Convert.ToString((byte)a.TIPOINVALIDO))),
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

		public async Task<IEnumerable<CampanhaModel>> PesquisaByCelularAsync(CampanhaModel c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (c.PaginaAtual.HasValue)
					{
						if (c.PaginaAtual.Value == 0)
							c.PaginaAtual = 1;
					}
					else
						c.PaginaAtual = 1;

					var p = new DynamicParameters();
					p.Add("ClienteID", c.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Celular", c.Celulares.Select(a => a.ToString()).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);

					string query = string.Format(@"WITH ENVIADAS AS
									(
									    SELECT TEXTO, CELULAR, C.CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, STATUSREPORT, TIPOCAMPANHAID, IDCLIENTE, OPERADORAID, DDD, DATAREPORT FROM CAMPANHAS C {0}
									    JOIN string_split(@Celular, ',') T ON C.CELULAR=CAST(T.value AS numeric(12,0))
									    WHERE C.CLIENTEID=@ClienteID
									)
									SELECT TEXTO, CELULAR, STATUSENVIO, STATUSREPORT, CAT.CARTEIRA, CA.ARQUIVO, T.TIPOCAMPANHA, F.NOME FORNECEDOR, IDCLIENTE, OPERADORAID, U.REGIAO, U.UF, C.DATAENVIAR, C.DATAREPORT, C.DDD FROM ENVIADAS C {0}
										JOIN CARTEIRAS CAT ON C.CARTEIRAID=CAT.CARTEIRAID
										JOIN UFDDD U ON C.DDD=U.DDD
										JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
										LEFT JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
										LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID
                                        ORDER BY CELULAR ", c.Usuario != null ? " JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);

					var res = await conn.QueryAsync(query, p, commandTimeout: Util.TIMEOUTEXECUTE);

					if (res != null && res.Any())
					{
						var r = res.Select(a => new CampanhaModel()
						{
							IDCliente = a.IDCLIENTE,
							Texto = a.TEXTO,
							Celular = a.CELULAR,
							DataEnviar = a.DATAENVIAR,
							DataReport = a.DATAREPORT,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA },
							Regiao = a.REGIAO,
							UF = a.UF,
							DDD = (byte)a.DDD,
							Report = RetornaReport((byte)a.STATUSENVIO, a.STATUSREPORT),
							Arquivo = a.ARQUIVO != null ? new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO } : null,
							Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), ((byte)a.OPERADORAID).ToString())),
							Fornecedor = new FornecedorModel() { FornecedorNome = a.FORNECEDOR },
							Registros = res.Count(),
							Paginas = c.Registros == 0 ? 0 : res.Count() / c.Registros
						}).OrderBy(a => a.DataEnviar)
						.Skip((c.PaginaAtual.Value - 1) * c.Registros);

						if (c.Registros != 0)
							return r.Take(c.Registros);

						return r;
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

		public async Task<int> AdicionaRatinho(CampanhaModel c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("Celular", c.Celular, DbType.Decimal, ParameterDirection.Input);

					return await conn.ExecuteAsync(@"INSERT INTO RATINHOS (CELULAR, DATA) VALUES (@Celular, @Data)", p, commandTimeout: 888);

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

		public async Task<int> GravaRetornoRatinhos(CampanhaModel c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Data", c.Data, DbType.DateTime, ParameterDirection.Input);
					p.Add("Celular", c.Celular, DbType.Decimal, ParameterDirection.Input);
					p.Add("Texto", c.Texto, DbType.String, ParameterDirection.Input);

					return await conn.ExecuteAsync(@"UPDATE RATINHOS_RECEBIMENTO SET DATARECEBIMENTO=@Data WHERE CELULAR=@Celular AND TEXTO=@Texto", p, commandTimeout: 888);

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
		public async Task<IEnumerable<CampanhaModel>> DetalhadoCampanhas(CampanhaModel c, int clienteid, int? usuarioid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("CarteiraID", c.CarteiraID, DbType.Int32, ParameterDirection.Input);
					p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", usuarioid, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", c.DataInicial, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", c.DataFinal, DbType.DateTime, ParameterDirection.Input);


					string query = @"WITH ENVIADAS AS
											(
											SELECT TEXTO, CELULAR, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO, STATUSREPORT, TIPOCAMPANHAID, IDCLIENTE, OPERADORAID, DDD FROM CAMPANHAS C
											WHERE C.CLIENTEID=@ClienteID AND DATAENVIAR BETWEEN @DataInicial AND @DataFinal
											)
											SELECT TEXTO, CELULAR, STATUSENVIO, STATUSREPORT, CAT.CARTEIRA, CA.ARQUIVO, T.TIPOCAMPANHA, NOME FORNECEDOR, IDCLIENTE, OPERADORAID, U.REGIAO, U.UF FROM ENVIADAS C
												JOIN CARTEIRAS CAT ON C.CARTEIRAID=CAT.CARTEIRAID
												JOIN UFDDD U ON C.DDD=U.DDD
												JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
												LEFT JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
												LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID";

					if (usuarioid.HasValue)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "AND C.USUARIOID=@UsuarioID ");


					if (c.CarteiraID.HasValue)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "AND C.CARTEIRAID=@CarteiraID ");

					var res = await conn.QueryAsync(query, commandTimeout: Util.TIMEOUTEXECUTE);

					if (res == null && res.Any())
						return res.Select(a => new CampanhaModel()
						{
							IDCliente = a.IDCLIENTE,
							Texto = a.TEXTO,
							Celular = a.CELULAR,
							DataEnviar = a.DATAENVIAR,
							DataReport = a.DATAREPORT,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA },
							Regiao = a.REGIAO,
							UF = a.UF,
							DDD = a.DDD,
							StatusReport = RetornaReport(a.STATUSENVIO, a.STATUSREPORT),
							Arquivo = a.ARQUIVO != null ? new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO } : null,
							Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), ((byte)a.OPERADORAID).ToString())),
							Fornecedor = new FornecedorModel() { FornecedorNome = a.NOME }
						}).OrderBy(a => a.DataEnviar);



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
		public async Task<IEnumerable<CampanhaModel>> ObterTodosDataEnviar(DateTime dataIn, DateTime dataOut, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataIn", dataIn, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataOut", dataOut, DbType.DateTime, ParameterDirection.Input);


					string query = @"SELECT C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME FORNECEDOR, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME USUARIO, U.LOGINUSER FROM CAMPANHAS C 
			JOIN CARTEIRAS CA ON C.CARTEIRAID=CA.CARTEIRAID
			JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
			JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
			LEFT JOIN [dbo].[CAMPANHAS_ARQUIVOS] A ON C.ARQUIVOID=A.ARQUIVOID
			LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
			WHERE CLIENTEID=@ClienteID AND C.DATAENVIAR BETWEEN @DataIn AND @DataOut
			GROUP BY C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME, C.CAMPANHAID,  U.LOGINUSER";

					if (u.HasValue)
						query = @"SELECT C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME FORNECEDOR, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME USUARIO FROM CAMPANHAS C 
			JOIN CARTEIRAS CA ON C.CARTEIRAID=CA.CARTEIRAID
			JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
			JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
			LEFT JOIN [dbo].[CAMPANHAS_ARQUIVOS] A ON C.ARQUIVOID=A.ARQUIVOID
			LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
			WHERE CLIENTEID=@ClienteID AND C.DATAENVIAR BETWEEN @DataIn AND @DataOut AND USUARIOID=@UsuarioID
			GROUP BY C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME, C.CAMPANHAID, U.LOGINUSER";

					var result = (await conn.QueryAsync(query, p, commandTimeout: 888)).Select(a => new CampanhaModel()
					{
						Celular = a.CELULAR,
						DataEnviar = a.DATAENVIAR,
						StatusReport = Util.Report(a.STATUSENVIO, a.STATUSREPORT ?? new Nullable<byte>()),
						DataReport = a.DATAREOPORT ?? new Nullable<int>(),
						Fornecedor = new FornecedorModel() { Nome = a.NOME },
						Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OPERADORAID)),
						Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
						IDCliente = a.IDCLIENTE,
						TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA },
						Texto = a.TEXTO,
						Usuario = new UsuarioModel() { Nome = a.USUARIO, LoginUser = a.LOGINUSER }
					});


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

		public async Task<IEnumerable<CampanhaModel>> ObterTodosDataEnviarCarteira(IEnumerable<CarteiraModel> carteiras, DateTime dataIn, DateTime dataOut, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var sb = new StringBuilder();

					await carteiras.ToObservable().ForEachAsync(m => sb.AppendFormat("{0},", m.CarteiraID));

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataIn", dataIn, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataOut", dataOut, DbType.DateTime, ParameterDirection.Input);
					p.Add("Carteiras", sb.ToString().TrimEnd(','), DbType.String, ParameterDirection.Input);


					string query = @"SELECT C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME FORNECEDOR, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME USUARIO, U.LOGINUSER FROM CAMPANHAS C 
			JOIN dbo.CommaSeparatedListToSingleColumn(@Carteiras) CO ON C.CARTEIRAID=CAST(CO.Item AS INT)
			JOIN CARTEIRAS CA ON CAST(CO.Item AS INT)=CA.CARTEIRAID
			JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
			JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
			LEFT JOIN [dbo].[CAMPANHAS_ARQUIVOS] A ON C.ARQUIVOID=A.ARQUIVOID
			LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
			WHERE CLIENTEID=@ClienteID AND C.DATAENVIAR BETWEEN @DataIn AND @DataOut
			GROUP BY C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME, C.CAMPANHAID,  U.LOGINUSER";

					if (u.HasValue)
						query = @"SELECT C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME FORNECEDOR, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME USUARIO FROM CAMPANHAS C 
			JOIN dbo.CommaSeparatedListToSingleColumn(@Carteiras) CO ON C.CARTEIRAID=CAST(CO.Item AS INT)
			JOIN CARTEIRAS CA ON CAST(CO.Item AS INT)=CA.CARTEIRAID
			JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
			JOIN TIPOCAMPANHA T ON C.TIPOCAMPANHAID=T.CODIGO
			LEFT JOIN [dbo].[CAMPANHAS_ARQUIVOS] A ON C.ARQUIVOID=A.ARQUIVOID
			LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID
			WHERE CLIENTEID=@ClienteID AND C.DATAENVIAR BETWEEN @DataIn AND @DataOut AND USUARIOID=@UsuarioID
			GROUP BY C.CELULAR, C.DATAENVIAR, C.STATUSREPORT, C.DATAREPORT, F.NOME, C.OPERADORAID, CA.CARTEIRA, C.IDCLIENTE, C.STATUSENVIO, T.TIPOCAMPANHA, C.TEXTO, U.NOME, C.CAMPANHAID, U.LOGINUSER";

					var result = (await conn.QueryAsync(query, p, commandTimeout: 888)).Select(a => new CampanhaModel()
					{
						Celular = a.CELULAR,
						DataEnviar = a.DATAENVIAR,
						StatusReport = Util.Report(a.STATUSENVIO, a.STATUSREPORT ?? new Nullable<byte>()),
						DataReport = a.DATAREOPORT,
						Fornecedor = new FornecedorModel() { Nome = a.NOME },
						Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OPERADORAID)),
						Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
						IDCliente = a.IDCLIENTE,
						TipoCampanha = new TipoCampanhaModel() { TipoCampanha = a.TIPOCAMPANHA },
						Texto = a.TEXTO,
						Usuario = new UsuarioModel() { Nome = a.USUARIO, LoginUser = a.LOGINUSER }
					});


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

		public async Task<IEnumerable<CampanhaModel>> DownByStatusOnly(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					string query = @"SELECT ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, CANCELADA, DATAENVIAR FROM CAMPANHAS_CONSOLIDADO WHERE CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal";

					if (u.HasValue)
						query = query.Insert(query.LastIndexOf("AND DATADIA"), "AND C.USUARIOID=@UsuarioID ");


					var result = await conn.QueryAsync(new CommandDefinition(query, p, flags: CommandFlags.Pipelined, commandTimeout: 888));

					if (result != null)
						return result.Select(a => new CampanhaModel()
						{
							Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO },
							DataEnviar = a.DATAENVIAR,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							Texto = a.TEXTO,
							Celular = a.CELULAR,
							DataDia = a.DATADIA,
							IDCliente = a.IDCLIENTE,
							Fornecedor = new FornecedorModel() { Nome = a.NOME },
							Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OPERADORAID))
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


		public async Task<IEnumerable<CampanhaModel>> DownByStatus(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataEnviar", co.DataEnviar, DbType.DateTime, ParameterDirection.Input);
					p.Add("StatusEnvio", statusenvio, DbType.Byte, ParameterDirection.Input);
					p.Add("ArquivoID", co.ArquivoID.Value, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID.Value, DbType.Int32, ParameterDirection.Input);
					p.Add("StatusEnvio", statusenvio, DbType.Byte, ParameterDirection.Input);

					string query = @"SELECT C.TEXTO, CA.ARQUIVO, C.CELULAR, CAT.CARTEIRA, C.DATAENVIAR, C.DATADIA, FORNECEDORID, C.CARTEIRAID, C.ARQUIVOID, IDCLIENTE, OPERADORAID, F.NOME, C.FORNECEDORID FROM CAMPANHAS C WITH (INDEX([IX_CAMPANHAS_STATUSCAMPANHAS]))
											JOIN [dbo].[CAMPANHAS_ARQUIVOS] CA ON C.ARQUIVOID=C.ARQUIVOID 
											JOIN CARTEIRAS CAT ON C.CARTEIRAID=CAT.CARTEIRAID
											JOIN FORNECEDOR F  ON C.FORNECEDOIR=F.FORNECEDORID
									WHERE C.CLIENTEID=@ClienteID AND STATUSENVIO=@StatusEnvio AND DATAENVIAR=@DataEnviar AND C.CARTEIRAID=@CarteiraID AND C.ARQUIVOID=@ArquivoID";

					if (u.HasValue)
					{
						p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
						query = query.Insert(query.LastIndexOf("AND DATADIA"), "AND C.USUARIOID=@UsuarioID ");
					}

					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new CampanhaModel()
						{
							Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO, ArquivoID = a.ARQUIVOID },
							DataEnviar = a.DATAENVIAR,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = a.CARTEIRAID },
							Texto = a.TEXTO,
							Celular = a.CELULAR,
							DataDia = a.DATADIA,
							IDCliente = a.IDCLIENTE,
							Fornecedor = new FornecedorModel() { Nome = a.NOME, FornecedorID = a.FORNECEDORID },
							Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OPERADORAID))
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



		public async Task<IEnumerable<decimal>> RetornaRejeitados() => await DALGeneric.GenericReturnAsync<decimal>("SELECT CELULAR FROM HELPER.dbo.[FILTRADO] WHERE DATA BETWEEN @DataIn AND @DataOut", param: new { DataIn = DateTime.Now.Date.AddDays(-60), DataOut = DateTime.Now.Date });



		public Task<IEnumerable<CampanhaModel>> ObterTodosAsync(CampanhaModel t, int? u)
		{

			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaModel>> ObterTodosPaginadoAsync(CampanhaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task Limpeza()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					await conn.ExecuteAsync(@"TRUNCATE TABLE [SESSION_ITENS];
                                            TRUNCATE TABLE [FILECARDS]", commandTimeout: Util.TIMEOUTEXECUTE);
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
