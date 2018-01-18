using Dapper;
using Microsoft.Extensions.Configuration;
using DAL;
using Helpers;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALCarteira : IDal<CarteiraModel>
	{

		/// <summary>
		/// Adiciona um ou mais itens ao banco de dados
		/// </summary>
		/// <param name="t">Lista de carteiras pra ser adcionados</param>
		/// <param name="c">cliente</param>
		/// <param name="u">usuário vincculado a carteira. Pode ser nulo</param>
		/// <returns></returns>
		public async Task AdicionarItensAsync(IEnumerable<CarteiraModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					foreach (var item in t)
					{
						var p = new DynamicParameters();
						p.Add("NameInFile", item.Carteira.NoAcento().Trim(), DbType.String, ParameterDirection.Input);
						p.Add("Carteira", item.Carteira.Trim(), DbType.String, ParameterDirection.Input);
						p.Add("IDCarteira", string.IsNullOrEmpty(item.IDCarteira) ? item.IDCarteira : item.IDCarteira.Trim(), DbType.String, ParameterDirection.Input);
						p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
						p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
						p.Add("Limite", item.Limite, DbType.Int32, ParameterDirection.Input);
						p.Add("Periodicidade", item.Periodicidade, DbType.Int16, ParameterDirection.Input);
						p.Add("DiaInicio", item.DiaInicio, DbType.Int32, ParameterDirection.Input);
						p.Add("PorcentagemAviso", item.PorcentagemAviso, DbType.Int32, ParameterDirection.Input);
						p.Add("Visivel", item.Visivel, DbType.Boolean, ParameterDirection.Input);
						p.Add("Higieniza", item.DiasHigienizacao > 0, DbType.Boolean, ParameterDirection.Input);
						p.Add("DiasHigienizacao", item.DiasHigienizacao, DbType.Int32, ParameterDirection.Input);
						p.Add("SegmentacaoID", item.Segmentacao == null ? null : (object)item.Segmentacao.SegmentacaoID, DbType.Int32, ParameterDirection.Input);
						p.Add("BloqueioEnvio", item.BloqueioEnvio, DbType.Boolean, ParameterDirection.Input);
						p.Add("HoraLimite", item.HoraLimite.Hours == 0 ? TimeSpan.FromMinutes(1355) : item.HoraLimite, DbType.Time, ParameterDirection.Input);


						p.Add("CarteiraID", dbType: DbType.Int32, direction: ParameterDirection.Output);

						await conn.ExecuteAsync(@"INSERT INTO dbo.CARTEIRAS(CARTEIRA,IDCARTEIRA,USUARIOID,CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID, BLOQUEIOENVIO,HORALIMITE) 
					VALUES (@Carteira, @IDCarteira, @usuarioID, @ClienteID, @Data, @Limite, @Periodicidade, @DiaInicio, @PorcentagemAviso, @Visivel, @Higieniza, @DiasHigienizacao, @SegmentacaoID,@BloqueioEnvio, @HoraLimite);
						SELECT @CarteiraID=SCOPE_IDENTITY()", p, transaction: tran, commandTimeout: 888);

						int carteiraID = p.Get<int>("CarteiraID");

						item.CarteiraID = carteiraID;

						foreach (var a in item.CarteiraTelefone)
						{
                            if (a.Numero > 0)
                            {
							    await conn.ExecuteAsync(@"INSERT INTO [dbo].[CARTEIRA_TELEFONES]([CARTEIRAID],[NUMERO],[DESCRICAO]) VALUES (@CarteiraID, @Numero, @Descricao)",
								    new
								    {
									    CarteiraID = carteiraID,
									    Numero = a.Numero,
									    Descricao = string.IsNullOrEmpty(a.Descricao) ? null : a.Descricao
                                    }, transaction: tran, commandTimeout: 888);
                            }
						}
					}

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.CARTEIRA, TiposLogAtividadeEnums.GRAVACAO);
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

		public async Task LimiteCarteira(int? carteiraid = null)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();


				try
				{
					var p = new DynamicParameters();
					p.Add("@DataInicial", DateTime.Now.Date.AddDays(-30), DbType.Date, ParameterDirection.Input);
					p.Add("@DataFinal", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
					p.Add("@CarteiraID", carteiraid, DbType.Int32, ParameterDirection.Input);

					GestorModel h = new GestorModel();


					var consolidado = new ConsolidadoModel();

					var query = @"SELECT COUNT(CAMPANHAID) QUANTIDADE, C.CARTEIRAID, C.CLIENTEID, DATADIA, G.LIMITE, G.DIAINICIO, EMAIL, NOME, G.CARTEIRA, ISNULL(G.PORCENTAGEMAVISO, 90) PORCENTAGEMENVIO
													FROM CAMPANHAS C JOIN GESTORESSMS G ON C.CARTEIRAID=G.CARTEIRAID WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=2 AND LIMITE IS NOT NULL
													GROUP BY C.CARTEIRAID, C.CLIENTEID, DATADIA, G.LIMITE, G.DIAINICIO, EMAIL, NOME, G.CARTEIRA, G.TELEFONE, G.PORCENTAGEMAVISO";


					if (carteiraid.HasValue)
						query = @"SELECT COUNT(CAMPANHAID) QUANTIDADE, C.CARTEIRAID, C.CLIENTEID, DATADIA, G.LIMITE, G.DIAINICIO, EMAIL, NOME, G.CARTEIRA,  ISNULL(G.PORCENTAGEMAVISO, 90) PORCENTAGEMENVIO
													FROM CAMPANHAS C JOIN GESTORESSMS G ON C.CARTEIRAID=G.CARTEIRAID WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=2  AND C.CARTEIRAID=@CarteiraID AND LIMITE IS NOT NULL
													GROUP BY C.CARTEIRAID, C.CLIENTEID, DATADIA, G.LIMITE, G.DIAINICIO, EMAIL, NOME, G.CARTEIRA, G.TELEFONE, G.PORCENTAGEMAVISO";



					var dados = (await conn.QueryAsync(query, p)).ToList();


					var result = dados
						.GroupBy(a => new
						{
							DataEnviar = (DateTime)a.DATADIA,
							Quantidade = (int)a.QUANTIDADE,
							CarteiraID = (int)a.CARTEIRAID,
							PorcentagemEnvio = (int)a.PORCENTAGEMENVIO,
							Limite = (int)a.LIMITE,
							ClienteID = (int)a.CLIENTEID,
							DiaInicio = (int)a.DIAINICIO,
							Carteira = (string)a.CARTEIRA

						}, (a, b) => new NotificacaoLimiteCarteiraModel()
						{
							Quantidade = a.Quantidade,
							DataEnviar = a.DataEnviar,
							DiaInicio = a.DiaInicio,
							PorcentagemAviso = a.PorcentagemEnvio,
							Limite = a.Limite,
							Carteira = new CarteiraModel() { CarteiraID = a.CarteiraID, Carteira = a.Carteira },
							Cliente = new ClienteModel() { ClienteID = a.ClienteID },
							Gestores = b.Select(k => new EmailViewModel() { Email = k.EMAIL, Nome = k.NOME })
						}).ToList();

					var notificacao = new List<NotificacaoLimiteCarteiraModel>() { };

					var _group = result.GroupBy(a => new
					{
						CarteiraID = a.Carteira.CarteiraID,
						ClienteID = a.Cliente.ClienteID,
						Carteira = a.Carteira.Carteira,
						Limite = a.Limite,
						PorcentagemAviso = a.PorcentagemAviso,
						DiaInicio = a.DiaInicio

					}, (a, b) => new NotificacaoLimiteCarteiraModel()
					{
						Carteira = new CarteiraModel() { CarteiraID = a.CarteiraID, Carteira = a.Carteira },
						Cliente = new ClienteModel() { ClienteID = a.ClienteID },
						DiaInicio = a.DiaInicio,
						PorcentagemAviso = a.PorcentagemAviso,
						Limite = a.Limite,
						Gestores = b.SelectMany(k => k.Gestores).GroupBy(k => new { Nome = k.Nome, Email = k.Email }, (l, m) => new EmailViewModel() { Nome = l.Nome, Email = l.Email })
					}).ToList();


					foreach (var item in _group)
					{
						var quantidade = result.Where(a => (a.DataEnviar >= DataCompareLeitura(item.DiaInicio) && a.DataEnviar <= DateTime.Now.Date) && a.Carteira.CarteiraID == item.Carteira.CarteiraID).Sum(a => a.Quantidade);
						item.PercentualUso = Math.Round(((decimal)quantidade / (decimal)item.Limite) * 100, 2);

						if ((int)item.PercentualUso >= item.PorcentagemAviso)
						{
							//encaminha e-mails
							await Util.SendEmailAsync(item.Gestores,
								$"Limite da carteira {item.Carteira.Carteira} atingido",
								Emails.NotificaLimiteCarteira(item.Carteira.Carteira, item.PorcentagemAviso),
								true,
								 TipoEmail.LIMITECARTEIRA
								);
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
		DateTime DataCompareLeitura(int diainicial)
		{
			var dataAtual = DateTime.Now;
			var dataInicial = new DateTime(dataAtual.Year, dataAtual.Month, diainicial);

			if (dataInicial < dataAtual)
				return dataInicial;
			else //30/6 29/6
				return dataInicial.AddMonths(-1);

		}
		public async Task<(IEnumerable<CampanhaModel>, IEnumerable<CampanhaModel>)> CarteirasToApi(IEnumerable<CampanhaModel> c, IEnumerable<CampanhaModel> i, int clienteid)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (i.Any())
					{
						string query = @"SELECT CARTEIRAID, CARTEIRA FROM CARTEIRAS C JOIN string_split(@CarteirasCampanhaValida, ',') S ON LOWER(C.CARTEIRA)=S.value WHERE CLIENTEID=@ClienteID;
									SELECT CARTEIRAID, CARTEIRA FROM CARTEIRAS C JOIN string_split(@CarteirasCampanhaInvalida, ',') S ON LOWER(C.CARTEIRA)=S.value WHERE CLIENTEID=@ClienteID";

						var p = new DynamicParameters();
						p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);
						p.Add("CarteirasCampanhaValida", c.GroupBy(a => a.CarteiraNome, (a, b) => a.ToLower()).Select(a => a).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);
						p.Add("CarteirasCampanhaInvalida", i.GroupBy(a => a.CarteiraNome, (a, b) => a.ToLower()).Select(a => a).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);

						var result = await conn.QueryMultipleAsync(query, p);

						if (result != null)
						{
							var validas = await result.ReadAsync<CarteiraModel>();
							var invalidas = await result.ReadAsync<CarteiraModel>();
							return (validas.Select(a => new CampanhaModel() { Carteira = a }),
								invalidas.Select(a => new CampanhaModel() { Carteira = a }));
						}
					}
					else
					{
						string query = @"SELECT CARTEIRAID, CARTEIRA FROM CARTEIRAS C JOIN string_split(@CarteirasCampanhaValida, ',') S ON LOWER(C.CARTEIRA)=S.value WHERE CLIENTEID=@ClienteID";

						var p = new DynamicParameters();
						p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);
						p.Add("CarteirasCampanhaValida", c.GroupBy(a => a.CarteiraNome, (a, b) => a.ToLower()).Select(a => a).Aggregate((a, b) => $"{a},{b}"), DbType.String, ParameterDirection.Input);

						var result = await conn.QueryAsync<CarteiraModel>(query, p);

						if (result != null)
							return (result.Select(a => new CampanhaModel() { Carteira = a }), null);

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

		public async Task AtualizaItensAsync(IEnumerable<CarteiraModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					string query = @"UPDATE dbo.CARTEIRAS SET 
						CARTEIRA=@Carteira,
						IDCARTEIRA=@IDCarteira,
						LIMITE=@Limite, 
						PERIODICIDADE=@Periodicidade, 
						DIAINICIO=@DiaInicio, 
						PORCENTAGEMAVISO=@PorcentagemAviso, 
						VISIVEL=@Visivel, 
						HIGIENIZA=@Higieniza, 
						DIASHIGIENIZACAO=@DiasHigienizacao,
						SEGMENTACAOID=@SegmentacaoID,
						HORALIMITE=@HoraLimite,
						BLOQUEIOENVIO=@BloqueioEnvio
						WHERE CARTEIRAID=@CarteiraID AND CLIENTEID=@ClienteID";

					foreach (var item in t)
					{
						await conn.ExecuteAsync("DELETE FROM CARTEIRA_TELEFONES WHERE CARTEIRAID=@CarteiraID",
							new { CarteiraID = item.CarteiraID },
							transaction: tran, commandTimeout: 888);

						foreach (var a in item.CarteiraTelefone)
						{
                            if (a.Numero > 0)
                            {
                                await conn.ExecuteAsync(@"INSERT INTO [dbo].[CARTEIRA_TELEFONES]([CARTEIRAID],[NUMERO],[DESCRICAO]) VALUES (@CarteiraID, @Numero, @Descricao)",
                                    new
                                    {
                                        CarteiraID = item.CarteiraID,
                                        Numero = a.Numero,
                                        Descricao = string.IsNullOrEmpty(a.Descricao) ? null : a.Descricao
                                    }, transaction: tran, commandTimeout: 888);
                            }
                        }
					}


					await conn.ExecuteAsync(query, t.Select(a => new
					{
						Carteira = a.Carteira.Trim(),
						IDCarteira = !string.IsNullOrEmpty(a.IDCarteira) ? a.IDCarteira.Trim() : null,
						UsuarioID = u,
						ClienteID = c,
						Data = DateTime.Now,
						Limite = a.Limite,
						Periodicidade = a.Periodicidade,
						Visivel = a.Visivel,
						Higieniza = a.DiasHigienizacao > 0,
						DiaInicio = a.DiaInicio,
						HoraLimite = a.HoraLimite.Hours == 0 ? TimeSpan.FromMinutes(1355) : a.HoraLimite,
						PorcentagemAviso = a.PorcentagemAviso,
						BloqueioEnvio = a.BloqueioEnvio,
						DiasHigienizacao = a.DiasHigienizacao,
						SegmentacaoID = a.Segmentacao == null ? new Nullable<int>() : a.Segmentacao.SegmentacaoID,
						CarteiraID = a.CarteiraID
					}), transaction: tran, commandTimeout: 888);

					tran.Commit();

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.CARTEIRA, TiposLogAtividadeEnums.ATUALIZACAO);
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

		public async Task<CarteiraModel> CarteiraHigienizacao(CarteiraModel t, int c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT DIASHIGIENIZACAO FROM dbo.CARTEIRAS WHERE CLIENTEID=@ClienteID AND CARTEIRAID=@CarteiraID AND HIGIENIZA=1";

					var p = new DynamicParameters();
					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", t.CarteiraID, DbType.Int32, ParameterDirection.Input);


					var result = await conn.QuerySingleOrDefaultAsync<CarteiraModel>(query, p);

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
		DateTime DataLeitura(int diainicio)
		{
			var dataatual = DateTime.Now.Date;

			var dataleitura = new DateTime(
				dataatual.Year,
				dataatual.Month,
				diainicio);

			var resultado = dataatual - dataleitura;

			if (resultado.Days < 0)
			{
				dataleitura = dataleitura.AddMonths(-1);
				resultado = dataatual - dataleitura;
				dataatual = dataatual.AddDays(-resultado.Days);
			}

			return dataleitura;

		}
		public async Task<CarteiraModel> BuscarItemByIDAsync(CarteiraModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{

					string query = string.Format(@"SELECT C.CARTEIRAID,CARTEIRA,IDCARTEIRA,C.USUARIOID,C.CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID, HORALIMITE, CT.NUMERO, CT.DESCRICAO, CT.CODIGO, C.BLOQUEIOENVIO, S.NOME FROM dbo.CARTEIRAS C
									LEFT JOIN CARTEIRA_TELEFONES CT ON C.CARTEIRAID=CT.CARTEIRAID {0}
									LEFT JOIN SEGMENTACAO S ON C.SEGMENTACAOID=S.CODIGO
									 WHERE C.CLIENTEID=@ClienteID AND C.CARTEIRAID=@CarteiraID AND ISEXCLUDED=0
									", u.HasValue ? "JOIN USUARIOS_CARTEIRA UC  ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("CarteiraID", t.CarteiraID, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);



					var carteiraUso = await conn.QueryAsync("SELECT COUNT(CAMPANHAID) QUANTIDADE, DATADIA, STATUSENVIO FROM CAMPANHAS WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND CARTEIRAID=@CarteiraID AND CLIENTEID=@ClienteID AND STATUSENVIO IN(0,2) GROUP BY DATADIA, STATUSENVIO",
						new
						{
							ClienteID = t.Cliente.ClienteID,
							DataInicial = DateTime.Now.Date.AddDays(-30),
							DataFinal = DateTime.Now.Date.AddDays(30),
							CarteiraID = t.CarteiraID


						});




					IEnumerable<CarteiraModel> _uso = new CarteiraModel[] { };

					if (carteiraUso.Any())
						_uso = carteiraUso.Select(a => new CarteiraModel() { Quantidade = a.QUANTIDADE, Data = a.DATADIA, StatusEnvioCarteira = a.STATUSENVIO });

					//var agendado = _uso.Where(k => k.Data >= DateTime.Now.Date && k.StatusEnvioCarteira == 0);

					var result = await conn.QueryAsync(query, p);

					if (result != null)
					{
						var dado = result.GroupBy(a => new CarteiraModel()
						{

							CarteiraID = a.CARTEIRAID,
							Carteira = a.CARTEIRA,
							IDCarteira = a.IDCARTEIRA,
							Usuario = a.USUARIOID == null ? null : new UsuarioModel() { UsuarioID = a.USUARIOID },
							Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
							Data = a.DATA,
							Ultimos7Dias = _uso.Any() ? _uso.Where(k => k.Data >= DateTime.Now.Date.AddDays(-7) && k.Data <= DateTime.Now.Date).Sum(k => k.Quantidade) : 0,
							Ultimos15Dias = _uso.Any() ? _uso.Where(k => k.Data >= DateTime.Now.Date.AddDays(-15) && k.Data <= DateTime.Now.Date).Sum(k => k.Quantidade) : 0,
							Ultimos30Dias = _uso.Any() ? _uso.Sum(k => k.Quantidade) : 0,
							Agendados = _uso.Any() ? _uso.Where(k => k.Data >= DateTime.Now.Date && k.StatusEnvioCarteira == 0).Sum(k => k.Quantidade) : 0,
							EnviadosHoje = _uso.Any() ? _uso.Where(k => k.Data == DateTime.Now.Date && k.StatusEnvioCarteira == 2).Sum(k => k.Quantidade) : 0,
							EnviadosPeriodo = a.DIAINICIO != null ? _uso.Any() ? _uso.Where(k => k.Data >= DataLeitura(Convert.ToInt32(a.DIAINICIO)) && k.StatusEnvioCarteira == 2).Sum(k => k.Quantidade) : 0 : 0,
							Limite = a.LIMITE,
							Periodicidade = a.PERIODICIDADE,
							DiaInicio = a.DIAINICIO,
							PorcentagemAviso = a.PORCENTAGEMAVISO,
							Visivel = a.VISIVEL,
							ConsumoPeriodo = a.DIAINICIO != null ? _uso.Any() ? _uso.Where(k => k.Data >= DataLeitura(Convert.ToInt32(a.DIAINICIO)) && (k.StatusEnvioCarteira == 2 || k.StatusEnvioCarteira == 0)).Sum(k => k.Quantidade) : 0 : 0,
							Higieniza = a.HIGIENIZA,
							DiasHigienizacao = a.DIASHIGIENIZACAO,
							BloqueioEnvio = a.BLOQUEIOENVIO ?? false,
							HoraLimite = a.HORALIMITE,

							Segmentacao = a.SEGMENTACAOID == null ? null : new SegmentacaoModel() { SegmentacaoID = a.SEGMENTACAOID, Nome = a.NOME }
						}, (a, b) => new CarteiraModel()
						{
							CarteiraID = a.CarteiraID,

							Carteira = a.Carteira,
							IDCarteira = a.IDCarteira,
							Usuario = a.Usuario,
							Cliente = a.Cliente,
							Data = a.Data,
							EnviadosPeriodo = a.EnviadosPeriodo,
							Limite = a.Limite,
							Periodicidade = a.Periodicidade,
							DiaInicio = a.DiaInicio,
							PorcentagemAviso = a.PorcentagemAviso,
							Visivel = a.Visivel,
							Higieniza = a.Higieniza,
							DiasHigienizacao = a.DiasHigienizacao,
							Ultimos15Dias = a.Ultimos15Dias,
							Agendados = a.Agendados,
							Ultimos30Dias = a.Ultimos30Dias,
							Ultimos7Dias = a.Ultimos7Dias,
							EnviadosHoje = a.EnviadosHoje,
							ConsumoPeriodo = a.ConsumoPeriodo,
							BloqueioEnvio = a.BloqueioEnvio,
							HoraLimite = a.HoraLimite,
							Segmentacao = a.Segmentacao,
							CarteiraTelefone = b.Where(m => m.NUMERO != null).Select(m => new CarteiraTelefonesModel() { Codigo = m.CODIGO, Descricao = m.DESCRICAO, Numero = m.NUMERO }).ToList()
						}, new CompareObject<CarteiraModel>((a, b) => a.CarteiraID == b.CarteiraID, i => (i.CarteiraID.GetHashCode())))
						.ElementAt(0);

						return dado;
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

		public async Task<IEnumerable<CarteiraModel>> BuscarItensAsync(CarteiraModel t, string s, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = "SELECT CARTEIRAID,CARTEIRA,IDCARTEIRA,USUARIOID,CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID FROM dbo.CARTEIRAS WHERE CLIENTEID=@ClienteID AND Carteira LIKE '%'+@Busca+'%' AND ISEXCLUDED=0";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Busca", s, DbType.String, ParameterDirection.Input, 5);

					if (u.HasValue)
					{
						query = "SELECT CARTEIRAID,CARTEIRA,IDCARTEIRA,USUARIOID,CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID,HORALIMITE FROM dbo.CARTEIRAS WHERE CLIENTEID=@ClienteID AND USUARIOID=@UsuarioID AND CARTEIRA LIKE '%'+@Busca+'%' ISEXCLUDED=0";
						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					}

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
					{

						return result.Select(a => new CarteiraModel()
						{
							CarteiraID = a.CARTEIRAID,
							Carteira = a.CARTEIRA,
							IDCarteira = a.IDCARTEIRA,
							Usuario = a.USUARIOID == null ? null : new UsuarioModel() { UsuarioID = a.USUARIOID },
							Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
							Data = a.DATA,
							Limite = a.LIMITE,
							Periodicidade = a.PERIODICIDADE,
							DiaInicio = a.DIAINICIO,
							PorcentagemAviso = a.PORCENTAGEMAVISO,
							Visivel = a.VISIVEL,
							Higieniza = a.HIGIENIZA,
							DiasHigienizacao = a.DIASHIGIENIZACAO,
							HoraLimite = a.HORALIMITE,
							Segmentacao = a.SEGMENTACAOID == null ? null : new SegmentacaoModel() { SegmentacaoID = a.SEGMENTACAOID }
						});

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


		public async Task ExcluirItensAsync(IEnumerable<CarteiraModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();

				try
				{
					var query = "DELETE FROM CARTEIRAS WHERE CARTEIRAID=@CarteiraID AND CLIENTEID=@ClienteID";

					if (u.HasValue)
						query = query.Insert(query.LastIndexOf("CLIENTEID=@ClienteID"), " USUARIOID=@UsuarioID AND ");


					await conn.ExecuteAsync(query, t.Select(a => new { CarteiraID = a.CarteiraID, ClienteID = c, UsuarioID = u }), commandTimeout: 888);

					try
					{
#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.CARTEIRA, TiposLogAtividadeEnums.EXCLUSAO);
#pragma warning restore 4014
					}
					catch { }
				}
				catch (Exception err)
				{

					if (
						(err.InnerException ?? err).Message.Contains("REQUISICAO_RELATORIO_CARTEIRAS")
						|| (err.InnerException ?? err).Message.Contains("FK_CAMPANHAS_CARTEIRAS")
						|| (err.InnerException ?? err).Message.Contains("FK_LOG_ATIVIDADE_CARTEIRAID")
                        || (err.InnerException ?? err).Message.Contains("FK_CAMPANHAS_CONSOLIDADOS_CARTEIRAS")
                        )
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

		public async Task<IEnumerable<CarteiraModel>> ObterTodosAsync(CarteiraModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (t.OrigemChamada == OrigemChamadaEnums.CADASTRO && u.HasValue)
						u = null;
					

					string query = string.Format(@"SELECT C.CARTEIRAID,CARTEIRA,IDCARTEIRA,C.USUARIOID,C.CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID, HORALIMITE, CT.NUMERO, CT.DESCRICAO, CT.CODIGO, C.BLOQUEIOENVIO, S.NOME FROM dbo.CARTEIRAS C {0}
									LEFT JOIN CARTEIRA_TELEFONES CT ON C.CARTEIRAID=CT.CARTEIRAID
									LEFT JOIN SEGMENTACAO S ON C.SEGMENTACAOID=S.CODIGO
									 WHERE C.CLIENTEID=@ClienteID AND C.ISEXCLUDED=0 ORDER BY CARTEIRA"
									, u.HasValue ? "JOIN USUARIOS_CARTEIRA UC  ON C.CARTEIRAID = UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

					var carteiraUso = await conn.QueryAsync("SELECT COUNT(CAMPANHAID) QUANTIDADE, DATADIA FROM CAMPANHAS WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND CARTEIRAID=@CarteiraID AND CLIENTEID=@ClienteID AND STATUSENVIO IN(0,1,2) GROUP BY DATADIA",
						new
						{
							ClienteID = t.Cliente.ClienteID,
							DataInicial = DateTime.Now.Date.AddDays(-30),
							DataFinal = DateTime.Now.Date.AddDays(30),
							CarteiraID = t.CarteiraID

						});

					var r = await conn.QueryAsync(query, p);

					var result = r.GroupBy(a => new CarteiraModel()
					{
						CarteiraID = a.CARTEIRAID,
						Carteira = a.CARTEIRA,
						IDCarteira = a.IDCARTEIRA,
						Usuario = a.USUARIOID == null ? null : new UsuarioModel() { UsuarioID = a.USUARIOID },
						Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
						Data = a.DATA,
						Limite = a.LIMITE,
						Periodicidade = a.PERIODICIDADE,
						DiaInicio = a.DIAINICIO,
						PorcentagemAviso = a.PORCENTAGEMAVISO,
						Visivel = a.VISIVEL,
						Higieniza = a.HIGIENIZA,
						DiasHigienizacao = a.DIASHIGIENIZACAO,
						Ultimos15Dias = 0,
						Ultimos30Dias = 0,
						Ultimos7Dias = 0,
						BloqueioEnvio = a.BLOQUEIOENVIO ?? false,
						EnviadosHoje = 0,
						HoraLimite = a.HORALIMITE,
						Segmentacao = a.SEGMENTACAOID == null ? null : new SegmentacaoModel() { SegmentacaoID = a.SEGMENTACAOID, Nome = a.NOME }
					}, (a, b) => new CarteiraModel()
					{
						CarteiraID = a.CarteiraID,
						Carteira = a.Carteira,
						IDCarteira = a.IDCarteira,
						Usuario = a.Usuario,
						Cliente = a.Cliente,
						Data = a.Data,
						Limite = a.Limite,
						Periodicidade = a.Periodicidade,
						DiaInicio = a.DiaInicio,
						PorcentagemAviso = a.PorcentagemAviso,
						Visivel = a.Visivel,
						Higieniza = a.Higieniza,
						DiasHigienizacao = a.DiasHigienizacao,
						Ultimos15Dias = a.Ultimos15Dias,
						Ultimos30Dias = a.Ultimos30Dias,
						Ultimos7Dias = a.Ultimos7Dias,
						EnviadosHoje = 0,
						BloqueioEnvio = a.BloqueioEnvio,
						HoraLimite = a.HoraLimite,
						Segmentacao = a.Segmentacao,
						CarteiraTelefone = b.Where(m => m.NUMERO != null).Select(m => new CarteiraTelefonesModel() { Codigo = m.CODIGO, Descricao = m.DESCRICAO, Numero = m.NUMERO }).ToList()
					},
					new CompareObject<CarteiraModel>((a, b) => a.CarteiraID == b.CarteiraID, i => (i.CarteiraID.GetHashCode()))
					);

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

		public async Task ExcluirItensUpdateAsync(IEnumerable<CarteiraModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{

				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();


				try
				{
					await conn.ExecuteAsync("DELETE FROM USUARIOS_CARTEIRA WHERE CARTEIRAID=@CarteiraID", t.Select(a => new { CarteiraID = a.CarteiraID }), transaction:tran, commandTimeout: Util.TIMEOUTEXECUTE);
					await conn.ExecuteAsync("DELETE FROM GESTOR_CARTEIRAS WHERE CARTEIRAID=@CarteiraID", t.Select(a => new { CarteiraID = a.CarteiraID }), transaction:tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    await conn.ExecuteAsync("DELETE FROM PADRAO_POSTAGENS WHERE CARTEIRAID=@CarteiraID", t.Select(a => new { CarteiraID = a.CarteiraID }), transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    await conn.ExecuteAsync("UPDATE CARTEIRAS SET ISEXCLUDED=1 WHERE CARTEIRAID=@CarteiraID AND CLIENTEID=@ClienteID", t.Select(a => new { CarteiraID = a.CarteiraID, ClienteID = c, UsuarioID = u }), transaction:tran, commandTimeout: Util.TIMEOUTEXECUTE);

					try
					{
						#pragma warning disable 4014
						new DALLogAtividade().AdicionarItensAsync(t, null, c, u, ModuloAtividadeEnumns.CARTEIRA, TiposLogAtividadeEnums.EXCLUSAO);
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

		public async Task<IEnumerable<CarteiraModel>> ObterTodosPaginadoAsync(CarteiraModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					if (t.OrigemChamada == OrigemChamadaEnums.CADASTRO && u.HasValue)
						u = null;


					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					p.Add("SegmentacaoID", t.Segmentacao != null ? (object)t.Segmentacao.SegmentacaoID : null, DbType.Int32, ParameterDirection.Input);
					p.Add("Search", t.Search.NoAcento(), DbType.String, ParameterDirection.Input);


					string query = string.Format(@"SELECT C.CARTEIRAID,CARTEIRA,IDCARTEIRA,C.USUARIOID,C.CLIENTEID,DATA,LIMITE,PERIODICIDADE,DIAINICIO,PORCENTAGEMAVISO,VISIVEL,HIGIENIZA,DIASHIGIENIZACAO,SEGMENTACAOID, HORALIMITE, CT.NUMERO, CT.DESCRICAO, CT.CODIGO, C.BLOQUEIOENVIO, S.NOME FROM dbo.CARTEIRAS C {0}
									LEFT JOIN CARTEIRA_TELEFONES CT ON C.CARTEIRAID=CT.CARTEIRAID
									LEFT JOIN SEGMENTACAO S ON C.SEGMENTACAOID=S.CODIGO
									 WHERE C.CLIENTEID=@ClienteID AND C.ISEXCLUDED=0 ORDER BY CARTEIRA", u.HasValue ? "JOIN USUARIOS_CARTEIRA UC ON C.CARTEIRAID=UC.CARTEIRAID AND UC.USUARIOID=@UsuarioID" : string.Empty);

					if (t.PaginaAtual.HasValue)
					{
						if (t.PaginaAtual.Value == 0)
							t.PaginaAtual = 1;
					}
					else
						t.PaginaAtual = 1;

					if (!string.IsNullOrEmpty(t.Search))
						query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), @"(UPPER(CAST(CARTEIRA AS VARCHAR(MAX)) COLLATE SQL_LATIN1_GENERAL_CP1251_CS_AS) LIKE UPPER('%'+@Search+'%')
                                                                                            OR UPPER(CAST(S.NOME AS VARCHAR(MAX)) COLLATE SQL_LATIN1_GENERAL_CP1251_CS_AS) LIKE UPPER('%'+@Search+'%')
                                                                                            OR UPPER(CAST(IDCARTEIRA AS VARCHAR(MAX)) COLLATE SQL_LATIN1_GENERAL_CP1251_CS_AS) LIKE UPPER('%'+@Search+'%')) AND ");

					if (t.Segmentacao != null)
						if (t.Segmentacao.SegmentacaoID > 0)
							query = query.Insert(query.LastIndexOf("C.CLIENTEID=@ClienteID"), @"SEGMENTACAOID=@SegmentacaoID AND ");

					var campanhas = await conn.QueryAsync<CampanhaModel>("SELECT COUNT(CAMPANHAID) QUANTIDADE, DATADIA, CARTEIRAID FROM CAMPANHAS C WHERE CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND C.STATUSENVIO IN(2,0) GROUP BY DATADIA, CARTEIRAID", new
					{
						DataInicial = DateTime.Now.AddMonths(-1),
						ClienteID = t.Cliente.ClienteID,
						DataFinal = DateTime.Now.AddMonths(1)
					});


					var _uso = (campanhas ?? new CampanhaModel[] { }).ToList();

					//a.DIAINICIO != null ? _uso.Any() ? _uso.Where(k => k.Data >= DataLeitura(Convert.ToInt32(a.DIAINICIO)) && k.Data <= DateTime.Now.Date).Sum(k => k.Quantidade) : 0 : 0,

					var result = await conn.QueryAsync(query, p);

					if (result != null || result.Any())
					{
						var dados = result.GroupBy(a => new CarteiraModel()
						{
							CarteiraID = a.CARTEIRAID,
							Carteira = a.CARTEIRA,
							IDCarteira = a.IDCARTEIRA,
							Usuario = a.USUARIOID == null ? null : new UsuarioModel() { UsuarioID = a.USUARIOID },
							Cliente = new ClienteModel() { ClienteID = a.CLIENTEID },
							Data = a.DATA,
							Limite = a.LIMITE,
							Periodicidade = a.PERIODICIDADE,
							DiaInicio = a.DIAINICIO,
							PorcentagemAviso = a.PORCENTAGEMAVISO,
							Visivel = a.VISIVEL,
							Higieniza = a.HIGIENIZA,
							DiasHigienizacao = a.DIASHIGIENIZACAO,
							Ultimos15Dias = 0,
							Ultimos30Dias = 0,
							Ultimos7Dias = 0,
							BloqueioEnvio = a.BLOQUEIOENVIO == null ? false : a.BLOQUEIOENVIO,
							EnviadosHoje = 0,
							HoraLimite = a.HORALIMITE,
							ConsumoPeriodo = a.DIAINICIO != null ? _uso.Where(k => (k.DataDia >= DataLeitura(Convert.ToInt32(a.DIAINICIO)) && k.DataDia <= DateTime.Now.Date) && k.CarteiraID.Value == (int)a.CARTEIRAID).Sum(k => k.Quantidade) : 0,
							Segmentacao = a.SEGMENTACAOID == null ? null : new SegmentacaoModel() { SegmentacaoID = a.SEGMENTACAOID, Nome = a.NOME }
						}, (a, b) => new CarteiraModel()
						{
							CarteiraID = a.CarteiraID,
							Carteira = a.Carteira,
							IDCarteira = a.IDCarteira,
							Usuario = a.Usuario,
							Cliente = a.Cliente,
							Data = a.Data,
							Limite = a.Limite,
							Periodicidade = a.Periodicidade,
							DiaInicio = a.DiaInicio,
							PorcentagemAviso = a.PorcentagemAviso,
							Visivel = a.Visivel,
							Higieniza = a.Higieniza,
							DiasHigienizacao = a.DiasHigienizacao,
							Ultimos15Dias = a.Ultimos15Dias,
							ConsumoPeriodo = a.ConsumoPeriodo,
							Ultimos30Dias = a.Ultimos30Dias,
							Ultimos7Dias = a.Ultimos7Dias,
							EnviadosHoje = 0,
							BloqueioEnvio = a.BloqueioEnvio,
							HoraLimite = a.HoraLimite,
							Segmentacao = a.Segmentacao,
							CarteiraTelefone = b.Where(m => m.NUMERO != null).Select(m => new CarteiraTelefonesModel() { Codigo = m.CODIGO, Descricao = m.DESCRICAO, Numero = m.NUMERO }).ToList()
						},
					new CompareObject<CarteiraModel>((a, b) => a.CarteiraID == b.CarteiraID, i => (i.CarteiraID.GetHashCode())));

						return dados.Select(a => new CarteiraModel()
						{
							CarteiraID = a.CarteiraID,
							Carteira = a.Carteira,
							IDCarteira = a.IDCarteira,
							Usuario = a.Usuario,
							Cliente = a.Cliente,
							Data = a.Data,
							Limite = a.Limite,
							Periodicidade = a.Periodicidade,
							DiaInicio = a.DiaInicio,
							PorcentagemAviso = a.PorcentagemAviso,
							Visivel = a.Visivel,
							ConsumoPeriodo = a.ConsumoPeriodo,
							Higieniza = a.Higieniza,
							DiasHigienizacao = a.DiasHigienizacao,
							Ultimos15Dias = a.Ultimos15Dias,
							Ultimos30Dias = a.Ultimos30Dias,
							Ultimos7Dias = a.Ultimos7Dias,
							EnviadosHoje = 0,
							BloqueioEnvio = a.BloqueioEnvio,
							HoraLimite = a.HoraLimite,
							Segmentacao = a.Segmentacao,
							CarteiraTelefone = a.CarteiraTelefone,
							Registros = dados.Count(),
							Paginas = dados.Count() / t.Registros
						})
						.Skip((t.PaginaAtual.Value - 1) * t.Registros)
						.Take(t.Registros);
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
