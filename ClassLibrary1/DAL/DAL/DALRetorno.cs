using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class DALRetorno
	{
		public async Task AdicionarItens(IEnumerable<RetornoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"INSERT INTO [dbo].[CAMPANHA_RETORNO] ([FORNECEDORID],[RETORNO],[DATARETORNO],[DATA],[CAMPANHAID],[CLASSIFICACAOID]) VALUES (@FornecedorID, @Retorno, @DataRetorno, @Data, @CampanhaID, @ClassificacaoID)", t.Select(a =>
					new
					{
						FornecedorID = a.FornecedorID,
						Retorno = a.RetornoCliente,
						DataRetorno = a.DataRetorno,
						Data = DateTime.Now,
						CampanhaID = a.CampanhaID,
						ClassificacaoID = 29
					}), commandTimeout: 888,
					transaction: tran);

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

		public async Task AtualizaItens(IEnumerable<RetornoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					string query = @"UPDATE CAMPANHA_RETORNO SET CLASSIFICACAO=@Classificacao WHERE CODIGO=@Codigo";

					await conn.ExecuteAsync(query, t.Select(a => new
					{
						Codigo = a.Codigo,
						Classificacao = (byte)a.Classificacao,
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

		public async Task AtualizaRetornoIoPeople(RetornoModel r)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("Codigo", r.Codigo, DbType.Int32, ParameterDirection.Input);
					p.Add("Comentario", r.ComentarioAdicional, DbType.String, ParameterDirection.Input);
					p.Add("ScoreClassificacao", r.Score, DbType.Decimal, ParameterDirection.Input);
					p.Add("ClassificacaoID", r.Classificacao, DbType.Int32, ParameterDirection.Input);
					await conn.ExecuteAsync("UPDATE CAMPANHA_RETORNO SET SCORECLASSIFICACAO=@ScoreClassificacao, COMENTARIO=@Comentario, CLASSIFICACAOID=@ClassificacaoID WHERE CODIGO=@Codigo", p, commandTimeout: 888);
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

		public Task<RetornoModel> BuscarItemByID(RetornoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<RetornoModel>> BuscarItens(RetornoModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItens(IEnumerable<RetornoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}


		public async Task<IEnumerable<RetornoModel>> RetornosAPI(RetornoModel r)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					var p = new DynamicParameters();
					p.Add("ClienteID", r.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", r.DataInicial, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", r.DataFinal, DbType.DateTime, ParameterDirection.Input);


					string query = @"WITH RETORNOS AS
									(
										SELECT C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, DATARETORNO, CAT.CARTEIRA, F.NOME FORNECEDOR, SCORECLASSIFICACAO, RC.CLASSIFICACAO CLASSIICACAOIOPEOPLE  FROM CAMPANHA_RETORNO CR 
										JOIN CAMPANHAS C  ON CR.CAMPANHAID=C.CAMPANHAID
										JOIN CARTEIRAS CAT ON  C.CARTEIRAID=CAT.CARTEIRAID
										JOIN FORNECEDOR F ON CR.FORNECEDORID=F.FORNECEDORID
										LEFT JOIN CAMPANHAS_ARQUIVOS CA  ON C.ARQUIVOID=CA.ARQUIVOID
										LEFT JOIN RETORNO_CLASSIFICACAO RC ON CR.CLASSIFICACAOID=RC.CODIGO
										WHERE C.CLIENTEID=@ClienteID AND CR.DATARETORNO BETWEEN @DataInicial AND @DataFinal
										GROUP BY CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CA.ARQUIVO, C.CAMPANHAID, CR.CODIGO, DATARETORNO, CAT.CARTEIRA, F.NOME, RC.CLASSIFICACAO,SCORECLASSIFICACAO
									)
									SELECT TEXTO, IDCLIENTE, CELULAR, RETORNO, CLASSIFICACAO, ARQUIVO, DATARETORNO, CARTEIRA,CLASSIICACAOIOPEOPLE,SCORECLASSIFICACAO FROM RETORNOS";


					if (!string.IsNullOrEmpty(r.Carteira.Trim()))
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "CAT.CARTEIRA=@Carteira AND");

					var result = await conn.QueryAsync(query, p);


					if (result != null)
						return result.Select(a => new RetornoModel()
						{
							Texto = a.TEXTO,
							IDCliente = a.IDCLIENTE,
							ClassificacaoIOPeople = a.CLASSIICACAOIOPEOPLE,
							Celular = a.CELULAR,
							RetornoCliente = a.RETORNO,
							Arquivo = a.ARQUIVO,
							FornecedorNome = a.FORNECEDOR,
							DataRetorno = a.DATARETORNO,
							Score = a.SCORECLASSIFICACAO,
							Carteira = a.CARTEIRA,
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

		public async Task<IEnumerable<ClassificacaoIOModel>> ClassificacaoIOPeople()
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					return await conn.QueryAsync<ClassificacaoIOModel>("SELECT CODIGO, CLASSIFICACAO FROM RETORNO_CLASSIFICACAO ORDER BY CLASSIFICACAO");
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

		public async Task<IEnumerable<RetornoModel>> DashBoard(int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();

					var datainicial = DateTime.Now.Date;
					var datafinal = datainicial.AddSeconds(86399);

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", datainicial, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", datafinal, DbType.DateTime, ParameterDirection.Input);


					var query = @"WITH RETORNOS AS (SELECT C.CARTEIRAID ,C.ARQUIVOID ,CLASSIFICACAOID 
                                FROM CAMPANHA_RETORNO CR JOIN CAMPANHAS C ON CR.CAMPANHAID = C.CAMPANHAID
                                WHERE CR.DATARETORNO BETWEEN @DataInicial AND @DataFinal AND C.CLIENTEID = @ClienteID)

                                SELECT RC.CLASSIFICACAO CLASSIFICACAOIO, COUNT(RC.CLASSIFICACAO) QUANTIDADE
                                FROM RETORNOS R LEFT JOIN RETORNO_CLASSIFICACAO RC ON R.CLASSIFICACAOID = RC.CODIGO
                                GROUP BY RC.CLASSIFICACAO
                                ORDER BY QUANTIDADE DESC";

					var result = await conn.QueryAsync(new CommandDefinition(query, p, commandType: CommandType.Text, commandTimeout: 888));


					if (result != null)
					{
						var lista = result.Select(a => new RetornoModel()
						{
							Quantidade = a.QUANTIDADE,
							ClassificacaoIOPeople = a.CLASSIFICACAOIO
						});

						return lista;
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

		public async Task<(IEnumerable<RetornoModel> item1, IEnumerable<RetornoModel> item2)> ObterTodosTupla(RetornoModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();

					if (t.DataFinal.Value.Hour == 0 && t.DataFinal.Value.Minute == 0)
						t.DataFinal = t.DataFinal.Value.AddSeconds(86399);

					var carteiras = new StringBuilder();
					await t.CarteiraList.ToObservable().ForEachAsync(a => carteiras.AppendFormat("{0},", a.CarteiraID));

					p.Add("ClienteID", t.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", t.DataInicial, DbType.DateTime, ParameterDirection.Input);
					p.Add("DataFinal", t.DataFinal, DbType.DateTime, ParameterDirection.Input);
					p.Add("CarteiraID", carteiras.Length > 0 ? carteiras.ToString().TrimEnd(',') : null, DbType.String, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", t.Search, DbType.String, ParameterDirection.Input);
					p.Add("PageSize", t.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("Classificacao", t.IDClassificaoP, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", t.PaginaAtual.Value, DbType.Int32, ParameterDirection.Input);

					var query = string.Format(@"DECLARE @TMP TABLE(
                                                FORNECEDORID	INT,
	                                                TEXTO				VARCHAR(160),
	                                                IDCLIENTE			VARCHAR(50),
	                                                CELULAR				NUMERIC(12,0),
	                                                RETORNOCLIENTE		VARCHAR(320),
	                                                CLASSIFICACAO		TINYINT,
	                                                ARQUIVO				VARCHAR(255),
	                                                CODIGO				INT,
	                                                DATARETORNO			DATETIME,
	                                                CARTEIRA			VARCHAR(150), 
	                                                USUARIOID			INT,
	                                                CARTEIRAID			INT,
	                                                CLASSIFICACAOIO		VARCHAR(150),
	                                                SCORECLASSIFICACAO	DECIMAL
	                                                );

                                                WITH RETORNOS AS
                                                (
                                                    SELECT CR.FORNECEDORID, C.TEXTO, C.IDCLIENTE, C.CELULAR, CR.RETORNO, CR.CLASSIFICACAO, CR.CODIGO, DATARETORNO,C.USUARIOID, C.CARTEIRAID, C.ARQUIVOID, CLASSIFICACAOID, SCORECLASSIFICACAO FROM CAMPANHA_RETORNO CR 
                                                    JOIN CAMPANHAS C  ON CR.CAMPANHAID=C.CAMPANHAID
                                                    WHERE CR.DATARETORNO BETWEEN @DataInicial AND @DataFinal AND C.CLIENTEID=@ClienteID {2} {3}
                                                )

                                                INSERT @TMP
                                                SELECT FORNECEDORID, TEXTO, IDCLIENTE, CELULAR, RETORNO, R.CLASSIFICACAO,  CA.ARQUIVO, R.CODIGO, DATARETORNO, CARTEIRA, R.USUARIOID, R.CARTEIRAID, RC.CLASSIFICACAO, SCORECLASSIFICACAO FROM RETORNOS R {0} {1}
                                                LEFT JOIN RETORNO_CLASSIFICACAO RC ON R.CLASSIFICACAOID=RC.CODIGO 
                                                JOIN CARTEIRAS CAT ON  R.CARTEIRAID=CAT.CARTEIRAID
                                                LEFT JOIN CAMPANHAS_ARQUIVOS CA  ON R.ARQUIVOID=CA.ARQUIVOID

                                                SELECT CLASSIFICACAOIO, COUNT(CLASSIFICACAOIO) QUANTIDADE FROM @TMP GROUP BY CLASSIFICACAOIO
                                                SELECT FORNECEDORID, TEXTO, IDCLIENTE, CELULAR, RETORNOCLIENTE, CLASSIFICACAO, ARQUIVO, CODIGO, DATARETORNO, CARTEIRA, USUARIOID, CARTEIRAID, CLASSIFICACAOIO, SCORECLASSIFICACAO FROM @TMP ORDER BY DATARETORNO OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY",

												t.CarteiraList.Any() ? "JOIN string_split(@CarteiraID, ',') T ON R.CARTEIRAID=CAST(T.VALUE AS INT)" : string.Empty,
														u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON R.CARTEIRAID=UC.CARTEIRAID" : string.Empty,
														!string.IsNullOrEmpty(t.Search) ? "AND C.CLIENTEID=@ClienteID AND (CR.RETORNO LIKE '%'+@Search+'%' OR C.TEXTO LIKE '%'+@Search+'%' OR C.CELULAR LIKE '%'+@Search+'%')" : string.Empty,
														t.IDClassificaoP.HasValue ? "AND CLASSIFICACAOID=@Classificacao" : string.Empty
														);



					var result = await conn.QueryMultipleAsync(new CommandDefinition(query, p, commandType: CommandType.Text, commandTimeout: 888));


					if (result != null)
					{
						var dados1 = await result.ReadAsync();

						var dados2 = await result.ReadAsync();

						var agrupados = dados2.Select(a => new RetornoModel()
						{
							RetornoCliente = a.RETORNOCLIENTE,
							Score = a.SCORECLASSIFICACAO,
							ClassificacaoIOPeople = a.CLASSIFICACAOIO,
							DataRetorno = a.DATARETORNO,
							IDCliente = a.IDCLIENTE,
							Celular = a.CELULAR,
							Texto = a.TEXTO,
							Codigo = a.CODIGO,
							Arquivo = a.ARQUIVO,
							Carteira = a.CARTEIRA,
							UsuarioID = a.USUARIOID
						});

						var lista = dados1.Select(a => new RetornoModel()
						{
							Quantidade = a.QUANTIDADE,
							ClassificacaoIOPeople = a.CLASSIFICACAOIO
						});

						return (agrupados, lista);
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
	}
}
