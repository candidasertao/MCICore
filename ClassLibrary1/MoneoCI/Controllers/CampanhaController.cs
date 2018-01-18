using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using MoneoCI.Helpers;
using MoneoCI.Repository;
using MoneoCI.Services;
using Helpers;
using Atributos;
using DTO;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Models.MonitoriaModel;

namespace MoneoCI.Controllers
{


	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Cliente,Usuario,AdminOnly")]
	public class CampanhaController : ControllerBase, IControllers<CampanhaModel>
	{

		public int ClienteID { get { return User.Identity.IsAuthenticated ? int.Parse(User.Claims.Where(a => a.Type == "clienteid").ElementAt(0).Value) : 0; } }

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Count() > 0)
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return null;
			}
		}

		readonly IHostingEnvironment _hostingEnvironment;
		readonly IRepository<CampanhaModel> repository = null;
		readonly IRepository<SessionDataModel> sessionrepository = null;
		readonly IMemoryCache _cache;
		HashSet<CampanhaModel> Campanhas { get; set; }

		public Dictionary<int, string> Chaves = new Dictionary<int, string>() { };

		static readonly FormOptions _defaultFormOptions = new FormOptions();

		public CampanhaController(IRepository<CampanhaModel> repos, IHostingEnvironment env, IMemoryCache cache, IRepository<SessionDataModel> repossession)
		{
			Campanhas = new HashSet<CampanhaModel>() { };
			repository = repos;
			_cache = cache;
			_hostingEnvironment = env;
			sessionrepository = repossession;
		}

		IEnumerable<string[]> ListaCelulares(Stream r)
		{
			r.Position = 0L;
			using (StreamReader reader = new StreamReader(r, Util.EncoderDefaultFiles, true))
			{
				while (reader.Peek() >= 0)
					yield return reader.ReadLine().Trim().TrimEnd(';').Split(";".ToCharArray());
			}
		}


		public string GuidUser { get { return HttpContext.Request.Headers["Guid"]; } }

		//const int SUBPAGINAID = 85;
		//const int PAGINAID = 120;

		[HttpGet("carteiras/")]
		[NivelPermissao(1, PaginaID = 120, SubPaginaID = 85)]
		public async Task<IActionResult> Carteiras() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.ENVIO });

		[HttpGet("leiautes/")]
		[NivelPermissao(1, PaginaID = 124, SubPaginaID = 0)]
		public async Task<IActionResult> Layouts() => await new UtilController().GenericCall<LeiauteModel>(ClienteID, UsuarioID, new LeiauteRepository(), new LeiauteModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.ENVIO });


		[HttpPost("celular/down/")]
		[NivelPermissao(1, PaginaID = 138, SubPaginaID = 0)]
		public async Task<IActionResult> DownByCelular([FromBody] CampanhaModel c)
		{
			try
			{
				c.Cliente = new ClienteModel { ClienteID = ClienteID };
				c.Registros = 0;
				c.PaginaAtual = 1;

				var dados = await new CampanhaRepository().PesquisaByCelularAsync(c, UsuarioID);

				if (dados == null || !dados.Any())
					return NoContent();

				var sb = new StringBuilder();

				sb.Append("CELULAR;DATA HORA;CARTEIRA;ARQUIVO;MENSAGEM;STATUS\r\n");

				await dados.ToObservable().ForEachAsync(a => sb.AppendFormat("{0};{1};{2};{3};{4};{5}\r\n",
														a.Celular,
														a.DataEnviar,
														a.Carteira.Carteira,
														a.Arquivo != null ? a.Arquivo.Arquivo : string.Empty,
														a.Texto,
														a.StatusReport.ToString()
													));


				using (var _mem = new MemoryStream(Util.EncoderDefaultFiles.GetBytes(sb.ToString())))
				{
					Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
					Response.Headers.Add("Content-Disposition", $"attachment;filename=celular.zip");
					return File(await _mem.ToZip($"celular.csv"), "application/x-zip-compressed");
				}
			}
			catch (Exception err)
			{
				return BadRequest((err.InnerException ?? err).Message);
			}
		}


		[HttpPost("celular/")]
		[NivelPermissao(1, PaginaID = 138, SubPaginaID = 0)]
		public async Task<IActionResult> PesquisaByCelularAsync([FromBody]CampanhaModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{

				if (c.PaginaAtual.HasValue)
				{
					if (c.PaginaAtual.Value == 0)
						c.PaginaAtual = 1;
				}
				else
					c.PaginaAtual = 1;

				c.Cliente = new ClienteModel { ClienteID = ClienteID };

				var dados = await new CampanhaRepository().PesquisaByCelularAsync(c, UsuarioID);

				if (dados == null || !dados.Any())
					return NoContent();
				/*
				var p = dados
				.GroupBy(a => new { Celular = a.Celular }, (a, m) => new
				{
					celular = a.Celular,
					campanhas = m.Select(k => new
					{
						dataenviar = k.DataEnviar,
						datareport = k.DataReport,
						carteira = k.Carteira.Carteira,
						tipocampanha = k.TipoCampanha.TipoCampanha,
						regiao = k.Regiao,
						uf = k.UF,
						ddd = k.DDD,
                        mensagem = k.Texto,
						statusreport = k.Report,
						arquivo = k.Arquivo.Arquivo,
						operadora = k.Operadora.ToString(),
						fornecedor = k.Fornecedor.FornecedorNome
					})
				});
                */
				var p = dados.Select(k =>
						new
						{
							celular = k.Celular,
							dataenviar = k.DataEnviar,
							datareport = k.DataReport,
							carteira = k.Carteira.Carteira,
							tipocampanha = k.TipoCampanha != null ? k.TipoCampanha.TipoCampanha : string.Empty,
							regiao = k.Regiao,
							uf = k.UF,
							ddd = k.DDD,
							mensagem = k.Texto,
							statusreport = k.Report,
							arquivo = k.Arquivo != null ? k.Arquivo.Arquivo : string.Empty,
							operadora = k.Operadora.ToString(),
							fornecedor = k.Fornecedor.FornecedorNome
						});

				b.Result = p;
				b.Itens = dados.ElementAt(0).Registros;
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		int? IDColuna(LeiauteModel l, string variavel)
		{

			var retorno = l.LeiauteVariaveis.SingleOrDefault(a => a.Variavel == variavel);

			if (retorno != null)
				return retorno.IDColuna - 1;

			return null;

		}

		List<VariavelModel> VariavelList(string[] valores, Dictionary<int, string> variaveis, params int?[] p)
		{
			int valor = 0;
			foreach (var item in p)
			{
				if (item.HasValue)
				{
					valor = item.Value + 1;
					if (variaveis.ContainsKey(valor))
						variaveis.Remove(valor);
				}
			}

			try
			{
				var itens = variaveis
					.Select(a => new VariavelModel(a.Key, a.Value, valores.ElementAt(a.Key - 1)))
					.ToList();
				return itens;
			}
			catch (Exception)
			{

				throw;
			}
		}

		readonly object _locker = new object();


		[DisableFormValueModelBinding]
		[HttpPost("uploadfiles/{leiauteid:int?}")]
		[NivelPermissao(1, PaginaID = 126, SubPaginaID = 87)]
		public async Task<IActionResult> UploadAsync(int? leiauteid)
		{
			IActionResult res = BadRequest();
			string contentType = Request.ContentType;

			if (!MultipartRequestHelper.IsMultipartContentType(contentType))
				throw new Exception($"Esperado uma requisição multipart {contentType}");

			var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
			var reader = new MultipartReader(boundary, HttpContext.Request.Body, 80 * 1024);
			var section = await reader.ReadNextSectionAsync();

			if (section.GetContentDispositionHeader().IsFileDisposition())
			{
				var fileSection = section.AsFileSection();
				using (var mem = new MemoryStream())
				{
					await fileSection.FileStream.CopyToAsync(mem);
					res = await MontaCampanhaUploaded(
						mem.ToArray(),
						false,
						section.ContentType,
						fileSection.FileName,
						leiauteid: leiauteid
						);
				}
			}
			return res;
		}

		TimeSpan EstimativaEntrega(int intervalo, int capacidade5min, int totalenvio, int lotes) => TimeSpan.FromMinutes(((((totalenvio / lotes) / (intervalo == 0 ? 5 : intervalo / 5))) / capacidade5min * 5) * lotes);


		[HttpGet("estimativamedia/{intervalo:int}/{capacidade5min:int}/{totalenvio:int}/{lotes:int}")]
		public IActionResult CalculaEstimativa(int intervalo, int capacidade5min, int totalenvio, int lotes) => Ok(EstimativaEntrega(intervalo, capacidade5min, totalenvio, lotes));

		[DisableFormValueModelBinding]
		[NivelPermissao(1, PaginaID = 127, SubPaginaID = 0)]
		[HttpPost("uploadpadrao/{leiauteid:int?}")]
		public async Task<IActionResult> UplaodPadraoAsync(int? leiauteid)
		{
			IActionResult res = BadRequest();

			string contentType = Request.ContentType;

			if (!MultipartRequestHelper.IsMultipartContentType(contentType))
				throw new Exception($"Esperado uma requisição multipart {contentType}");

			var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
			var reader = new MultipartReader(boundary, HttpContext.Request.Body, 80 * 1024);
			var section = await reader.ReadNextSectionAsync();

			if (section.GetContentDispositionHeader().IsFileDisposition())
			{
				var fileSection = section.AsFileSection();
				using (var mem = new MemoryStream())
				{
					await fileSection.FileStream.CopyToAsync(mem);
					res = await MontaCampanhaUploaded(mem.ToArray(), true, section.ContentType, fileSection.FileName, leiauteid: leiauteid);
				}
			}
			return res;
		}

		string MontaVariaveis(IEnumerable<VariavelModel> variaveis) => variaveis.Any() ? variaveis.Select(j => $"{j.IDColuna}|{j.Variavel}|{j.Valor.NoAcento().ToAlphabetGSM()}").Aggregate((l, m) => $"{l};{m}") : string.Empty;


		Regex regPadraoArquivo = new Regex("([0-9]{4,9}).(txt|csv|TXT|CSV)", RegexOptions.Compiled);

		async Task<(HashSet<CampanhaModel> campanhas, List<CampanhaModel> invalidos)> MontaCampanhas(HashSet<CampanhaModel> c, List<CampanhaModel> invalidos, bool ispadraoenvio)
		{
			var celularesInvalidos = new List<CampanhaModel>() { };

			if (invalidos.Any())
				celularesInvalidos.AddRange(invalidos);

			var prefixos = await Util.CacheFactory<IEnumerable<PrefixoModel>>(_cache, "prefixos", _hostingEnvironment);

			if (prefixos == null || !prefixos.Any())
				throw new Exception("Não foi possível carregar a lista de prefixos");

			#region CELULARES_INVALIDOS
			//adicionando inválidos do tipo CELULARINVALIDO
			var _celularInvalido = (from a in Campanhas
									join b in prefixos on a.Celular.ToPrefixo() equals b.Prefixo into ps
									from b in ps.DefaultIfEmpty()
									where b == null
									select new CampanhaModel()
									{
										Texto = a.Texto,
										Carteira = a.Carteira,
										TipoCampanha = a.TipoCampanha,
										IDCliente = a.IDCliente,
										Arquivo = a.Arquivo,
										Celular = a.Celular,
										TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO,
										Variaveis = a.Variaveis,
										ArquivoZip = a.ArquivoZip
									}).ToList();

			if (_celularInvalido.Any())
			{
				celularesInvalidos.AddRange(_celularInvalido);
				Campanhas = Campanhas.Except(_celularInvalido, new CompareObject<CampanhaModel>(
					Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();
			}
			#endregion

			#region BLACKLIST

			var lBlacklist = await new BlacklistRepository().GetAll(new BlackListModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, null);

			if (lBlacklist != null && lBlacklist.Any())
			{
				var _blackLIst = (from a in Campanhas
								  join b in lBlacklist on a.Celular equals b.Celular into ps
								  from b in ps.DefaultIfEmpty()
								  where b != null
								  select new CampanhaModel()
								  {
									  Carteira = a.Carteira,
									  TipoCampanha = a.TipoCampanha,
									  IDCliente = a.IDCliente,
									  Texto = a.Texto,
									  Arquivo = a.Arquivo,
									  Celular = a.Celular,
									  TipoInvalido = TiposInvalidosEnums.BLACKLIST,
									  ArquivoZip = a.ArquivoZip,
									  Variaveis = a.Variaveis

								  }).ToList();

				if (_blackLIst.Count > 0)
				{
					celularesInvalidos.AddRange(_blackLIst);

					Campanhas = Campanhas.Except(_blackLIst, new CompareObject<CampanhaModel>(
						Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();
				}
			}
			#endregion

			#region ACIMA160CARACTERES
			var _acima160caracteres = Campanhas
				.Where(a => !string.IsNullOrEmpty(a.Texto))
				.Where(a => a.Texto.Length > 160);

			if (_acima160caracteres.Any())
			{
				celularesInvalidos.AddRange(_acima160caracteres.Select(a => new CampanhaModel()
				{
					Arquivo = a.Arquivo,
					Carteira = a.Carteira,
					TipoCampanha = a.TipoCampanha,
					Celular = a.Celular,
					Texto = a.Texto,
					IDCliente = a.IDCliente,
					TipoInvalido = TiposInvalidosEnums.ACIMA160CARACTERES,
					ArquivoZip = a.ArquivoZip,
					Variaveis = a.Variaveis
				}));
				Campanhas = Campanhas.Where(a => a.Texto.Length <= 160).ToHashSet();
			}
			#endregion

			#region FILTRADO

			var filtrado = await Util.CacheFactory<HashSet<decimal>>(_cache, "quarentena", _hostingEnvironment);

			if (filtrado != null && filtrado.Any())
			{
				var dadosfitrados = from a in Campanhas.AsParallel()
									join b in filtrado.AsParallel() on a.Celular equals b
									into ps
									from b in ps.DefaultIfEmpty()
									where b != 0
									select new CampanhaModel()
									{
										Carteira = a.Carteira,
										TipoCampanha = a.TipoCampanha,
										Arquivo = a.Arquivo,
										Celular = a.Celular,
										Texto = a.Texto,
										IDCliente = a.IDCliente,
										TipoInvalido = TiposInvalidosEnums.FILTRADO,
										ArquivoZip = a.ArquivoZip,
										Variaveis = a.Variaveis
									};

				Campanhas = Campanhas.Except(dadosfitrados.AsParallel(), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();
				celularesInvalidos.AddRange(dadosfitrados);
			}
			#endregion

			#region HIGIENIZADO
			if (ispadraoenvio)
			{
				if (Campanhas.ElementAt(0).Carteira.Higieniza)
				{
					var carteira = Campanhas.ElementAt(0).Carteira;

					if (carteira.DiasHigienizacao.HasValue)
					{
						var higienizados = (await new CampanhaRepository().HigienizaCarteira(
										Campanhas,
										carteira.CarteiraID.Value,
										carteira.DiasHigienizacao.Value,
										ClienteID,
										UsuarioID)).ToList();

						if (higienizados.Any())
						{
							var dadoshigienizados = (from a in Campanhas
													 join _b in higienizados on a.Celular equals _b.Celular into ps
													 from _b in ps.DefaultIfEmpty()
													 where _b != null
													 select new CampanhaModel()
													 {
														 Texto = a.Texto,
														 Carteira = a.Carteira,
														 TipoCampanha = a.TipoCampanha,
														 IDCliente = a.IDCliente,
														 Arquivo = a.Arquivo,
														 Celular = a.Celular,
														 TipoInvalido = TiposInvalidosEnums.HIGIENIZADO,
														 Variaveis = a.Variaveis,
														 ArquivoZip = a.ArquivoZip
													 });

							Campanhas = Campanhas.Except(dadoshigienizados.AsParallel(), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();
							celularesInvalidos.AddRange(dadoshigienizados);
						} 
					}
				}
			}
			#endregion

			//vinculando as campanhas com operadora DESCONHECIDA
			Campanhas = Campanhas
				.Join(prefixos, a => a.Celular.ToPrefixo(), b => b.Prefixo, (a, b) => new CampanhaModel()
				{
					Texto = a.Texto,
					IDCliente = a.IDCliente,
					Arquivo = a.Arquivo,
					Celular = a.Celular,
					Operadora = b.Operadora,
					ArquivoZip = a.ArquivoZip,
					Variaveis = a.Variaveis,
					Carteira = a.Carteira,
					TipoCampanha = a.TipoCampanha,
				}).ToHashSet();

			return (Campanhas, celularesInvalidos);
		}

		async Task<List<string[]>> ListaCelulares(IEnumerable<byte> r)
		{
			using (var mem = new MemoryStream(r.AsParallel().ToArray()))
			{
				using (var reader = new StreamReader(mem, Encoding.UTF7, true))
				{
					string linha = null;
					var linhas = new List<string[]>() { };
					while (reader.Peek() >= 0)
					{
						linha = await reader.ReadLineAsync();
						if (!string.IsNullOrEmpty(linha))
							linhas.Add(linha
								.Trim()
								.TrimEnd(';')
								.Split(";".ToCharArray()));
					}
					return linhas;
				}
			}
		}
		(int indice, int quantidade) VariavelEspecial(LeiauteModel l, string v)
		{
			var _variaveis = l.LeiauteVariaveis.SingleOrDefault(k => k.Variavel == v);

			if (_variaveis == null)
				return (0, 0);

			return (_variaveis.InicioLeitura.Value, _variaveis.QuantidadeCaracteres.Value);
		}

		async Task<IEnumerable<CampanhaModel>> ListaCelularesLayoutEspecial(IEnumerable<byte> r, LeiauteModel l)
		{
			var camps = new List<CampanhaModel>() { };
			int contador = 0;
			CampanhaModel camp = null;
			string mensagem = null;
			switch (l.Nome)
			{
				case "FC1R":

					using (var mem = new MemoryStream(r.AsParallel().ToArray()))
					{
						using (var reader = new StreamReader(mem, Encoding.UTF7, true))
						{
							string linha = null;


							while (reader.Peek() >= 0)
							{
								camp = new CampanhaModel();
								linha = await reader.ReadLineAsync();

								if (contador == 1)
									mensagem = linha.Trim().Substring(1);
								else if (contador > 1 && linha.Length >= 108) //linha de dados
								{
									linha = linha.Trim();
									var _telefone = VariavelEspecial(l, "#telrec");
									var _numero = VariavelEspecial(l, "#numero");
									var _idcliente = VariavelEspecial(l, "#idcliente");
									var _nome = VariavelEspecial(l, "#nomecli");

									camp.IDCliente = linha.Substring(_idcliente.indice, _idcliente.quantidade).Trim();
									camp.Texto = mensagem.Replace("$NOMECLI$", linha.Substring(_nome.indice, _nome.quantidade).Trim())
														.Replace("$TELREC$", linha.Substring(_telefone.indice, _telefone.quantidade).Trim())
														.Substring(1)
														.NoAcento()
														.ToAlphabetGSM();

									camp.TipoInvalido = TiposInvalidosEnums.VALIDO;

									if (camp.Texto.Length > 160)
										camp.TipoInvalido = TiposInvalidosEnums.ACIMA160CARACTERES;

									if (!string.IsNullOrEmpty(linha.Substring(_numero.indice, _numero.quantidade).CleanInvalidCaracteres()))
										camp.Celular = decimal.Parse(linha.Substring(_numero.indice, _numero.quantidade).Trim().CleanInvalidCaracteres());
									else
										camp.TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO;

									camps.Add(camp);
								}
								contador++;
							}


						}
					}
					break;
				case "CSLOG":

					break;
			}

			return camps;
		}

		async Task<IActionResult> MontaCampanhaUploaded(byte[] linhas, bool ispadraoenvio, string contentype, string arquivo, int? leiauteid = null, List<CampanhaModel> campanhas = null, List<CampanhaModel> invalidos = null, CampanhaResultModel campResult = null, LeiauteModel leiauteModel = null, int? idfile = null)
		{
			arquivo = arquivo.NoAcento();

			var erroUplaod = new List<ErroUploadArquivoModel>() { };

			List<string[]> foradoPadrao = new List<string[]>() { };

			List<CampanhaResultModel> _campResult = new List<CampanhaResultModel>() { };

			var _bDTO = new BaseEntityDTO<IEnumerable<CampanhaResultModel>>() { Start = DateTime.Now };
			IActionResult res = null;

			try
			{
				List<string> arquivosInZip = new List<string>() { };
				List<CampanhaResultModel> campanhaResult = new List<CampanhaResultModel>(await CampsResults(idfile));
				Dictionary<string, string> arquivosForadoPadrao = new Dictionary<string, string>() { };
				List<CampanhaModel> celularesInvalidos = new List<CampanhaModel>() { };
				var session = new List<SessionDataModel>() { };
				var f = (await new FornecedorRepository().FornecedorTelaEnvio(ClienteID)).ToList();
				List<CampanhaResultModel> camps = new List<CampanhaResultModel>() { };
				FileUploadMultiplesModel fileuploadmultiples = new FileUploadMultiplesModel();

				bool isziped = false;

				IEnumerable<LeiauteModel> _layout = new LeiauteModel[] { }.AsEnumerable();

				LeiauteModel layout = null;

				if (leiauteid.HasValue)
					layout = await new LeiauteRepository().FindById(new LeiauteModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, LeiauteID = leiauteid.Value }, UsuarioID);
				else
				{
					_layout = leiauteModel == null ? await new LeiauteRepository().GetAll(new LeiauteModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID) : new LeiauteModel[] { leiauteModel };
					layout = leiauteModel ?? _layout.SingleOrDefault(a => a.Padrao);
				}

				

				if (layout == null)
					throw new Exception("Leiaute diferente");

				if (!layout.LeiauteVariaveis.Any())
					throw new Exception("Layout sem variáveis");

				int colunas = layout.LeiauteVariaveis.Count();

				List<FileUp> fileup = new List<FileUp>() { };
				List<string[]> _dados = new List<string[]>() { };
				int colunaCelular = layout.LeiauteVariaveis.SingleOrDefault(a => a.Variavel == "#numero").IDColuna - 1;
				int? colunaTexto = IDColuna(layout, "#mensagem");
				int? colunaIdCliente = IDColuna(layout, "#idcliente");
				var variaveis = layout.LeiauteVariaveis.Select(a => new { Indice = a.IDColuna, Variavel = a.Variavel }).ToDictionary(a => a.Indice, a => a.Variavel);
				var fileupload = new List<FileUploadModel>() { };
				var _campInternal = new CampanhaModel();

				fileuploadmultiples.Arquivo = arquivo;


				#region Montagem FileUpload
				if (contentype == "application/x-zip-compressed" || contentype == "application/zip")
				{
					var itens = await linhas.ToUnZip();
					isziped = true;
					fileup.AddRange(itens.Select(a => new FileUp()
					{
						FileName = a.Key,
						Linhas = a.Value.ToArray(),
						FileZip = arquivo,

					}));

					await Util.UploadAamazon(linhas, $"{ClienteID}/{arquivo}", "moneoup");

					var fileExists = (await new CampanhaRepository().ArquivoExistente(fileup.Select(a => a.FileName), ClienteID)).ToList();


					if (fileExists.Any())
					{
						erroUplaod.AddRange(fileExists.Select(k => new ErroUploadArquivoModel(k, TipoErroUploadArquivoEnum.DUPLICADO, "Arquivo já existente no sistema")));
						throw new Exception("Arquivo já existente no sistema");
					}

					if (ispadraoenvio)
					{

						var padroes = await new PadraoPostagensRepository().PadroesToEnvio(fileup.Select(a => new
						{
							Key = a.FileName,
							Value = regPadraoArquivo.Replace(a.FileName, string.Empty)
						}).ToDictionary(a => a.Key, a => a.Value), ClienteID, UsuarioID);



						foreach (var item in padroes.Padrao.Where(a => !a.TipoCampanha.Visivel))
							erroUplaod.Add(new ErroUploadArquivoModel(item.Padrao, TipoErroUploadArquivoEnum.CARTEIRANAOATIVA, $"O tipo de campanha associado ao padrão de envio está inativo"));

						foreach (var item in padroes.Padrao.Where(a => !a.Carteira.Visivel || a.Carteira.BloqueioEnvio))
						{
							if (!item.Carteira.Visivel)
								erroUplaod.Add(new ErroUploadArquivoModel(item.Padrao, TipoErroUploadArquivoEnum.CARTEIRANAOATIVA, $"Um padrão selecionado {item.Padrao} possui uma carteira inativa {item.Carteira.Carteira}"));

							if (item.Carteira.BloqueioEnvio)
								erroUplaod.Add(new ErroUploadArquivoModel(item.Padrao, TipoErroUploadArquivoEnum.CARTEIRANAOATIVA, $"Um padrão selecionado {item.Padrao} possui uma carteira bloqueada pra envio {item.Carteira.Carteira}"));
						}

						var gestores = await new GestorRepository().GestorByCarteirasEnvio(padroes.Padrao.Where(a => a.Carteira.Visivel || !a.Carteira.BloqueioEnvio).Select(a => a.Carteira.CarteiraID.Value), ClienteID);

						if (gestores != null || gestores.Any())
							gestores = gestores.ToList();


						if (padroes.Padrao.Any())
						{
							var _fileup = (from a in fileup
										   join b in padroes.Padrao on regPadraoArquivo.Replace(a.FileName, string.Empty) equals b.Padrao into ps
										   from b in ps.DefaultIfEmpty()

										   select new FileUp()
										   {
											   Gestores = b == null ? null : gestores.Where(m => m.CarteiraID == b.CarteiraID).GroupBy(m => new GestorModel() { Nome = m.Nome, GestorID = m.GestorID }, (m, n) => new GestorModel()
											   {
												   Nome = m.Nome,
												   GestorID = m.GestorID,
												   Telefones = n.Select(p => p.Celular).ToList(),
												   Emails = n.Select(p => p.Email).ToList()
											   },
											   new CompareObject<GestorModel>((m, n) => m.GestorID == n.GestorID, i => i.GestorID.GetHashCode())),
											   ArquivoPadrao = regPadraoArquivo.Replace(a.FileName, string.Empty),
											   FileName = a.FileName,
											   Linhas = a.Linhas,
											   FileZip = arquivo,
											   Carteira = b == null ? null : b.Carteira,
											   TipoCampanha = b == null ? null : b.TipoCampanha,
											   Leiaute = b == null ? null : b.Leiaute,
											   ForaPadrao = b == null
										   }).ToList();


							fileup.Clear();

							foreach (var item in _fileup)
								if ((item.Carteira == null || item.Carteira.Visivel) && (item.TipoCampanha == null || item.TipoCampanha.Visivel)) fileup.Add(item);

						}
					}
				}
				else
				{

					var fileExists = await new CampanhaRepository().ArquivoExistente(new string[] { arquivo }, ClienteID);

					if (fileExists.Any())
						throw new Exception("Arquivo já existente no sistema");

					var _fileup = new FileUp()
					{
						FileName = arquivo,
						Linhas = linhas
					};

					CarteiraModel carteira = null;
					TipoCampanhaModel tipocampanha = null;

					await Util.UploadAamazon(linhas, $"{ClienteID}/{arquivo}", "moneoup");

					if (ispadraoenvio)
					{
						if (!regPadraoArquivo.IsMatch(arquivo))
							throw new Exception("Nomenclatura de arquivo fora do padrão");

						_fileup.ArquivoPadrao = regPadraoArquivo.Replace(_fileup.FileName, string.Empty);

						var padroes = await new PadraoPostagensRepository().PadroesToEnvio(new FileUp[] { _fileup }.ToDictionary(a => a.FileName, a => a.ArquivoPadrao), ClienteID, UsuarioID);

						if (padroes.Padrao == null || !padroes.Padrao.Any())
						{
							_fileup.ForaPadrao = true;
						}
						else
						{
							if (padroes.Padrao.Any(a => !a.TipoCampanha.Visivel))
							{
								var tipo = padroes.Padrao.Where(a => !a.TipoCampanha.Visivel);
								throw new Exception($"Um padrão selecionado {tipo.ElementAt(0).Padrao} possui um tipo inativo {tipo.ElementAt(0).TipoCampanha.TipoCampanha}");
							}

							if (padroes.Padrao.Any(a => !a.Carteira.Visivel))
							{
								var tipo = padroes.Padrao.Where(a => !a.Carteira.Visivel);
								throw new Exception($"Um padrão selecionado {padroes.Padrao.ElementAt(0).Padrao} possui uma carteira inativa {padroes.Padrao.ElementAt(0).Carteira.Carteira}");
							}


							if (padroes.Padrao.Any(a => a.Carteira.BloqueioEnvio))
							{
								var tipo = padroes.Padrao.Where(a => a.Carteira.BloqueioEnvio);
								throw new Exception($"Um padrão selecionado {padroes.Padrao.ElementAt(0).Padrao} possui uma carteira bloqueada pra envio {padroes.Padrao.ElementAt(0).Carteira.Carteira}");
							}


							layout = _layout.Where(l => l.LeiauteID == padroes.Padrao.ElementAt(0).Leiaute.LeiauteID).ElementAt(0);
							colunas = layout.LeiauteVariaveis.Count();
							colunaCelular = layout.LeiauteVariaveis.SingleOrDefault(a => a.Variavel == "#numero").IDColuna - 1;
							colunaTexto = IDColuna(layout, "#mensagem");
							colunaIdCliente = IDColuna(layout, "#idcliente");
							variaveis = layout.LeiauteVariaveis.Select(a => new { Indice = a.IDColuna, Variavel = a.Variavel }).ToDictionary(a => a.Indice, a => a.Variavel);

							carteira = padroes.Padrao.ElementAt(0).Carteira;

							if (padroes.Padrao.ElementAt(0).Carteira == null)
								throw new Exception($"Carteira {carteira.Carteira} não localizado para o padrão {padroes.Padrao.ElementAt(0).Padrao}");
							else if (!padroes.Padrao.ElementAt(0).Carteira.Visivel)
								throw new Exception($"Carteira {carteira.Carteira} não ativa para envio");

							tipocampanha = padroes.Padrao.ElementAt(0).TipoCampanha;

							_fileup.Carteira = carteira;
							_fileup.Gestores = await new GestorRepository().GestorByCarteira(carteira.CarteiraID.Value, ClienteID);
							_fileup.TipoCampanha = tipocampanha;
							_fileup.ForaPadrao = false;
							_fileup.Leiaute = layout;
						}
					}

					fileup.Add(_fileup);
				}
				#endregion

				
				await fileup
					.Where(a => !a.ForaPadrao)
					.ToObservable().ForEachAsync(async a =>
					{

						if (layout.IsEspecial)
						{
							var dados = await ListaCelularesLayoutEspecial(a.Linhas, layout);
							Campanhas = dados.Where(k => k.TipoInvalido == TiposInvalidosEnums.VALIDO)
							.Select(k => new CampanhaModel()
							{
								Celular = k.Celular,
								Texto = k.Texto,
								TipoCampanha = a.TipoCampanha,
								Arquivo = new ArquivoCampanhaModel() { Arquivo = a.FileName },
								ArquivoZip = k.ArquivoZip
							}).ToHashSet();

							celularesInvalidos.AddRange(dados.Where(k => k.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO));
							celularesInvalidos.AddRange(dados.Where(k => k.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES));
						}
						else
						{
							var dados = await ListaCelulares(a.Linhas);
							a.Registros = dados.Count;
							dados.AsParallel().ForAll(k =>
							{
								lock (_locker)
								{
									if (k.Length == colunas)
									{
										_campInternal = new CampanhaModel()
										{
											Carteira = a.Carteira,
											TipoCampanha = a.TipoCampanha,
											Texto = colunaTexto.HasValue ? k.ElementAt(colunaTexto.Value).Trim().NoAcento().ToAlphabetGSM() : null,
											IDCliente = colunaIdCliente.HasValue ? k.ElementAt(colunaIdCliente.Value).Trim() : null,
											Arquivo = new ArquivoCampanhaModel() { Arquivo = a.FileName },
											ArquivoZip = a.FileZip,
											Variaveis = VariavelList(k, variaveis, colunaTexto, colunaIdCliente, colunaCelular)
										};

										if (!string.IsNullOrEmpty(k.ElementAt(colunaCelular).CleanInvalidCaracteres()))
										{
											_campInternal.Celular = decimal.Parse(k.ElementAt(colunaCelular).CleanInvalidCaracteres()).NormalizeCell();

											if (!Campanhas.Add(_campInternal))
											{
												_campInternal.TipoInvalido = TiposInvalidosEnums.DUPLICADO;
												celularesInvalidos.Add(_campInternal);
											}
										}
										else
										{
											_campInternal.TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO;
											string numero = Regex.Replace(k.ElementAt(colunaCelular), "\\D+", string.Empty);

											decimal _celular = 0M;
											decimal.TryParse(numero, out _celular);

											_campInternal.Celular = _celular;
											celularesInvalidos.Add(_campInternal);
										}
									}
									else
										celularesInvalidos.Add(new CampanhaModel()
										{
											TipoInvalido = TiposInvalidosEnums.LEIAUTEINVALIDO,
											ForadoPadrao = k,
											Arquivo = new ArquivoCampanhaModel() { Arquivo = arquivo},
											ArquivoZip= a.FileZip
										});
								}
							});
						}



					});


				foreach (var item in fileup.Where(a => a.ForaPadrao))
				{
					int id = await new CampanhaRepository().InsertFileCards(item.FileName, GuidUser);

					var _camp = new CampanhaResultModel()
					{
						Situacao = "FORAPADRAO",
						Leiaute = layout,
						ID = id,
						Fornecedor = f,
						Arquivo = item.FileName,
						ArquivoForaPadrao = true
					};

					_campResult.Add(_camp);

					session.Add(new SessionDataModel(GuidUser) { Key = $"foradopadrao_{id}_{item.ArquivoPadrao}", Data = DateTime.Now, Value = item.Linhas });
					session.Add(new SessionDataModel(GuidUser) { Key = id.ToString(), Data = DateTime.Now, Value = Util.EncoderDefaultFiles.GetBytes(JsonConvert.SerializeObject(_camp)) });
				}



				if (session.Any())
				{
					await sessionrepository.Add(session, ClienteID, UsuarioID);
					session.Clear();
				}


				if (fileup.Any(a => !a.ForaPadrao))
				{
					var _campanha = await MontaCampanhas(Campanhas, celularesInvalidos, ispadraoenvio);
					Campanhas = _campanha.campanhas;
					celularesInvalidos = _campanha.invalidos;

					if (!Campanhas.Any())
						throw new Exception("Sem registros válidos pra envio");

					if (isziped) //sendo zip e não padrao de envio, o campanha result deve ser um só
					{
						if (!ispadraoenvio)
						{
							_campResult.Add(new CampanhaResultModel()
							{

								Variaveis = layout.LeiauteVariaveis.Where(k => !k.Variavel.Equals("#numero") && !k.Variavel.Equals("#idcliente")).Select(option => option.Variavel),
								PermitirLoteAtrasado = false,
								ArquivoForaPadrao = false,
								IsMsgEmpty = !colunaTexto.HasValue,
								Mensagem = string.Empty,
								Situacao = "PENDENTE",
								Arquivos = fileup.Select(a => a.FileName).ToList(),
								Registros = fileup.Sum(a => a.Registros),
								Leiaute = layout,
								Fornecedor = f.Select(j => new FornecedorModel()
								{
									Nome = j.Nome,
									FornecedorID = j.FornecedorID,
									Distribuicao = j.Distribuicao,
									Data = j.Data,
									CapacidadeTotal = j.CapacidadeTotal,
									Entrega = j.Entrega,
									Capacidade5M = j.Capacidade5M,
									Eficiencia = j.Eficiencia,
									Quantidade = j.Quantidade,
									Agendados = j.Agendados,
									EstimativaEntrega = EstimativaEntrega(5, j.Capacidade5M, j.Agendados + ((int)(j.Distribuicao / 100 * Campanhas.Where(option => option.ArquivoZip == arquivo).Count())), 1)
								}).ToList(),
								Arquivo = arquivo,
								CampanhasLista = Campanhas.Where(k => k.ArquivoZip == arquivo).ToList(),
								Campanhas = Campanhas.Where(k => k.ArquivoZip == arquivo).Take(10).Select(j => new CampanhaListagemResult() { Celular = j.Celular, Texto = j.Texto }).ToList(),
								TotalInvalidos = celularesInvalidos.Where(j => j.ArquivoZip == arquivo).Count(),
								CampanhaInvalida = celularesInvalidos.Where(j => j.ArquivoZip == arquivo).ToList(),
								RegistrosValidos = Campanhas.Where(j => j.ArquivoZip == arquivo).Count(),
								Invalidos = new CampanhaInvalidos()
								{
									Acima160Caracteres = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES).Count(),
									Blacklist = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.BLACKLIST).Count(),
									CelularInvalido = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO).Count(),
									Higienizado = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count(),
									Filtrado = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.FILTRADO).Count(),
									ForaPadrao = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).Count(),
									Duplicados = celularesInvalidos.Where(j => j.ArquivoZip == arquivo && j.TipoInvalido == TiposInvalidosEnums.DUPLICADO).Count()
								}
							});
						}
						else
						{
							_campResult.AddRange(fileup.Where(a => !a.ForaPadrao).Select(a => new CampanhaResultModel() //sendo padraoenvio, cada arquivo é montado
							{
								Gestores = a.Gestores,
								Carteira = a.Carteira,
								TipoCampanha = a.TipoCampanha,
								Variaveis = layout.IsEspecial ? new string[] { } : layout.LeiauteVariaveis.Where(k => !k.Variavel.Equals("#numero") && !k.Variavel.Equals("#idcliente")).Select(option => option.Variavel),
								PermitirLoteAtrasado = false,
								ArquivoForaPadrao = false,
								IsMsgEmpty = !colunaTexto.HasValue,
								Mensagem = string.Empty,
								Situacao = "PENDENTE",
								Arquivos = new string[] { },
								Registros = a.Registros,
								Leiaute = layout.IsEspecial ? null : layout,
								Fornecedor = f.Select(j => new FornecedorModel()
								{
									Nome = j.Nome,
									FornecedorID = j.FornecedorID,
									Distribuicao = j.Distribuicao,
									Data = j.Data,
									CapacidadeTotal = j.CapacidadeTotal,
									Entrega = j.Entrega,
									Capacidade5M = j.Capacidade5M,
									Eficiencia = j.Eficiencia,
									Quantidade = j.Quantidade,
									Agendados = j.Agendados,
									EstimativaEntrega = EstimativaEntrega(5, j.Capacidade5M, j.Agendados + ((int)(j.Distribuicao / 100 * Campanhas.Where(option => option.ArquivoZip == arquivo).Count())), 1)
								}).ToList(),
								Arquivo = a.FileName,
								CampanhasLista = Campanhas.Where(k => k.ArquivoZip == arquivo).ToList(),
								Campanhas = Campanhas.Where(k => k.ArquivoZip == arquivo).Take(10).Select(j => new CampanhaListagemResult() { Celular = j.Celular, Texto = j.Texto }).ToList(),
								TotalInvalidos = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName).Count(),
								CampanhaInvalida = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName).ToList(),
								RegistrosValidos = Campanhas.Where(j => j.Arquivo.Arquivo == a.FileName).Count(),
								Invalidos = new CampanhaInvalidos()
								{
									Acima160Caracteres = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES).Count(),
									Blacklist = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.BLACKLIST).Count(),
									CelularInvalido = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO).Count(),
									Higienizado = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count(),
									Filtrado = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.FILTRADO).Count(),
									ForaPadrao = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).Count(),
									Duplicados = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.DUPLICADO).Count()
								}
							}));
						}
					}
					else
					{
						_campResult.AddRange(fileup.Where(a => !a.ForaPadrao).Select(a => new CampanhaResultModel() //sendo padraoenvio, cada arquivo é montado
						{
							Gestores = a.Gestores,
							Variaveis = layout.LeiauteVariaveis.Where(k => !k.Variavel.Equals("#numero") && !k.Variavel.Equals("#idcliente")).Select(option => option.Variavel),
							PermitirLoteAtrasado = false,
							Carteira = a.Carteira,
							TipoCampanha = a.TipoCampanha,
							ArquivoForaPadrao = false,
							IsMsgEmpty = !colunaTexto.HasValue,
							Mensagem = string.Empty,
							Situacao = "PENDENTE",

							Arquivos = new string[] { },
							Registros = a.Registros,
							Leiaute = layout,
							Fornecedor = f.Select(j => new FornecedorModel()
							{
								Nome = j.Nome,
								FornecedorID = j.FornecedorID,
								Distribuicao = j.Distribuicao,
								Data = j.Data,
								CapacidadeTotal = j.CapacidadeTotal,
								Entrega = j.Entrega,
								Capacidade5M = j.Capacidade5M,
								Eficiencia = j.Eficiencia,
								Quantidade = j.Quantidade,
								Agendados = j.Agendados,
								EstimativaEntrega = EstimativaEntrega(5, j.Capacidade5M, j.Agendados + ((int)(j.Distribuicao / 100 * Campanhas.Where(option => option.Arquivo.Arquivo == arquivo).Count())), 1)
							}).ToList(),
							Arquivo = a.FileName,
							CampanhasLista = Campanhas.Where(k => k.Arquivo.Arquivo == a.FileName).ToList(),
							Campanhas = Campanhas.Where(k => k.Arquivo.Arquivo == a.FileName).Take(10).Select(j => new CampanhaListagemResult() { Celular = j.Celular, Texto = j.Texto }).ToList(),
							TotalInvalidos = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName).Count(),
							CampanhaInvalida = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName).ToList(),
							RegistrosValidos = Campanhas.Where(j => j.Arquivo.Arquivo == a.FileName).Count(),
							Invalidos = new CampanhaInvalidos()
							{
								Acima160Caracteres = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES).Count(),
								Blacklist = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.BLACKLIST).Count(),
								CelularInvalido = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO).Count(),
								Higienizado = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count(),
								Filtrado = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.FILTRADO).Count(),
								ForaPadrao = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).Count(),
								Duplicados = celularesInvalidos.Where(j => j.Arquivo.Arquivo == a.FileName && j.TipoInvalido == TiposInvalidosEnums.DUPLICADO).Count()
							}
						}));
					}


					if (Campanhas.Any() && _campResult.Any())
					{

						foreach (var item in _campResult.Where(a => !a.ArquivoForaPadrao))
						{
							item.ID = await new CampanhaRepository().InsertFileCards(item.Arquivo, GuidUser);

							//gravando as inválidas leiaute padrão
							if (item.CampanhaInvalida.Any(a => a.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO))
								session.Add(new SessionDataModel(GuidUser)
								{
									Key = $"foradopadrao_{item.ID}",
									Data = DateTime.Now,
									Value = item.CampanhaInvalida.Where(j => j.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{ArrayForaPadrao(k.ForadoPadrao)}\r\n")).ToArray()
								});
							else
								session.Add(new SessionDataModel(GuidUser)
								{
									Key = $"invalidos_{item.ID}",
									Data = DateTime.Now,
									Value = item.CampanhaInvalida.Where(j => j.TipoInvalido != TiposInvalidosEnums.LEIAUTEINVALIDO).SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
								});


							//gravando os válidos
							session.Add(new SessionDataModel(GuidUser)
							{
								Key = item.Arquivo,
								Data = DateTime.Now,
								Value = item.CampanhasLista.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{k.DataEnviar};{k.Operadora};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							});

							item.CampanhasLista.Clear();
							item.CampanhaInvalida.Clear();

							//gravando o campanha result
							session.Add(new SessionDataModel(GuidUser)
							{
								Key = item.ID.ToString(),
								Data = DateTime.Now,
								Value = Util.EncoderDefaultFiles.GetBytes(JsonConvert.SerializeObject(item))
							});
						}
						await sessionrepository.Add(session, ClienteID, UsuarioID);
					}
				}



				_campResult = await ChecaExistente(_campResult, Campanhas);

				Campanhas.Clear();


				campanhaResult.AddRange(_campResult);
				_bDTO.Result = campanhaResult.Join(_campResult, a => a.Arquivo, b => b.Arquivo, (a, b) => CampanhaResultObj(b));

				_bDTO.Observacao = erroUplaod;
				_bDTO.End = DateTime.Now;
				_bDTO.Itens = campanhaResult.Count;
				res = Ok(_bDTO);
			}
			catch (Exception err)
			{
				_bDTO.Observacao = erroUplaod;
				_bDTO.End = DateTime.Now;
				_bDTO.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_bDTO);
			}

			return res;
		}

		async Task<List<CampanhaResultModel>> ChecaExistente(List<CampanhaResultModel> c, HashSet<CampanhaModel> campanha)
		{

			var campanhaResult = (await CampsResults()).ToList();

			campanhaResult = campanhaResult.Except(c, new CompareObject<CampanhaResultModel>((a, b) => a.ID == b.ID, i => i.ID.GetHashCode())).ToList();

			foreach (var item in campanhaResult)
			{
				var camps = await CampanhaList(item.Arquivo);
				campanha = campanha.Except(camps, new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();

				if (!campanha.Any())
					throw new Exception("Sem dados suficientes para o arquivo");
			}

			return c;

		}

		string ArrayForaPadrao(string[] s) => s.Select(k => k).Aggregate((a, b) => $"{a};{b}");


		[HttpPut("padraoenvio/{id:int}/")]
		public async Task<IActionResult> VinculaPadraoEnvio([FromBody] PadraoPostagensModel p, int id)
		{
			var _bDTO = new BaseEntityDTO<IEnumerable<CampanhaResultModel>>() { Start = DateTime.Now };
			IActionResult res = null;

			try
			{
				p.Padrao = regPadraoArquivo.Replace(p.Padrao, string.Empty);
				var padrao = await new PadraoPostagensRepository().Adicionaitem(p, ClienteID, UsuarioID);


				var _camps = await CampsResults(id);
				var camp = _camps.ElementAtOrDefault(0);


				var fileExists = await new CampanhaRepository().ArquivoExistente(new string[] { camp.Arquivo }, ClienteID);

				if (fileExists.Any())
					throw new Exception($"Arquivo {camp.Arquivo} já existente no sistema");

				int registros = 0;
				if (padrao.Padrao.Equals(regPadraoArquivo.Replace(camp.Arquivo, string.Empty))) //avaliando a igualdade da sequência
				{

					var retorno = await sessionrepository.FindById(new SessionDataModel(GuidUser) { Key = $"foradopadrao_{id}_{regPadraoArquivo.Replace(camp.Arquivo, string.Empty)}" }, UsuarioID);

					var _layout = await new LeiauteRepository().GetAll(new LeiauteModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
					if (!_layout.Any(a => a.Padrao))
						throw new Exception("Sem layouts definidos");

					var layout = _layout.SingleOrDefault(a => a.Padrao);

					if (layout == null)
						throw new Exception("Sem layout padrão definido");

					if (!layout.LeiauteVariaveis.Any())
						throw new Exception("Layout sem variáveis");

					var carteira = padrao.Carteira;
					var tipocampanha = padrao.TipoCampanha;

					int colunaCelular = layout.LeiauteVariaveis.SingleOrDefault(a => a.Variavel == "#numero").IDColuna - 1;
					int? colunaTexto = IDColuna(layout, "#mensagem");
					int? colunaIdCliente = IDColuna(layout, "#idcliente");
					var variaveis = layout.LeiauteVariaveis.Select(a => new { Indice = a.IDColuna, Variavel = a.Variavel }).ToDictionary(a => a.Indice, a => a.Variavel);
					int colunas = layout.LeiauteVariaveis.Count();
					var celularesInvalidos = new List<CampanhaModel>() { };
					var _campInternal = new CampanhaModel();

					var _fileup = new FileUp()
					{
						FileName = camp.Arquivo,
						Linhas = retorno.Value
					};



					var dados = await ListaCelulares(retorno.Value);
					registros += dados.Count;
					Campanhas = new HashSet<CampanhaModel>() { };
					dados.AsParallel().ForAll(k =>
					{
						lock (_locker)
						{

							if (k.Length == colunas)
							{
								_campInternal = new CampanhaModel()
								{
									Carteira = camp.Carteira,
									TipoCampanha = camp.TipoCampanha,
									Texto = colunaTexto.HasValue ? k.ElementAt(colunaTexto.Value).Trim().NoAcento().ToAlphabetGSM() : null,
									IDCliente = colunaIdCliente.HasValue ? k.ElementAt(colunaIdCliente.Value).Trim() : null,
									Arquivo = new ArquivoCampanhaModel() { Arquivo = camp.Arquivo },
									Variaveis = VariavelList(k, variaveis, colunaTexto, colunaIdCliente, colunaCelular)
								};

								if (!string.IsNullOrEmpty(k.ElementAt(colunaCelular).CleanInvalidCaracteres()))
								{
									_campInternal.Celular = decimal.Parse(k.ElementAt(colunaCelular).CleanInvalidCaracteres()).NormalizeCell();

									if (!Campanhas.Add(_campInternal))
									{
										_campInternal.TipoInvalido = TiposInvalidosEnums.DUPLICADO;
										celularesInvalidos.Add(_campInternal);
									}
								}
								else
								{
									_campInternal.TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO;
									string numero = Regex.Replace(k.ElementAt(colunaCelular), "\\D+", string.Empty);

									decimal _celular = 0M;
									decimal.TryParse(numero, out _celular);

									_campInternal.Celular = _celular;
									celularesInvalidos.Add(_campInternal);
								}
							}
							else
								celularesInvalidos.Add(new CampanhaModel()
								{
									TipoInvalido = TiposInvalidosEnums.LEIAUTEINVALIDO,
									ForadoPadrao = k,
									Arquivo = new ArquivoCampanhaModel() { Arquivo = camp.Arquivo}
									
								});

						
						}
					});

					var _campanha = await MontaCampanhas(Campanhas, celularesInvalidos, true);
					Campanhas = _campanha.campanhas;
					celularesInvalidos = _campanha.invalidos;


					camp.Carteira = carteira;
					camp.TipoCampanha = tipocampanha;
					camp.ArquivoForaPadrao = false;
					camp.Variaveis = layout.LeiauteVariaveis.Where(a => !a.Variavel.Equals("#numero") || !a.Variavel.Equals("#idcliente")).Select(option => option.Variavel);
					camp.PermitirLoteAtrasado = false;
					camp.IsMsgEmpty = !colunaTexto.HasValue;
					camp.Mensagem = string.Empty;
					camp.Situacao = "PENDENTE";
					camp.Registros = registros;
					camp.Leiaute = layout;
					camp.Fornecedor = await new FornecedorRepository().FornecedorTelaEnvio(ClienteID);
					camp.CampanhasLista = Campanhas.ToList();
					camp.Campanhas = Campanhas.Take(10).Select(j => new CampanhaListagemResult() { Celular = j.Celular, Texto = j.Texto }).ToList();
					camp.TotalInvalidos = celularesInvalidos.Count();
					camp.CampanhaInvalida = celularesInvalidos.ToList();
					camp.RegistrosValidos = Campanhas.Count();
					camp.Invalidos = new CampanhaInvalidos()
					{
						Acima160Caracteres = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES).Count(),
						Blacklist = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.BLACKLIST).Count(),
						CelularInvalido = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO).Count(),
						Higienizado = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count(),
						Filtrado = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.FILTRADO).Count(),
						ForaPadrao = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.LEIAUTEINVALIDO).Count(),
						Duplicados = celularesInvalidos.Where(j => j.TipoInvalido == TiposInvalidosEnums.DUPLICADO).Count()
					};

					await sessionrepository.Add(new SessionDataModel[] {
							new SessionDataModel(GuidUser) {
								Key = camp.Arquivo,
								Data =DateTime.Now,
								Value = camp.CampanhasLista.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{k.DataEnviar};{k.Operadora};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							},
							new SessionDataModel(GuidUser) {
								Key = $"invalidos_{camp.ID}",
								Data =DateTime.Now,
								Value = camp.CampanhaInvalida.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							}
						}, ClienteID, UsuarioID);

					await new SessionDataRepository().RemoveByKey(new SessionDataModel[] { new SessionDataModel(GuidUser) { Key = $"foradopadrao_{regPadraoArquivo.Replace(camp.Arquivo, string.Empty)}" } }, ClienteID, UsuarioID);

					camp.CampanhasLista.Clear();
					camp.CampanhaInvalida.Clear();
				}

				Campanhas.Clear();
				_bDTO.Result = new CampanhaResultModel[] { camp }.Select(a => CampanhaResultObj(a));
				_bDTO.End = DateTime.Now;
				_bDTO.Itens = 1;
				res = Ok(_bDTO);
			}
			catch (Exception err)
			{

				_bDTO.End = DateTime.Now;
				_bDTO.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_bDTO);
			}

			return res;


		}

		[HttpPost("monitoria/lotes/{carteiraid:int}/{arquivoid:int}")]
		public async Task<IActionResult> RetornaLotes(int carteiraid, int arquivoid, [FromBody]MonitoriaModel m)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CampanhaGridLotesModel>>() { Start = DateTime.Now };

			try
			{
				var dados = await new CampanhaRepository().RetornaLotes(m, carteiraid, arquivoid, ClienteID, UsuarioID);
				b.Result = new CampanhaGridLotesModel[] { };


				if (dados.Any())
					b.Result = dados;

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpGet("mudaleiaute/{id:int}/{leiauteid:int}")]
		public async Task<IActionResult> MudaLayout(int id, int leiauteid)
		{
			var campanhaResult = await CampsResults(id);
			var camps = campanhaResult.ElementAtOrDefault(0);

			return await MontaCampanhaUploaded(new byte[] { },
				false,
				null,
				camps.Arquivo,
				leiauteid,
				await CampanhaList(camps.Arquivo),
				await CellsInvalidos(id),
				camps,
				await new LeiauteRepository().FindById(new LeiauteModel() { LeiauteID = leiauteid, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID), id);
		}

		[HttpGet("get/invalido/{id:int}/{tipo:int}")]
		public async Task<IActionResult> DownInvalido(int id, int tipo)
		{
			try
			{

				var _tipo = ((TiposInvalidosEnums)Enum.Parse(typeof(TiposInvalidosEnums), tipo.ToString()));

				List<CampanhaModel> campsInvalidos = new List<CampanhaModel>() { };

				if (_tipo == TiposInvalidosEnums.LEIAUTEINVALIDO)
					campsInvalidos = await CellsInvalidos(id, TiposInvalidosEnums.LEIAUTEINVALIDO);
				else
					campsInvalidos = await CellsInvalidos(id);

				var dados = campsInvalidos.Where(k => k.TipoInvalido == _tipo).SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray();

				if (_tipo == TiposInvalidosEnums.LEIAUTEINVALIDO)
					dados = campsInvalidos.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{ArrayForaPadrao(k.ForadoPadrao)}\r\n")).ToArray();

				using (var _mem = new MemoryStream(dados))
				{
					Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
					Response.Headers.Add("Content-Disposition", $"attachment;filename={_tipo}.zip");
					return File(await _mem.ToZip($"{_tipo}.csv"), "application/x-zip-compressed");
				}
			}
			catch (Exception err)
			{
				return BadRequest((err.InnerException ?? err).Message);
			}
		}

		[HttpPost("invalidos/down/{tipo:int}")]
		[NivelPermissao(1, PaginaID = 136, SubPaginaID = 0)]
		public async Task<IActionResult> DownloadInvalidosReport([FromBody] ConsolidadoModel c, int tipo)
		{
			try
			{
				if (c.Arquivo == "ENVIO SIMPLES")
					c.Arquivo = null;


				var dados = await new CampanhaRepository().DownloadCelularesInvalidos(c, ClienteID, UsuarioID, (byte)tipo);

				if (!dados.Any())
					return NoContent();

				var sb = new StringBuilder();
				await dados.ToObservable().ForEachAsync(a =>
					sb.AppendFormat("{0};{1};{2};{3};{4}\r\n", a.Celular, a.IDCliente, a.Texto, a.DataEnviar, a.Carteira.Carteira)
				);

				sb.Insert(0, "CELULAR;IDCLIENTE;TEXTO;DATAENVIAR;CARTEIRA\r\n");

				using (var _mem = new MemoryStream(Util.EncoderDefaultFiles.GetBytes(sb.ToString())))
				{
					string arquivo = $"{((TiposInvalidosEnums)Enum.Parse(typeof(TiposInvalidosEnums), tipo.ToString())).ToString()}_{Path.GetFileNameWithoutExtension(c.Arquivo)}";

					Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
					Response.Headers.Add("Content-Disposition", $"attachment;filename={arquivo}.zip");
					return File(await _mem.ToZip($"{arquivo}.csv"), "application/x-zip-compressed");
				}
			}
			catch (Exception err)
			{
				return BadRequest((err.InnerException ?? err).Message);
			}
		}

		[HttpPost("montamensagem/{id:int}")]
		public async Task<IActionResult> MontaMensagem(int id, [FromBody] CampanhaResultModel c)
		{
			var _bDTO = new BaseEntityDTO<CampanhaResultModel>() { Start = DateTime.Now };
			IActionResult res = null;
			CampanhaResultModel camps = new CampanhaResultModel();

			try
			{
				var campanhaResult = await CampsResults(id);
				camps = campanhaResult.ElementAtOrDefault(0);
				camps.CampanhasLista = await CampanhaList(camps.Arquivo);
				camps.CampanhaInvalida = await CellsInvalidos(id);

				if (camps.Variaveis.Any())
				{

					int quantVariaveis = camps.Variaveis.Count();
					var variaveis = camps.Variaveis.Aggregate((a, b) => $"{a},{b}");


					foreach (var item in camps.Variaveis)
					{
						if (!c.Mensagem.Contains(item))
							throw new Exception($"As variáveis constantes na mensagem, não combinam com as variáveis do layout {variaveis}");
					}

					if (camps.IsMsgEmpty)
					{
						string mensagem = c.Mensagem;
						await camps.CampanhasLista.ToObservable().ForEachAsync(k =>
						{

							foreach (var _k in k.Variaveis)
								mensagem = mensagem.Replace(_k.Variavel, _k.Valor);
							k.Texto = mensagem.NoAcento().ToAlphabetGSM();
							if (k.Texto.Length > 160)
							{
								k.TipoInvalido = TiposInvalidosEnums.ACIMA160CARACTERES;
								camps.CampanhaInvalida.Add(k);
							}
						});
					}
				}
				else
				{
					string mensagem = c.Mensagem;
					await camps.CampanhasLista.ToObservable().ForEachAsync(k =>
					{
						k.Texto = c.Mensagem.NoAcento().ToAlphabetGSM();

						if (k.Texto.Length > 160)
						{
							k.TipoInvalido = TiposInvalidosEnums.ACIMA160CARACTERES;
							camps.CampanhaInvalida.Add(k);
						}
					});
				}



				if (camps.CampanhaInvalida.Any(a => a.TipoInvalido == TiposInvalidosEnums.ACIMA160CARACTERES))
					camps.CampanhasLista = camps.CampanhasLista.Where(a => a.Texto.Length <= 160).ToList();


				if (!camps.CampanhasLista.Any())
					throw new Exception("Campanha vazia após a validação de mensagens com mais de 160 caracteres");



				camps.Campanhas = camps.CampanhasLista.Take(10).Select(a => new CampanhaListagemResult() { Celular = a.Celular, Texto = a.Texto }).ToList();


				await sessionrepository.Update(new SessionDataModel[] {
							new SessionDataModel(GuidUser) {
								Key = $"invalidos_{id}",
								Value = camps.CampanhaInvalida.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							},
							new SessionDataModel(GuidUser) {
								Key = camps.Arquivo,
								Value = camps.CampanhasLista.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{k.DataEnviar};{k.Operadora};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							}
						}, ClienteID, UsuarioID);

				camps.CampanhasLista.Clear();
				camps.CampanhaInvalida.Clear();

				await sessionrepository.Update(new SessionDataModel[] {
							new SessionDataModel(GuidUser) {
								Key = id.ToString(),
								Value = Util.EncoderDefaultFiles.GetBytes(JsonConvert.SerializeObject(CampanhaResultObj(camps)))
							}
						}, ClienteID, UsuarioID);



				_bDTO.Result = CampanhaResultObj(camps);
				_bDTO.End = DateTime.Now;
				_bDTO.Itens = 1;
				res = Ok(_bDTO);
			}
			catch (Exception err)
			{
				camps.MensagemInvalida = true;
				_bDTO.Result = CampanhaResultObj(camps);
				_bDTO.End = DateTime.Now;
				_bDTO.Error = (err.InnerException ?? err).Message;
				//_bDTO.Observacao = new { Tipo = "mensageminvalida", Erro = (err.InnerException ?? err).Message };
				res = Ok(_bDTO);
			}
			return res;
		}

		async Task<List<CampanhaModel>> CellsInvalidos(int id, TiposInvalidosEnums? tipo = TiposInvalidosEnums.VALIDO)
		{
			var camps = new List<CampanhaModel>() { };

			string invalido = $"invalidos_{id}";

			if (tipo.HasValue)
				if (tipo.Value == TiposInvalidosEnums.LEIAUTEINVALIDO)
					invalido = $"foradopadrao_{id}";

			var retorno = await sessionrepository.FindById(new SessionDataModel(GuidUser) { Key = invalido }, UsuarioID);

			if (retorno == null)
				return camps;

			using (var mem = new MemoryStream(retorno.Value))
			{
				using (var streamReader = new StreamReader(mem, Util.EncoderDefaultFiles, true))
				{
					string linha = null;


					if (tipo.Value != TiposInvalidosEnums.LEIAUTEINVALIDO)
					{
						while (streamReader.Peek() >= 0)
						{
							linha = await streamReader.ReadLineAsync();
							string[] linhas = linha.Split(";".ToCharArray());


							camps.Add(new CampanhaModel()
							{
								Arquivo = new ArquivoCampanhaModel(linhas.ElementAt(3)),
								Celular = decimal.Parse(linhas.ElementAt(1)),
								Texto = linhas.ElementAt(2),
								IDCliente = linhas.ElementAt(0),
								TipoInvalido = ((TiposInvalidosEnums)Enum.Parse(typeof(TiposInvalidosEnums), linhas.ElementAt(4))),
								ArquivoZip = linhas.ElementAt(5),
								Variaveis = !string.IsNullOrEmpty(linhas.ElementAtOrDefault(6)) && linha.Length > 7 ? Enumerable.Range(6, linhas.Count() - 6).Select(l => linhas.ElementAt(l)).Select(k => new VariavelModel(int.Parse(k.Split("|".ToCharArray())[0]), k.Split("|".ToCharArray())[1], k.Split("|".ToCharArray())[2])) : new VariavelModel[] { }
							});
						}
					}
					else if (tipo == TiposInvalidosEnums.LEIAUTEINVALIDO)
					{
						while (streamReader.Peek() >= 0)
						{
							linha = await streamReader.ReadLineAsync();
							string[] linhas = linha.Split(";".ToCharArray());
							camps.Add(new CampanhaModel() { ForadoPadrao = linha.Split(";".ToCharArray()) });
						}
					}
				}
			}

			return camps;
		}

		async Task<List<CampanhaModel>> CampanhaList(string arquivo, bool? ispadrao = false)
		{

			var camps = new List<CampanhaModel>() { };

			var retorno = await sessionrepository.FindById(new SessionDataModel(GuidUser) { Key = arquivo }, UsuarioID);

			if (retorno == null)
				return camps;

			using (var mem = new MemoryStream(retorno.Value))
			{
				using (var streamReader = new StreamReader(mem, Util.EncoderDefaultFiles, true))
				{
					string linha = null;
					string[] linhas = null;
					while (streamReader.Peek() >= 0)
					{
						linha = await streamReader.ReadLineAsync();
						linhas = linha.Split(";".ToCharArray());

						camps.Add(new CampanhaModel()
						{
							Arquivo = new ArquivoCampanhaModel(linhas.ElementAt(3)),
							Celular = decimal.Parse(linhas.ElementAt(1)),
							Texto = linhas.ElementAt(2),
							IDCliente = linhas.ElementAt(0),
							DataEnviar = DateTime.Parse(linhas.ElementAt(4)),
							Operadora = ((OperadorasEnums)Enum.Parse(typeof(OperadorasEnums), linhas.ElementAt(5))),
							ArquivoZip = linhas.ElementAt(6),
							Variaveis = !string.IsNullOrEmpty(linhas.ElementAt(7)) ? Enumerable.Range(7, linhas.Count() - 7).Select(l => linhas.ElementAt(l)).Select(k => new VariavelModel(int.Parse(k.Split("|".ToCharArray())[0]), k.Split("|".ToCharArray())[1], k.Split("|".ToCharArray())[2])).ToList() : new List<VariavelModel>() { }
						});
					}
				}
			}

			return camps;
		}


		//public Task<IActionResult> MontaMensagem(string msg)
		/// <summary>
		/// extrai a quantidade de consumo de sms das campanhas em execução
		/// </summary>
		/// <param name="carteiraid"></param>
		/// <returns></returns>
		async Task<int> QuantidadeCampanhasCarteiras(int carteiraidatual, int idarquivo)
		{
			var camps = new List<CampanhaModel>() { };
			var dados = await new SessionDataRepository().ObterTodosAsyncByNumericKey(new SessionDataModel(GuidUser));
			CampanhaResultModel campanhaResultModel;
			foreach (var item in dados)
			{
				campanhaResultModel = await item.Value.ByteArrayToOject<CampanhaResultModel>();
				if (campanhaResultModel.Carteira != null)
					if (campanhaResultModel.Carteira.CarteiraID == carteiraidatual)
						camps.AddRange(await CampanhaList(campanhaResultModel.Arquivo));
			}

			return camps.Count;
		}

		async Task<IEnumerable<CampanhaResultModel>> CampsResults(int? id = null)
		{
			var camps = new List<CampanhaResultModel>() { };

			if (id.HasValue)
			{
				var dados = await sessionrepository.FindById(new SessionDataModel(GuidUser) { Key = id.ToString() }, UsuarioID);
				var item = await dados.Value.ByteArrayToOject<CampanhaResultModel>();
				item.CodigoSession = dados.Codigo;
				camps.Add(item);
			}
			else
			{

				var dados = await new SessionDataRepository().ObterTodosAsyncByNumericKey(new SessionDataModel(GuidUser));
				foreach (var item in dados)
				{
					var _item = await item.Value.ByteArrayToOject<CampanhaResultModel>();
					_item.CodigoSession = item.Codigo;
					camps.Add(_item);
				}
			}
			return camps;
		}
		//}
		/// <summary>
		/// Faz a vinculação da carteira a lista atual de envios
		/// </summary>
		/// <param name="id">id da carteira</param>
		/// <param name="file">nome do arquivo</param>
		/// <returns></returns>
		[HttpGet("set/carteira/{id:int}/id/{idfile:int}")]
		[NivelPermissao(1, PaginaID = 126, SubPaginaID = 87)]
		public async Task<IActionResult> GravaArquivo(int id, int idfile)
		{
			var _camps = await CampsResults(idfile);

			if (!_camps.Any())
				throw new Exception("Não encontrado");

			var _bDTO = new BaseEntityDTO<CampanhaResultModel>() { Start = DateTime.Now };
			var camps = _camps.ElementAt(0);

			IActionResult res = null;
			try
			{

				var carteira = await new CarteiraRepository()
													   .FindById(new CarteiraModel()
													   {
														   Cliente = new ClienteModel() { ClienteID = ClienteID },
														   CarteiraID = id
													   }, UsuarioID);

				if (!carteira.Visivel)
					throw new Exception($"Carteira {carteira.Carteira} não autorizada");

				if (carteira.BloqueioEnvio)
					throw new Exception($"Carteira {carteira.Carteira} com bloqueio de envio");

				camps.Gestores = await new GestorRepository().GestorByCarteira(id, ClienteID);
				camps.Carteira = carteira;
				camps.CampanhasLista = await CampanhaList(camps.Arquivo);
				camps.CampanhaInvalida = await CellsInvalidos(idfile);


				if (camps.Carteira.Limite.HasValue)
				{
					int quantidadeTotalCarteira = await QuantidadeCampanhasCarteiras(id, idfile);
					if (camps.CampanhasLista.Count == quantidadeTotalCarteira) quantidadeTotalCarteira = 0;

					camps.Carteira.QuantidadeDisponivel = (camps.Carteira.Limite.Value - camps.Carteira.ConsumoPeriodo) - quantidadeTotalCarteira;

					if (camps.Carteira.QuantidadeDisponivel < 0)
					{
						throw new Exception($"Carteira {camps.Carteira} com limite ultrapassado: {camps.Carteira.Limite.Value.ToString("N0")}, disponível: {(camps.Carteira.Limite.Value - camps.Carteira.ConsumoPeriodo).ToString("N0")}");
					}
					else if (camps.Carteira.QuantidadeDisponivel < camps.CampanhasLista.Count + quantidadeTotalCarteira)//se o total disponível é menor do que a quantidade de sms a ser enviada e já postada pendente aguardando envio
					{
						throw new Exception($"Quantidade disponível de {camps.Carteira.QuantidadeDisponivel.ToString("N0")} da carteira {camps.Carteira.Carteira} é menor do que a quantidade de campanhas: {(camps.CampanhasLista.Count + quantidadeTotalCarteira).ToString("N0")}");
					}
				}

				#region HIGIENIZADO
				if (camps.Carteira.Higieniza)
				{
					var dados = new List<CampanhaModel>() { };
					var higienizados = (await new CampanhaRepository().HigienizaCarteira(camps.CampanhasLista, camps.Carteira.CarteiraID.Value, camps.Carteira.DiasHigienizacao.Value, ClienteID, UsuarioID)).ToList();

					if (higienizados.Any())
					{
						dados = (from a in camps.CampanhasLista
								 join _b in higienizados on a.Celular equals _b.Celular into ps
								 from _b in ps.DefaultIfEmpty()
								 where _b != null
								 select new CampanhaModel()
								 {
									 IDCliente = a.IDCliente,
									 Celular = _b.Celular,
									 Texto = a.Texto,
									 Arquivo = a.Arquivo,
									 ArquivoZip = a.ArquivoZip,
									 Variaveis = a.Variaveis,
									 TipoInvalido = TiposInvalidosEnums.HIGIENIZADO
								 }).ToList();
					}


					//checando em outras postagens

					var campanhaResult = await CampsResults(idfile);
					var campanhasOutrasPostagens = new List<CampanhaModel>() { };

					var resultCamp = campanhaResult.Where(a => a.Carteira != null);

					if (resultCamp.Any())
					{
						var postagemCarteiras = resultCamp.Where(a => a.Carteira.CarteiraID == id);

						if (postagemCarteiras.Any())
						{
							foreach (var item in postagemCarteiras)
								campanhasOutrasPostagens.AddRange(await CampanhaList(item.Arquivo));
						}

						dados.AddRange(campanhasOutrasPostagens.Join(camps.CampanhasLista, a => a.Celular, b => b.Celular, (a, _b) => new CampanhaModel()
						{
							Carteira = a.Carteira,
							TipoCampanha = a.TipoCampanha,
							Celular = _b.Celular,
							IDCliente = a.IDCliente,
							Arquivo = a.Arquivo,
							TipoInvalido = TiposInvalidosEnums.VALIDO,
							Operadora = a.Operadora,
							ArquivoZip = a.ArquivoZip,
							Variaveis = a.Variaveis,
							DataEnviar = a.DataEnviar,
							Texto = a.Texto
						}));

					}


					if (dados.Any())
					{
						camps.CampanhasLista = camps.CampanhasLista.Except(dados,
							new CompareObject<CampanhaModel>(
								Util.CompareItemCampanha(),
							Util.CampanhaHashCode()))
							.ToList();

						if (!camps.CampanhasLista.Any())
						{
							throw new Exception($"Arquivo de ID {idfile} com o nome {camps.Arquivo} sem registros válidos após higienização");
						}


						camps.CampanhaInvalida.AddRange(dados);
						camps.Invalidos.Higienizado = dados.Count();
						camps.RegistrosValidos = camps.RegistrosValidos - dados.Count();
						camps.TotalInvalidos = camps.TotalInvalidos + dados.Count();
					}
					else
					{
						camps.Invalidos.Higienizado = 0;

						if (camps.CampanhaInvalida.Any(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO))
						{
							int _higienizados = camps.CampanhaInvalida.Where(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count();

							camps.TotalInvalidos = camps.TotalInvalidos - _higienizados;
							camps.RegistrosValidos = camps.RegistrosValidos + _higienizados;
							camps.CampanhasLista.AddRange(camps.CampanhaInvalida.Where(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Select(a => new CampanhaModel()
							{
								Carteira = a.Carteira,
								TipoCampanha = a.TipoCampanha,
								Celular = a.Celular,
								IDCliente = a.IDCliente,
								Arquivo = a.Arquivo,
								TipoInvalido = TiposInvalidosEnums.VALIDO,
								Operadora = a.Operadora,
								ArquivoZip = a.ArquivoZip,
								Variaveis = a.Variaveis,
								DataEnviar = a.DataEnviar,
								Texto = a.Texto
							}));

							camps.CampanhaInvalida = camps.CampanhaInvalida.Where(a => a.TipoInvalido != TiposInvalidosEnums.HIGIENIZADO).ToList();
						}
					}
				}
				else
				{
					camps.Invalidos.Higienizado = 0;

					if (camps.CampanhaInvalida.Any(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO))
					{
						int _higienizados = camps.CampanhaInvalida.Where(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Count();

						camps.TotalInvalidos = camps.TotalInvalidos - _higienizados;
						camps.RegistrosValidos = camps.RegistrosValidos + _higienizados;
						camps.CampanhasLista.AddRange(camps.CampanhaInvalida.Where(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO).Select(a => new CampanhaModel()
						{
							Carteira = a.Carteira,
							TipoCampanha = a.TipoCampanha,
							Celular = a.Celular,
							IDCliente = a.IDCliente,
							Arquivo = a.Arquivo,
							TipoInvalido = TiposInvalidosEnums.VALIDO,
							Operadora = a.Operadora,
							ArquivoZip = a.ArquivoZip,
							Variaveis = a.Variaveis,
							DataEnviar = a.DataEnviar,
							Texto = a.Texto
						}));

						camps.CampanhaInvalida = camps.CampanhaInvalida.Where(a => a.TipoInvalido != TiposInvalidosEnums.HIGIENIZADO).ToList();
					}
				}

				#endregion

				var invalidos = camps.CampanhaInvalida.ToList();
				var retorno = CampanhaResultObj(camps);

				await sessionrepository.Update(new SessionDataModel[] {
							new SessionDataModel(GuidUser) {
								Key = $"invalidos_{idfile}",
								Value =invalidos.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{(byte)k.TipoInvalido};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							},
							new SessionDataModel(GuidUser) {
								Key = camps.Arquivo,
								Value = camps.CampanhasLista.SelectMany(k => Util.EncoderDefaultFiles.GetBytes($"{k.IDCliente};{k.Celular};{k.Texto};{k.Arquivo.Arquivo};{k.DataEnviar};{k.Operadora};{k.ArquivoZip};{MontaVariaveis(k.Variaveis)}\r\n")).ToArray()
							}

						}, ClienteID, UsuarioID);

				camps.CampanhaInvalida.Clear();
				camps.CampanhasLista.Clear();

				await sessionrepository.Update(new SessionDataModel[] { new SessionDataModel(GuidUser) {
								Key = idfile.ToString(),
								Value = Util.EncoderDefaultFiles.GetBytes(JsonConvert.SerializeObject(retorno))
							}}, ClienteID, UsuarioID);

				_bDTO.Result = retorno;
				_bDTO.End = DateTime.Now;
				_bDTO.Itens = 1;
				res = Ok(_bDTO);
			}
			catch (Exception err)
			{
				_bDTO.End = DateTime.Now;
				_bDTO.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_bDTO);
			}
			return res;
		}

		CampanhaResultModel CampanhaResultObj(CampanhaResultModel a)
		{
			return new CampanhaResultModel()
			{
				Gestores = a.Gestores,
				MensagemInvalida = a.MensagemInvalida,
				ArquivoForaPadrao = a.ArquivoForaPadrao,
				Variaveis = a.Variaveis,
				Situacao = a.Situacao,
				Arquivo = a.Arquivo,
				Campanhas = a.Campanhas,
				Carteira = a.Carteira,
				Arquivos = a.Arquivos,
				DataEnviar = a.DataEnviar,
				Intervalo = a.Intervalo,
				IsMsgEmpty = a.IsMsgEmpty,
				Invalidos = a.Invalidos,
				Mensagem = a.Mensagem,
				TipoCampanha = a.TipoCampanha,
				Registros = a.Registros,
				RegistrosValidos = a.RegistrosValidos,
				ID = a.ID,
				TotalInvalidos = a.TotalInvalidos,
				Fornecedor = a.Fornecedor,
				GridCampanhasLote = a.GridCampanhasLote,
				Leiaute = a.Leiaute,
				PermitirLoteAtrasado = a.PermitirLoteAtrasado
			};
		}

		[HttpGet("set/remove/{id:int}")]
		public async Task<IActionResult> RemoveArquivo(int id)
		{
			var camps = new List<CampanhaResultModel>(await CampsResults());

			if (!camps.Where(a => a.ID == id).Any())
				throw new Exception($"ID {id} não localizado");

			var camp = camps.Where(a => a.ID == id).ElementAt(0);

			await Util.DeleteFileAamazon(camp.Arquivo, $"moneoup/{ClienteID}/");

			await new SessionDataRepository().RemoveByKey(new SessionDataModel[] {
				new SessionDataModel(GuidUser) { Key = id.ToString() },
				new SessionDataModel(GuidUser) { Key = camp.Arquivo },
				new SessionDataModel(GuidUser) { Key = $"invalidos_{id}" }
			}, ClienteID, UsuarioID);

			camps.Remove(camp);

			await new CampanhaRepository().ExcludeFileCards(GuidUser, id);
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CampanhaResultModel>>() { Start = DateTime.Now, Itens = camps.Count };

			try
			{
				b.Result = camps.Select(a => CampanhaResultObj(a));

				if (!b.Result.Any())
					return NoContent();

				GC.Collect();
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;

		}

		[HttpPost("set/montalote/id/{id:int}")]
		[NivelPermissao(1, PaginaID = 126, SubPaginaID = 87)]
		public async Task<IActionResult> MontarLote([FromBody]CampanhaGridLotesModel c, int id)
		{
			var dados = await CampsResults(id);

			CampanhaResultModel camps = new CampanhaResultModel();

			if (!dados.Any())
				throw new Exception($"ID {id} não localizado");

			camps = dados.ElementAt(0);

			var b = new BaseEntityDTO<CampanhaResultModel>() { Start = DateTime.Now };
			var dataEnviar = new DateTime();
			var datahora = string.Format("{0} {1}", c.Data, c.Hora);

			if (DateTime.TryParse(datahora, out dataEnviar))
				c.DataEnviar = dataEnviar;
			else
			{
				b.End = DateTime.Now;
				b.Error = $"Data/Hora em formato inválido pra conversão: {datahora}";
				return BadRequest(b);
			}


			if (dataEnviar.Date < DateTime.Now.Date)
			{
				b.End = DateTime.Now;
				b.Error = $"O dia da campanha não pode ser menor do que o dia atual: {dataEnviar}";
				return BadRequest(b);
			}


			if (camps.PermitirLoteAtrasado)
			{
				if (dataEnviar < DateTime.Now.AddSeconds(-4))
				{
					b.End = DateTime.Now;
					b.Error = $"A hora do envio {dataEnviar} não pode ser menor o do que a hora atual: {DateTime.Now}";
					return BadRequest(b);
				}
			}

			if (camps.GridCampanhasLote.Count > 0)
				camps.GridCampanhasLote.Clear();

			//carregando as campanhas na campanhalist originada da session

			camps.CampanhasLista = await CampanhaList(camps.Arquivo);



			camps.GridCampanhasLote.AddRange(camps
									.CampanhasLista
									.TakeGroup(c.Lotes,
									c.DataEnviar,
									c.Intervalos));


			if (camps.GridCampanhasLote.Any())
				camps.Situacao = "PRONTO";

			var campanhas = camps.CampanhasLista;
			camps.DataEnviar = c.DataEnviar;

			var retorno = CampanhaResultObj(camps);

			camps.CampanhasLista.Clear();

			await sessionrepository.Update(new SessionDataModel[] {
							new SessionDataModel(GuidUser) {
								Key = id.ToString(),
								Value = Util.EncoderDefaultFiles.GetBytes(JsonConvert.SerializeObject(retorno))
							}
						},
						ClienteID,
						UsuarioID);



			b.Result = retorno;
			b.End = DateTime.Now;
			b.Itens = 1;
			return Ok(b);
		}



		/// <summary>
		/// Obtém o JSON atual da página de envio
		/// </summary>
		/// <returns>JSON</returns>
		[HttpGet("uploaded/")]
		public async Task<IActionResult> GetUploaded()
		{

			var camps = await CampsResults();

			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CampanhaResultModel>>() { Start = DateTime.Now, Itens = camps.Count() };

			try
			{

				b.Result = camps.Select(a => CampanhaResultObj(a));

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;

		}
		[HttpGet("requisicaocarteira/get")]
		[NivelPermissao(1, PaginaID = 133, SubPaginaID = 97)]
		public async Task<IActionResult> ListarRequisicaoRelatorio()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<CampanhaRequisicaoRelatorioModel>>() { Start = DateTime.Now };

			try
			{
				b.Result = await new CampanhaRepository().ListarRequisicaoRelatorio(ClienteID, UsuarioID);

				if (b.Result == null || !b.Result.Any())
					return NoContent();

				b.Itens = b.Result.Count();
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpPost("detalhado/")]
		public async Task<IActionResult> RetornarDetalhado([FromBody] CampanhaModel c)
		{
			c.Cliente = new ClienteModel() { ClienteID = ClienteID };


			var dados = await new CampanhaRepository().DetalhadoGenerico(c);

			return Ok();
		}

		[HttpPost("requisicaocarteira/add")]
		[NivelPermissao(1, PaginaID = 133, SubPaginaID = 97)]
		public async Task<IActionResult> AdicionarRequisicao([FromBody]CampanhaRequisicaoRelatorioModel m)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now, Itens = 1 };

			try
			{
				if (m.Emails == null || !m.Emails.Any())
					throw new Exception("Sem e-mails na requisição");

				m.Emails = m.Emails.Where(a => Util.RegexEmail.IsMatch(a));

				if (!m.Emails.Any())
					throw new Exception("Não há e-mail válido");

				m.Arquivo = $"{Guid.NewGuid()}.zip";
				await new CampanhaRepository().CadastraRequsicao(m, ClienteID, UsuarioID);

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}
		[AllowAnonymous]
		[HttpGet("r/d/{arquivo}")]
		public async Task<IActionResult> RequisicaoDetalhadoDown(string arquivo)
		{
			try
			{

				var clienteid = ClienteID == 0 ? await new CampanhaRepository().GetClienteIDByArquivo(arquivo) : ClienteID;

				if (clienteid > 0)
				{
					var stream = await Util.DownloadFileS3("moneoup", arquivo, clienteid);

					using (var mem = new MemoryStream())
					{
						await stream.CopyToAsync(mem);
						Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
						Response.Headers.Add("Content-Disposition", $"attachment;filename={arquivo}");
						mem.Position = 0L;
						return File(mem.ToArray(), "application/x-zip-compressed");
					}
				}
				return Ok();
			}
			catch (Exception erro)
			{
				return BadRequest($"Houve um erro no download do arquivo {arquivo}");
			}
		}

		//[HttpPost("detalhado/")]
		//public async Task<IActionResult> Detalhado([FromBody]CampanhaRequisicaoRelatorioModel m)
		//{
		//	IActionResult res = null;
		//	var b = new BaseEntityDTO<IEnumerable<CampanhaModel>>() { Start = DateTime.Now, Itens = 1 };

		//	try
		//	{

		//		m.Arquivo = $"{Guid.NewGuid()}.zip";
		//		await new CampanhaRepository().RelatorioDetalhado(m, ClienteID, UsuarioID);
		//		b.End = DateTime.Now;
		//		res = Ok(b);
		//	}
		//	catch (Exception err)
		//	{
		//		b.End = DateTime.Now;
		//		b.Error = (err.InnerException ?? err).Message;
		//		res = BadRequest(b);
		//	}
		//	return res;
		//}

		//campanha/updateenviada
		[HttpPost("updateenviada/")]
		public async Task<IActionResult> AtualizaItemStatusReport([FromBody] IEnumerable<CampanhaModel> t)
		{

			IActionResult res = null;
			var b = new BaseEntityDTO<CampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				await new CampanhaRepository().UpdateStatusReport(t);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		//campanha/updatestatusreport
		[HttpPost("updatestatusreport/")]
		public async Task<IActionResult> AtualizaItemCampanhaEnviada([FromBody] IEnumerable<CampanhaModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<CampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				await new CampanhaRepository().UpdateCampanhaEnviada(t);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		//campanha/add
		[HttpPut("add/")]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<CampanhaModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<CampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };
			var clienteID = 1;
			try
			{

				await repository.Add(t, clienteID, null);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		//campanha/update
		[HttpPost("update/")]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<CampanhaModel> t)
		{

			IActionResult res = null;
			var b = new BaseEntityDTO<CampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };
			var clienteID = 1;

			try
			{
				await repository.Update(t, clienteID, null);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		//campanha/delete
		[HttpDelete("delete/")]
		public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<CampanhaModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<CampanhaModel>() { Start = DateTime.Now, Itens = t.Count() };
			var clienteID = 1;

			try
			{
				await repository.Remove(t, clienteID, null);
				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}


		[HttpGet("checageminvalidos/")]
		public async Task<IActionResult> AtualizaInvalidos()
		{
			//IActionResult res = null;
			//var b = new BaseEntityDTO<IEnumerable<CampanhaResultModel>>() { Start = DateTime.Now };

			//try
			//{

			//	var campanhaResult = (await CampsResults()).ToList();
			//	Campanhas = new HashSet<CampanhaModel>() { };
			//	var celularesInvalidos = new List<CampanhaModel>() { };


			//	foreach (var item in campanhaResult.Where(a => !a.ArquivoForaPadrao))
			//	{
			//		var c = await CampanhaList(item.Arquivo);

			//		c.AsParallel().ForAll(a =>
			//		{
			//			lock (_locker)
			//				if (!Campanhas.Add(a))
			//					celularesInvalidos.Add(a);
			//		});
			//	}

			//	if (celularesInvalidos.Any())
			//	{
			//		foreach (var arquivo in celularesInvalidos.GroupBy(a => a.Arquivo.Arquivo, (a, k) => a))
			//		{
			//			var camps = campanhaResult.Where(a => a.Arquivo == arquivo);

			//			if (camps.Any())
			//			{
			//				var camp = camps.ElementAt(0);

			//				await Util.DeleteFileAamazon(camp.Arquivo, $"moneoup/{ClienteID}/");

			//				await new SessionDataRepository().RemoveByKey(new SessionDataModel[] {
			//												new SessionDataModel(GuidUser) { Key = camp.ID.ToString() },
			//												new SessionDataModel(GuidUser) { Key = camp.Arquivo },
			//												new SessionDataModel(GuidUser) { Key = $"invalidos_{camp.ID}" }
			//											}, ClienteID, UsuarioID);

			//				await new CampanhaRepository().ExcludeFileCards(GuidUser, camp.ID);

			//				campanhaResult.Remove(camp);
			//			}
			//		}
			//	}

			//	b.Result = campanhaResult.Select(a => CampanhaResultObj(a));
			//	b.Itens = campanhaResult.Count;
			//	b.End = DateTime.Now;
			//	res = Ok(b);
			//}
			//catch (Exception err)
			//{
			//	b.End = DateTime.Now;
			//	b.Error = (err.InnerException ?? err).Message;
			//	res = BadRequest(b);
			//}

			//return res;

			return Ok();

		}

		[HttpPut("enviosimples/")]
		[NivelPermissao(1, PaginaID = 128, SubPaginaID = 0)]
		public async Task<IActionResult> EnvioSimples([FromBody] CampanhaModel c)
		{
			IActionResult res = null;
			var _b = new BaseEntityDTO<string>() { Start = DateTime.Now };


			try
			{

				if (!c.Celulares.Any())
					throw new Exception("Lista vazia de celulares");

				if (c.Carteira == null)
					throw new Exception("Carteira não informada");

				if (!c.Carteira.CarteiraID.HasValue)
					throw new Exception("Carteira não informada");


				Campanhas = new HashSet<CampanhaModel>() { };

				//separando os inválidos
				var celularesInvalidos = new List<CampanhaModel>() { };

				c.TipoInvalido = TiposInvalidosEnums.VALIDO;
				c.DataEnviar = DateTime.Now;

				if (c.Texto.Length > 160)
					throw new Exception("Texto acima de 160 caracteres");

				c.Texto = c.Texto.NoAcento().ToAlphabetGSM();
				c.DataEnviar = DateTime.Now;


				foreach (var item in c.Celulares.Distinct())
				{
					Campanhas.Add(new CampanhaModel()
					{
						Carteira = c.Carteira,
						Texto = c.Texto,
						IDCliente = c.IDCliente,
						DataEnviar = c.DataEnviar,
						Celular = item,
						TipoInvalido = TiposInvalidosEnums.VALIDO,
					});
				}



				if (!Campanhas.Any())
					throw new Exception("Lista de campanha deve ter ao menos uma campanha");






				Campanhas = Campanhas.AsParallel().NonoDigito(await Util.CacheFactory<IEnumerable<PrefixoModel>>(_cache, "nextel", _hostingEnvironment)).Item1.ToHashSet();


				#region CELULARES_INVALIDOS
				var prefixos = await Util.CacheFactory<IEnumerable<PrefixoModel>>(_cache, "prefixos", _hostingEnvironment);

				//adicionando inválidos do tipo CELULARINVALIDO
				celularesInvalidos.AddRange((from a in Campanhas
											 join b in prefixos.ToList() on a.Celular.ToPrefixo() equals b.Prefixo into ps
											 from b in ps.DefaultIfEmpty()
											 where b == null
											 select new CampanhaModel()
											 {
												 Carteira = a.Carteira,
												 Texto = a.Texto,
												 IDCliente = a.IDCliente,
												 DataEnviar = a.DataEnviar,
												 Celular = a.Celular,
												 TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO,
											 }));



				if (celularesInvalidos.Any(a => a.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO))
					Campanhas = Campanhas.Except(celularesInvalidos.Where(a => a.TipoInvalido == TiposInvalidosEnums.CELULARINVALIDO), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();

				if (!Campanhas.Any())
					throw new Exception("Lista com celulares inválidos");

				#endregion

				#region BLACKLIST

				var blacklist = await new BlacklistRepository().GetAll(new BlackListModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, null);

				if (blacklist != null || blacklist.Any())
					celularesInvalidos.AddRange(from a in Campanhas
												join b in blacklist on a.Celular equals b.Celular into ps
												from b in ps.DefaultIfEmpty()
												where b != null
												select new CampanhaModel()
												{
													Carteira = a.Carteira,
													IDCliente = a.IDCliente,
													Texto = a.Texto,
													Celular = a.Celular,
													DataEnviar = a.DataEnviar,
													TipoInvalido = TiposInvalidosEnums.BLACKLIST,
												});

				if (celularesInvalidos.Any(a => a.TipoInvalido == TiposInvalidosEnums.BLACKLIST))
					Campanhas = Campanhas.Except(celularesInvalidos.Where(a => a.TipoInvalido == TiposInvalidosEnums.BLACKLIST), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();


				if (!Campanhas.Any())
					throw new Exception("Lista com registros na tabela de Blacklist");
				#endregion

				var carteira = await new CarteiraRepository()
														   .FindById(new CarteiraModel()
														   {
															   Cliente = new ClienteModel() { ClienteID = ClienteID },
															   CarteiraID = c.Carteira.CarteiraID
														   }, UsuarioID);

				if (!carteira.Visivel)
					throw new Exception("Carteira não disponibilzada pra envio");

				if (carteira.BloqueioEnvio)
					throw new Exception($"Carteira com bloqueio de envio");



				if (carteira.Limite.HasValue)
				{
					carteira.QuantidadeDisponivel = (carteira.Limite.Value - carteira.ConsumoPeriodo) - Campanhas.Count;

					if (carteira.QuantidadeDisponivel < 0)
						throw new Exception($"Carteira {carteira.Carteira} com limite ultrapassado: {carteira.Limite.Value.ToString("N0")}, disponível: {(carteira.Limite.Value - carteira.ConsumoPeriodo).ToString("N0")}");
				}

				#region HIGIENIZA
				if (carteira.Higieniza)
				{
					var higienizados = (await new CampanhaRepository().HigienizaCarteira(Campanhas, c.Carteira.CarteiraID.Value, carteira.DiasHigienizacao.Value, ClienteID, UsuarioID)).ToList();

					if (higienizados.Any())
						celularesInvalidos.AddRange(from a in Campanhas
													join b in higienizados on a.Celular equals b.Celular into ps
													from b in ps.DefaultIfEmpty()
													where b != null
													select new CampanhaModel()
													{
														Carteira = a.Carteira,
														IDCliente = a.IDCliente,
														Texto = a.Texto,
														Celular = a.Celular,
														DataEnviar = a.DataEnviar,
														TipoInvalido = TiposInvalidosEnums.HIGIENIZADO
													});


					if (celularesInvalidos.Any(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO))
						Campanhas = Campanhas.Except(celularesInvalidos.Where(a => a.TipoInvalido == TiposInvalidosEnums.HIGIENIZADO), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();

				}
				#endregion


				if (!carteira.Visivel)
					throw new Exception("Campanhas não disponíveis após higienização pela carteira selecionada");

				#region FILTRADO
				var filtrado = await Util.CacheFactory<HashSet<decimal>>(_cache, "quarentena", _hostingEnvironment);

				if (filtrado.Any())
				{
					celularesInvalidos.AddRange(from a in Campanhas.AsParallel()
												join b in filtrado.AsParallel() on a.Celular equals b
												into ps
												from b in ps.DefaultIfEmpty()
												where b != 0
												select new CampanhaModel()
												{
													Carteira = a.Carteira,
													IDCliente = a.IDCliente,
													Texto = a.Texto,
													Celular = a.Celular,
													DataEnviar = a.DataEnviar,
													TipoInvalido = TiposInvalidosEnums.FILTRADO
												});

					if (celularesInvalidos.Any(a => a.TipoInvalido == TiposInvalidosEnums.FILTRADO))
						Campanhas = Campanhas.Except(celularesInvalidos.Where(a => a.TipoInvalido == TiposInvalidosEnums.FILTRADO), new CompareObject<CampanhaModel>(Util.CompareItemCampanha(), Util.CampanhaHashCode())).ToHashSet();
				}
				#endregion

				Campanhas = Campanhas
				.Join(prefixos.ToList(), a => a.Celular.ToPrefixo(), b => b.Prefixo, (a, b) => new CampanhaModel()
				{
					Carteira = a.Carteira,
					IDCliente = a.IDCliente,
					Texto = a.Texto,
					Celular = a.Celular,
					DataEnviar = a.DataEnviar,
					Operadora = b.Operadora
				}).ToHashSet();

				var f = await new FornecedorRepository().FornecedorTelaEnvio(ClienteID);

				var totalCampanhas = Campanhas.Count;

				if (totalCampanhas >= f.Count())
				{
					foreach (var item in f)
						await Campanhas.Where(a => a.Fornecedor == null)
							.Take((int)(totalCampanhas * (item.Distribuicao / 100)))
							.ToObservable()
							.ForEachAsync(a => a.Fornecedor = item);
				}
				else
				{

					int contador = 0;
					foreach (var item in f.OrderByDescending(a => a.Distribuicao))
					{
						if (Campanhas.ElementAtOrDefault(contador) != null)
							Campanhas.ElementAt(contador).Fornecedor = item;

						contador++;
					}
				}





				var fornecedor = f.Where(k => k.Distribuicao == f.Max(l => l.Distribuicao)).ElementAt(0);


				if (UsuarioID.HasValue)
				{
					var saldo = await new UsuarioRepository().SaldoUsuario(ClienteID, UsuarioID.Value);

					if (saldo.Item1 < 0 && !saldo.Item2)
						throw new Exception("Grupo ou Usuário sem saldo pra envio!");
					else if (!saldo.Item2 && saldo.Item1 < Campanhas.Count)
						throw new Exception($"Usuário com saldo atual de {saldo.Item1} sem saldo pra envio de {Campanhas.Count}");
				}


				await new CampanhaRepository().EnviarSMSApi(
						Campanhas.Select(a => new CampanhaModel()
						{
							Celular = a.Celular,
							Data = DateTime.Now,
							Texto = a.Texto,
							Operadora = a.Operadora,
							StatusEnvio = 1,
							IDCliente = a.IDCliente,
							Carteira = a.Carteira,
							Fornecedor = fornecedor,
							DataDia = a.DataEnviar.Date,
							TipoSMS = Tipo.LONGCODE,
							DataEnviar = a.DataEnviar

						}),
						celularesInvalidos.Select(a => new CampanhaModel()
						{
							Cliente = new ClienteModel() { ClienteID = ClienteID },
							Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null,
							Celular = a.Celular,
							Data = DateTime.Now,
							DataEnviar = a.DataEnviar,
							IDCliente = a.IDCliente,
							Texto = a.Texto,
							TipoInvalido = a.TipoInvalido,
							Carteira = a.Carteira
						}),
						ClienteID, UsuarioID);

				_b.Result = $"Envio efetuado com sucesso {Campanhas.Count}";
				_b.Itens = c.Celulares.Count();
				_b.End = DateTime.Now;
				res = Ok(_b);
			}
			catch (Exception err)
			{
				_b.End = DateTime.Now;
				_b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(_b);
			}

			return res;

		}

		[HttpPut("arquivo/enviarsms/")]
		[NivelPermissao(1, PaginaID = 126, SubPaginaID = 87)]
		public async Task<IActionResult> ArquivoEnviarSMS([FromBody]IEnumerable<CampanhaResultModel> c)
		{
			return await EnviarSMS(c);
		}

		[HttpGet("arquivo/set/remove/{id:int}")]
		[NivelPermissao(1, PaginaID = 126, SubPaginaID = 87)]
		public async Task<IActionResult> ArquivoRemoveArquivo(int id)
		{
			return await RemoveArquivo(id);
		}

		[HttpPut("padrao/enviarsms/")]
		[NivelPermissao(1, PaginaID = 127, SubPaginaID = 0)]
		public async Task<IActionResult> PadraoEnviarSMS([FromBody]IEnumerable<CampanhaResultModel> c)
		{
			return await EnviarSMS(c);
		}

		[HttpGet("padrao/set/remove/{id:int}")]
		[NivelPermissao(1, PaginaID = 127, SubPaginaID = 0)]
		public async Task<IActionResult> PadraoRemoveArquivo(int id)
		{
			return await RemoveArquivo(id);
		}


		[HttpPut("enviarsms/")]
		public async Task<IActionResult> EnviarSMS([FromBody]IEnumerable<CampanhaResultModel> c)
		{


			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<dynamic>>() { Start = DateTime.Now };

			var camps = (await CampsResults()).ToList();


			try
			{
				var campanhasFinal = new List<CampanhaModel>() { };
				var campanhasInvalido = new List<CampanhaModel>() { };
				var campanhasForaPadrao = new List<CampanhaModel>() { };

				foreach (var a in c)
				{

					var campanhaResult = camps.Where(l => l.ID == a.ID).ElementAt(0);
					campanhaResult.GridCampanhasLote.Clear();
					campanhaResult.CampanhasLista = await CampanhaList(campanhaResult.Arquivo);
					campanhaResult.TipoCampanha = a.TipoCampanha;





					int lotes = a.GridCampanhasLote.Count;

					if (lotes == 0)
						a.GridCampanhasLote.Add(new CampanhaGridLotesModel() { DataEnviar = a.DataEnviar, Intervalos = 5, Lote = 1, Quantidade = campanhaResult.CampanhasLista.Count });


					var intervalo = a.GridCampanhasLote.ElementAt(0).Intervalos;


					//gravando os cells inválidos
					var invalidos = await CellsInvalidos(a.ID);

					var foraPadrao = await CellsInvalidos(a.ID, TiposInvalidosEnums.LEIAUTEINVALIDO);

					var carteira = campanhaResult.Carteira;

					if (foraPadrao.Any())
						campanhasInvalido.AddRange(foraPadrao.Select(k => new CampanhaModel()
						{
							TipoInvalido = TiposInvalidosEnums.LEIAUTEINVALIDO,
							Arquivo = new ArquivoCampanhaModel() { Arquivo = campanhaResult.Arquivo },
							Carteira = carteira,
							DataDia = a.DataEnviar,
							DataEnviar = a.DataEnviar,
							ForadoPadrao = k.ForadoPadrao,
							TipoCampanha = a.TipoCampanha
						}));


					campanhasInvalido.AddRange(invalidos.Select(m => new CampanhaModel()
					{
						Arquivo = new ArquivoCampanhaModel() { Arquivo = campanhaResult.Arquivo },
						Carteira = campanhaResult.Carteira,
						IDCliente = m.IDCliente,
						Celular = m.Celular,
						Texto = m.Texto,
						Cliente = new ClienteModel() { ClienteID = ClienteID, PosPago = true },
						Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null,
						TipoInvalido = m.TipoInvalido,
						DataEnviar = a.DataEnviar,
						TipoCampanha = a.TipoCampanha
					}));


					if (campanhaResult.CampanhasLista.Any(k => string.IsNullOrEmpty(k.Texto)))
						throw new Exception("o campo mensagem não pode estar vazio");

					if (!a.GridCampanhasLote.Sum(k => k.Quantidade).Equals(campanhaResult.CampanhasLista.Count))
						throw new Exception($"A soma das campanhas atuais {campanhaResult.CampanhasLista.Count} não combina com a soma dos lotes: {a.GridCampanhasLote.Sum(k => k.Quantidade)} para o ID {a.ID} e arquivo: {a.Arquivo}");

					int contador = 0;

					foreach (var item in a.GridCampanhasLote)
						foreach (var _item in campanhaResult.CampanhasLista.Where(k => !k.Atualizado).Take(item.Quantidade))
						{
							_item.DataEnviar = contador == 0 ? item.DataEnviar : item.DataEnviar.AddMinutes(item.Intervalos);
							_item.Atualizado = true;
						}


					int _fornecedor = a.Fornecedor.Count();

					var agrupadoDia = campanhaResult.CampanhasLista.GroupBy(option => option.DataEnviar, (dataenviar, _campanhas) => new
					{
						Data = dataenviar,
						Campanhas = _campanhas
					})
					.OrderBy(k => k.Data);

					foreach (var item in agrupadoDia)
					{
						var totalHora = item.Campanhas.Count();
						a.Fornecedor.ToList().ForEach(m =>
						{
							var _total = Math.Round((decimal)totalHora / 100M * m.Distribuicao, 0);

							foreach (var _item in item.Campanhas
							.Where(k => k.Fornecedor == null)
							.Take((int)_total))
								_item.Fornecedor = new FornecedorModel() { FornecedorID = m.FornecedorID };
						});
					}

					campanhasFinal.AddRange(campanhaResult.CampanhasLista.Select((m, n) => new CampanhaModel()
					{
						Arquivo = m.Arquivo,
						Carteira = a.Carteira,
						Cliente = new ClienteModel() { ClienteID = ClienteID, PosPago = false },
						Celular = m.Celular,
						Texto = m.Texto,
						IDCliente = m.IDCliente,
						Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null,
						DataEnviar = m.DataEnviar,
						TipoCampanha = a.TipoCampanha,
						Data = DateTime.Now,
						DataDia = m.DataEnviar.Date,
						StatusEnvio = m.DataEnviar > DateTime.Now ? 0 : 1,
						TipoSMS = Tipo.LONGCODE,
						Operadora = m.Operadora,
						TipoInvalido = TiposInvalidosEnums.VALIDO,
						ArquivoZip = m.ArquivoZip,
						Fornecedor = m.Fornecedor == null ? a.Fornecedor.ElementAt(n % _fornecedor) : m.Fornecedor
					}));

					//adicionando os telefones de gestores nas campanhas




					if (campanhaResult.Gestores.Any())
					{
						bool campanhasMaiorGestor = campanhaResult.Gestores.Count() < campanhasFinal.Count;

						campanhasFinal.InsertRange(0, campanhaResult.Gestores.SelectMany(m => m.Telefones)
							.Select((m, n) => new CampanhaModel()
							{
								Arquivo = campanhaResult.CampanhasLista.ElementAt(campanhasMaiorGestor ? n : 0).Arquivo,
								Carteira = a.Carteira,
								Cliente = new ClienteModel() { ClienteID = ClienteID, PosPago = false },
								Celular = m,
								Texto = campanhaResult.CampanhasLista.ElementAt(campanhasMaiorGestor ? n : 0).Texto,
								IDCliente = campanhaResult.CampanhasLista.ElementAt(campanhasMaiorGestor ? n : 0).IDCliente,
								Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null,
								DataEnviar = a.DataEnviar,
								TipoCampanha = a.TipoCampanha,
								Data = DateTime.Now,
								DataDia = a.DataEnviar.Date,
								StatusEnvio = a.DataEnviar > DateTime.Now ? 0 : 1,
								TipoSMS = Tipo.LONGCODE,
								Operadora = OperadorasEnums.VIVO,
								TipoInvalido = TiposInvalidosEnums.VALIDO,
								Fornecedor = campanhasFinal.ElementAt(0).Fornecedor,
								ArquivoZip = Path.GetExtension(a.Arquivo).ToLower() == ".zip" ? a.Arquivo : null
							}));
					}

					//montagem dos ratinhos
				}

				b.Itens = campanhasFinal.Count;

				var dados = new List<dynamic>() { };


				await new CampanhaRepository().AdicionaCampanhaAsync(campanhasFinal, campanhasInvalido, ClienteID, UsuarioID);

				foreach (var item in campanhasFinal.GroupBy(k => new { CarteiraID = k.Carteira.CarteiraID, Carteira = k.Carteira.Carteira }, (k, l) => new { CarteiraID = k.CarteiraID, Carteira = k.Carteira, Campanhas = l })) //enviando e-mails por carteira
				{
					var emailsGestores = await new GestorRepository().GestoresEmailEnvio(ClienteID, item.CarteiraID);
					var _emails = emailsGestores.SelectMany(k => k.Emails.Select(j => new EmailViewModel() { Nome = k.Nome, Email = j }));
					var conteudoEmail = Emails.EmailGestoresEnvio(item.Campanhas.Where(k => k.TipoInvalido == TiposInvalidosEnums.VALIDO));
					await Util.SendEmailAsync(_emails, $"Início de Campanha {item.Carteira.ToUpper()}", conteudoEmail, true, TipoEmail.ENVIOSMS);
				}

				await sessionrepository.Remove(new SessionDataModel[] { new SessionDataModel(GuidUser) }, ClienteID, UsuarioID);

				await new CampanhaRepository().ExcludeFileCards(GuidUser);

				IEnumerable<string> arquivosRetorno = new string[] { };

				if (c != null || c.Any())
					foreach (var item in c)
					{
						//var arq = item.Arquivos != null || item.Arquivos.Any() ? item.Arquivos : new string[] { };

						if (item.Arquivos != null)
							arquivosRetorno = item.Arquivos;


						dados.Add(new
						{
							arquivo = item.Arquivo,
							arquivos = arquivosRetorno,
							quantidade = arquivosRetorno != null || arquivosRetorno.Any() ? arquivosRetorno.Join(campanhasFinal, m => m, n => n.Arquivo.Arquivo, (m, n) => n).Count() : campanhasFinal.Where(k => k.Arquivo.Arquivo == item.Arquivo).Count(),
							id = item.ID
						});
					}
				else
					dados.Add(new
					{
						arquivo = string.Empty,
						arquivos = string.Empty,
						quantidade = campanhasFinal.Count,
						id = 1
					});

				b.Result = dados;

				b.End = DateTime.Now;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).ToString();

				if (b.Error.Contains("CK_USUARIOS_SALDO") || b.Error.Contains("CK_USUARIOS_SALDO"))
					b.Error = "Não há saldo disponível pra o envio";
				else if (b.Error.Contains("CK_GRUPOUSUARIOS_SALDO"))
					b.Error = "Não há saldo disponível no grupo pra o envio";

				res = BadRequest(b);
			}


			return res;

		}

		[HttpPost("monitoria/suspenso/{carteiraid:int?}")]
		[NivelPermissao(1, PaginaID = 129, SubPaginaID = 91)]
		public async Task<IActionResult> MonitoriaActionsSuspensos(int? carteiraid, [FromBody] MonitoriaModel m)
		{
			return await MonitoriaActions(carteiraid, 4, m);
		}

		[HttpPost("monitoria/agendado/{carteiraid:int?}")]
		[NivelPermissao(1, PaginaID = 129, SubPaginaID = 90)]
		public async Task<IActionResult> MonitoriaActionsAgendados(int? carteiraid, [FromBody] MonitoriaModel m)
		{
			return await MonitoriaActions(carteiraid, 0, m);
		}

		private async Task<IActionResult> MonitoriaActions(int? carteiraid, byte acao, MonitoriaModel m)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<MonitoriaModel>() { Start = DateTime.Now };
			try
			{
				b.Result = await new CampanhaRepository().MonitoriaActions(m, acao, ClienteID, UsuarioID, carteiraid);
				b.End = DateTime.Now;
				b.Itens = 1;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpGet("monitoriahoje/")]
		[NivelPermissao(1, PaginaID = 129, SubPaginaID = 89)]
		public async Task<IActionResult> MonitoriaHoje()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<MonitoriaModel>() { Start = DateTime.Now };
			try
			{

				b.Result = await new CampanhaRepository().MonitoriaHoje(ClienteID, UsuarioID);
				b.End = DateTime.Now;
				b.Itens = 1;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpGet("quantidadebystatus/")]
		//está dentro de monitoria hoje (ficará com mesmo nível)
		[NivelPermissao(1, PaginaID = 129, SubPaginaID = 89)]
		public async Task<IActionResult> QuantidadeByStatus()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<StatusQuantidade>>() { Start = DateTime.Now };
			try
			{

				b.Result = await new CampanhaRepository().QuantidadeByStatus(ClienteID, UsuarioID);
				b.End = DateTime.Now;
				b.Itens = b.Result.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		[HttpPost("reagendar/{carteiraid:int}/{arquivoid:int}")]
		//[NivelPermissao(1, PaginaID = 129, SubPaginaID = 89)]
		public async Task<IActionResult> Reagendar(int carteiraid, int arquivoid, [FromBody] IEnumerable<CampanhaGridLotesModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				if (t.Any(a => a.DataEnviar.Minute % 5 > 0))
					throw new Exception("Minutos tem que ser múltiplos de 5");

				if (t.Any(a => a.DataEnviar.DateTimeNoSecond() < DateTime.Now.DateTimeNoSecond()))
					throw new Exception("Data/Hora não pode ser menor do que a data/hora atual");


				var registrosAfetados = await new CampanhaRepository().ActionCampanhas(t, arquivoid, carteiraid, 0, ClienteID, UsuarioID, ActionCamp.REAGENDAR);

				b.Result = registrosAfetados > 0 ? $"{registrosAfetados} foram atualizado(s) com sucesso" : $"{registrosAfetados} foi atualizado com sucesso";
				b.End = DateTime.Now;
				b.Itens = registrosAfetados;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpPost("suspender/{carteiraid:int}/{arquivoid:int}")]
		public async Task<IActionResult> Suspender(int carteiraid, int arquivoid, [FromBody] IEnumerable<CampanhaGridLotesModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				var registrosAfetados = await new CampanhaRepository().ActionCampanhas(t, arquivoid, carteiraid, 4, ClienteID, UsuarioID, ActionCamp.SUSPENDER);

				b.Result = registrosAfetados > 0 ? $"{registrosAfetados} foram atualizado(s) com sucesso" : $"{registrosAfetados} foi atualizado com sucesso";
				b.End = DateTime.Now;
				b.Itens = registrosAfetados;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;

		}

		[HttpPost("cancelar/{carteiraid:int}/{arquivoid:int}")]
		public async Task<IActionResult> Cancelar(int carteiraid, int arquivoid, [FromBody] IEnumerable<CampanhaGridLotesModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				var registrosAfetados = await new CampanhaRepository().ActionCampanhas(t, arquivoid, carteiraid, 5, ClienteID, UsuarioID, ActionCamp.CANCELAR);

				b.Result = registrosAfetados > 0 ? $"{registrosAfetados} foram atualizado(s) om sucesso" : $"{registrosAfetados} foi atualizado com sucesso";
				b.End = DateTime.Now;
				b.Itens = registrosAfetados;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpPost("reativar/{carteiraid:int}/{arquivoid:int}")]
		public async Task<IActionResult> Reativar(int carteiraid, int arquivoid, [FromBody] IEnumerable<CampanhaGridLotesModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				var registrosAfetados = await new CampanhaRepository().ActionCampanhas(t, arquivoid, carteiraid, 0, ClienteID, UsuarioID, ActionCamp.REATIVAR);

				b.Result = registrosAfetados > 0 ? $"{registrosAfetados} foram atualizado(s) om sucesso" : $"{registrosAfetados} foi atualizado com sucesso";
				b.End = DateTime.Now;
				b.Itens = registrosAfetados;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpGet("acao/{acao}/{statusenvioold:int}")]
		public async Task<IActionResult> ActionsLote(string acao, int statusenvioold)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{


				var _action = ((ActionCamp)Enum.Parse(typeof(ActionCamp), acao.ToUpper()));

				byte statusenvio = 0;
				switch (_action)
				{
					case ActionCamp.CANCELAR: statusenvio = 5; break;
					case ActionCamp.SUSPENDER: statusenvio = 4; break;
					case ActionCamp.REATIVAR: statusenvio = 0; break;
				}

				var registrosAfetados = await new CampanhaRepository().ActionsLoteCampanha(statusenvio, (byte)statusenvioold, ClienteID, UsuarioID, _action);

				b.Result = registrosAfetados > 0 ? $"{registrosAfetados} foram atualizado(s) om sucesso" : $"{registrosAfetados} foi atualizado com sucesso";
				b.End = DateTime.Now;
				b.Itens = registrosAfetados;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}

		[HttpPost("atualizadistribuicao/")]
		public async Task<IActionResult> AtualizaDistribuicao([FromBody] IEnumerable<FornecedorModel> f)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<string>() { Start = DateTime.Now };
			try
			{
				var itensAtualizados = await new FornecedorRepository().AtualizaDistribuicao(f, ClienteID);
				b.Result = $"{itensAtualizados} atualizados com sucesso!";
				b.End = DateTime.Now;
				b.Itens = f.Count();
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;

		}
		[HttpGet("dashboard/")]
		public async Task<IActionResult> DashBoard()
		{

			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{

				var dados = await new CampanhaRepository().DashBoard(ClienteID, null);


				if (!dados.Item1.Any() && !dados.Item2.Any())
					return NoContent();

				b.Result = new
				{
					agendamentofornecedor = dados.Item1.Select(a => new
					{
						quantidade = a.Quantidade,
						hora = a.Hora
					}),
					graficodash = dados.Item2.GroupBy(a => a.DataEnviar.Hour, (a, m) => new
					{
						agendadofornecedor = 0,
						dataenviar = a,
						quantidade = m.Sum(l => l.Quantidade),
						entregues = m.Where(l => l.StatusEnvio == 2).Sum(l => l.Quantidade),
						agendadas = m.Where(l => l.StatusEnvio == 0).Sum(l => l.Quantidade),
						enviando = m.Where(l => l.StatusEnvio == 1).Sum(l => l.Quantidade)
					}),
					statusdash = dados.Item2.GroupBy(a => a.DataEnviar.Date, (a, m) => new
					{
						datadia = a,
						statusenvio = new
						{
							agendados = m.Where(l => l.StatusEnvio == 0).Sum(l => l.Quantidade),
							suspensos = m.Where(l => l.StatusEnvio == 4).Sum(l => l.Quantidade),
							enviando = m.Where(l => l.StatusEnvio == 1).Sum(l => l.Quantidade),
							erros = m.Where(l => l.StatusEnvio == 3).Sum(l => l.Quantidade),
							cancelados = m.Where(l => l.StatusEnvio == 5).Sum(l => l.Quantidade),
							entregues = m.Where(l => l.StatusEnvio == 2).Sum(l => l.Quantidade)
						}
					})
				};

				b.End = DateTime.Now;
				b.Itens = 1;
				res = Ok(b);
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}

			return res;
		}








		public async Task<IActionResult> GetAll()
		{
			throw new NotImplementedException();
		}


		public Task<IActionResult> GetByIDAsync(int id)
		{
			throw new NotImplementedException();
		}


		public Task<IActionResult> Search(string s)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] CampanhaModel t)
		{
			throw new NotImplementedException();
		}
	}
}
