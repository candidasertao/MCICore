using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DAL
{
	public class DALConsolidado : IDal<ConsolidadoModel>
	{

		public async Task<IEnumerable<ConsolidadoModel>> DownloadConsolidado(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID, DbType.Int32, ParameterDirection.Input);


					var query = string.Format(@"SELECT ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, CANCELADA, DATAENVIAR, C.CARTEIRA, CAR.ARQUIVO, U.NOME, F.NOME FORNECEDOR FROM CAMPANHAS_CONSOLIDADO CC {0}
									JOIN CARTEIRAS C ON CC.CARTEIRAID=C.CARTEIRAID
									JOIN FORNECEDOR F ON CC.FORNECEDORID=F.FORNECEDORID
									LEFT JOIN CAMPANHAS_ARQUIVOS CAR ON CC.ARQUIVOID=CAR.ARQUIVOID
									LEFT JOIN USUARIOS U ON CC.USUARIOID=U.USUARIOID
									WHERE CC.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal ORDER BY DATAENVIAR",
									u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);



					if (u.HasValue)
						query = query.Insert(query.LastIndexOf("CC.CLIENTEID=@ClienteID"), "CC.USUARIOID=@UsuarioID AND ");


					if (co.CarteiraID.HasValue)
						query = query.Insert(query.LastIndexOf("CC.CLIENTEID=@ClienteID"), "CC.CARTEIRAID=@CarteiraID AND ");

					var result = await conn.QueryAsync(query, p, commandTimeout: Util.TIMEOUTEXECUTE);

					if (result != null || result.Any())
						return result.Select(a => new ConsolidadoModel()
						{
							Enviados = a.ENVIADA,
							Excluidas = a.EXCLUIDA,
							Erros = a.ERRO,
							Suspensos = a.SUSPENSA,
							Entregues = a.ENTREGUE,
							Expiradas = a.EXPIRADA,
							DataEnviar = a.DATAENVIAR,
							Canceladas = a.CANCELADA,
							Carteira = a.CARTEIRA,
							Arquivo = a.ARQUIVO,
							UsuarioNome = a.NOME,
							FornecedorNome = a.FORNECEDOR
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

		public async Task<IEnumerable<ConsolidadoModel>> ComparativoFornecedor(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("Fornecedores", co.Fornecedores.Select(a => a.ToString()).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);


					var query = string.Format(@"SELECT ENVIADAS, ENTREGUE, ATRASO, DATADIA, NOME FROM CAMPANHAS_CONSOLIDADO CC
												JOIN string_split(@Fornecedores,',') T ON CC.FORNECEDORID=CAST(T.VALUE AS INT)
												JOIN FORNECEDOR_CLIENTE FC ON CAST(T.VALUE AS INT)=FC.FORNECEDORID AND CC.CLIENTEID=FC.CLIENTEID
												JOIN FORNECEDOR F ON CC.FORNECEDORID=F.FORNECEDORID
												WHERE CC.CLIENTEID=@ClienteID AND DATADIA BETWEEN @Datainicial AND @DataFinal AND FC.STATUSFORNECEDOR=0",
									u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON CC.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);


					var result = await conn.QueryAsync(query, p, commandTimeout: Util.TIMEOUTEXECUTE);

					if (result != null || result.Any())
						return result.Select(a => new ConsolidadoModel()
						{
							Enviadas = a.ENVIADAS,
							Entregues = a.ENTREGUE,
							Atraso = TimeSpan.FromSeconds(a.ATRASO),
							DataDia = a.DATADIA,
							FornecedorNome = a.NOME
						}).OrderBy(a=>a.DataDia);

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
		public async Task<(IEnumerable<ConsolidadoModel>, IEnumerable<ConsolidadoModel>)> Consolidados(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID, DbType.Int32, ParameterDirection.Input);
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual.HasValue ? co.PaginaAtual.Value : 1, DbType.Int32, ParameterDirection.Input);
                    p.Add("Registros", direction: ParameterDirection.Output, dbType: DbType.Int32);


                    var query = string.Format(@"DECLARE @TMP TABLE (QUANTIDADE INT, ENVIADA INT, EXCLUIDA INT, ERRO INT, SUSPENSA INT, ENTREGUE INT, EXPIRADA INT, DATADIA DATE, CANCELADA INT, ENVIADAS INT, NAOENVIADAS INT);
									            WITH ENVIADAS AS
									            (
									            SELECT C.CODIGO, ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, CANCELADA, ENVIADAS, NAOENVIADAS FROM CAMPANHAS_CONSOLIDADO C {0}
                                                WHERE CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal {1}

									            )
									            INSERT INTO @TMP
									            SELECT COUNT(CODIGO), SUM(ENVIADA), SUM(EXCLUIDA), SUM(ERRO), SUM(SUSPENSA), SUM(ENTREGUE), SUM(EXPIRADA), DATADIA, SUM(CANCELADA), SUM(ENVIADAS), SUM(NAOENVIADAS) FROM ENVIADAS GROUP BY DATADIA 
									            SET @Registros = (SELECT COUNT(*) FROM @TMP);
									            SELECT SUM(QUANTIDADE) QUANTIDADE, SUM(EXCLUIDA) EXCLUIDA, SUM(ENTREGUE) ENTREGUE, SUM(SUSPENSA) SUSPENSA, SUM(EXPIRADA) EXPIRADA, SUM(CANCELADA) CANCELADA, SUM(ENVIADA) ENVIADA, SUM(ERRO) ERRO, SUM(ENVIADAS) ENVIADAS, SUM(NAOENVIADAS) NAOENVIADAS  FROM @TMP
									            SELECT ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, CANCELADA, ENVIADAS, NAOENVIADAS FROM @TMP ORDER BY DATADIA OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY",
									            u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty,
									            co.CarteiraID.HasValue ? "AND C.CARTEIRAID=@CarteiraID" : string.Empty
									        );




					var result = await conn.QueryMultipleAsync(query, p, commandTimeout: Util.TIMEOUTEXECUTE);



					if (result != null)
					{
						var dados1 = await result.ReadAsync();

						var dados2 = await result.ReadAsync();


						if (dados1.Any() && dados2.Any())
							return (dados1.Select(a => new ConsolidadoModel()
							{
								Excluidas = a.EXCLUIDA,
								Erros = a.ERRO,
								Suspensos = a.SUSPENSA,
								Entregues = a.ENTREGUE,
								Expiradas = a.EXPIRADA,
								NaoEnviadas = a.NAOENVIADAS,
								Enviados = a.ENVIADA,
								Enviadas = a.ENVIADAS,
								Quantidade = a.QUANTIDADE,
								Canceladas = a.CANCELADA,
                                Registros = p.Get<int>("Registros")
                            }), dados2.Select(a => new ConsolidadoModel()
							{
								Excluidas = a.EXCLUIDA,
								Erros = a.ERRO,
								NaoEnviadas = a.NAOENVIADAS,
								Enviados = a.ENVIADA,
								Enviadas = a.ENVIADAS,
								Suspensos = a.SUSPENSA,
								Entregues = a.ENTREGUE,
								Expiradas = a.EXPIRADA,
								DataDia = a.DATADIA,
								Canceladas = a.CANCELADA
                            }));
					}



					return (new ConsolidadoModel[] { }, new ConsolidadoModel[] { });

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

		public async Task AtualizaItensAsync(IEnumerable<ConsolidadoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"UPDATE CAMPANHAS_CONSOLIDADO SET CARTEIRAID=@CarteiraID WHERE CLIENTEID=@ClienteID AND CODIGO=@Codigo", t.Select(a => new { CarteiraID = a.CarteiraID, Codigo = a.Codigo, ClienteID = c }), transaction: tran, commandTimeout: 888);
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

		public async Task<IEnumerable<ConsolidadoModel>> Carteiras(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual.HasValue ? co.PaginaAtual.Value : 1, DbType.Int32, ParameterDirection.Input);

					string query = null;

					//if(co.CarteiraID.HasValue)
					query = string.Format(@"SELECT SUM(ENVIADAS) ENVIADAS, CC.CARTEIRAID, DATADIA, C.CARTEIRA FROM [dbo].[CAMPANHAS_CONSOLIDADO] CC
																							JOIN CARTEIRAS C ON CC.CARTEIRAID=C.CARTEIRAID {0}
																							 WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND C.CLIENTEID=@ClienteID GROUP BY CC.CARTEIRAID, DATADIA, C.CARTEIRA ORDER BY SUM(ENVIADAS) DESC",
																							 u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON CC.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);


					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result
							.Where(a => a.ENVIADAS > 0)
							.Select(a => new ConsolidadoModel()
							{
								Enviados = a.ENVIADAS,
								Carteira = a.CARTEIRA,
								DataDia = a.DATADIA,
								CarteiraID = a.CARTEIRAID
							}); ;


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

		Regex DataRegex = new Regex("([0-9]{2})(01|02|03|04|05|06|07|08|09|10|11|12)([0-9]{4})?(.csv|.txt)", RegexOptions.Compiled);

		Regex DataFullRegex = new Regex("([0-9]{4})(01|02|03|04|05|06|07|08|09|10|11|12)([0-9]{2})");

		DateTime dataValidada(string arquivo, DateTime dataenviar)
		{

			if (string.IsNullOrEmpty(arquivo))
				return dataenviar.Date;

			arquivo = arquivo.ToLower();

			Match _match = null;

			try
			{
				DateTime dataValidade = dataenviar;

				if (DataRegex.IsMatch(arquivo))
				{
					_match = DataRegex.Match(arquivo);
					var ano = DateTime.Now.Year;

					if (string.IsNullOrEmpty(_match.Groups[3].Value))
						ano = dataenviar.Year;

					string data = $"{ int.Parse(_match.Groups[1].Value)}/{int.Parse(_match.Groups[2].Value)}/{ano}";

					var _data = new DateTime();

					if (DateTime.TryParse(data, out _data))
						dataValidade = _data;
				}
				else if (DataFullRegex.IsMatch(arquivo))
				{
					_match = DataFullRegex.Match(arquivo);

					string data = $"{int.Parse(_match.Groups[1].Value)}/{ int.Parse(_match.Groups[2].Value)}/{int.Parse(_match.Groups[3].Value)}";
					var _data = new DateTime();
					if (DateTime.TryParse(data, out _data))
						dataValidade = _data;
				}

				return dataValidade.Date;
			}
			catch
			{

				return dataenviar.Date;
			}

		}






		public async Task<IEnumerable<ConsolidadoInvalidosModel>> RelatorioInvalidosAsync(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (co.PaginaAtual.HasValue)
					{
						if (co.PaginaAtual.Value == 0)
							co.PaginaAtual = 1;
					}
					else
						co.PaginaAtual = 1;

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("Carteira", co.CarteiraList.Any() ? co.CarteiraList.Select(a => a.CarteiraID.ToString()).Aggregate((a, b) => $"{a},{b}") : null, DbType.String, ParameterDirection.Input);
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual, DbType.Int32, ParameterDirection.Input);
					p.Add("Registros", direction: ParameterDirection.Output, dbType: DbType.Int32);
					p.Add("QuantidadeTotal", direction: ParameterDirection.Output, dbType: DbType.Int32);

					var query = string.Format(@"DECLARE @TMP TABLE (DATADIA DATE, ARQUIVO VARCHAR(255), CELULARINVALIDO INT, BLACKLIST INT, ACIMA160CARACTERES INT, HIGIENIZADO INT, FILTRADO INT, CARTEIRA VARCHAR(150), CARTEIRAID INT, LEIAUTEINVALIDO INT, DUPLICADO INT)
                                        IF @Carteira IS NOT NULL
                                        BEGIN
											
	                                        INSERT @TMP
	                                      	SELECT DATADIA, ARQUIVO, ISNULL([0], 0) CELULARINVALIDO, ISNULL([1], 0) BLACKLIST, ISNULL([2],0) ACIMA160CARACTERES, ISNULL([3], 0) HIGIENIZADO, ISNULL([5],0) FILTRADO, CARTEIRA, CARTEIRAID, ISNULL([6],0) LEIAUTEINVALIDO, ISNULL([7],0) DUPLICADO FROM (
											SELECT DATADIA, QUANTIDADE, ARQUIVO, TIPOINVALIDO, C.CARTEIRA, CIC.CARTEIRAID FROM [dbo].[CELULARES_INVALIDOS_CONSOLIDADO] CIC
											JOIN string_split(@Carteira,',') T ON CIC.CARTEIRAID=CAST(T.VALUE AS INT)
											JOIN CARTEIRAS C ON C.CARTEIRAID=CAST(T.VALUE AS INT)
											WHERE CIC.CLIENTEID=@ClienteID AND CIC.DATADIA BETWEEN @DataInicial AND @DataFinal) AS PVT PIVOT(SUM(QUANTIDADE) FOR TIPOINVALIDO IN([0],[1],[2],[3],[5],[6],[7])) AS P

	                                        SET @Registros = (SELECT COUNT(*) FROM @TMP);
											SET @QuantidadeTotal =(SELECT SUM(ACIMA160CARACTERES)+SUM(HIGIENIZADO)+SUM(CELULARINVALIDO)+SUM(FILTRADO)+SUM(BLACKLIST)+SUM(LEIAUTEINVALIDO)+SUM(DUPLICADO) FROM @TMP);

	                                        SELECT ACIMA160CARACTERES, HIGIENIZADO, CELULARINVALIDO, FILTRADO, ARQUIVO, DATADIA, CARTEIRA, BLACKLIST, CARTEIRAID, LEIAUTEINVALIDO, DUPLICADO FROM @TMP  ORDER BY DATADIA ASC, ARQUIVO ASC  OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;
                                        END
                                        ELSE
                                        BEGIN
	                                        INSERT @TMP
	                                       	SELECT DATADIA, ARQUIVO, ISNULL([0], 0) CELULARINVALIDO, ISNULL([1], 0) BLACKLIST, ISNULL([2],0) ACIMA160CARACTERES, ISNULL([3], 0) HIGIENIZADO, ISNULL([5],0) FILTRADO, CARTEIRA, CARTEIRAID,  ISNULL([6],0) LEIAUTEINVALIDO, ISNULL([7],0) DUPLICADO FROM (
											SELECT DATADIA, QUANTIDADE, ARQUIVO, TIPOINVALIDO, C.CARTEIRA, CIC.CARTEIRAID FROM [dbo].[CELULARES_INVALIDOS_CONSOLIDADO] CIC
											JOIN CARTEIRAS C ON C.CARTEIRAID=CIC.CARTEIRAID {0}
											WHERE CIC.CLIENTEID=@ClienteID AND CIC.DATADIA BETWEEN @DataInicial AND @DataFinal) AS PVT PIVOT(SUM(QUANTIDADE) FOR TIPOINVALIDO IN([0],[1],[2],[3],[5],[6],[7])) AS P

	                                        SET @Registros = (SELECT COUNT(*) FROM @TMP)
											SET @QuantidadeTotal =(SELECT SUM(ACIMA160CARACTERES)+SUM(HIGIENIZADO)+SUM(CELULARINVALIDO)+SUM(FILTRADO)+SUM(BLACKLIST)+SUM(LEIAUTEINVALIDO)+SUM(DUPLICADO) FROM @TMP);

	                                        SELECT ACIMA160CARACTERES, HIGIENIZADO, CELULARINVALIDO, FILTRADO, ARQUIVO, BLACKLIST, DATADIA, CARTEIRA, BLACKLIST, CARTEIRAID, LEIAUTEINVALIDO, DUPLICADO FROM @TMP T ORDER BY DATADIA ASC, ARQUIVO ASC OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;
                                        END;", u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON CIC.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);





					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new ConsolidadoInvalidosModel()
						{
							QuantidadeTotal = p.Get<int>("QuantidadeTotal"),
							Arquivo = a.ARQUIVO ?? "ENVIO SIMPLES",
							DataEnviar = a.DATADIA,
							Carteira = a.CARTEIRA,
							Acima160Caracteres = a.ACIMA160CARACTERES,
							BlackList = a.BLACKLIST,
							CelularInvalido = a.CELULARINVALIDO,
							Higienizado = a.HIGIENIZADO,
							Filtrado = a.FILTRADO,
							LayoutInvalido = a.LEIAUTEINVALIDO,
							Duplicado = a.DUPLICADO,
							Registros = p.Get<int>("Registros"),
							Paginas = p.Get<int>("Registros") / co.Registros,
							CarteiraID = a.CARTEIRAID,
							Quantidade = a.ACIMA160CARACTERES + a.HIGIENIZADO + a.FILTRADO + a.CELULARINVALIDO + a.BLACKLIST + a.DUPLICADO + a.LEIAUTEINVALIDO
						});

					return new ConsolidadoInvalidosModel[] { };

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


		public async Task<IEnumerable<ConsolidadoModel>> RelatorioArquivosAsync(ConsolidadoModel co, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (co.PaginaAtual.HasValue)
					{
						if (co.PaginaAtual.Value == 0)
							co.PaginaAtual = 1;
					}
					else
						co.PaginaAtual = 1;

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("Carteira", co.CarteiraList.Any() ? co.CarteiraList.Select(a => a.CarteiraID.ToString()).Aggregate((a, b) => $"{a},{b}") : null, DbType.String, ParameterDirection.Input);
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual, DbType.Int32, ParameterDirection.Input);
					p.Add("Registros", direction: ParameterDirection.Output, dbType: DbType.Int32);
					p.Add("QuantidadeTotal", direction: ParameterDirection.Output, dbType: DbType.Int32);
                    
                    var query = string.Format(@"DECLARE @TMP TABLE (QUANTIDADE INT, ARQUIVO VARCHAR(255), DATADIA DATE, CARTEIRA VARCHAR(150))
                                                IF @Carteira IS NOT NULL
                                                BEGIN
	                                                INSERT @TMP
	                                                SELECT SUM(ENVIADAS)+SUM(NAOENVIADAS) QUANTIDADE, CA.ARQUIVO, DATADIA, CART.CARTEIRA  FROM CAMPANHAS_CONSOLIDADO C {0}
	                                                JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID
	                                                JOIN string_split(@Carteira,',') T ON C.CARTEIRAID=CAST(T.value AS INT)
											        JOIN CARTEIRAS CART ON CART.CARTEIRAID=CAST(T.value AS INT)
	                                                WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal GROUP BY CA.ARQUIVO, C.DATADIA, CART.CARTEIRA 

	                                                SET @Registros = (SELECT COUNT(*) FROM @TMP);
											        SET @QuantidadeTotal =(SELECT SUM(QUANTIDADE) FROM @TMP)

	                                                SELECT QUANTIDADE, ARQUIVO, DATADIA, CARTEIRA FROM @TMP T ORDER BY DATADIA ASC, CARTEIRA ASC, ARQUIVO ASC OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;
                                                END
                                                ELSE
                                                BEGIN
	                                                INSERT @TMP
	                                                SELECT SUM(ENVIADAS)+SUM(NAOENVIADAS) QUANTIDADE, CA.ARQUIVO, DATADIA, CART.CARTEIRA FROM CAMPANHAS_CONSOLIDADO C {0}
											        JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID
											        JOIN CARTEIRAS CART ON C.CARTEIRAID=CART.CARTEIRAID
	                                                WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal GROUP BY CA.ARQUIVO, C.DATADIA, CART.CARTEIRA

	                                                SET @Registros = (SELECT COUNT(*) FROM @TMP);
											        SET @QuantidadeTotal =(SELECT SUM(QUANTIDADE) FROM @TMP);

	                                                SELECT QUANTIDADE, ARQUIVO, DATADIA, CARTEIRA FROM @TMP T ORDER BY DATADIA ASC, CARTEIRA ASC, ARQUIVO ASC OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;
                                                END", u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID = @UsuarioID" : string.Empty);




					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new ConsolidadoModel()
						{

							Arquivo = a.ARQUIVO,
							Quantidade = a.QUANTIDADE,
							DataDia = a.DATADIA,
							Carteira = a.CARTEIRA,
							Registros = p.Get<int>("Registros"),
							Paginas = p.Get<int>("Registros") / co.Registros,
							QuantidadeTotal = p.Get<int>("QuantidadeTotal")
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


		public async Task<IEnumerable<ConsolidadoModel>> Especializado(ConsolidadoModel co, int c, int? u, int? carteiraid = null)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (co.PaginaAtual.HasValue)
					{
						if (co.PaginaAtual.Value == 0)
							co.PaginaAtual = 1;
					}
					else
						co.PaginaAtual = 1;

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID.HasValue ? co.CarteiraID : null, DbType.Int32, ParameterDirection.Input);
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", co.Search, DbType.String, ParameterDirection.Input);
					p.Add("Registros", direction: ParameterDirection.Output, dbType: DbType.Int32);

					var query = string.Format(@"DECLARE @TMP TABLE (
									CARTEIRA  VARCHAR(150),   
									SPCAPITAL  INT,  
									SPGRANDE  INT,  
									DEMAISDDD  INT,  
									ENVIADAS   INT,  
									EXCLUIDA  INT,  
									DATADIA   DATE,  
									CODIGO   INT,  
									ARQUIVO   VARCHAR(150),  
									INVALIDOS  INT,  
									NOME   VARCHAR(150),  
									USUARIOID  INT,  
									FORNECEDORID INT,
									FORNECEDOR VARCHAR(150)
									);  
									  WITH REGISTROS AS(  
										SELECT CA.CARTEIRA, SPGRANDE, SPCAPITAL, DEMAISDDD, ENVIADAS, EXCLUIDA, DATADIA, C.CODIGO, CAR.ARQUIVO, 0 INVALIDOS, U.NOME, C.USUARIOID, C.FORNECEDORID, F.NOME FORNECEDOR FROM CAMPANHAS_CONSOLIDADO C   
										JOIN CARTEIRAS CA ON C.CARTEIRAID=CA.CARTEIRAID {0}
										JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
										LEFT JOIN USUARIOS U ON C.USUARIOID=U.USUARIOID  
										LEFT JOIN CAMPANHAS_ARQUIVOS CAR ON C.ARQUIVOID=CAR.ARQUIVOID  
										WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal
									   GROUP BY CA.CARTEIRA, SPGRANDE, SPCAPITAL, DEMAISDDD, ENVIADAS, EXCLUIDA, DATADIA, C.CODIGO, CAR.ARQUIVO, U.NOME, C.USUARIOID, C.FORNECEDORID, F.NOME
									  ) 
									INSERT @TMP  
									SELECT CARTEIRA, SUM(SPGRANDE), SUM(SPCAPITAL), SUM(DEMAISDDD), SUM(ENVIADAS), SUM(EXCLUIDA), DATADIA, MAX(CODIGO) CODIGO, ARQUIVO, SUM(INVALIDOS), NOME, USUARIOID, FORNECEDORID, FORNECEDOR FROM REGISTROS  GROUP BY CARTEIRA, ARQUIVO, NOME, USUARIOID, FORNECEDORID,FORNECEDOR, DATADIA

									SET @Registros=(SELECT COUNT(*) FROM  @TMP)

									SELECT CARTEIRA, SPGRANDE, SPCAPITAL, DEMAISDDD, ENVIADAS, EXCLUIDA, DATADIA, CODIGO, ARQUIVO, INVALIDOS, NOME, USUARIOID, FORNECEDORID, FORNECEDOR FROM @TMP ORDER BY DATADIA, CARTEIRA, ARQUIVO  OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;",
									u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty
									);




					var cliente = new ClienteModel();

					if (!u.HasValue)
						cliente.Nome = (await new DALClientes().BuscarItemByID(new ClienteModel() { ClienteID = c }, null)).Nome;


					if (!string.IsNullOrEmpty(co.Search))
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), @"(CAR.ARQUIVO LIKE '%'+@Search+'%' 
																							OR CA.CARTEIRA LIKE '%'+@Search+'%' 
																							OR U.NOME LIKE '%'+@Search+'%') AND ");


					var result = await conn.QueryAsync(query, p, commandTimeout: 888, commandType: CommandType.Text);

					if (result.Any())
						return result.Select(a => new ConsolidadoModel()
						{
							Arquivo = a.ARQUIVO,
							Carteira = a.CARTEIRA,
							SpGrande = a.SPGRANDE,
							SpCapital = a.SPCAPITAL,
							DemaisDDD = a.DEMAISDDD,
							Validade = dataValidada(a.ARQUIVO, a.DATADIA),
							Enviados = a.ENVIADAS,
							Excluidas = a.EXCLUIDA,
							Codigo = a.CODIGO,
							CelularInvalido = a.INVALIDOS,
							Registros = p.Get<int>("Registros"),
							Paginas = p.Get<int>("Registros") / co.Registros,
							FornecedorNome = a.FORNECEDOR,
							DataDia = a.DATADIA,
							UsuarioNome = a.NOME == null ? cliente.Nome : a.NOME
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
		public async Task<IEnumerable<CampanhaModel>> DownCancelados(ConsolidadoModel co, int c, int? u, int? carteiraid = null)
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
					p.Add("CarteiraID", co.CarteiraID.HasValue ? co.CarteiraID : null, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", co.Search, DbType.String, ParameterDirection.Input);

					var cliente = new ClienteModel();

					string query = @"SELECT CELULAR, TEXTO, IDCLIENTE, CA.CARTEIRA, A.ARQUIVO, DATAENVIAR, CAMPANHAID FROM CAMPANHAS C
									JOIN CARTEIRAS CA ON C.CARTEIRAID=CA.CARTEIRAID
									LEFT JOIN CAMPANHAS_ARQUIVOS A ON C.ARQUIVOID=A.ARQUIVOID
									WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=5 ORDER BY DATAENVIAR";


					if (carteiraid.HasValue)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "C.CARTEIRAID=@CarteiraID AND");

					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new CampanhaModel()
						{
							CampanhaID = a.CAMPANHAID,
							Celular = a.CELULAR,
							Texto = a.TEXTO,
							IDCliente = a.IDCLIENTE,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO },
							DataEnviar = a.DATAENVIAR
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
		public async Task<IEnumerable<ConsolidadoModel>> DownEspecializado(ConsolidadoModel co, int c, int? u)
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
					p.Add("CarteiraID", co.CarteiraID.HasValue ? co.CarteiraID : null, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", co.Search, DbType.String, ParameterDirection.Input);

					var cliente = new ClienteModel();

					string query = @"SELECT CA.CARTEIRA, SPGRANDE, SPCAPITAL, DEMAISDDD, C.ENVIADA, C.ENTREGUE, C.CANCELADA, C.SUSPENSA, C.ERRO, EXCLUIDA, C.EXPIRADA, DATAENVIAR, C.CODIGO, CAR.ARQUIVO, U.NOME, C.USUARIOID, C.FORNECEDORID, F.NOME AS FORNECEDOR, T.TIPOCAMPANHA  FROM CAMPANHAS_CONSOLIDADO C 
															JOIN CARTEIRAS CA ON C.CARTEIRAID = CA.CARTEIRAID
															JOIN FORNECEDOR F ON C.FORNECEDORID=F.FORNECEDORID
															LEFT JOIN TIPOCAMPANHA  T ON C.[TIPOCAMPANHAID]=T.CODIGO
															LEFT JOIN USUARIOS U ON C.USUARIOID = U.USUARIOID
															LEFT JOIN CAMPANHAS_ARQUIVOS CAR ON C.ARQUIVOID = CAR.ARQUIVOID
															 WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal
															 ORDER BY CODIGO";
					if (!u.HasValue)
						cliente.Nome = (await new DALClientes().BuscarItemByID(new ClienteModel() { ClienteID = c }, null)).Nome;

					if (!string.IsNullOrEmpty(co.Search))
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "(CAR.ARQUIVO LIKE '%'+@Search+'%' OR CA.CARTEIRA LIKE '%'+@Search+'%') AND ");

					if (co.CarteiraID.HasValue)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "C.CARTEIRAID=@CarteiraID AND ");

					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
						return result.Select(a => new ConsolidadoModel()
						{
							Entregues = a.ENTREGUE,
							Excluidas = a.EXCLUIDA,
							Expiradas = a.EXPIRADA,
							Enviados = a.ENVIADA,
							Canceladas = a.CANCELADA,
							Erros = a.ERRO,
							Suspensos = a.SUSPENSA,
							Arquivo = a.ARQUIVO,
							Carteira = a.CARTEIRA,
							SpGrande = a.SPGRANDE,
							SpCapital = a.SPCAPITAL,
							DemaisDDD = a.DEMAISDDD,
							Validade = dataValidada(a.ARQUIVO, Convert.ToDateTime(a.DATAENVIAR).Date),
							Codigo = a.CODIGO,
							FornecedorNome = a.FORNECEDOR,
							DataEnviar = a.DATAENVIAR,
							TipoCampanha = a.TIPOCAMPANHA,
							UsuarioNome = a.NOME == null ? cliente.Nome : a.NOME
						}); ;


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

		public async Task<IEnumerable<CampanhaModel>> ConsolidadoByStatus(ConsolidadoModel co, byte statusenvio, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();

					#region dados básicos pra paginação
					p.Add("PageSize", co.Registros, DbType.Int32, ParameterDirection.Input);
					p.Add("PageNumber", co.PaginaAtual.HasValue ? co.PaginaAtual : 1, DbType.Int32, ParameterDirection.Input);
					p.Add("DataInicial", co.DataInicial, DbType.Date, ParameterDirection.Input);
					p.Add("DataFinal", co.DataFinal, DbType.Date, ParameterDirection.Input);
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", co.Search, DbType.String, ParameterDirection.Input);
					p.Add("CarteiraID", co.CarteiraID, DbType.Int32, ParameterDirection.Input);
					p.Add("StatusEnvio", statusenvio, DbType.Byte, ParameterDirection.Input);
					p.Add("Registros", DbType.Int32, direction: ParameterDirection.Output);

					#endregion

					var query = string.Format(@"DECLARE @TMP TABLE (ARQUIVO VARCHAR(150), ARQUIVOID INT, CARTEIRA VARCHAR(150), CARTEIRAID INT, DATAENVIAR SMALLDATETIME, QUANTIDADE INT, USUARIOID INT);

								WITH CONSOLIDADA AS  
								 (  
								  SELECT C.CARTEIRAID, C.ARQUIVOID, DATAENVIAR, C.USUARIOID, CAMPANHAID, CAT.CARTEIRA FROM CAMPANHAS C {0}
											JOIN CARTEIRAS CAT ON C.CARTEIRAID=CAT.CARTEIRAID  
								  WHERE C.CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=@StatusEnvio  
								 )
								 INSERT @TMP
								 SELECT  ARQUIVO, C.ARQUIVOID, CARTEIRA, CARTEIRAID, DATAENVIAR, COUNT(CAMPANHAID) QUANTIDADE, C.USUARIOID   
								 FROM CONSOLIDADA C  
								 LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID  
								 GROUP BY ARQUIVO, C.ARQUIVOID, CARTEIRA, CARTEIRAID, DATAENVIAR, C.USUARIOID ORDER BY DATAENVIAR;

								SET @Registros = (SELECT COUNT(*) FROM @TMP);

								 SELECT ARQUIVO, ARQUIVOID, CARTEIRA, CARTEIRAID, DATAENVIAR, QUANTIDADE, USUARIOID FROM @TMP ORDER BY DATAENVIAR OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY;",
								u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty

								 );

					if (co.CarteiraID.HasValue)
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), "C.CARTEIRAID=@CARTEIRAID AND ");

					if (!string.IsNullOrEmpty(co.Search))
						query = query.Replace("LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID", "LEFT JOIN CAMPANHAS_ARQUIVOS CA ON C.ARQUIVOID=CA.ARQUIVOID WHERE CARTEIRA LIKE '%'+@Search+'%' OR ARQUIVO LIKE '%'+@Search+'%'");


					var result = await conn.QueryAsync(query, p, commandTimeout: 888);

					if (result != null)
					{
						var dados = result.Select(a => new CampanhaModel()
						{
							Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO, ArquivoID = a.ARQUIVOID },
							DataEnviar = a.DATAENVIAR,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA, CarteiraID = a.CARTEIRAID },
							Quantidade = a.QUANTIDADE,
							Paginas = p.Get<int>("Registros") / co.Registros,
							Registros = p.Get<int>("Registros")
						});

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

		public Task AdicionarItensAsync(IEnumerable<ConsolidadoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<ConsolidadoModel> BuscarItemByIDAsync(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<ConsolidadoModel>> BuscarItensAsync(ConsolidadoModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<ConsolidadoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<ConsolidadoModel>> ObterTodosAsync(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<ConsolidadoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<ConsolidadoModel>> ObterTodosPaginadoAsync(ConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
