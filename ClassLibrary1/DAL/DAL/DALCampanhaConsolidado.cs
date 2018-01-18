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
	public class DALCampanhaConsolidado : IDal<CampanhaConsolidadoModel>
	{


		public async Task AdicionarItensAsync(IEnumerable<CampanhaConsolidadoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{

					await conn.ExecuteAsync(@"INSERT INTO [dbo].[CAMPANHAS_CONSOLIDADO]([CARTEIRAID],[ARQUIVOID],[ACIMA160CARACTERES],[CELULARINVALIDO],[BLACKLIST],[DATAENVIAR],[USUARIOID],[CLIENTEID],[HIGIENIZADO],[ENVIADA],[EXCLUIDA],[ERRO],[SUSPENSA],[ENTREGUE],[EXPIRADA],[DATADIA])
	VALUES (@CarteiraID, @ArquivoID, @Acima160Caracteres, @CelularInvalido,@Blacklist, @DataEnviar, @UsuarioID, @ClienteID, @Higienizado, @Enviada, @Excluida, @Erro, @Suspensa, @Entregue, @Expirada, @DataDia)", t.Select(a => new
					{
						CarteiraID = a.Carteira.CarteiraID,
						ArquivoID=a.Arquivo.ArquivoID,
						Acima160Caracteres=a.Acima160Caracteres,
						CelularInvalido=a.CelularInvalido,
						DataEnviar=a.DataEnviar,
						UsuarioID=a.Usuario.UsuarioID,
						ClienteID=a.Cliente.ClienteID,
						Higienizado=a.Higienizado,
						Enviada=a.Enviada,
						Excluida =a.Excluida,
						Erro=a.Erro,
						Suspensa=a.Suspensa,
						Entregue=a.Entregue,
						Expirada=a.Expirada,
						DataDia=a.DataEnviar.Date,
						Blacklist = a.Blacklist
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

		public async Task AtualizaItensAsync(IEnumerable<CampanhaConsolidadoModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
				
					await conn.ExecuteAsync(@"UPDATE [dbo].[CAMPANHAS_CONSOLIDADO]
								   SET [CARTEIRAID] =@CarteiraID
									  ,[ARQUIVOID] = @ArquivoID
									  ,[ACIMA160CARACTERES] = @Acima160Caracteres
									  ,[CELULARINVALIDO] = @CelularInvalido
									  ,[BLACKLIST] = @Blacklist
									  ,[DATAENVIAR] = @DataEnviar
									  ,[USUARIOID] = @UsuarioID
									  ,[CLIENTEID] = @ClienteID
									  ,[HIGIENIZADO] = @Higienizado
									  ,[ENVIADA] = @Enviada
									  ,[EXCLUIDA] = @Excluida
									  ,[ERRO] = @Erro
									  ,[SUSPENSA] = @Suspensa
									  ,[ENTREGUE] = @Entregue
									  ,[EXPIRADA] = @Expirada
									  ,[DATADIA] = @DataDia
								 WHERE CODIGO=@Codigo", t.Select(a => new
					{
						CarteiraID = a.Carteira.CarteiraID,
						ArquivoID = a.Arquivo.ArquivoID,
						Acima160Caracteres = a.Acima160Caracteres,
						CelularInvalido = a.CelularInvalido,
						DataEnviar = a.DataEnviar,
						UsuarioID = u,
						ClienteID = c,
						Higienizado = a.Higienizado,
						Enviada = a.Enviada,
						Excluida = a.Excluida,
						Erro = a.Erro,
						Suspensa = a.Suspensa,
						Entregue = a.Entregue,
						Expirada = a.Expirada,
						DataDia = a.DataEnviar.Date,
						Blacklist = a.Blacklist
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

		public Task<CampanhaConsolidadoModel> BuscarItemByIDAsync(CampanhaConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaConsolidadoModel>> BuscarItensAsync(CampanhaConsolidadoModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensAsync(IEnumerable<CampanhaConsolidadoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItensUpdateAsync(IEnumerable<CampanhaConsolidadoModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaConsolidadoModel>> ObterTodosAsync(CampanhaConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<CampanhaConsolidadoModel>> ObterTodosDataEnviar(CampanhaConsolidadoModel t, DateTime dataIn, DateTime dataOut, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					string query = @"SELECT 
			CC.ACIMA160CARACTERES, CC.CELULARINVALIDO, CC.BLACKLIST, CC.HIGIENIZADO, 
			CC.ENVIADA, CC.EXCLUIDA, CC.ERRO, CC.SUSPENSA, CC.ENTREGUE, CC.EXPIRADA,
			CC.DATAENVIAR, CC.DATADIA, C.CARTEIRA, CA.ARQUIVO
			FROM CAMPANHAS_CONSOLIDADO CC
			JOIN CARTEIRAS C ON CC.CARTEIRAID = C.CARTEIRAID
			LEFT JOIN CAMPANHAS_ARQUIVO CA ON CC.ARQUIVOID = CA.ARQUIVOID
			WHERE CC.CLIENTEID = @ClienteID AND (CC.DATADIA BETWEEN @DataIn AND @DataOut)";

					var p = new DynamicParameters();
					p.Add("ClienteID", t.Cliente.ClienteID, DbType.Int32, ParameterDirection.Input);
					p.Add("Codigo", t.Codigo, DbType.Int32, ParameterDirection.Input);
					p.Add("DataIn", dataIn, DbType.Date, ParameterDirection.Input);
					p.Add("DataOut", dataOut, DbType.Date, ParameterDirection.Input);

					if (u.HasValue)
					{
						query = @"SELECT 
			CC.ACIMA160CARACTERES, CC.CELULARINVALIDO, CC.BLACKLIST, CC.HIGIENIZADO, 
			CC.ENVIADA, CC.EXCLUIDA, CC.ERRO, CC.SUSPENSA, CC.ENTREGUE, CC.EXPIRADA,
			CC.DATAENVIAR, CC.DATADIA, C.CARTEIRA, CA.ARQUIVO
			FROM CAMPANHAS_CONSOLIDADO CC
			JOIN CARTEIRAS C ON CC.CARTEIRAID = C.CARTEIRAID
			LEFT JOIN CAMPANHAS_ARQUIVO CA ON CC.ARQUIVOID = CA.ARQUIVOID
			WHERE CC.CLIENTEID = @ClienteID AND (CC.DATADIA BETWEEN @DataIn AND @DataOut) AND USUARIOID=@UsuarioID";

						p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);
					}

					if (t.Carteira != null)
					{
						query = query.Insert(query.Length, "CC.CARTEIRAID=@CarteiraID");
						p.Add("CarteiraID", t.Carteira.CarteiraID, DbType.Int32, ParameterDirection.Input);
					}

					var result = await conn.QueryAsync<dynamic>(query, p);

					if (result != null)
					{
						return result.Select(a=> new CampanhaConsolidadoModel()
						{
							Acima160Caracteres = a.ACIMA160CARACTERES,
							CelularInvalido = a.CELULARINVALIDO,
							Blacklist = a.BLACKLIST,
							Higienizado = a.HIGIENIZADO,
							Enviada = a.ENVIADA,
							Excluida = a.EXCLUIDA,
							Erro = a.ERRO,
							Suspensa = a.SUSPENSA,
							Entregue = a.ENTREGUE,
							Expirada = a.EXPIRADA,
							DataEnviar = a.DATAENVIAR,
							DataDia = a.DATADIA,
							Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
							Arquivo = a.ARQUIVO == null ? null : new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO }

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

		public Task<IEnumerable<CampanhaConsolidadoModel>> ObterTodosPaginadoAsync(CampanhaConsolidadoModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
