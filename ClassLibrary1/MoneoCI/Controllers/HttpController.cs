using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using MoneoCI.Repository;
using Helpers;
using DTO;
using Models;
using Models.Fornecedor.Pontal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Models.Fornecedor.ZenviaModel;

namespace MoneoCI.Controllers
{

	[Produces("application/json")]
	[Route("v1/[controller]")]
	public class HttpController : Controller
	{


		//envio de SMS
		HashSet<CampanhaModel> Campanhas { get; set; }
		public int ClienteID { get; set; }
		public int FornecedorID { get; set; }
		public int? UsuarioID { get; set; }
		readonly IMemoryCache _cache;

		const string VALIDAUDIENCE = "ClienteAPI";

		public HttpController(IMemoryCache cache)
		{
			_cache = cache;
		}

		int RetornaStatusEnvio(int fornecedorID, object statusretorno)
		{
			int statusvalue = 0;

			//tratamento pra cada fornecedor no retorno do status
			switch (fornecedorID)
			{
				case 2:
					statusvalue = 2;
					break;
			}

			return statusvalue;
		}

		/// <summary>
		/// Filtragem de report do fornecedor, pra conversão ao formato do Moneo
		/// </summary>
		/// <param name="report">report numérico ou alfa numérico dado pelo fornecedor</param>
		/// <param name="fornecedorid">id do fornecedor obtido no Token</param>
		/// <returns>ReportDelivery</returns>
		ReportDeliveryEnums StatusReport(object report, int fornecedorid)
		{
			ReportDeliveryEnums _report = ReportDeliveryEnums.ENVIADA;


			switch (fornecedorid)
			{
				case (int)FornecedorEnum.Zenvia:
					switch (report)
					{
						case "04":
							_report = ReportDeliveryEnums.EXCLUIDA;
							break;
						case "03":
							_report = ReportDeliveryEnums.ENTREGUE;
							break;
					}
					break;
				case (int)FornecedorEnum.Conectta:
					switch (report)
					{
						case "0":
							_report = ReportDeliveryEnums.ENTREGUE;
							break;
						case "68":
							_report = ReportDeliveryEnums.EXCLUIDA;
							break;
						case "70":
							_report = ReportDeliveryEnums.EXPIRADA;
							break;
					}
					break;
				case (int)FornecedorEnum.Pontal:
					switch (report)
					{
						case "5":
							_report = ReportDeliveryEnums.ENTREGUE;
							break;
						case "6":
							_report = ReportDeliveryEnums.EXCLUIDA;
							break;
						case "13":
							_report = ReportDeliveryEnums.EXPIRADA;
							break;
						case "7":
							_report = ReportDeliveryEnums.BLACKLIST;
							break;
					}
					break;
			}
			return _report;
		}

		ClaimsPrincipal ClaimsByToken(string token, string audience)
		{
			TokenValidationParameters par = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				RequireExpirationTime = false,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("#m0n30c!&c0n3ctt@")),
				ValidateAudience = true,
				ValidateIssuer = false,
				ValidAudience = audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};

			SecurityToken validToken = null;

