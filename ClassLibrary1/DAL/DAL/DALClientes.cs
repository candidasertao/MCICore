using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DAL
{
	public class DALClientes
	{
		public async Task<int> AlteraSaldoUsuario(ClienteModel t, int c, int quantidade, SqlTransaction tran = null, SqlConnection conn = null)
		{
			var p = new DynamicParameters();
			p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
			p.Add("Quantidade", quantidade, DbType.Int32, ParameterDirection.Input);

			var query = "UPDATE C SET SALDO=C.SALDO-@Quantidade FROM CLIENTES C WHERE C.CLIENTEID=@ClienteID AND POSPAGO=0";

			if (tran != null && conn != null)
				return await conn.ExecuteAsync(query, p, transaction: tran);
			else
			{
				using (var conexao = new SqlConnection(Util.ConnString))
				{
					try
					{
						await conexao.OpenAsync();

						return await conexao.ExecuteAsync(query, p, commandTimeout: 888);
					}
					catch (Exception err)
					{
						throw err;
					}
					finally
					{
						conexao.Close();
					}
				}
			}

		}
		public async Task<int> AdicionarItens(IEnumerable<ClienteModel> t)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{

					int clienteid = 0;
					var item = t.ElementAt(0);


					var p = new DynamicParameters();
					p.Add("Nome", item.Nome, DbType.String, ParameterDirection.Input);
					p.Add("CNPJ", item.CNPJ, DbType.String, ParameterDirection.Input);
					p.Add("Endereco", item.Endereco, DbType.String, ParameterDirection.Input);
					p.Add("Numero", item.Numero, DbType.String, ParameterDirection.Input);
					p.Add("Complemento", item.Complemento, DbType.String, ParameterDirection.Input);
					p.Add("Bairro", item.Bairro, DbType.String, ParameterDirection.Input);
					p.Add("CEP", item.CEP, DbType.String, ParameterDirection.Input);
					p.Add("Cidade", item.Cidade, DbType.String, ParameterDirection.Input);
					p.Add("UF", item.UF, DbType.String, ParameterDirection.Input);
					p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);
					p.Add("ClienteID", DbType.Int32, direction: ParameterDirection.Output);


					await conn.ExecuteAsync(@"INSERT INTO [dbo].[CLIENTES]([NOME],[CNPJ],[ENDERECO],[NUMERO],[COMPLEMENTO],[BAIRRO],[CEP],[CIDADE],[UF],[DATA]) 
											VALUES (@Nome, @CNPJ, @Endereco, @Numero, @Complemento, @Bairro, @CEP, @Cidade, @UF, @Data); 
                                            SELECT @ClienteID=SCOPE_IDENTITY();", p, transaction: tran,
					commandTimeout: 888);

					clienteid = p.Get<int>("ClienteID");

					await conn.ExecuteAsync(@"INSERT INTO [dbo].[CLIENTES_CONTATO]([CLIENTEID],[TELEFONE],[EMAIL],[DESCRICAO]) VALUES (@ClienteID, @Telefone, @Email,@Descricao)", item.Contatos.Select(m => new { Telefone = m.Celular, Email = m.Email, ClienteID = clienteid, Descricao = m.Descricao }), transaction: tran);


					tran.Commit();

					return clienteid;
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

		public async Task AtualizaItens(IEnumerable<ClienteModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				SqlTransaction tran = conn.BeginTransaction();

				try
				{
					var cliente = t.ElementAt(0);

					var p = new DynamicParameters();

					p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
					p.Add("Nome", cliente.Nome, DbType.String, ParameterDirection.Input);
					p.Add("Endereco", cliente.Endereco, DbType.String, ParameterDirection.Input);
					p.Add("Numero", cliente.Numero, DbType.String, ParameterDirection.Input);
					p.Add("Complemento", cliente.Complemento, DbType.String, ParameterDirection.Input);
					p.Add("Bairro", cliente.Bairro, DbType.String, ParameterDirection.Input);
					p.Add("CEP", cliente.CEP, DbType.String, ParameterDirection.Input);
					p.Add("Cidade", cliente.Cidade, DbType.String, ParameterDirection.Input);
					p.Add("UF", cliente.UF, DbType.String, ParameterDirection.Input);

					await conn.ExecuteAsync(@"UPDATE [dbo].[CLIENTES]
                                            SET [NOME] = @Nome
                                                ,[ENDERECO] = @Endereco
                                                ,[NUMERO] =@Numero
                                                ,[COMPLEMENTO] = @Complemento
                                                ,[BAIRRO] = @Bairro
                                                ,[CEP] = @CEP
                                                ,[CIDADE] = @Cidade
                                                ,[UF] = @UF
                                            WHERE CLIENTEID=@ClienteID", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    await conn.ExecuteAsync(@"DELETE FROM CLIENTES_CONTATO WHERE CLIENTEID=@ClienteID", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    if (cliente.Contatos != null && cliente.Contatos.Any())
                        await conn.ExecuteAsync(@"INSERT INTO [dbo].[CLIENTES_CONTATO]([CLIENTEID],[TELEFONE],[EMAIL],[DESCRICAO]) VALUES (@ClienteID, @Telefone, @Email,@Descricao)"
                                                    , cliente.Contatos.Select(m => new { Telefone = m.Celular, Email = m.Email, ClienteID = c, Descricao = string.IsNullOrEmpty(m.Descricao) ? null: m.Descricao })
                                                    , transaction: tran
                                                    , commandTimeout: Util.TIMEOUTEXECUTE);

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

		public async Task<ClienteModel> ClienteLogin(ClienteModel c)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("CNPJ", c.CNPJ, DbType.String, ParameterDirection.Input);

					var result = await conn.QuerySingleOrDefaultAsync<ClienteModel>(@"SELECT CLIENTEID, NOME FROM CLIENTES C WHERE C.CNPJ=@CNPJ AND ATIVO=1", p);

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

		public async Task<IEnumerable<FornecedorModel>> FornecedoresCliente(ClienteModel t, int? u, int? f = null)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.ClienteID, DbType.Int32, ParameterDirection.Input);
                    p.Add("FornecedorID", f, DbType.Int32, ParameterDirection.Input);

                    var result = await conn.QueryAsync(string.Format(@"SELECT FC.FORNECEDORID, NOME, DISTRIBUICAO, CAPACIDADEENVIO, F.BAIRRO, F.CIDADE, F.CPFCNPJ, F.ENDERECO, FC.USUARIO, FC.SENHA, STATUSFORNECEDOR, STATUSOPERACIONAL FROM FORNECEDOR_CLIENTE FC JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID WHERE CLIENTEID=@ClienteID {0};", f.HasValue ? " AND F.FORNECEDORID = @FornecedorID " : string.Empty), p);

					if (result != null || result.Any())
						return result.Select(a => new FornecedorModel()
						{
							Distribuicao = a.DISTRIBUICAO ?? 0,
							Nome = a.NOME,
							Capacidade5M = a.ENVIOACADA5MIN ?? 0,
							CapacidadeTotal = a.CAPACIDADEENVIO ?? 0,
							FornecedorID = a.FORNECEDORID,
							Cidade = a.CIDADE,
							Bairro = a.BAIRRO,
							StatusOperacionalFornecedor = ((StatusOperacionalFornecedorEnum)Enum.Parse(typeof(StatusOperacionalFornecedorEnum), ((byte)a.STATUSOPERACIONAL).ToString())),
							StatusFornecedor = ((StatusFornecedorEnums)Enum.Parse(typeof(StatusFornecedorEnums), ((byte)a.STATUSFORNECEDOR).ToString())),							
							Endereco = a.ENDERECO,
                            Login = new LoginViewModel { Username = a.USUARIO, Password = a.SENHA }
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

		public async Task<ClienteModel> BuscarItemByID(ClienteModel t, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				try
				{
					var p = new DynamicParameters();
					p.Add("ClienteID", t.ClienteID, DbType.Int32, ParameterDirection.Input);

					var result = (await conn.QueryAsync<dynamic>(@"SELECT [NOME],[CNPJ],[ENDERECO],[NUMERO],[COMPLEMENTO],[BAIRRO],[CEP],[CIDADE],[UF],[DATA],CC.EMAIL,CC.TELEFONE, CC.EMAIL FROM [dbo].[CLIENTES] C
					LEFT JOIN CLIENTES_CONTATO CC ON C.CLIENTEID=CC.CLIENTEID
					WHERE C.CLIENTEID=@ClienteID
					GROUP BY [NOME],[CNPJ],[ENDERECO],[NUMERO],[COMPLEMENTO],[BAIRRO],[CEP],[CIDADE],[UF],[DATA],CC.EMAIL,CC.TELEFONE, CC.EMAIL", p));

					if (result != null)
						return result.GroupBy(a => new { Nome = a.NOME, CNPJ = a.CNPJ, Endereco = a.ENDERECO, Numero = a.NUMERO, Bairro = a.BAIRRO, CEP = a.CEP, Cidade = a.CIDADE, UF = a.UF, Data = a.DATA, Complemento = a.COMPLEMENTO }, (a, b) =>
												  new ClienteModel()
												  {
													  Bairro = a.Bairro,
													  CEP = a.CEP,
													  Cidade = a.Cidade,
													  CNPJ = a.CNPJ,
													  Complemento = a.Complemento,
													  Data = a.Data,
													  Endereco = a.Endereco,
													  Nome = a.Nome,
													  Numero = a.Numero,
													  UF = a.UF,
													  Contatos = b.Select(k => new ContatoModel() { Celular = k.TELEFONE, Email = k.EMAIL })
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

		public Task<IEnumerable<ClienteModel>> BuscarItens(ClienteModel t, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItens(IEnumerable<ClienteModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<ClienteModel>> ObterTodos(ClienteModel r, int? u)
		{
			throw new
				NotImplementedException();
		}
        
        public async Task<dynamic> GetInfoEnvio(int c, int u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("UsuarioID", u, DbType.Int32, ParameterDirection.Input);

                    var result = await conn.QueryAsync(@"SELECT 
                                                            IIF(MAX(P.CODIGO) IS NOT NULL, 1, 0) AS PADRAOPOSTAGEM,
                                                            IIF(MAX(CT.CARTEIRAID) IS NOT NULL, 1, 0) AS CARTEIRA,
                                                            IIF(MAX(TP.CODIGO) IS NOT NULL, 1, 0) AS TIPOCAMPANHA,
                                                            IIF(MAX(L.LEIAUTEID) IS NOT NULL, 1, 0) AS LAYOUT,
                                                            IIF(MAX(FC.FORNECEDORID) IS NOT NULL, 1, 0) AS FORNECEDOR
                                                        FROM CLIENTES C(NOLOCK)
                                                            LEFT JOIN PADRAO_POSTAGENS P (NOLOCK) ON P.CLIENTEID = C.CLIENTEID
                                                            LEFT JOIN (SELECT CT.CARTEIRAID ,CLIENTEID
	                                                                    FROM CARTEIRAS CT(NOLOCK)
	                                                                    LEFT JOIN USUARIOS_CARTEIRA UC(NOLOCK) ON UC.CARTEIRAID = CT.CARTEIRAID
	                                                                    WHERE (@UsuarioID = 0 OR UC.USUARIOID = @UsuarioID)
		                                                                    AND CT.VISIVEL = 1 AND CT.ISEXCLUDED = 0 AND CT.BLOQUEIOENVIO = 0) CT ON CT.CLIENTEID = C.CLIENTEID
                                                            LEFT JOIN TIPOCAMPANHA TP(NOLOCK) ON TP.CLIENTEID = C.CLIENTEID
		                                                        AND TP.VISIVEL = 1
                                                            LEFT JOIN LAYOUT L (NOLOCK)ON L.CLIENTEID = C.CLIENTEID																
                                                            LEFT JOIN FORNECEDOR_CLIENTE FC(NOLOCK) ON FC.CLIENTEID = C.CLIENTEID
		                                                        AND FC.STATUSFORNECEDOR = 0
                                                        WHERE C.CLIENTEID = @ClienteID
                                                        GROUP BY C.CLIENTEID", p);

                    if (result != null && result.Any())
                    {
                        var r = result.Select(a => new
                        {
                            carteira = a.CARTEIRA == 1 ? true : false,
                            padraopostagem = a.PADRAOPOSTAGEM == 1 ? true : false,
                            tipocampanha = a.TIPOCAMPANHA == 1 ? true : false,
                            leiaute = a.LAYOUT == 1 ? true : false,
                            fornecedor = a.FORNECEDOR == 1 ? true : false
                        }).ElementAt(0);

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

    }
}
