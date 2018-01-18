using Amazon.S3.Transfer;
using DAL;
using Dapper;
using FastMember;
using Helpers;
using Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleTest
{
	public class Program
	{
		class ArquivoFTP
		{
			public string Arquivo { get; set; }
			public string TipoCampanha { get; set; }
			public string CPF { get; set; }
			public DateTime DataPostagem { get; set; }
			public DateTime DataProcessamento { get; set; }
			public DateTime? Vencimento { get; set; }
			public int Mes { get; set; }
			public int Ano { get; set; }
			public string Carteira { get; set; }
		}

		public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

		//		-!EuVwuwo)
		static async Task GetPrefixos()
		{

			using (var reader = new StreamReader(File.OpenRead(@"D:\TestesConecttaSMS\5002015_PJ_SMSPREV_RECOVERYWO_1002.txt"), Encoding.UTF7, true))
			{

				var teste = DateTime.Now;

				//var item = await ToZip(reader.BaseStream, "teste.csv");

				//var j = await reader.ReadToEndAsync();
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://conecttasms.com.br/http/getprefixos.ashx");
			try
			{


				//var j = ((Operadoras)Enum.Parse(typeof(Operadoras), "3"));


				var prefixos = new List<PrefixoModel>() { };


				//		<PrefixosConectta>
				//<Prefixos Prefixo="1192016" Operadora="CLARO" />
				//<Prefixos Prefixo="1194039" Operadora="NEXTEL" />
				//<Prefixos Prefixo="1194040" Operadora="CLARO" />
				//<Prefixos Prefixo="1194041" Operadora="CLARO" />
				//<Prefixos Prefixo="1194042" Operadora="CLARO" />


				request.Method = "GET";
				request.ContentType = "application/xml";

				using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
				{
					using (Stream responseStream = response.GetResponseStream())
					{
						using (StreamReader responseReader = new StreamReader(responseStream))
						{
							//while(responseReader.Peek()>=0)

							XDocument xdoc = XDocument.Parse(await responseReader.ReadToEndAsync());

							var items = xdoc.Element("PrefixosConectta").Elements("Prefixos").Select(a => new PrefixoModel() { Prefixo = int.Parse(a.Attribute("Prefixo").Value), Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.Attribute("Operadora").Value)) }).ToList();
							DALPrefixo dal = new DALPrefixo();

							await dal.AdicionarItens(items, 0, null);
						}
					}
				}
			}
			catch (Exception err)
			{
				throw err;
			}

		}
		static readonly object _lock = new object();

		static decimal CorrigiTelefone(string s)
		{
			if (s.Remove(s.Length - 4).Length == 6 && !s.Substring(2).StartsWith("7"))
				return decimal.Parse(s.Insert(2, "9"));

			return decimal.Parse(s);
		}
		async static Task ChecaInvalidos()
		{
			var prefixos = await new DALPrefixo().ObterTodos();

			var linhas = File.ReadAllLines(@"D:\Clientes\siscom\BASE_CONECTTA.csv")
				.Skip(1)
				.Select(a => a.Split(";".ToCharArray())).Select(a => new
				{
					ddd = byte.Parse(a.ElementAt(1)),
					numero = CorrigiTelefone($"{a.ElementAt(1)}{a.ElementAt(2)}"),
					idlciente = a.ElementAt(0)
				});

			var result = (from a in linhas join b in prefixos on a.numero.NormalizeCell().ToPrefixo() equals b.Prefixo into ps from b in ps.DefaultIfEmpty() where b == null select a).ToList();

			File.WriteAllLines(@"D:\Clientes\siscom\BASE_CONECTTA_invalidos.csv", result.Select(a => $"{a.idlciente};{a.ddd};{a.numero}"));

		}
		static string NormalizaOperadora(string o)
		{
			switch (o)
			{
				case "EUTV CONSULTORIA E INTERMEDIAÇ": o = "EUTV"; break;
				case "TIM CELULAR S.A.": o = "TIM"; break;
				case "OI MÓVEL S.A. - EM RECUPERAÇÃO": o = "OI"; break;
				case "DATORA MOBILE TELECOMUNICACOES": o = "DATORA"; break;
				case "PORTO SEGURO TELECOMUNICACOES": o = "PORTO"; break;
				case "CLARO S.A.": o = "CLARO"; break;
				case "TELEFÔNICA-TELESP": o = "VIVO"; break;
				case "UNIC EL DO BRASIL TELECOMUNICA": o = "CLARO"; break;
				case "TERAPAR PARTICIPAÇÕES LTDA": o = "TERAPAR"; break;
				case "ALGAR CELULAR S/A": o = "ALGAR"; break;
				case "Options computadores & eletrôn": o = "OPTIONS"; break;
				case "SERCOMTEL CELULAR S.A.": o = "SERCOMTEL"; break;
				case "NEXTEL TELECOMUNICACOES LTDA": o = "NEXTEL"; break;
			}
			return o;
		}

		async static Task AtualizaPrefixos()
		{
			List<PrefixoModel> prefixos = new List<PrefixoModel>() { };

			using (var conn = new SqlConnection("Data Source=mssql1.160d.com.br;Initial Catalog=MONEOSI;User Id=moneo;Password=rv2b7000438dm;"))
			{
				await conn.OpenAsync();

				var tran = conn.BeginTransaction();

				try
				{

					var _result = await conn.QueryAsync<dynamic>("SELECT PREFIXO, OPERADORAID FROM PREFIXOS", transaction: tran);

					prefixos.AddRange(_result.Select(a => new PrefixoModel()
					{
						Prefixo = a.PREFIXO,
						Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OPERADORAID.ToString()))
					}));


					var _prefixos = File.ReadAllLines(@"D:\Prefixos\CE_M_122741.txt", Encoding.UTF7)
											.Select(a => new PrefixoModel { Prefixo = int.Parse(a.Substring(0, 7)), OperadoraNome = NormalizaOperadora(a.Substring(7, 30).Trim()) })
											.GroupBy(a => new PrefixoModel() { Prefixo = a.Prefixo, OperadoraNome = a.OperadoraNome, Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.OperadoraNome)) },
											(a, b) => a, new CompareObject<PrefixoModel>((a, b) => a.Prefixo == b.Prefixo, a => a.Prefixo.GetHashCode())).ToList();

					var result = _prefixos.Except(prefixos, new CompareObject<PrefixoModel>((a, b) => a.Prefixo == b.Prefixo, a => a.Prefixo.GetHashCode())).Where(a => a.Prefixo.ToString().Length > 6).ToList();




					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					using (var reader = ObjectReader.Create(result.Select(a => new { Prefixo = a.Prefixo, OperadoraID = (byte)a.Operadora }), "Prefixo", "OperadoraID"))
					{

						bcp.DestinationTableName = "PREFIXOS";
						bcp.ColumnMappings.Add("Prefixo", "PREFIXO");
						bcp.ColumnMappings.Add("OperadoraID", "OPERADORAID");

						await bcp.WriteToServerAsync(reader);
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

		public static IEnumerable<PortabilidadeModel> LineGenerator(StreamReader sr)
		{
			string linha = null;
			string[] linhas = null;

			while (sr.Peek() >= 0)
			{
				linha = sr.ReadLine();
				linhas = linha.Split(";".ToCharArray());
				if (linhas[1].Length == 10 || linhas[1].Length == 11)
					yield return new PortabilidadeModel()
					{
						CodigoOperadora = int.Parse(linhas[2]),
						Celular = decimal.Parse(linhas[1])
					};
			}
		}
		static async Task GetPortabilidade()
		{


			HttpClient client = new HttpClient();

			client.SetBasicAuthentication("conectta", "12.conn.34");

			using (HttpResponseMessage response = await client.GetAsync("http://virgo.spo.iagente.net.br/bdo/current.gz", HttpCompletionOption.ResponseContentRead))
			{
				using (Stream s = await response.Content.ReadAsStreamAsync())
				{
					using (var mem = new MemoryStream())
					{
						await s.CopyToAsync(mem);
						mem.Position = 0L;

						using (var memFinal = new MemoryStream())
						{
							using (GZipStream g = new GZipStream(mem, CompressionMode.Decompress))
							{
								await g.CopyToAsync(memFinal);
							}
							memFinal.Position = 0L;

							var _portabilidade = new HashSet<PortabilidadeModel>() { };

							using (var r = new StreamReader(memFinal, Encoding.UTF7))
							{
								_portabilidade = new HashSet<PortabilidadeModel>(LineGenerator(r).AsParallel());
							}

							if (_portabilidade.Any())
							{

								var prefixos = await new DALPrefixo().ObterTodos();

								var operadoras = File.ReadAllLines(@"d:\Portabilidade\Operadoras.csv")
									.Select(a => new
									{
										Codigo = int.Parse(a.Split(";".ToCharArray())[2]),
										Nome = a.Split(";".ToCharArray())[0]
									});

								var _itens = _portabilidade.AsParallel().Join(operadoras.AsParallel(), a => a.CodigoOperadora, _b => _b.Codigo, (a, _b) => new PortabilidadeModel()
								{
									Celular = a.Celular,
									NomeOperadora = _b.Nome
								})
											.Join(prefixos.AsParallel(), a => a.Celular.ToPrefixo(), _b => _b.Prefixo, (a, _b) => new CampanhaModel()
											{
												Celular = a.Celular,
												Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.NomeOperadora))
											});

								//Console.WriteLine(_itens.Count);
								await new DALPortabilidade().AdicionarItensAsync(_itens, default(int), default(int));
							}
						}


					}
				}
			}









			//var linhas = File.ReadAllLines(@"d:\Portabilidade\current").AsParallel();

			//Console.WriteLine(linhas.Count());

			//using (StreamReader s = new StreamReader(File.OpenRead(@"d:\Portabilidade\current")))
			//{
			//	while (s.Peek() >= 0)
			//		listagem.Add(await s.ReadLineAsync());
			//}



			//campanha = new HashSet<CampanhaModel>(linhas.Skip(1)
			//	.Select(a => a.Split(";".ToCharArray()))
			//	.Where(a => a[1].Length == 10 || a[1].Length == 11)
			//	.Select(a => new
			//	{
			//		Codigo = int.Parse(a[2]),
			//		Numero = decimal.Parse(a[1])
			//	})
			//	.Join(operadoras.AsParallel(), a => a.Codigo, b => b.Codigo, (a, b) => new { Celular = a.Numero, Operadora = b.Nome })
			//	.Join(prefixos.AsParallel(), a => (int)ToPrefixo(a.Celular), b => b.Prefixo, (a, b) => new CampanhaModel() { Celular = a.Celular, Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.Operadora)) }));



			//using (var conn = new SqlConnection("Data Source=mssql.conecttasms.com.br;Initial Catalog=sms_conectta;User Id=conecttasms;Password=ms6tgs;"))
			//{
			//	IEnumerable<CampanhaModel> camps;

			//	await conn.OpenAsync();
			//	try
			//	{

			//		var result = await conn.QueryAsync<dynamic>("SELECT CELULAR, OPERADORA FROM PORTABILIDADE", commandTimeout:888);

			//		if (result != null)
			//		{
			//			camps = result.Select(a => new CampanhaModel()
			//			{
			//				Celular = decimal.Parse(a.CELULAR),
			//				Operadora = ((Operadoras)Enum.Parse(typeof(Operadoras), a.OPERADORA))
			//			}).ToList();

			//		}

			//	}
			//	catch (Exception err)
			//	{
			//		throw err;
			//	}
			//	finally
			//	{
			//		conn.Close();
			//	}
			//}
		}
		class Paginas
		{
			public string Pagina { get; set; }
			public string SubPagina { get; set; }
			public string Grupo { get; set; }
			public Paginas(string pagina, string subgrupo, string grupo)
			{
				Pagina = pagina;
				SubPagina = subgrupo;
				Grupo = grupo;
			}
		}

		static async Task CarregaLinhasAsync()
		{
			var _paginas = new List<Paginas>() { };


			var linhas = File.ReadAllLines(@"D:\Projetos\MCI\paginas.csv").Select(a => a).ToList();
			string[] paginagrupo = { };


			foreach (var item in linhas)
			{
				string pagina = null;
				string subp = string.Empty;
				string grupop = string.Empty;

				paginagrupo = item.Split("/".ToCharArray());
				pagina = paginagrupo[1];
				grupop = paginagrupo[0];

				if (pagina.Contains(":"))
				{
					subp = pagina.Split(":".ToCharArray())[1].Trim().Replace(" ", "-");
					pagina = pagina.Split(":".ToCharArray())[0].Trim();
				}
				_paginas.Add(new Paginas(pagina, subp, grupop));

			}


			using (var conn = new SqlConnection("Data Source=conecttadb.cdsj2hoqj9ao.us-east-1.rds.amazonaws.com;Initial Catalog=MONEOSI;User Id=conectta;Password=rv2b7000438dm;"))
			{
				await conn.OpenAsync();
				SqlTransaction tran = null;

				try
				{

					tran = conn.BeginTransaction();

					foreach (var _grupo in _paginas.GroupBy(a => a.Grupo, (a, b) => new { Grupo = a, Lista = b }))
					{
						var p = new DynamicParameters();
						p.Add("Grupo", _grupo.Grupo, DbType.String, ParameterDirection.Input);
						p.Add("GrupoID", DbType.Int32, direction: ParameterDirection.Output);
						await conn.ExecuteAsync("INSERT INTO GRUPOPAGINAS (GRUPO) VALUES (@Grupo); SELECT @GrupoID=SCOPE_IDENTITY();", p, transaction: tran);

						foreach (var _page in _grupo.Lista.GroupBy(a => a.Pagina, (a, b) => new { Pagina = a, Lista = b }))
						{
							var page = new DynamicParameters();
							page.Add("GrupoID", p.Get<int>("GrupoID"), DbType.Int32, ParameterDirection.Input);
							page.Add("Pagina", _page.Pagina, DbType.String, ParameterDirection.Input);
							page.Add("PaginaID", DbType.Int32, direction: ParameterDirection.Output);
							await conn.ExecuteAsync("INSERT INTO PAGINAS (PAGINA, GRUPOID) VALUES (@Pagina, @GrupoID);SELECT @PaginaID=SCOPE_IDENTITY();", page, transaction: tran);

							foreach (var _subpage in _page.Lista.Where(a => a.Pagina == _page.Pagina))
							{

								if (!string.IsNullOrEmpty(_subpage.SubPagina))
								{
									var subpage = new DynamicParameters();
									subpage.Add("PaginaID", page.Get<int>("PaginaID"), DbType.Int32, ParameterDirection.Input);
									subpage.Add("SubPagina", _subpage.SubPagina, DbType.String, ParameterDirection.Input);
									await conn.ExecuteAsync("INSERT INTO SUBPAGINAS (SUBPAGINA, PAGINAID) VALUES (@SubPagina, @PaginaID);", subpage, transaction: tran);
								}
							}
						}
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
		static void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
		{
			// Process event.
			Console.WriteLine("{0}/{1}", e.TransferredBytes, e.TotalBytes);
		}
		static async Task GravaUFS()
		{
			var ufs = File.ReadAllLines(@"D:\Lixo\ddd_uf.csv").Select(a => a.Split(";".ToCharArray()));


			using (var conn = new SqlConnection("Data Source=moneoci-express.cdsj2hoqj9ao.us-east-1.rds.amazonaws.com;Initial Catalog=MONEOCI;User Id=moneo;Password=rv2b7000438dm;"))
			{
				await conn.OpenAsync();
				SqlTransaction tran = null;

				try
				{
					tran = conn.BeginTransaction();

					await conn.ExecuteAsync("INSERT INTO [dbo].[UFDDD]([DDD],[REGIAO],[UF]) VALUES (@DDD, @Regiao, @UF)",
						ufs.Select(a => new
						{
							DDD = byte.Parse(a.ElementAt(1)),
							Regiao = a.ElementAt(2),
							UF = a.ElementAt(0)
						}), transaction: tran);

					tran.Commit();
				}
				catch
				{
					tran.Rollback();
				}
				finally
				{
					tran.Dispose();
					conn.Close();
				}
			}
		}


		static Regex DataRegex = new Regex("([0-9]{2})(01|02|03|04|05|06|07|08|09|10|11|12)(2017|2018)?", RegexOptions.Compiled);

		static DateTime? Vencimento(string s)
		{
			if (!string.IsNullOrEmpty(s))
				if (DataRegex.IsMatch(s))
				{
					var _data = DataRegex.Match(s);
					return new DateTime(string.IsNullOrEmpty(_data.Groups[3].Value) ? DateTime.Now.Year : int.Parse(_data.Groups[3].Value), int.Parse(_data.Groups[2].Value), int.Parse(_data.Groups[1].Value));
				}


			return null;
		}

		static async void TesteEnvioTuGo()
		{

		}

		static string CPF(string cpf)
		{

			if (!string.IsNullOrEmpty(cpf))
				if (Regex.IsMatch(cpf, "\\d+"))
				{
					cpf = Regex.Match(cpf, "\\d+").Value;

					if (cpf.Length >= 12)
						return long.Parse(cpf.Substring(cpf.Length)).ToString("00000000000");
					else
						return long.Parse(cpf).ToString("00000000000");
				}

			return cpf;
		}

		static async Task UploadRejeitados()
		{

			var camps = new HashSet<RejeitadosModel>() { };

			Console.WriteLine("father confirmações");
			// father confirmações
			using (var conn = new SqlConnection("Data Source=SRVSMSFATHER;Initial Catalog=SMSSERVICE;User Id=servicesms;Password=ms6tgs;"))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("DataIn", DateTime.Now.Date.AddDays(-90), DbType.DateTime);
					p.Add("DataOut", DateTime.Now.Date.AddMinutes(1439), DbType.DateTime);

					var itens = await conn.QueryAsync<RejeitadosModel>("GETREJEITADAS", p, commandTimeout: 8888, commandType: CommandType.StoredProcedure);

					foreach (var item in itens)
						camps.Add(item);
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

			Console.WriteLine("conectta antigo");
			//conectta antigo
			using (var conn = new SqlConnection("Data Source=mssql1.160d.com.br;Initial Catalog=sms_conectta;User Id=conecttasms;Password=ms6tgs;"))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("DataIn", DateTime.Now.Date.AddDays(-90), DbType.DateTime);
					p.Add("DataOut", DateTime.Now.Date.AddMinutes(1439), DbType.DateTime);

					var itens = await conn.QueryAsync<RejeitadosModel>("SELECT SUBSTRING(CELULAR, 3, LEN(CELULAR)) CELULAR, DATAREPORT FROM CAMPANHAS WHERE DATAENVIAR BETWEEN @DataIn AND @DataOut AND STATUSENVIO=2 AND TIPO=4 AND STATUSREPORT='DELETED'", p, commandTimeout: 8888);

					foreach (var item in itens)
						camps.Add(item);
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

			Console.WriteLine("160d shortcode");
			//160d shortcode
			using (var conn = new SqlConnection("Data Source=mssql1.160d.com.br;Initial Catalog=160_shortcode;User Id=sa;Password=!rv2b7000438dm;"))
			{
				await conn.OpenAsync();
				try
				{
					var p = new DynamicParameters();
					p.Add("DataIn", DateTime.Now.Date.AddDays(-90), DbType.DateTime);
					p.Add("DataOut", DateTime.Now.Date.AddMinutes(1439), DbType.DateTime);

					var itens = await conn.QueryAsync<RejeitadosModel>("SELECT CELULAR, DATAREPORT DATA FROM CAMPANHA WHERE STATUSREPORT=68 AND DATAREPORT BETWEEN @DataIn AND @DataOut", p, commandTimeout: 8888);

					foreach (var item in itens)
						camps.Add(item);
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

			Console.WriteLine("gravação final");
			//gravação final
			using (var conn = new SqlConnection("Data Source=mssql1.160d.com.br;Initial Catalog=HELPER;User Id=sa;Password=!rv2b7000438dm;"))
			{
				await conn.OpenAsync();

				var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync("TRUNCATE TABLE HELPER.dbo.FILTRADO", transaction: tran);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(camps.Select(m => new
						{
							Numero = m.Celular,
							m.Data
						}),
						"Numero", "Data"))
						{
							bcp.NotifyAfter = 25000;
							bcp.SqlRowsCopied += (a, b) => { Console.WriteLine(b.RowsCopied); };
							bcp.DestinationTableName = "HELPER.dbo.FILTRADO";
							bcp.ColumnMappings.Add("Numero", "CELULAR");
							bcp.ColumnMappings.Add("Data", "DATACADASTRO");
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

		static TimeSpan EstimativaEntrega(int intervalo, int capacidade5min, int totalenvio, int lotes) => TimeSpan.FromMinutes(((((totalenvio / lotes) / (intervalo == 0 ? 5 : intervalo / 5))) / capacidade5min * 5) * lotes);

		static async Task MainAsync(string[] args)
		{

			//await ChecaInvalidos();

			//await AtualizaPrefixos();
			await UploadRejeitados();

			//CampanhaModel ratinho = new CampanhaModel();
			//ratinho.Celular = 71981273460;
			//ratinho.Texto = "test";

			//var json = JsonConvert.SerializeObject(ratinho);

			//int capacidade5min = 500;
			//int lotes = 5;
			//int intervalo = 5;
			//int totalenvio = 5000;

			//int tempototalenvioesperado = intervalo * lotes;
			//int totalporlote = totalenvio / lotes;
			//int totalacada5min = totalporlote / (intervalo / 5);
			//int capacidadealem = totalacada5min - capacidade5min;
			//int capacidadealemmin = capacidadealem / capacidade5min * 5;
			//int tempoenvioporlote = totalacada5min / capacidade5min * 5;
			//int tempototalfornecedor = tempoenvioporlote * lotes;

			//TimeSpan.FromMinutes(tempototalfornecedor);

			//var time = ((((totalenvio / lotes) / (intervalo == 0 ? 5 : intervalo / 5))) / capacidade5min * 5) * lotes;

			//time = time / capacidade5min * 5;

			//var tempo = EstimativaEntrega(5, 500, 5000, 5);


			//await AtualizaPrefixos();

			//await UploadRejeitados();
			//using (var conn = new SqlConnection("Data Source=moneoci-express.cdsj2hoqj9ao.us-east-1.rds.amazonaws.com;Initial Catalog=MONEOCI;User Id=moneo;Password=rv2b7000438dm;"))
			//{
			//	await conn.OpenAsync();

			//	try
			//	{
			//		var camps = new List<CampanhaModel>() { };

			//		//var dados = await conn.QueryAsync(@"SELECT DATAENVIAR, CA.ARQUIVO, CAR.CARTEIRA, CAMPANHAID FROM CAMPANHAS C JOIN [dbo].[CAMPANHAS_ARQUIVOS] CA ON C.ARQUIVOID=CA.ARQUIVOID JOIN CARTEIRAS CAR ON C.CARTEIRAID=CAR.CARTEIRAID WHERE DATADIA='2017-6-4' AND C.CLIENTEID=1");


			//		camps=File.ReadAllLines(@"D:\Lixo\arquivo.csv")
			//			.AsParallel()
			//			.Select(a => a.Split(";".ToCharArray()))
			//			.Select(a => new CampanhaModel()
			//			{
			//				Carteira = new CarteiraModel() { Carteira = a.ElementAt(2) },
			//				Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ElementAt(1) },
			//				DataEnviar = DateTime.Parse(a.ElementAt(0)),
			//				CampanhaID = int.Parse(a.ElementAt(3))
			//			}).ToList();

			//		//camps = dados.Select(a => new CampanhaModel()
			//		//{
			//		//	Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
			//		//	Arquivo = new ArquivoCampanhaModel() { Arquivo = a.ARQUIVO },
			//		//	DataEnviar = a.DATAENVIAR,
			//		//	CampanhaID = a.CAMPANHAID
			//		//}).ToList();

			//		EmailViewModel[] emails = new EmailViewModel[] { };
			//		var conteudoEmail = await Emails.RelatorioAnaliticoRetorno(camps);
			//		await Util.SendEmailAsync(emails, $"Relatório analítico do dia {camps.ElementAt(0).DataEnviar.ToShortDateTime()}", conteudoEmail.Item1, true, TipoEmail.RELATORIOANALITICO, conteudoEmail.Item2);


			//	}
			//	catch (Exception)
			//	{

			//		throw;
			//	}
			//	finally
			//	{
			//		conn.Close();
			//	}

			//await GetPortabilidade();

			//await new DirectoryInfo(@"D:\Clientes\Flex\").GetFiles("*.*")
			//	.ToObservable()
			//	.ForEachAsync(async a=>
			//{
			//	using (var file = a.OpenRead())
			//	using (var mem = new MemoryStream())
			//	{
			//		await file.CopyToAsync(mem);
			//		Util.UploadAamazon(mem.ToArray(), a.Name, "moneoup");
			//	}
			//});

			//var t = "hahauh";
			//await UploadRejeitados();

			//CarregaLinhasAsync();
			//}
			//var client = new DiscoveryClient("http://conecttaoffice:12201");
			//client.Policy.RequireHttps = false;
			//var disco = await client.GetAsync();

			//var tokenClient = new TokenClient(disco.TokenEndpoint, "api", "ms6tgsoem2650");

			//var tokenResponse = await tokenClient.RequestClientCredentialsAsync("fornecedor");

			//if (tokenResponse.IsError)
			//{
			//	Console.WriteLine(tokenResponse.Error);
			//	return;
			//}

			//Console.WriteLine(tokenResponse.Json);
			//Console.ReadLine();
			//await GetPortabilidade();
			//string valor = "415274060672";

			//if (valor.Length >= 11)
			//{
			//	valor = valor.Substring(valor.Length - 11);
			//}
			//await UploadFilesRedeBrasil(DateTime.Parse(args[0]), DateTime.Parse(args[1]));


			//string email = "dtpontes@hotmail.com";

			//var teste = (DateTime.Now- new DateTime()).TotalMinutes / 5 * 5;

			//var _t = new DateTime().AddMinutes(teste);

			//int diainicio = 8;
			//DateTime dataatual = DateTime.Now;

			//var dataleitura = new DateTime(dataatual.Year, dataatual.Month, diainicio);

			//var resultado = dataatual - dataleitura;

			//if (resultado.Days < 0)
			//{
			//	dataleitura=dataleitura.AddMonths(-1);
			//	resultado=dataatual - dataleitura;
			//}



			//await GravaUFS();

			//var query = "SELECT CAMPANHAID, ARQUIVOID, CARTEIRAID, DATAENVIAR, FORNECEDORID FROM CAMPANHAS WHERE DATADIA = '2017-4-26' AND STATUSENVIO = 2";

			//using (var conn = new SqlConnection("Data Source=moneoci-express.cdsj2hoqj9ao.us-east-1.rds.amazonaws.com;Initial Catalog=MONEOCI;User Id=moneo;Password=rv2b7000438dm;"))
			//{
			//	await conn.OpenAsync();
			//	SqlTransaction tran = null;

			//	try
			//	{

			//		tran = conn.BeginTransaction();

			//		var dados = await conn.QueryAsync<dynamic>(query, transaction: tran);

			//		var campanhas = dados.Select(a => new CampanhaModel()
			//		{
			//			CampanhaID = a.CAMPANHAID,
			//			Arquivo = new ArquivoCampanhaModel() { ArquivoID = a.ARQUIVOID },
			//			Carteira = new CarteiraModel() { CarteiraID = a.CARTEIRAID },
			//			DataEnviar = a.DATAENVIAR,
			//			Fornecedor = new FornecedorModel() { FornecedorID = a.FORNECEDORID },
			//			Atualizado = false
			//		}).ToList();

			//		var _dic = new Dictionary<int, int>() { };
			//		_dic.Add(60, 0);
			//		_dic.Add(15, 70);
			//		_dic.Add(25, 68);



			//		foreach (var item in campanhas.GroupBy(a => new { DataEnviar = a.DataEnviar, CarteiraID = a.Carteira.CarteiraID, ArquivoID = a.Arquivo.ArquivoID, FornecedorID = a.Fornecedor.FornecedorID },
			//			(a, b) => new { DataEnviar = a.DataEnviar, CarteiraID = a.CarteiraID, ArquivoID = a.ArquivoID, FornecedorID = a.FornecedorID, Dados = b }))
			//		{
			//			var total = item.Dados.Count();

			//			foreach (var _codigo in _dic)
			//				foreach (var d in item.Dados.Where(a => !a.Atualizado).Take(_codigo.Key * total / 100))
			//				{
			//					d.StatusReport = ((ReportDeliveryEnums)Enum.Parse(typeof(ReportDeliveryEnums), _codigo.Value.ToString()));
			//					d.Atualizado = true;
			//					d.DataReport = d.DataEnviar.AddHours(2);
			//				}


			//		}

			//		await conn.ExecuteAsync(@"CREATE TABLE #TMP (
			//										CAMPANHAID		INT,
			//										STATUSREPORT	TINYINT,
			//										DATAREPORT		DATETIME)",
			//										transaction: tran);

			//		using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
			//		{
			//			using (var reader = ObjectReader.Create(campanhas.Select(m => new
			//			{
			//				CampanhaID = m.CampanhaID,
			//				StatusReport = m.StatusReport,
			//				DataReport = m.DataReport
			//			}),
			//			"CampanhaID", "StatusReport", "DataReport"))
			//			{
			//				bcp.DestinationTableName = "#TMP";
			//				bcp.ColumnMappings.Add("CampanhaID", "CAMPANHAID");
			//				bcp.ColumnMappings.Add("StatusReport", "STATUSREPORT");
			//				bcp.ColumnMappings.Add("DataReport", "DATAREPORT");
			//				bcp.BulkCopyTimeout = 888;

			//				await bcp.WriteToServerAsync(reader);
			//				bcp.Close();
			//			}
			//		}


			//		var atualizado = await conn.ExecuteAsync(@"UPDATE C SET C.STATUSREPORT=T.STATUSREPORT, C.DATAREPORT=T.DATAREPORT FROM CAMPANHAS C JOIN #TMP T ON C.CAMPANHAID=T.CAMPANHAID", transaction: tran, commandTimeout: 888);

			//		tran.Commit();

			//	}
			//	catch (Exception)
			//	{
			//		tran.Rollback();
			//		throw;
			//	}
			//	finally
			//	{
			//		tran.Dispose();
			//	}

			//}
			////var lista = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8,9,10,11,12,13 };

			////var h = lista.Select((a, b) => new { Valor = a, Fornecedor = b % 10});


			////lista.ElementAt(1 % 2);

			//var timespan = TimeSpan.FromSeconds(2621);




			//try
			//{


			//var linhas = File.ReadAllLines(@"D:\Clientes\Verreshi\TESTECONECTTA_20170420.csv").Select(a => a.Split(";".ToCharArray()))
			//	.Select(a => Encoding.ASCII.GetBytes($"{a.ElementAt(0).Trim()};{a.ElementAt(1).Replace("[NOME]", a.ElementAt(2))}\r\n")).SelectMany(a => a).ToArray();


			//using (FileStream f = File.Create(@"D:\Clientes\Verreshi\TESTECONECTTA_20170420_final.csv"))
			//{
			//	var file = linhas;
			//	f.Write(file, 0, linhas.Length);
			//}

			//string query = @"UPDATE CAMPANHAS  SET STATUSENVIO=@StatusEnvio WHERE DATAENVIAR=@DataEnviar AND CLIENTEID=@ClienteID AND ARQUIVOID=@ArquivoID AND CARTEIRAID=@CarteiraID AND STATUSENVIO IN(0,4)";

			//            var j =query.Insert(query.LastIndexOf("AND ARQUIVOID"), "AND USUARIOID=@UsuarioID ");





			//var manager = new ArchiveTransferManager("AKIAICXA7YQZPALLPSPQ", "ToeXHkcARWr+U9ArX9qtYG11+jbPvolk8AnaZqTs", Amazon.RegionEndpoint.USEast1);
			// Upload an archive.
			//var archiveId = await manager.UploadAsync("teste", "upload archive test", @"D:\TestesConecttaSMS\SMS_PF_CL31_0106.txt");


			//            await manager.DownloadAsync("teste", "hwETE4BYjYZcaP5Sx2Y7N_5SeD3yMEybEFUc7P4KXW15Hi2T9ZxQ9WEerx_OyIKzDoHUKhw-SleB9wbfvjMCU_fExQC--lrwT2Vstv9fJrF4il1g6gM1n68qPsYuhIQoBnT2fvKB7w", @"D:\TestesConecttaSMS\outro.txt");

			//            //result
			//            //var _id = archiveId.ArchiveId;

			//           // Console.WriteLine("Archive ID: (Copy and save this ID for use in other examples.) : {0}", _id);
			//            Console.WriteLine("To continue, press Enter");
			//            Console.ReadKey();
			//        }
			//        catch (AmazonGlacierException e) { Console.WriteLine(e.Message); }
			//        catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
			//        catch (Exception e) { Console.WriteLine(e.Message); }
			//        Console.WriteLine("To continue, press Enter");
			//        Console.ReadKey();

			//        var teste = "teste".Sha256();

			//        //await GetPortabilidade();

			//        TransferUtility fileTransferUtility = new TransferUtility("AKIAIUTCNVWZCKHGBHGQ", "K+x6PaiQq9Jibeb0JOKW1uKPZVV9nNt7N1dLz6A9", Amazon.RegionEndpoint.USEast1);

			//        //Amazon.ElastiCache.AmazonElastiCacheClient cliente = new Amazon.ElastiCache.AmazonElastiCacheClient()

			//        foreach (var item in Directory.GetFiles(@"D:\TestesConecttaSMS\").Take(2))
			//        {



			//             var fileTransfer = new TransferUtilityUploadRequest { BucketName = "canalassembleia/34", FilePath = item, Key = Path.GetFileName(item) };
			//            fileTransfer.UploadProgressEvent += new EventHandler<UploadProgressArgs>((a,e)=> Console.WriteLine("{0}/{1} {2}", e.TransferredBytes, e.TotalBytes, e.FilePath));
			//            await fileTransferUtility.UploadAsync(fileTransfer);
			//        }






			//var client = new DiscoveryClient("http://conecttaoffice:12201/");
			//client.Policy.RequireHttps = false;
			//var disco = await client.GetAsync();

			//// request token
			//var tokenClient = new TokenClient(disco.TokenEndpoint, "oauthClient", "supers");
			//var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api1");



			//var clienteHttp = new HttpClient();
			//clienteHttp.SetBearerToken(tokenResponse.AccessToken);
			//var response = await clienteHttp.GetAsync("http://conecttaoffice:12201/api/tipocampanha/");



			//if (!response.IsSuccessStatusCode)
			//{
			//	Console.WriteLine(response.StatusCode);
			//}
			//else
			//Console.WriteLine(response.StatusCode);

			//if (tokenResponse.IsError)
			//{
			//	Console.WriteLine(tokenResponse.Error);
			//	return;
			//}

			//Console.WriteLine(tokenResponse.Json);
			Console.ReadLine();
		}

		static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://conecttaoffice:12201/connect/token");

			request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "password",
				["username"] = email,
				["password"] = password,
				["client_id"] = "mvc",
				["client_secret"] = "901564A5-E7FE-42CB-B10D-61EF6A8F3654"
			});

			var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
			response.EnsureSuccessStatusCode();

			var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

			if (payload["error"] != null)
				throw new InvalidOperationException("An error occurred while retrieving an access token.");

			return (string)payload["access_token"];
		}
		public static async Task<string> GetResourceAsync(HttpClient client, string token)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, "http://conecttaoffice:12201/api/tipocampanha/get");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync();
		}

	}

}