			return new JwtSecurityTokenHandler().ValidateToken(
				token,
				par,
				out validToken);
		}



		#region CLIENTE
		[HttpPost("c/sendsms/")]
		public async Task<IActionResult> PostaSMS([FromBody] HeaderCampanhaModel c)
		{
			List<ErroResultModel> erroResult = new List<ErroResultModel>() { };

			try
			{

				if (string.IsNullOrEmpty(c.Token))
					throw new Exception("Token não presente");


				var validator = new CampanhaModelValidatorSendSMS();
				Campanhas = new HashSet<CampanhaModel>() { };


				if (!c.Campanhas.Any())
					throw new Exception("Lista de campanha deve ter ao menos uma campanha");

				await c.Campanhas.ToObservable().ForEachAsync(a =>
					{
						if (validator.Validate(a).IsValid)
							Campanhas.Add(a);
					});

				if (!Campanhas.Any())
					throw new Exception("Sem dados na lista após validação");

				var dados = ClaimsByToken(c.Token, "ClienteAPI");

				ClienteID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "clienteid").Value);

				if (ClienteID == 0)
					throw new Exception("ClienteID sem valor no token");

				if (dados.Claims.Any(a => a.Type == "usuarioid"))
					UsuarioID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "usuarioid").Value);


				Campanhas = Campanhas.AsParallel().NonoDigito(_cache.Get<List<PrefixoModel>>("nextel")).Item1.ToHashSet();

				//separando os inválidos
				var celularesInvalidos = new List<CampanhaModel>() { };


				#region CELULARES_INVALIDOS
				//adicionando inválidos do tipo CELULARINVALIDO
				var _celularInvalido = (from a in Campanhas
										join b in _cache.Get<IEnumerable<PrefixoModel>>("prefixos") on a.Celular.ToPrefixo() equals b.Prefixo into ps
										from b in ps.DefaultIfEmpty()
										where b == null
										select new CampanhaModel()
										{
											CarteiraNome = a.CarteiraNome,
											Texto = a.Texto,
											IDCliente = a.IDCliente,
											DataEnviar = a.DataEnviar,
											Celular = a.Celular,
											TipoInvalido = TiposInvalidosEnums.CELULARINVALIDO,
										}).ToList();



				if (_celularInvalido.Any())
				{
					erroResult.Add(new ErroResultModel() { ErroTipo = TiposInvalidosEnums.CELULARINVALIDO, Quantidade = _celularInvalido.Count });
					celularesInvalidos.AddRange(_celularInvalido);
					Campanhas = Campanhas.Except(_celularInvalido, new CompareObject<CampanhaModel>(
						(a, b) => a.Celular == b.Celular &&
								a.Texto == b.Texto,
						i => (i.Celular.GetHashCode() ^
								i.Texto.GetHashCode()))).ToHashSet();


				}

				if (!Campanhas.Any())
					throw new Exception("Lista com celulares inválidos");

				#endregion

				#region BLACKLIST

				var _blackLIst = (from a in Campanhas
								  join b in await new BlacklistRepository().GetAll(new BlackListModel() { Cliente = new ClienteModel() { ClienteID = ClienteID } }, null) on a.Celular equals b.Celular into ps
								  from b in ps.DefaultIfEmpty()
								  where b != null
								  select new CampanhaModel()
								  {
									  CarteiraNome = a.CarteiraNome,
									  IDCliente = a.IDCliente,
									  Texto = a.Texto,
									  Celular = a.Celular,
									  DataEnviar = a.DataEnviar,
									  TipoInvalido = TiposInvalidosEnums.BLACKLIST,
								  }).ToList();

				if (_blackLIst.Count > 0)
				{
					erroResult.Add(new ErroResultModel() { ErroTipo = TiposInvalidosEnums.BLACKLIST, Quantidade = _celularInvalido.Count });

					celularesInvalidos.AddRange(_blackLIst);

					Campanhas = Campanhas.Except(_blackLIst, new CompareObject<CampanhaModel>(
						(a, b) => a.Celular == b.Celular &&
								a.Texto == b.Texto,
						i => (i.Celular.GetHashCode() ^
								i.Texto.GetHashCode()))).ToHashSet();


				}
				if (!Campanhas.Any())
					throw new Exception("Lista com registros na tabela de Blacklist");


				#endregion

				#region ACIMA160CARACTERES
				var _acima160caracteres = Campanhas.Where(a => a.Texto.Length > 160);

				if (_acima160caracteres.Any())
				{
					erroResult.Add(new ErroResultModel() { ErroTipo = TiposInvalidosEnums.ACIMA160CARACTERES, Quantidade = _celularInvalido.Count });

					celularesInvalidos.AddRange(_acima160caracteres.Select(a => new CampanhaModel()
					{
						CarteiraNome = a.CarteiraNome,
						DataEnviar = a.DataEnviar,
						Celular = a.Celular,
						Texto = a.Texto,
						IDCliente = a.IDCliente,
						TipoInvalido = TiposInvalidosEnums.ACIMA160CARACTERES,
					}));
					Campanhas = Campanhas.Where(a => a.Texto.Length <= 160).ToHashSet();
				}

				if (!Campanhas.Any())
					throw new Exception("Mensagens com mais de 160 caracteres");


				#endregion

				//vinculando as campanhas com operadora DESCONHECIDA
				Campanhas = Campanhas
					.Join(_cache.Get<List<PrefixoModel>>("prefixos"), a => a.Celular.ToPrefixo(), b => b.Prefixo, (a, b) => new CampanhaModel()
					{
						CarteiraNome = a.CarteiraNome,
						Texto = a.Texto,
						DataEnviar = a.DataEnviar,
						IDCliente = a.IDCliente,
						Celular = a.Celular,
						Operadora = b.Operadora
					}).ToHashSet();


				//recuperando carteiraid

				var _carteiras = await new CarteiraRepository().CarteirasToApi(Campanhas, celularesInvalidos, ClienteID);

				if (_carteiras.Item1 != null && _carteiras.Item2 != null)
				{
					Campanhas = (from a in Campanhas
								 join b in _carteiras.Item1 on a.Carteira equals b.Carteira into ps
								 from b in ps.DefaultIfEmpty()
								 where b != null
								 select new CampanhaModel()
								 {
									 Celular = a.Celular,
									 DataEnviar = a.DataEnviar,
									 Texto = a.Texto,
									 Carteira = new CarteiraModel() { CarteiraID = b.CarteiraID },
									 IDCliente = a.IDCliente,
									 Operadora = a.Operadora
								 }).ToHashSet();

					if (celularesInvalidos.Any())
						if (_carteiras.Item2 != null || _carteiras.Item2.Any())
							celularesInvalidos = (from a in celularesInvalidos
												  join b in _carteiras.Item2 on a.Carteira equals b.Carteira into ps
												  from b in ps.DefaultIfEmpty()
												  where b != null
												  select new CampanhaModel()
												  {
													  TipoInvalido = a.TipoInvalido,
													  Celular = a.Celular,
													  DataEnviar = a.DataEnviar,
													  Texto = a.Texto,
													  Carteira = new CarteiraModel() { CarteiraID = b.CarteiraID },
													  IDCliente = a.IDCliente
												  }).ToList();
				}
				else
					throw new Exception("Um ou mais carteiras na listagem não localizado no sistema. Verifique a grafia correta da carteira");



				if (!Campanhas.Any())
					throw new Exception("Sem campanha válida após a busca da(s) carteira(s) no sistema");



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
							StatusEnvio = a.DataEnviar.DateTimeMinuteInterval() < DateTime.Now ? 1 : 0,
							IDCliente = a.IDCliente,
							Carteira = a.Carteira,
							Fornecedor = new FornecedorModel() { FornecedorID = 1 },
							DataDia = a.DataEnviar.Date,
							TipoSMS = Tipo.LONGCODE,
							DataEnviar = a.DataEnviar.DateTimeMinuteInterval(),
							Cliente = new ClienteModel() { ClienteID = ClienteID },
							Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null
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
							Carteira = new CarteiraModel() { CarteiraID = a.CarteiraID }
						}),
						ClienteID, UsuarioID);

				return Ok(new ResultModel()
				{
					Total = c.Campanhas.Count(),
					Validos = Campanhas.Count,
					Erros = erroResult
				});


			}
			catch (Exception err)
			{
				return BadRequest(new { error = (err.InnerException ?? err).Message });

			}

		}

		[HttpGet("c/retornos/")]
		public async Task<IActionResult> RetornosCientes([FromBody] RetornoModel r)
		{
			try
			{
				var validator = await Util.ValidaRequisicao(new RetornoModelValidatorApi(), r);

				if (!validator.Item1)
					throw new Exception(validator.Item2);


				var dados = ClaimsByToken(r.Token, "ClienteAPI");

				ClienteID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "clienteid").Value);

				if (ClienteID == 0)
					throw new Exception("ClienteID sem valor no token");

				if (dados.Claims.Any(a => a.Type == "usuarioid"))
					UsuarioID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "usuarioid").Value);

				r.ClienteID = ClienteID;
				r.UsuarioID = UsuarioID;

				var retornos = await new RetornoRepository().RetornosAPI(r);

				if (retornos == null)
					throw new Exception("sem dados para a consulta");

				return Ok(retornos.Select(a => new
				{
					idcliente = a.IDCliente,
					celular = a.Celular,
					dataenviar = a.DataEnviar,
					carteira = a.Carteira,
					texto = a.Texto,
					fornecedor = a.FornecedorNome,
					retornocliente = a.RetornoCliente,
					dataretorno = a.DataRetorno
				}));

			}
			catch (Exception err)
			{
				return BadRequest(new { error = (err.InnerException ?? err).Message });

			}
		}

		[HttpGet("c/confirmacoes/")]
		public async Task<IActionResult> ConfirmacoesEntrega([FromBody] CampanhaModel c)
		{
			try
			{

				var validator = await Util.ValidaRequisicao(new CampanhaModelValidatorApi(), c);

				if (!validator.Item1)
					throw new Exception(validator.Item2);

				var dados = ClaimsByToken(c.Token, "ClienteAPI");

				ClienteID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "clienteid").Value);

				if (ClienteID == 0)
					throw new Exception("ClienteID sem valor no token");

				if (dados.Claims.Any(a => a.Type == "usuarioid"))
					UsuarioID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "usuarioid").Value);

				c.Cliente = new ClienteModel() { ClienteID = ClienteID };
				if (UsuarioID.HasValue)
					c.Usuario = new UsuarioModel() { UsuarioID = UsuarioID.Value };

				var result = await new CampanhaRepository().DetalhadoCampanhas(c, ClienteID, UsuarioID);

				if (result.Any())
					return Ok(result.Select(a => new
					{
						texto = a.Texto,
						celular = a.Celular,
						dataenviar = a.DataEnviar,
						carteira = a.Carteira.Carteira,
						arquivo = a.Arquivo != null ? a.Arquivo.Arquivo : string.Empty,
						tipocampanha = a.TipoCampanha.TipoCampanha,
						operadora = a.Operadora.ToString(),
						uf = a.UF,
						regiao = a.Regiao,
						fornecedor = a.Fornecedor.Nome,
						statusreport = a.StatusReport.ToString()
					}));
				else
					throw new Exception($"Sem dados para a pesquisa do dia {c.DataInicial} a {c.DataFinal}");



			}
			catch (Exception err)
			{
				return BadRequest(new { error = (err.InnerException ?? err).Message });

			}
		}
		#endregion

		async Task<IActionResult> PostaRetornoIoPeople(ReplyGenericModel r, int fornecedorid)
		{
			try
			{

				var campanha = await new CampanhaRepository().DadosIOPeople(new CampanhaModel() { CampanhaID = long.Parse(r.reference) }, fornecedorid);

				if (campanha == null)
					throw new Exception("Campanha inválida");

				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://iopeople2.pixon.cloud/moneo");
				httpWebRequest.Method = "POST";
				httpWebRequest.Accept = "application/json";
				httpWebRequest.ContentType = "application/json";

				var s = await httpWebRequest.GetRequestStreamAsync();
				r.mailingName = campanha.FileName;
				r.messageOriginal = campanha.Texto;
				r.sentOriginal = campanha.DataEnviar;

				byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new IOPeopleModel(new ReplyGenericModel[] { r })));
				await s.WriteAsync(bytes, 0, bytes.Length);
				var _response = await httpWebRequest.GetResponseAsync();

				return Ok();

			}
			catch (Exception err)
			{
				return BadRequest(new { error = (err.InnerException ?? err).Message });

			}
		}

		#region Ratinhos
		[HttpPost("ratinho/retorno")]
		public async Task<IActionResult> GravaRetornoRatinhos([FromBody] CampanhaModel c)
		{

			try
			{
				var validator = await Util.ValidaRequisicao(new CampanhaModelValidatorApiRatinho(), c);
				if (!validator.Item1)
					throw new Exception(validator.Item2);

				await new CampanhaRepository().GravaRetornoRatinhos(c);

				return Ok();
			}
			catch (Exception err)
			{
				return BadRequest(new { erro = (err.InnerException ?? err).Message });
			}
		}

		[HttpPut("ratinho/add")]
		public async Task<IActionResult> AdicionaRatinhos([FromBody] CampanhaModel c)
		{
			try
			{
				if (string.IsNullOrEmpty(c.Token))
					throw new Exception("Token não presente");

				ClaimsByToken(c.Token, "RatinhoAPI");

				if (c.Celular == 0)
					throw new Exception("O celular deve conter um valor");
				else
				{
					if (c.Celular.ToString().Length != 11)
						throw new Exception("Tamanho do celular inválido");
				}
				await new CampanhaRepository().AdicionaRatinho(c);

				return Ok();
			}
			catch (Exception err)
			{
				var _err = (err.InnerException ?? err).Message;

				if (_err.Contains("IX_RATINHOS_CELULAR"))
					_err = $"{c.Celular} já está cadastrado no sistema";
				else if (_err.Contains("Signature validation failed"))
					_err = "Falha na validação do Token";

				return BadRequest(new { erro = _err });
			}

		}

		#endregion

		//[HttpPost("f/iopeople/{token}")]
		//public async value<IActionResult> RetornoIoPeople([FromBody] object c, string token)
		//{
		//	try
		//	{
		//		return Ok();
		//	}
		//	catch (Exception)
		//	{
		//		return BadRequest();
		//		throw;
		//	}
		//}

		[HttpPost("f/testerecepcao")]
		public async Task<IActionResult> TesteRecepcao([FromBody] object c)
		{
			await Task.Delay(10);

			JObject json = JObject.Parse(c.ToString());

			var itens = json["sendSmsMultiResponse"]["sendSmsResponseList"];


			return Ok();


		}
		#region Fornecedor
		[HttpPost("f/callback/{token}")]
		public async Task<IActionResult> GenericCallBack([FromBody] object c, string token)
		{
			try
			{



				if (string.IsNullOrEmpty(token))
					throw new Exception("Token não presente");

				var dados = ClaimsByToken(token, "FornecedorAPI");

				if (dados.Claims.Any(a => a.Type == "fornecedorid"))
					FornecedorID = int.Parse(dados.Claims.SingleOrDefault(a => a.Type == "fornecedorid").Value);

				var fornecedor = new FornecedorModel()
				{
					FornecedorID = FornecedorID,
					ApiKey = dados.Claims.SingleOrDefault(a => a.Type == JwtRegisteredClaimNames.Jti).Value
				};

				var campanha = new CampanhaModel();
				
				var retornos = new List<RetornoModel>() { };

				if (await new FornecedorRepository().IsApiKeyFornecedor(fornecedor))
				{
					switch (fornecedor.FornecedorID)
					{
						case (int)FornecedorEnum.Zenvia:
							var zenvia = JsonConvert.DeserializeObject<ZenviaJson>(c.ToString());

							if (zenvia.CallbackMtRequest != null)//confirmação de entrega
							{
								campanha.CampanhaID = long.Parse(zenvia.CallbackMtRequest.id);
								campanha.StatusReport = StatusReport(zenvia.CallbackMtRequest.status, fornecedor.FornecedorID);
								campanha.DataReport = zenvia.CallbackMtRequest.received;
							}
							else if (zenvia.CallBackMoRequest != null)//retorno de cliente
							{
								retornos.Add(new RetornoModel()
								{
									CampanhaID = long.Parse(zenvia.CallBackMoRequest.id),
									RetornoCliente = zenvia.CallBackMoRequest.body,
									DataRetorno = zenvia.CallBackMoRequest.received
								});
								
							}

							break;
						case (int)FornecedorEnum.Conectta:
							var conectta = JsonConvert.DeserializeObject<RetornoConectta>(c.ToString());

							if (string.IsNullOrEmpty(conectta.retorno))
							{
								campanha.CampanhaID = long.Parse(conectta.id);
								campanha.StatusReport = StatusReport(conectta.statuscode, fornecedor.FornecedorID);
								campanha.DataReport = conectta.datareport;
							}
							else
							{
								retornos.Add(new RetornoModel()
								{
									CampanhaID = long.Parse(conectta.id),
									RetornoCliente = conectta.retorno,
									DataRetorno = conectta.dataretorno
								});
							}
							break;
						case (int)FornecedorEnum.Pontal:

							var pontal = JsonConvert.DeserializeObject<PontalModel>(c.ToString());

							if (pontal.type == "api_message")//confirmação de entrega
							{
								switch (pontal.status)
								{
									case 4:
									case 14:
									case 10:
										campanha.StatusEnvio = 3;
										break;
									case 5:
									case 7:
									case 13:
									case 6:
										campanha.StatusReport = StatusReport(pontal.status, fornecedor.FornecedorID);
										break;
								}
								campanha.CampanhaID = long.Parse(pontal.reference);
								campanha.DataReport = DateTime.Now;
							}
							else if (pontal.type == "api_reply") //retorno de cliente
							{
								
								retornos.AddRange(JsonConvert.DeserializeObject<retorno>(c.ToString())
									.replies
									.Select(a => new RetornoModel()
								{
									CampanhaID=long.Parse(a.reference),
									RetornoCliente=a.message,
									DataRetorno=a.received
								}));
							}
							break;
					}
				}

				if (campanha.CampanhaID > 0)//MT
					await new CampanhaRepository().AtualizaItensStatusReport(new CampanhaModel[] { campanha });
				else //MO
				{

					await new RetornoRepository().AddByApi(retornos);

					#region IOPeople Disparos Retornos
#pragma warning disable 4014

					foreach (var retornoModel in retornos)
					{
						PostaRetornoIoPeople(new ReplyGenericModel()
						{
							received = retornoModel.DataRetorno,
							messageOriginal = retornoModel.RetornoCliente,
							reference = retornoModel.CampanhaID.ToString()
						}, fornecedor.FornecedorID);
					}
					
				
#pragma warning restore 4014 
					#endregion
	
				}

				return Ok();


			}
			catch (Exception err)
			{

				return BadRequest($"Erro {(err.InnerException ?? err).Message}");
			}
		}

		[HttpGet("f/mt/{token}/{campanhaid:int}/{report}/{datareport}")]
		public async Task<IActionResult> CallBackMT(string token, int campanhaid, string report, string datareport)
		{
			var dataReport = new DateTime();
			if (DateTime.TryParse(datareport, out dataReport))
				return await GenericCallBack(new CampanhaModel()
				{
					DataReport = dataReport,
					CampanhaID = campanhaid,
					Report = report,
					Token = token
				}, token);

			return BadRequest();
		}

		[HttpGet("f/mo/{token}/{campanhaid:int}/{retorno}/{dataretorno}")]
		public async Task<IActionResult> CallBackMO(string token, int campanhaid, string retorno, string dataretorno)
		{
			var dataRetorno = new DateTime();
			if (DateTime.TryParse(dataretorno, out dataRetorno))
				return await GenericCallBack(new RetornoModel()
				{
					DataRetorno = dataRetorno,
					CampanhaID = campanhaid,
					RetornoCliente = retorno,
					Token = token
				}, token);

			return BadRequest();
		}
		#endregion

	}
}
