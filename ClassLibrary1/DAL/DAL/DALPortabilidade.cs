using Dapper;
using FastMember;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class DALPortabilidade : IDal<CampanhaModel>
	{
		public async Task AdicionarItensAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			using (var conn = new SqlConnection(Util.ConnString))
			{
				await conn.OpenAsync();

				var tran = conn.BeginTransaction();

				try
				{
					await conn.ExecuteAsync(@"TRUNCATE TABLE HELPER.dbo.PORTABILIDADE", transaction: tran, commandTimeout: Util.TIMEOUTEXECUTE);

					using (var bcp = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
					{
						using (var reader = ObjectReader.Create(t.Select(m => new
						{
							Numero = m.Celular,
							OperadoraID = (byte)m.Operadora
						}),
						"Numero", "OperadoraID"))
						{
							bcp.DestinationTableName = "HELPER.dbo.PORTABILIDADE";
							bcp.ColumnMappings.Add("Numero", "NUMERO");
							bcp.ColumnMappings.Add("OperadoraID", "OPERADORAID");
							bcp.BulkCopyTimeout = Util.TIMEOUTEXECUTE;
							await bcp.WriteToServerAsync(reader);
							bcp.Close();
						}
					}

					tran.Commit();
				}
				catch (Exception)
				{

					tran.Rollback();
					throw;
				}
			}
		}
		IEnumerable<PortabilidadeModel> LineGenerator(StreamReader sr)
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

		public async Task ProcessaPortabilidade()
		{

			var p = new HashSet<PortabilidadeModel>() { };

			using (var reader = File.OpenRead(@"D:\Portabilidade\base_completa.txt"))
			{
				using (var r = new StreamReader(reader, Encoding.UTF7))
				{
					p = new HashSet<PortabilidadeModel>(LineGenerator(r).AsParallel());
				}
			}

			var _cellp = p.Where(a => a.Celular == 71987968877);

			var prefixos = await new DALPrefixo().ObterTodos();

			using (var reader = new StreamReader(await Util.DownloadFileS3("moneoup", "Operadoras.csv", 0), Encoding.ASCII))
			{
				List<dynamic> operadoras = new List<dynamic>() { };
				string linha = null;
				string[] linhas = null;

				while (reader.Peek() >= 0)
				{
					linha = await reader.ReadLineAsync();
					linhas = linha.Split(";".ToCharArray());

					operadoras.Add(new
					{
						Codigo = int.Parse(linhas.ElementAt(2)),
						Nome = linhas.ElementAt(0)
					});
				}



				var _itens = p.AsParallel().Join(operadoras.AsParallel(), a => a.CodigoOperadora, _b => _b.Codigo, (a, _b) => new PortabilidadeModel()
				{
					Celular = a.Celular,
					NomeOperadora = _b.Nome
				})
							.Join(prefixos.AsParallel(), a => a.Celular.ToPrefixo(), _b => _b.Prefixo, (a, _b) => new CampanhaModel()
							{
								Celular = a.Celular,
								Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.NomeOperadora))
							}).ToHashSetEx();

				var _cell = _itens.Where(a => a.Celular == 71987968877);

				await new DALPortabilidade().AdicionarItensAsync(_itens, default(int), default(int));

			}
		}

		public async Task GetPortabilidade()
		{

			using (HttpClient client = new HttpClient())
			{

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
									//Console.WriteLine(_portabilidade.Count);
								}
	
								var prefixos = await new DALPrefixo().ObterTodos();

								using (var reader = new StreamReader(await Util.DownloadFileS3("moneoup", "Operadoras.csv", 0), Encoding.ASCII))
								{
									List<dynamic> operadoras = new List<dynamic>() { };
									string linha = null;
									string[] linhas = null;

									while (reader.Peek() >= 0)
									{
										linha = await reader.ReadLineAsync();
										linhas = linha.Split(";".ToCharArray());

										operadoras.Add(new
										{
											Codigo = int.Parse(linhas.ElementAt(2)),
											Nome = linhas.ElementAt(0)
										});
									}

									var _itens = _portabilidade.AsParallel().Join(operadoras.AsParallel(), a => a.CodigoOperadora, _b => _b.Codigo, (a, _b) => new PortabilidadeModel()
									{
										Celular = a.Celular,
										NomeOperadora = _b.Nome
									})
												.Join(prefixos.AsParallel(), a => a.Celular.ToPrefixo(), _b => _b.Prefixo, (a, _b) => new CampanhaModel()
												{
													Celular = a.Celular,
													Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), a.NomeOperadora))
												}).ToHashSetEx();

									await new DALPortabilidade().AdicionarItensAsync(_itens, default(int), default(int));
									

								}
							}
						}
					}
				}
			}
		}

		public Task AtualizaItensAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
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

		public Task ExcluirItensUpdateAsync(IEnumerable<CampanhaModel> t, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaModel>> ObterTodosAsync(CampanhaModel t, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<CampanhaModel>> ObterTodosPaginadoAsync(CampanhaModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
