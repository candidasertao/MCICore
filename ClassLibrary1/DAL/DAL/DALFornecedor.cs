using Dapper;
using FastMember;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class DALFornecedor : IDal<FornecedorModel>
    {
        Random rnd = new Random();

        public async Task AdicionarItensAsync(IEnumerable<FornecedorModel> t, int c, int? u)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {

                    var fornececedor = t.ElementAt(0);

                    var p = new DynamicParameters();
                    p.Add("Nome", fornececedor.Nome, DbType.String, ParameterDirection.Input, 50);
                    p.Add("CPFCNPJ", fornececedor.CPFCNPJ, DbType.String, ParameterDirection.Input, 14);
                    p.Add("Endereco", fornececedor.Endereco, DbType.String, ParameterDirection.Input, 150);
                    p.Add("Numero", fornececedor.Numero, DbType.String, ParameterDirection.Input, 10);
                    p.Add("Complemento", fornececedor.Complemento, DbType.String, ParameterDirection.Input, 100);
                    p.Add("Bairro", fornececedor.Bairro, DbType.String, ParameterDirection.Input, 100);
                    p.Add("Cidade", fornececedor.Cidade, DbType.String, ParameterDirection.Input, 150);
                    p.Add("UF", fornececedor.UF, DbType.String, ParameterDirection.Input, 2);
                    p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);

                    p.Add("FornecedorID", DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync(@"INSERT INTO [dbo].[FORNECEDOR]([NOME],[CPFCNPJ],[ENDERECO],[NUMERO],[COMPLEMENTO],[BAIRRO],[CIDADE],[UF],[DATA]) VALUES (@Nome, @CPFCNPJ, @Endereco, @Numero, @Complemento,@Bairro, @Cidade, @UF, @Data);SELECT @FornecedorID=SCOPE_IDENTITY()", p, tran, commandTimeout: 888);

                    await conn.ExecuteAsync(@"INSERT INTO [dbo].[FORNECEDOR_CONTATO]([FORNECEDORID],[TELEFONE],[EMAIL],[DESCRICAO]) VALUES (@FornecedorID, @Telefone, @Email,@Descricao)", fornececedor.Contatos.Select(m => new { Telefone = m.Celular, Email = m.Email, FornecedorID = p.Get<int>("FornecedorID"), Descricao = m.Descricao }), transaction: tran);


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

        //public async Task<int> Redistribuicao(IEnumerable<CampanhaModel> campanhas, 
        //	TipoOcorrenciaFornecedor tipoocorrencia)
        //{

        //}
        public async Task<int> AdicionarItemAsync(FornecedorModel fornececedor, int c, int? u)
        {

            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {


                    var p = new DynamicParameters();
                    p.Add("Nome", fornececedor.Nome, DbType.String, ParameterDirection.Input, 50);
                    p.Add("CPFCNPJ", fornececedor.CPFCNPJ, DbType.String, ParameterDirection.Input, 14);
                    p.Add("Endereco", fornececedor.Endereco, DbType.String, ParameterDirection.Input, 150);
                    p.Add("Numero", fornececedor.Numero, DbType.String, ParameterDirection.Input, 10);
                    p.Add("Complemento", fornececedor.Complemento, DbType.String, ParameterDirection.Input, 100);
                    p.Add("Bairro", fornececedor.Bairro, DbType.String, ParameterDirection.Input, 100);
                    p.Add("Cidade", fornececedor.Cidade, DbType.String, ParameterDirection.Input, 150);
                    p.Add("UF", fornececedor.UF, DbType.String, ParameterDirection.Input, 2);
                    p.Add("Data", DateTime.Now, DbType.DateTime, ParameterDirection.Input);

                    p.Add("FornecedorID", DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync(@"INSERT INTO [dbo].[FORNECEDOR]([NOME],[CPFCNPJ],[ENDERECO],[NUMERO],[COMPLEMENTO],[BAIRRO],[CIDADE],[UF],[DATA]) VALUES (@Nome, @CPFCNPJ, @Endereco, @Numero, @Complemento,@Bairro, @Cidade, @UF, @Data);SELECT @FornecedorID=SCOPE_IDENTITY()", p, tran, commandTimeout: 888);

                    await conn.ExecuteAsync(@"INSERT INTO [dbo].[FORNECEDOR_CONTATO]([FORNECEDORID],[TELEFONE],[EMAIL],[DESCRICAO]) VALUES (@FornecedorID, @Telefone, @Email,@Descricao)", fornececedor.Contatos.Select(m => new { Telefone = m.Celular, Email = m.Email, FornecedorID = p.Get<int>("FornecedorID"), Descricao = m.Descricao }), transaction: tran);


                    tran.Commit();


                    return p.Get<int>("FornecedorID");
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

        public async Task<IEnumerable<dynamic>> ListaFornecedoresCadastro()
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    return await conn.QueryAsync(@"SELECT CPFCNPJ, EMAIL, F.FORNECEDORID, NOME   FROM FORNECEDOR F JOIN [dbo].[FORNECEDOR_CONTATO] FC ON F.FORNECEDORID=FC.FORNECEDORID WHERE EMAIL IS NOT NULL GROUP BY CPFCNPJ, EMAIL, F.FORNECEDORID, NOME", commandTimeout: 888);


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

        public async Task<IEnumerable<FornecedorModel>> FornecedoresAtivosEnvio()
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    return await conn.QueryAsync<FornecedorModel>(@"SELECT FORNECEDORID FROM FORNECEDOR_CLIENTE FC WHERE STATUSFORNECEDOR=0 GROUP BY FORNECEDORID", commandTimeout: 888);


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
        public async Task<int> RedistribuiLotes(IEnumerable<FornecedorMinModel> t, int arquivoid, int carteiraid, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    var campanhas = (await conn.QueryAsync<CampanhaModel>("SELECT CAMPANHAID, DATAENVIAR, STATUSENVIO FROM CAMPANHAS WHERE CLIENTEID=@ClienteID AND CARTEIRAID=@CarteiraID AND ARQUIVOID=@ArquivoID AND STATUSENVIO IN(0,4) AND DATADIA=@DataDia",
                        new
                        {
                            ClienteID = c,
                            ArquivoID = arquivoid,
                            CarteiraID = carteiraid,
                            DataDia = DateTime.Now.Date
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE)).ToList();


                    foreach (var item in t)
                    {
                        foreach (var _item in campanhas.Where(a => !a.Atualizado).Take(Convert.ToInt32(Math.Ceiling((item.Distribuicao.Value / 100) * campanhas.Count()))))
                        {
                            _item.Fornecedor = new FornecedorModel() { FornecedorID = item.FornecedorID };
                            _item.Atualizado = true;
                        }
                    }

                    await conn.ExecuteAsync(@"CREATE TABLE #TMP (
											CODIGO INT PRIMARY KEY IDENTITY(1,1), 
											CAMPANHAID INT, 
											FORNECEDORID INT, 
											CLIENTEID INT,
											DATAENVIAR SMALLDATETIME,
											STATUSENVIO TINYINT)",
                                            transaction: tran);

                    using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                    {
                        using (var reader = ObjectReader.Create(campanhas.Select(m => new
                        {
                            CampanhaID = m.CampanhaID,
                            FornecedorID = m.Fornecedor.FornecedorID,
                            ClienteID = c,
                            DataEnviar = m.DataEnviar,
                            StatusEnvio = m.StatusEnvio

                        }),
                        "CampanhaID", "FornecedorID", "ClienteID", "DataEnviar", "StatusEnvio"))
                        {
                            bcp.DestinationTableName = "#TMP";
                            bcp.ColumnMappings.Add("CampanhaID", "CAMPANHAID");
                            bcp.ColumnMappings.Add("FornecedorID", "FORNECEDORID");
                            bcp.ColumnMappings.Add("ClienteID", "CLIENTEID");
                            bcp.ColumnMappings.Add("DataEnviar", "DATAENVIAR");
                            bcp.ColumnMappings.Add("StatusEnvio", "STATUSENVIO");
                            bcp.BulkCopyTimeout = 888;

                            await bcp.WriteToServerAsync(reader);
                            bcp.Close();
                        }
                    }

                    await conn.ExecuteAsync(@"UPDATE C SET FORNECEDORID=T.FORNECEDORID FROM CAMPANHAS C JOIN #TMP T ON C.CAMPANHAID=T.CAMPANHAID AND C.CLIENTEID=T.CLIENTEID", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    await conn.ExecuteAsync(@"DECLARE @TMP TABLE(QUANTIDADE INT, CARTEIRAID INT, ARQUIVOID INT, FORNECEDORID INT, DATAENVIAR SMALLDATETIME, CLIENTEID INT, STATUSENVIO TINYINT);
												WITH ENVIADAS AS(SELECT CAMPANHAID, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO FROM CAMPANHAS WHERE CLIENTEID=@ClienteID AND DATADIA=@DataDia AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID)
												INSERT @TMP												
												SELECT COUNT(CAMPANHAID) QUANTIDADE, CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, @ClienteID, STATUSENVIO  FROM ENVIADAS GROUP BY CARTEIRAID, ARQUIVOID, FORNECEDORID, DATAENVIAR, STATUSENVIO
												UPDATE FC SET QUANTIDADE=IIF(T.STATUSENVIO=4 OR T.STATUSENVIO=5,0, T.QUANTIDADE) FROM FORNECEDOR_CAMPANHAS FC JOIN @TMP T ON FC.ARQUIVOID=T.ARQUIVOID AND FC.CARTEIRAID=T.CARTEIRAID AND FC.CLIENTEID=T.CLIENTEID AND FC.FORNECEDORID=T.FORNECEDORID AND FC.DATAENVIAR=T.DATAENVIAR", new
                    {
                        DataDia = DateTime.Now.Date,
                        CarteiraID = carteiraid,
                        ArquivoID = arquivoid,
                        ClienteID = c,
                    }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    await conn.ExecuteAsync(@"DROP TABLE #TMP", transaction: tran);

                    tran.Commit();

                    return campanhas.Count;

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


        public async Task<int> AtualizaAPIKey(FornecedorModel f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("ApiKey", f.ApiKey, DbType.String, ParameterDirection.Input, 50);
                    p.Add("FornecedorID", f.FornecedorID, DbType.Int32, ParameterDirection.Input);
                    return await conn.ExecuteAsync(@"UPDATE FORNECEDOR SET APIKEY=@ApiKey WHERE FORNECEDORID=@FornecedorID", p, commandTimeout: 888);

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

        public async Task AtualizaItensAsync(IEnumerable<FornecedorModel> t, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {

                    var fornecedor = t.ElementAt(0);

                    var p = new DynamicParameters();

                    p.Add("FornecedorID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("Nome", fornecedor.Nome, DbType.String, ParameterDirection.Input);
                    p.Add("Endereco", fornecedor.Endereco, DbType.String, ParameterDirection.Input);
                    p.Add("Numero", fornecedor.Numero, DbType.String, ParameterDirection.Input);
                    p.Add("Complemento", fornecedor.Complemento, DbType.String, ParameterDirection.Input);
                    p.Add("Bairro", fornecedor.Bairro, DbType.String, ParameterDirection.Input);
                    p.Add("CEP", fornecedor.CEP, DbType.String, ParameterDirection.Input);
                    p.Add("Cidade", fornecedor.Cidade, DbType.String, ParameterDirection.Input);
                    p.Add("UF", fornecedor.UF, DbType.String, ParameterDirection.Input);

                    await conn.ExecuteAsync(@"UPDATE [dbo].[FORNECEDOR]
                                            SET [NOME] = @Nome
                                                ,[ENDERECO] = @Endereco
                                                ,[NUMERO] =@Numero
                                                ,[COMPLEMENTO] = @Complemento
                                                ,[BAIRRO] = @Bairro
                                                ,[CEP] = @CEP
                                                ,[CIDADE] = @Cidade
                                                ,[UF] = @UF
                                            WHERE FORNECEDORID=@FornecedorID", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    await conn.ExecuteAsync(@"DELETE FROM FORNECEDOR_CONTATO WHERE FORNECEDORID=@FornecedorID", p, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    if (fornecedor.Contatos != null && fornecedor.Contatos.Any())
                        await conn.ExecuteAsync(@"INSERT INTO [dbo].[FORNECEDOR_CONTATO]([FornecedorID],[TELEFONE],[EMAIL],[DESCRICAO]) VALUES (@FornecedorID, @Telefone, @Email, @Descricao)"
                                                    , fornecedor.Contatos.Select(m => new { Telefone = m.Celular, Email = m.Email, FornecedorID = c, Descricao = string.IsNullOrEmpty(m.Descricao) ? null : m.Descricao })
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
        public async Task<int> FornecedoresCliente(int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    return await conn.QuerySingleAsync<int>("SELECT COUNT(CODIGO) ITENS FROM [dbo].[FORNECEDOR_CLIENTE] WHERE CLIENTEID=@ClienteID", p);
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

        public async Task<FornecedorModel> FornecedorByLogin(FornecedorModel f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("CPFCNPJ", f.CPFCNPJ, DbType.String, ParameterDirection.Input);

                    var result = await conn.QuerySingleOrDefaultAsync<FornecedorModel>("SELECT FORNECEDORID, NOME FROM FORNECEDOR WHERE ATIVOLOGIN=1 AND [CPFCNPJ]=@CPFCNPJ", p);

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
        public async Task<FornecedorModel> BuscarItemByIDAsync(FornecedorModel t, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    var p = new DynamicParameters();
                    p.Add("FornecedorID", t.FornecedorID, DbType.Int32, ParameterDirection.Input);
                    
                    var result = await conn.QueryAsync(@"SELECT F.NOME, F.CPFCNPJ, F.ENDERECO, F.NUMERO, F.COMPLEMENTO, F.BAIRRO, F.CIDADE, F.CEP, F.UF, FC.TELEFONE, FC.EMAIL
                                                        FROM FORNECEDOR F(NOLOCK)
                                                        JOIN FORNECEDOR_CONTATO FC (NOLOCK) ON FC.FORNECEDORID = F.FORNECEDORID
                                                        WHERE  F.FORNECEDORID = @FornecedorID", p);

                    if (result != null)
                        return result.GroupBy(a => new { Nome = a.NOME, CPFCNPJ = a.CPFCNPJ, Endereco = a.ENDERECO, Numero = a.NUMERO, Bairro = a.BAIRRO, CEP = a.CEP, Cidade = a.CIDADE, UF = a.UF, Complemento = a.COMPLEMENTO }, (a, b) =>
                                                  new FornecedorModel()
                                                  {
                                                      Nome = a.Nome,
                                                      CPFCNPJ = a.CPFCNPJ,
                                                      Endereco = a.Endereco,
                                                      Numero = a.Numero,
                                                      Bairro = a.Bairro,
                                                      CEP = a.CEP,
                                                      Cidade = a.Cidade,
                                                      UF = a.UF,
                                                      Complemento = a.Complemento,                                                      
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

        public async Task AtualizaStatusFornecedorCliente(IEnumerable<FornecedorModel> f, int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    await conn.ExecuteAsync(@"UPDATE FORNECEDOR_CLIENTE SET STATUSFORNECEDOR=@StatusFornecedor, DISTRIBUICAO=@Distribuicao WHERE CODIGO=@Codigo AND CLIENTEID=@ClienteID AND FORNECEDORID=@FornecedorID",
                            f.Select(a => new
                            {
                                Codigo = a.Codigo,
                                Distribuicao = a.Distribuicao,
                                FornecedorID = a.FornecedorID,
                                ClienteID = c,
                                StatusFornecedor = (byte)a.StatusFornecedor
                            }),
                            transaction: tran,
                            commandTimeout: 888);

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

        public async Task<IEnumerable<FornecedorModel>> FornececedoresCadastro(int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);


                    var result = await conn.QueryAsync(@"SELECT  DISTRIBUICAO, DATAVINCULO, NOME, CAPACIDADEENVIO, ENVIOACADA5MIN, COUNT(CAMPANHAID) QUANTIDADE, FC.STATUSFORNECEDOR, STATUSENVIO, FC.FORNECEDORID, FC.CODIGO FROM FORNECEDOR_CLIENTE FC 
		JOIN FORNECEDOR F ON FC.FORNECEDORID = F.FORNECEDORID
		LEFT JOIN CAMPANHAS C ON FC.FORNECEDORID = C.FORNECEDORID AND C.CLIENTEID = FC.CLIENTEID AND C.DATADIA >= @DataDia AND STATUSENVIO IN(2, 0)
		WHERE FC.CLIENTEID = @ClienteID
		GROUP  BY ATIVO, VISIVEL, FC.CLIENTEID, DISTRIBUICAO, DATAVINCULO, NOME, CAPACIDADEENVIO, ENVIOACADA5MIN, DISTRIBUICAO, STATUSFORNECEDOR, c.STATUSENVIO, FC.FORNECEDORID, FC.CODIGO ORDER BY NOME", p);

                    if (result != null)
                        return result.GroupBy(a => new
                        {
                            Codigo = a.CODIGO,
                            Nome = a.NOME,
                            FornecedorID = a.FORNECEDORID,
                            Capacidade = a.CAPACIDADEENVIO == null ? 0 : a.CAPACIDADEENVIO,
                            Data = a.DATAVINCULO,
                            Distribuicao = a.DISTRIBUICAO == null ? 0 : a.DISTRIBUICAO,
                            Capacidade5M = a.ENVIOACADA5MIN == null ? 0 : a.ENVIOACADA5MIN,
                            StatusFornecedor = ((StatusFornecedorEnums)Enum.Parse(typeof(StatusFornecedorEnums), ((byte)a.STATUSFORNECEDOR).ToString()))
                        }, (a, b) => new FornecedorModel()
                        {
                            Codigo = a.Codigo,
                            Nome = a.Nome,
                            FornecedorID = a.FornecedorID,
                            Distribuicao = a.Distribuicao,
                            Data = a.Data,
                            CapacidadeTotal = a.Capacidade,
                            Entrega = rnd.Next(3, 20),
                            Capacidade5M = a.Capacidade5M,
                            Eficiencia = 97.5M,
                            StatusFornecedor = a.StatusFornecedor,
                            Quantidade = b.Any(k => k.STATUSENVIO == 2) ? b.Where(k => k.STATUSENVIO == 2).Sum(k => k.QUANTIDADE) : 0,
                            Agendados = b.Any(k => k.STATUSENVIO == 0) ? b.Where(k => k.STATUSENVIO == 0).Sum(k => k.QUANTIDADE) : 0
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

        public async Task<int> AtualizaDistribuicao(IEnumerable<FornecedorModel> f, int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();
                int itensatualizados = 0;

                try
                {
                    if (!f.Sum(a => a.Distribuicao).Equals(100M))
                        throw new Exception("O cálculo dadistribuicao deve ser igual a 100");

                    itensatualizados = await conn.ExecuteAsync(@"UPDATE [dbo].[FORNECEDOR_CLIENTE] SET DISTRIBUICAO=@Distribuicao WHERE CLIENTEID=@ClienteID AND FORNECEDORID=@FornecedorID",
                         f.Select(a => new
                         {
                             ClienteID = c,
                             Distribuicao = a.Distribuicao,
                             FornecedorID = a.FornecedorID
                         }),
                         transaction: tran,
                         commandTimeout: 888);

                    tran.Commit();

                    return itensatualizados;

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

        public async Task<IEnumerable<FornecedorModel>> Redistribuicao(int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {

                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);


                    var result = await conn.QueryAsync(@"WITH ENVIADAS AS
														(
														SELECT COUNT(CAMPANHAID) QUANTIDADE, STATUSENVIO, FORNECEDORID FROM CAMPANHAS WHERE CLIENTEID = @ClienteID AND DATADIA = @DataDia GROUP BY STATUSENVIO, FORNECEDORID
														)
														SELECT QUANTIDADE ENVIOSHOJE, [CAPACIDADEENVIO] CAPACIDADE, NOME, DISTRIBUICAO, STATUSENVIO, FC.FORNECEDORID, STATUSFORNECEDOR FROM FORNECEDOR_CLIENTE FC
														JOIN FORNECEDOR F ON FC.FORNECEDORID = F.FORNECEDORID
														LEFT JOIN ENVIADAS E ON FC.FORNECEDORID = E.FORNECEDORID
														WHERE[CAPACIDADEENVIO] IS NOT NULL AND DISTRIBUICAO IS NOT NULL AND FC.CLIENTEID=@ClienteID ORDER BY NOME", p);

                    if (result.Any())
                    {
                        var dados = result
                            .GroupBy(a => new FornecedorModel()
                            {
                                FornecedorID = a.FORNECEDORID,
                                Distribuicao = a.DISTRIBUICAO,
                                Nome = a.NOME,
                                Eficiencia = 98.9M,
                                StatusFornecedor = ((StatusFornecedorEnums)Enum.Parse(typeof(StatusFornecedorEnums), ((byte)a.STATUSFORNECEDOR).ToString())),
                                CapacidadeGlobal = a.CAPACIDADE,
                                CapacidadeTotal = a.ENVIOSHOJE == null ? 0 : Math.Round((decimal)a.ENVIOSHOJE / (decimal)a.CAPACIDADE * 100, 2),
                                Entrega = rnd.Next(3, 65)
                            },
                            (a, b) => new FornecedorModel()
                            {
                                FornecedorID = a.FornecedorID,
                                StatusFornecedor = a.StatusFornecedor,
                                Distribuicao = a.Distribuicao,
                                Nome = a.Nome,
                                CapacidadeGlobal = a.CapacidadeGlobal,
                                EnviosHoje = a.CapacidadeTotal == 0 ? 0 : b.Sum(m => m.ENVIOSHOJE),
                                CapacidadeTotal = a.CapacidadeTotal == 0 ? a.CapacidadeGlobal : Math.Round((decimal)b.Sum(m => m.ENVIOSHOJE) / (decimal)a.CapacidadeGlobal * 100, 2),
                                Eficiencia = 98.9M,
                                Agendados = a.CapacidadeTotal == 0 ? 0 : b.Where(option => option.STATUSENVIO == (byte)4).Sum(option => option.ENVIOSHOJE ?? 0),
                                Entrega = rnd.Next(3, 65),
                                EntregaTime = TimeSpan.FromMinutes(rnd.Next(3, 65))
                            }, new CompareObject<FornecedorModel>((a, b) => a.FornecedorID == b.FornecedorID, i => i.FornecedorID.GetHashCode()));

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
            //            SELECT COUNT(*) QUANTFORNECEDOR, C.FORNECEDORID, FC.QUANTIDADE, DISTRIBUICAO FROM CAMPANHAS C
            //JOIN(SELECT NOME, ATIVO, VISIVEL, SUM(CAPACIDADE) QUANTIDADE, FORNECEDORID, CLIENTEID, DISTRIBUICAO FROM FORNECEDORES_CAPACIDADE GROUP BY NOME, ATIVO, VISIVEL, FORNECEDORID, CLIENTEID, DISTRIBUICAO) FC ON C.FORNECEDORID = FC.FORNECEDORID AND FC.CLIENTEID = C.CLIENTEID
            //WHERE C.CLIENTEID = 1 AND DATADIA = '2017-4-14' GROUP BY C.FORNECEDORID, FC.FORNECEDORID, FC.QUANTIDADE, DISTRIBUICAO

        }

        public async Task<bool> IsApiKeyFornecedor(FornecedorModel f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("ApiKey", f.ApiKey, DbType.String, ParameterDirection.Input, 50);
                    p.Add("FornecedorID", f.FornecedorID, DbType.Int32, ParameterDirection.Input);

                    var result = await conn.QueryFirstOrDefaultAsync<int>(@"SELECT COUNT(FORNECEDORID) QUANT FROM FORNECEDOR WHERE FORNECEDORID=@FornecedorID AND APIKEY=@ApiKey", p, commandTimeout: 888);

                    return result > 0;

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

        public async Task<(
                        IEnumerable<ConsolidadoModel> item1,
                        IEnumerable<ConsolidadoModel> item2,
                        IEnumerable<ConsolidadoModel> item3,
                        FornecedorModel f
                        )> Relatorio(ConsolidadoModel cm, int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataInicial", cm.DataInicial, DbType.Date, ParameterDirection.Input);
                    p.Add("DataFinal", cm.DataFinal, DbType.Date, ParameterDirection.Input);
                    p.Add("FornecedorID", cm.FornecedorID, DbType.Int32, ParameterDirection.Input);
                    p.Add("CarteiraID", cm.CarteiraID, DbType.Int32, ParameterDirection.Input);

                    var query = string.Format(@"SELECT SUM(ENVIADA) ENVIADA, SUM(ENTREGUE) ENTREGUE, SUM(EXPIRADA) EXPIRADA, SUM(EXCLUIDA) EXCLUIDA, SUM(ERRO) ERRO, SUM(ENVIADAS) ENVIADAS, FORNECEDORID, DATADIA, CARTEIRAID
                                                FROM [dbo].[CAMPANHAS_CONSOLIDADO]
								                WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND CLIENTEID=@ClienteID AND FORNECEDORID=@FornecedorID {0} GROUP BY DATADIA, FORNECEDORID, CARTEIRAID;
								                SELECT RC.FORNECEDORID, RC.QUANTIDADE, RC.CLASSIFICACAOID, RC.CLIENTEID, RC.DATADIA, RC.CARTEIRAID, RC.ARQUIVOID, RC.USUARIOID, R.CLASSIFICACAO
                                                FROM RETORNO_CONSOLIDADO RC(NOLOCK)
                                                INNER JOIN RETORNO_CLASSIFICACAO R (NOLOCK)ON R.CODIGO = RC.CLASSIFICACAOID
                                                WHERE RC.DATADIA BETWEEN @DataInicial AND @DataFinal AND RC.CLIENTEID=@ClienteID AND RC.FORNECEDORID=@FornecedorID {0};
								                SELECT SUM(ENVIADA) ENVIADA, DATADIA FROM [dbo].[CAMPANHAS_CONSOLIDADO] WHERE DATADIA BETWEEN @DataInicial AND @DataFinal AND CLIENTEID=@ClienteID {0} GROUP BY DATADIA;
								                WITH ENVIADAS AS(SELECT CAST(IIF([DATAENVIOFORNECEDOR] IS NOT NULL, DATEDIFF(SECOND, DATAENVIAR, [DATAENVIOFORNECEDOR]),0) AS BIGINT) DIFERENCA FROM CAMPANHAS WHERE CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal AND STATUSENVIO=2 AND FORNECEDORID=@FornecedorID {0} ) 
								                SELECT AVG(DIFERENCA) ATRASO, DISTRIBUICAO, CAPACIDADEENVIO CAPACIDADETOTAL, [ENVIOACADA5MIN] CAPACIDADE5M  FROM ENVIADAS 
								                JOIN FORNECEDOR_CLIENTE FC ON FC.FORNECEDORID=@FornecedorID AND FC.CLIENTEID=@ClienteID
								                JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID
								                GROUP BY FC.CAPACIDADEENVIO, [ENVIOACADA5MIN], DISTRIBUICAO", cm.CarteiraID.HasValue ? " AND CARTEIRAID = @CarteiraID " : string.Empty);


                    if (cm.CarteiraID.HasValue)
                        query = query.Insert(query.LastIndexOf("AND CLIENTEID"), "AND CARTEIRAID=@CarteiraID ");


                    var result = await conn.QueryMultipleAsync(query, p);

                    if (result != null)
                    {
                        var dados1 = await result.ReadAsync();
                        var consolidado = dados1.Select(a => new ConsolidadoModel()
                        {
                            FornecedorID = a.FORNECEDORID,
                            DataDia = a.DATADIA,
                            Entregues = a.ENTREGUE,
                            Enviados = a.ENVIADA,
                            Enviadas = a.ENVIADAS,
                            Erros = a.ERRO,
                            CarteiraID = a.CARTEIRAID,
                            Excluidas = a.EXCLUIDA
                        });

                        var dados2 = await result.ReadAsync();
                        var retornoconsolidado = dados2.Select(a => new ConsolidadoModel()
                        {
                            FornecedorID = a.FORNECEDORID,
                            DataDia = a.DATADIA,
                            CarteiraID = a.CARTEIRAID,
                            ArquivoID = a.ARQUIVOID,
                            Classificacao = a.CLASSIFICACAO,
                            ClassificacaoID = a.CLASSIFICACAOID,
                            Quantidade = a.QUANTIDADE
                        });

                        var dados3 = await result.ReadAsync();
                        var dadosConsolidados = dados3.Select(a => new ConsolidadoModel()
                        {
                            Enviados = a.ENVIADA,
                            DataDia = a.DATADIA
                        });

                        var f = await result.ReadSingleOrDefaultAsync();

                        return (consolidado, retornoconsolidado, dadosConsolidados,
                             f != null ? new FornecedorModel()
                             {
                                 Atraso = TimeSpan.FromSeconds(f.ATRASO),
                                 Distribuicao = f.DISTRIBUICAO,
                                 CapacidadeTotal = f.CAPACIDADETOTAL,
                                 Capacidade5M = f.CAPACIDADE5M
                             } : null);

                    }
                    return (new ConsolidadoModel[] { }, new ConsolidadoModel[] { }, new ConsolidadoModel[] { }, null);

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

        public Task<IEnumerable<FornecedorModel>> BuscarItensAsync(FornecedorModel t, string s, int? u)
        {
            throw new NotImplementedException();
        }

        public async Task<(IEnumerable<FornecedorCampanhaModel>, IEnumerable<CampanhaModel>)> Monitoria(int c)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

                    var result = await conn.QueryMultipleAsync(@"WITH ENVIADAS AS
																(
																    SELECT CAMPANHAID, FORNECEDORID, STATUSENVIO, DATAENVIOFORNECEDOR AS DATAENVIAR, DATEDIFF(SECOND, DATAENVIAR, ISNULL(DATAENVIOFORNECEDOR, DATAENVIAR)) DIFERENCA FROM CAMPANHAS (NOLOCK) WHERE DATADIA=@DataDia AND CLIENTEID=@ClienteID AND STATUSENVIO IN(2,0)
																)
																SELECT AVG(CAST(DIFERENCA AS BIGINT)) DIFERENCA, COUNT(CAMPANHAID) QUANTIDADE, STATUSENVIO, FC.CAPACIDADEENVIO CAPACIDADE, NOME, DATAENVIAR, FC.FORNECEDORID  FROM FORNECEDOR_CLIENTE FC
																		JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID
																		LEFT JOIN ENVIADAS E ON E.FORNECEDORID=FC.FORNECEDORID AND FC.CLIENTEID=@ClienteID
																		WHERE FC.STATUSFORNECEDOR=0
																		GROUP BY STATUSENVIO, FC.CAPACIDADEENVIO, NOME, DATAENVIAR, FC.FORNECEDORID

																SELECT SUM(QUANTIDADE) QUANTIDADE, DATEPART(HOUR,DATAENVIAR) DATAENVIAR, FORNECEDORID FROM FORNECEDOR_CAMPANHAS WHERE DATAENVIAR BETWEEN @DataDia AND DATEADD(MINUTE, 1439, @DataDia) AND CLIENTEID=@ClienteID GROUP BY DATEPART(HOUR,DATAENVIAR), FORNECEDORID;", p);

                    if (result != null)
                    {
                        var dados1 = await result.ReadAsync();
                        var dados2 = await result.ReadAsync();

                        var total = dados1.Sum(a => a.QUANTIDADE);

                        return (
                            dados1.Select(a => new FornecedorCampanhaModel()
                            {
                                Quantidade = a.QUANTIDADE,
                                Atraso = (int)(a.DIFERENCA ?? 0),
                                Distribuicao = Util.Percentual(a.QUANTIDADE, total),
                                DataEnviar = a.DATAENVIAR ?? DateTime.Now,
                                Nome = a.NOME,
                                Capacidade = a.CAPACIDADE,
                                StatusEnvio = (byte)(a.STATUSENVIO ?? 0),
                                FornecedorID = a.FORNECEDORID
                            }).OrderBy(k => k.Nome),
                        dados2.Select(a => new CampanhaModel()
                        {
                            Quantidade = a.QUANTIDADE,
                            Hora = a.DATAENVIAR,
                            FornecedorID = a.FORNECEDORID
                        }));
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

        public Task ExcluirItensAsync(IEnumerable<FornecedorModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }
        Random rndEnviado = new Random();

        public async Task<IEnumerable<FornecedorModel>> DashBoard(int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

                    var result = await conn.QueryAsync(@"WITH ENVIADAS AS (SELECT CAMPANHAID ,IIF([DATAENVIOFORNECEDOR] IS NOT NULL, DATEDIFF(SECOND, DATAENVIAR, [DATAENVIOFORNECEDOR]), 0) DIFERENCA ,FORNECEDORID ,CLIENTEID FROM CAMPANHAS(NOLOCK) WHERE CLIENTEID = @ClienteID AND DATADIA = @DataDia AND STATUSENVIO = 2)
                                                        SELECT F.NOME ,F.FORNECEDORID , FC.CAPACIDADEENVIO AS CAPACIDADE ,IIF(B.DIFERENCA IS NOT NULL, B.DIFERENCA, 0) AS DIFERENCA ,IIF(B.QUANTIDADE IS NOT NULL, B.QUANTIDADE, 0) AS QUANTIDADE
                                                        FROM FORNECEDOR_CLIENTE FC(NOLOCK)
                                                        INNER JOIN FORNECEDOR F(NOLOCK) ON F.FORNECEDORID = FC.FORNECEDORID
                                                        LEFT JOIN (SELECT AVG(CAST(E.DIFERENCA AS BIGINT)) DIFERENCA ,E.FORNECEDORID ,E.CLIENTEID ,COUNT(E.CAMPANHAID) QUANTIDADE FROM ENVIADAS E(NOLOCK) GROUP BY E.FORNECEDORID ,E.CLIENTEID) AS B ON B.FORNECEDORID = FC.FORNECEDORID
                                                        WHERE FC.CLIENTEID = @ClienteID AND FC.STATUSFORNECEDOR = 0;", p);

                    if (result != null || result.Any())
                    {
                        var total = result.Select(x => (decimal)x.QUANTIDADE).Sum();

                        var r = result.Select(a => new FornecedorModel()
                        {
                            Quantidade = a.QUANTIDADE,
                            Atraso = a.DIFERENCA == null ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(a.DIFERENCA),
                            Distribuicao = a.QUANTIDADE > 0 ? Math.Round(((decimal)a.QUANTIDADE * 100) / total, 2) : 0,
                            Nome = a.NOME,
                            CapacidadeTotal = a.CAPACIDADE,
                            FornecedorID = a.FORNECEDORID
                        }).OrderBy(k => k.Nome);

                        return r;
                    }
                    return new FornecedorModel[] { };

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
        public async Task<IEnumerable<FornecedorModel>> FornecedoresTelaEnvio(int clienteid)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    var p = new DynamicParameters();
                    p.Add("ClienteID", clienteid, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.DateTime, ParameterDirection.Input);

                    var result = await conn.QueryAsync(@"SELECT NOME, FC.FORNECEDORID, F.DATA, [CAPACIDADEENVIO] CAPACIDADE, DISTRIBUICAO, C.STATUSENVIO, COUNT(CAMPANHAID) QUANTIDADE, FC.ENVIOACADA5MIN FROM FORNECEDOR_CLIENTE FC 
															JOIN FORNECEDOR F ON FC.FORNECEDORID=F.FORNECEDORID
															LEFT JOIN CAMPANHAS C ON FC.FORNECEDORID=C.FORNECEDORID AND C.CLIENTEID=FC.CLIENTEID AND C.DATADIA>=@DataDia AND STATUSENVIO IN(2,0)
															WHERE FC.CLIENTEID=@ClienteID AND STATUSFORNECEDOR=0
															GROUP BY NOME, FC.FORNECEDORID, F.DATA, [CAPACIDADEENVIO], DISTRIBUICAO, C.STATUSENVIO, FC.ENVIOACADA5MIN ORDER BY NOME", p);

                    if (result != null)
                        return result.GroupBy(a => new { Nome = a.NOME, FornecedorID = a.FORNECEDORID, Capacidade = a.CAPACIDADE, Data = a.DATA, Distribuicao = a.DISTRIBUICAO, Capacidade5M = a.ENVIOACADA5MIN }, (a, b) => new FornecedorModel()
                        {
                            Nome = a.Nome,
                            FornecedorID = a.FornecedorID,
                            Distribuicao = a.Distribuicao,
                            Data = a.Data,
                            CapacidadeTotal = a.Capacidade,
                            Entrega = rnd.Next(3, 20),
                            Capacidade5M = a.Capacidade5M,
                            Eficiencia = 97.5M,
                            Quantidade = b.Any(k => k.STATUSENVIO == 2) ? b.Where(k => k.STATUSENVIO == 2).Sum(k => k.QUANTIDADE) : 0,
                            Agendados = b.Any(k => k.STATUSENVIO == 0) ? b.Where(k => k.STATUSENVIO == 0).Sum(k => k.QUANTIDADE) : 0
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

        public Task<IEnumerable<FornecedorModel>> ObterTodosAsync(FornecedorModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task ExcluirItensUpdateAsync(IEnumerable<FornecedorModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FornecedorModel>> ObterTodosPaginadoAsync(FornecedorModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<FornecedorClienteModel>> ObterTodosPaginadoFornecedorClienteAsync(FornecedorClienteModel t, int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    var p = new DynamicParameters();
                    p.Add("FornecedorID", f, DbType.Int32, ParameterDirection.Input);
                    p.Add("Status", t.isIntegrado, DbType.Boolean, ParameterDirection.Input);
                    p.Add("Data", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

                    if (t.PaginaAtual.HasValue)
                    {
                        if (t.PaginaAtual.Value == 0)
                            t.PaginaAtual = 1;
                    }
                    else
                        t.PaginaAtual = 1;

                    var query = string.Format(@"WITH CONTATO AS (
	                                                SELECT TELEFONE, EMAIL, CLIENTEID, ROW_NUMBER() OVER (PARTITION BY CLIENTEID ORDER BY CODIGO ASC) AS REGISTRO
	                                                FROM CLIENTES_CONTATO
                                                )
                                                SELECT FC.CODIGO, F.FORNECEDORID, F.NOME AS FORNECEDOR, FC.DISTRIBUICAO, 
                                                FC.CAPACIDADEENVIO, FC.ENVIOACADA5MIN, FC.TIPOSMS, FC.STATUSFORNECEDOR, FC.USUARIO, FC.SENHA, FC.TOKEN, FC.STATUSOPERACIONAL
                                                ,CAST(CASE WHEN FC.STATUSFORNECEDOR IN (0, 1, 2) THEN 1 WHEN  FC.STATUSFORNECEDOR = 3 THEN 0 END AS BIT) ISINTEGRADO
                                                ,C.CNPJ, CC.TELEFONE, CC.EMAIL, C.CLIENTEID, C.NOME AS CLIENTE
                                                FROM FORNECEDOR_CLIENTE FC (NOLOCK)
                                                JOIN FORNECEDOR F (NOLOCK)ON F.FORNECEDORID = FC.FORNECEDORID
                                                JOIN CLIENTES C (NOLOCK)ON C.CLIENTEID = FC.CLIENTEID
                                                LEFT JOIN CONTATO CC ON CC.CLIENTEID = C.CLIENTEID
	                                                AND CC.REGISTRO = 1
                                                WHERE FC.FORNECEDORID = @FornecedorID {0} ;
                                                SELECT CODIGO, FORNECEDORID, CLIENTEID, CAPACIDADE, DATA_INICIAL AS DATAINICIAL, DATA_FINAL AS DATAFINAL
                                                FROM [FORNECEDOR_CAPACIDADE_EXTRA]
                                                WHERE [DATA_INICIAL] >= @Data OR [DATA_FINAL] >= @Data"
                                                , t.isIntegrado ? " AND FC.STATUSFORNECEDOR IN (0, 1) " : " AND FC.STATUSFORNECEDOR IN (2, 3) ");

                    var result = await conn.QueryMultipleAsync(query, p);

                    if (result != null)
                    {
                        var dados1 = await result.ReadAsync();
                        var dados2 = await result.ReadAsync();

                        var r = dados1.Select(a =>
                        new FornecedorClienteModel
                        {
                            Codigo = a.CODIGO,
                            Fornecedor = new FornecedorModel { FornecedorID = a.FORNECEDORID, Nome = a.FORNECEDOR },
                            Cliente = new ClienteModel { ClienteID = a.CLIENTEID, Nome = a.CLIENTE, Email = a.EMAIL, Telefone = a.TELEFONE ?? 0, CNPJ = a.CNPJ },
                            Distribuicao = a.DISTRIBUICAO,
                            Capacidade = a.CAPACIDADEENVIO,
                            Envio5min = a.ENVIOACADA5MIN,
                            Tipo = (Tipo?)a.TIPOSMS,
                            Usuario = a.USUARIO,
                            Senha = a.SENHA,
                            Token = a.TOKEN,
                            StatusOperacional = a.STATUSOPERACIONAL,
                            isIntegrado = a.ISINTEGRADO,
                            Capacidades = dados2.Where(k => k.CLIENTEID == a.CLIENTEID && k.FORNECEDORID == a.FORNECEDORID).Select(e => new FornecedorCapacidadeExtraModel
                            {
                                Codigo = e.CODIGO,
                                Capacidade = e.CAPACIDADE,
                                DataInicial = e.DATAINICIAL,
                                DataFinal = e.DATAFINAL
                            }),
                            Registros = dados1.Count(),
                        })
                        .Skip((t.PaginaAtual.Value - 1) * t.Registros)
                        .Take(t.Registros);

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

        public async Task AdicionarFornecedorCapacidadeExtraAsync(IEnumerable<FornecedorCapacidadeExtraModel> t, int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var query = @"INSERT INTO FORNECEDOR_CAPACIDADE_EXTRA ([FORNECEDORID], [CLIENTEID], [CAPACIDADE] ,[DATA_INICIAL] ,[DATA_FINAL]) VALUES (@FornecedorID, @ClienteiD, @Capacidade, @DataInicial, @DataFinal)";

                        await conn.ExecuteAsync(query, new
                        {
                            FornecedorID = f,
                            ClienteiD = o.ClienteID,
                            Capacidade = o.Capacidade,
                            DataInicial = o.DataInicial,
                            DataFinal = o.DataFinal
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }
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

        public async Task AtualizarFornecedorClienteAsync(FornecedorClienteModel t, int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    var statusFornecedorOld = await conn.QuerySingleAsync<int>(@"SELECT STATUSFORNECEDOR FROM FORNECEDOR_CLIENTE WHERE CODIGO = @Codigo AND FORNECEDORID = @FornecedorID", new
                    {
                        Codigo = t.Codigo,
                        FornecedorID = f
                    }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

                    var status = t.StatusFornecedor;

                    if (statusFornecedorOld <= 1 && status == 2)
                        status = statusFornecedorOld;

                    var query = @"UPDATE FORNECEDOR_CLIENTE SET 
                                    CAPACIDADEENVIO = @CapacidadeDeEnvio,
                                    ENVIOACADA5MIN = @EnvioaCada5min,
                                    TIPOSMS = @TipoSms,
                                    STATUSFORNECEDOR = @StatusFornecedor,
                                    USUARIO = @Usuario,
                                    SENHA = @Senha,
                                    TOKEN = @Token
                                WHERE CODIGO = @Codigo AND FORNECEDORID = @FornecedorID";

                    await conn.ExecuteAsync(query, new
                    {
                        CapacidadeDeEnvio = t.Capacidade,
                        EnvioaCada5min = t.Envio5min,
                        TipoSms = (int?)t.Tipo,
                        StatusFornecedor = status,
                        Usuario = t.Usuario,
                        Senha = t.Senha,
                        Token = t.Token,
                        Codigo = t.Codigo,
                        FornecedorID = f
                    }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

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

        public async Task RemoverFornecedorCapacidadeExtraAsync(IEnumerable<FornecedorCapacidadeExtraModel> t, int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var query = @"DELETE [FORNECEDOR_CAPACIDADE_EXTRA] WHERE CODIGO = @Codigo AND FORNECEDORID = @FornecedorID";

                        await conn.ExecuteAsync(query, new
                        {
                            Codigo = o.Codigo,
                            FornecedorID = f
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }

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


        public async Task<FornecedorMonitoria> MonitoriaFornecedor(int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("FornecedorID", f, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);
                    p.Add("Agora", DateTime.Now, DbType.Date, ParameterDirection.Input);

                    var query = @"WITH ENVIOS AS
                                (
	                                SELECT CAMPANHAID, CLIENTEID, FORNECEDORID, STATUSENVIO, DATEPART(HOUR, DATAENVIAR) HORA, CAST(DATEDIFF(SECOND, DATAENVIAR, ISNULL(DATAENVIOFORNECEDOR, DATAENVIAR))AS BIGINT) DIFERENCA
	                                FROM CAMPANHAS (NOLOCK)
	                                WHERE DATADIA = @DataDia AND FORNECEDORID = @FornecedorID AND STATUSENVIO <= 3
                                )
                                SELECT 
                                C.NOME AS CLIENTE,
                                C.CLIENTEID,
                                SUM (IIF(E.STATUSENVIO = 2, 1, 0)) AS ENVIADOS,
                                SUM (IIF(E.STATUSENVIO IN (2, 3), 1, 0)) AS RECEBIDOS,
                                IIF(FCS.QUANTIDADE IS NOT NULL, FCS.QUANTIDADE, 0) AS PREVISTOS,
                                SUM (IIF(E.STATUSENVIO = 3, 1, 0)) AS ERROS,
                                E.HORA,
                                FC.CAPACIDADEENVIO AS CAPACIDADE,
                                AVG(E.DIFERENCA) AS ATRASO
                                FROM FORNECEDOR_CLIENTE FC 
                                JOIN CLIENTES C (NOLOCK) ON C.CLIENTEID = FC.CLIENTEID
                                LEFT JOIN ENVIOS E (NOLOCK) ON E.CLIENTEID = FC.CLIENTEID
	                                AND E.FORNECEDORID = FC.FORNECEDORID
                                LEFT JOIN (
	                                SELECT QUANTIDADE, CLIENTEID, FORNECEDORID, DATEPART(HH, DATAENVIAR) HORA FROM FORNECEDOR_CAMPANHAS (NOLOCK)
	                                WHERE FORNECEDORID = @FornecedorID
	                                AND CAST(DATAENVIAR AS DATE) = @DataDia
	                                ) FCS ON FCS.CLIENTEID = FC.CLIENTEID AND FCS.HORA = E.HORA
                                WHERE FC.FORNECEDORID = @FornecedorID
                                GROUP BY C.NOME, C.CLIENTEID, E.HORA, FC.CAPACIDADEENVIO, FCS.QUANTIDADE;

                                SELECT CODIGO, DATAINICIO, DATAFIM, ISATIVO
                                FROM FORNECEDOR_SERVICO (NOLOCK)
                                WHERE FORNECEDORID = @FornecedorID
	                                AND (DATAFIM >= @Agora OR DATAFIM IS NULL)";

                    var result = await conn.QueryMultipleAsync(query, p);

                    if (result != null)
                    {

                        var data1 = await result.ReadAsync();
                        var data2 = await result.ReadAsync();

                        var r = new FornecedorMonitoria
                        {
                            Detalhamento = data1.GroupBy(g => new { g.CLIENTE, g.CLIENTEID, g.CAPACIDADE }, (a, g) => new DetalhamentoFornecedorMonitoria
                            {
                                Cliente = a.CLIENTE,
                                ClienteID = a.CLIENTEID,
                                Enviado = g.Sum(k => k.ENVIADOS),
                                Recebido = g.Sum(k => k.RECEBIDOS),
                                Previsto = g.Sum(k => k.PREVISTOS),
                                Erro = g.Sum(k => k.ERROS),
                                Capacidade = a.CAPACIDADE,
                                Consumo = Util.Percentual(g.Sum(k => k.PREVISTOS), a.CAPACIDADE)
                            }).OrderBy(k => k.Cliente),
                            Grafico = new GraficoFornecedorMonitoria
                            {
                                Previsto = data1.Sum(k => k.PREVISTOS),
                                Recebido = data1.Sum(k => k.RECEBIDOS),
                                Enviado = data1.Sum(k => k.ENVIADOS),
                                Erro = data1.Sum(k => k.ERROS),
                                Consumo = Util.Percentual(data1.Sum(k => k.PREVISTOS), data1.GroupBy(g => new { g.CLIENTEID, g.CAPACIDADE }, (a, g) => new { CAPACIDADE = a.CAPACIDADE }).Sum(k => k.CAPACIDADE))
                            },
                            Clientes = data1.GroupBy(g => new { g.CLIENTE, g.CLIENTEID, g.CAPACIDADE }, (a, g) => new ClienteFornecedorMonitoria
                            {
                                Cliente = a.CLIENTE,
                                ClienteID = a.CLIENTEID,
                                Capacidade = a.CAPACIDADE,
                                Entrega = g.Where(k => k.ATRASO != null).Any() ? TimeSpan.FromSeconds(g.Where(k => k.ATRASO != null).Average(k => (long)k.ATRASO)) : TimeSpan.FromSeconds(0),
                                Eficiencia = 0,
                                Lancamentos = g.Where(k => k.HORA != null).Select(h => new LancamentoFornecedorMonitoria
                                {
                                    Hora = h.HORA,
                                    Previsto = h.PREVISTOS,
                                    Recebido = h.RECEBIDOS,
                                    Enviado = h.ENVIADOS,
                                    Erro = h.ERROS
                                })
                            }),
                            Servico = data2.Select(s => new FornecedorServicoModel
                            {
                                Id = s.CODIGO,
                                DataInicio = s.DATAINICIO,
                                DataFim = s.DATAFIM,
                                isAtivo = s.ISATIVO
                            })
                        };

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

        public async Task<dynamic> Previsto(int f)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var p = new DynamicParameters();
                    p.Add("FornecedorID", f, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

                    var query = string.Format(@"WITH PREVISTOS AS (
	                                                SELECT CAMPANHAID, CLIENTEID, FORNECEDORID, CAST(DATAENVIAR AS DATE) AS DATA
	                                                FROM CAMPANHAS C (NOLOCK)
	                                                WHERE C.DATADIA >= @DataDia
		                                                AND STATUSENVIO = 0
		                                                AND FORNECEDORID = @FornecedorID
                                                )
                                                SELECT C.NOME, P.DATA, FC.CAPACIDADEENVIO AS CAPACIDADE, COUNT(P.CAMPANHAID) PREVISTOS
                                                FROM PREVISTOS P (NOLOCK)
                                                JOIN FORNECEDOR_CLIENTE FC (NOLOCK) ON FC.FORNECEDORID = P.FORNECEDORID
	                                                AND FC.CLIENTEID = P.CLIENTEID
                                                JOIN CLIENTES C (NOLOCK) ON C.CLIENTEID = P.CLIENTEID
                                                GROUP BY C.NOME, P.DATA, FC.CAPACIDADEENVIO");

                    var result = await conn.QueryAsync(query, p);

                    if (result != null)
                    {
                        var r = new
                        {
                            datainicial = result.Min(k => k.DATA),
                            datafinal = result.Max(k => k.DATA),
                            previstos = result.GroupBy(g => new { g.DATA }, (a, b) => new
                            {
                                data = a.DATA,
                                clientes = b.Select(c => new
                                {
                                    nome = c.NOME,
                                    previstos = c.PREVISTOS,
                                    capacidade = c.CAPACIDADE,
                                    consumo = Util.Percentual(c.PREVISTOS, c.CAPACIDADE)
                                }).OrderBy(k => k.nome),
                                total = new
                                {
                                    previstos = b.Sum(k => k.PREVISTOS),
                                    capacidade = b.Sum(k => k.CAPACIDADE),
                                    consumo = Util.Percentual(b.Sum(k => k.PREVISTOS), b.Sum(k => k.CAPACIDADE))
                                }
                            }).OrderBy(k => k.data)
                        };

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

        public async Task AdicionarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var validacao = @"SELECT COUNT(*) 
                                        FROM FORNECEDOR_SERVICO(NOLOCK)
                                        WHERE FORNECEDORID = @FornecedorID	
	                                        AND ((DATAINICIO between @DataInicial and @DataFinal) OR (DATAFIM between @DataInicial and @DataFinal))";

                        var existe = await conn.QuerySingleAsync<int>(validacao, new { FornecedorID = f, DataInicial = o.DataInicio, DataFinal = o.DataFim }, tran);

                        if (!o.DataFim.HasValue)
                            o.isAtivo = true;

                        if (existe > 0)
                            throw new Exception("Uma interrupção de serviço já está agendada para periodo informado.");

                        var query = string.Format(@"INSERT INTO FORNECEDOR_SERVICO ([FORNECEDORID], [DATAINICIO], [DATAFIM], [ISATIVO]) VALUES (@FornecedorID, @DataInicio, @DataFim, @Ativo); {0}",
                                                    o.isAtivo ? "UPDATE FORNECEDOR_CLIENTE SET STATUSOPERACIONAL = 1 WHERE FORNECEDORID = @FornecedorID" : "");

                        await conn.ExecuteAsync(query, new
                        {
                            FornecedorID = f,
                            DataInicio = o.DataInicio,
                            DataFim = o.DataFim,
                            Ativo = o.isAtivo
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }

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

        public async Task<bool> isPodeAgendarInterrupcaoServico(int f, int? id)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();

                try
                {
                    var query = string.Format(@"SELECT COUNT(*)
                                FROM FORNECEDOR_SERVICO(NOLOCK)
                                WHERE FORNECEDORID = @FornecedorID
	                                AND DATAFIM IS NULL {0}", id.HasValue ? "AND CODIGO <> @Codigo" : string.Empty);

                    var ilimitado = await conn.QuerySingleAsync<int>(query, new { FornecedorID = f, Codigo = id });

                    return (ilimitado == 0);
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

        public async Task AtualizarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var validacao = @"SELECT COUNT(*) 
                                        FROM FORNECEDOR_SERVICO(NOLOCK)
                                        WHERE FORNECEDORID = @FornecedorID	
	                                        AND ((DATAINICIO between @DataInicial and @DataFinal) OR (DATAFIM between @DataInicial and @DataFinal))";

                        var existe = await conn.QuerySingleAsync<int>(validacao, new { FornecedorID = f, DataInicial = o.DataInicio, DataFinal = o.DataFim }, tran);

                        if (!o.DataFim.HasValue)
                            o.isAtivo = true;

                        if (existe > 0)
                            throw new Exception("Uma interrupção de serviço já está agendada para periodo informado.");

                        var query = string.Format(@"UPDATE FORNECEDOR_SERVICO SET [DATAINICIO] = @DataInicio, [DATAFIM] = @DataFim, [ISATIVO] = @isAtivo WHERE [FORNECEDORID] = @FornecedorID AND CODIGO = @Id; {0}",
                                                    o.isAtivo ? "UPDATE FORNECEDOR_CLIENTE SET STATUSOPERACIONAL = 1 WHERE FORNECEDORID = @FornecedorID" : "");

                        await conn.ExecuteAsync(query, new
                        {
                            FornecedorID = f,
                            DataInicio = o.DataInicio,
                            DataFim = o.DataFim,
                            Id = o.Id,
                            isAtivo = o.isAtivo
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }

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

        public async Task FinalizarFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var queryCheck = @"SELECT ISATIVO
                                        FROM FORNECEDOR_SERVICO (NOLOCK)
                                        WHERE [FORNECEDORID] = @FornecedorID AND CODIGO = @Id";

                        var check = await conn.QueryAsync<bool>(queryCheck, new { FornecedorID = f, Id = o.Id }, tran);

                        if (check == null || !check.Any())
                            throw new Exception("Interrupção de serviço não localizada.");

                        if (!check.ElementAt(0))
                            throw new Exception("A interrupção de serviço já foi finalizada.");

                        var query = @"UPDATE FORNECEDOR_SERVICO SET [DATAFIM] = @Hoje, [ISATIVO] = 0 WHERE [FORNECEDORID] = @FornecedorID AND CODIGO = @Id;
                                    UPDATE FORNECEDOR_CLIENTE SET STATUSOPERACIONAL = 0 WHERE FORNECEDORID = @FornecedorID";

                        await conn.ExecuteAsync(query, new
                        {
                            FornecedorID = f,
                            Hoje = DateTime.Now,
                            Id = o.Id
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }

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

        public async Task ExcluirFornecedorServico(int f, IEnumerable<FornecedorServicoModel> t)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    foreach (var o in t)
                    {
                        var queryCheck = @"SELECT ISATIVO
                                        FROM FORNECEDOR_SERVICO (NOLOCK)
                                        WHERE [FORNECEDORID] = @FornecedorID AND CODIGO = @Id";

                        var check = await conn.QueryAsync<bool>(queryCheck, new { FornecedorID = f, Id = o.Id }, tran);

                        if (check == null || !check.Any())
                            throw new Exception("Interrupção de serviço não localizada.");

                        if (check.ElementAt(0))
                            throw new Exception("A interrupção de serviço está ativa.");

                        var query = @"DELETE FORNECEDOR_SERVICO WHERE [FORNECEDORID] = @FornecedorID AND CODIGO = @Id;";

                        await conn.ExecuteAsync(query, new
                        {
                            FornecedorID = f,
                            Id = o.Id
                        }, transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);
                    }

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
    }
}
