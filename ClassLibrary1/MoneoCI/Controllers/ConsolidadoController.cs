using MoneoCI.Repository;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DTO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Reactive.Concurrency;
using System.Text;
using Microsoft.Net.Http.Headers;
using System.IO;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Atributos;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize(Roles = "Usuario,Cliente,AdminOnly")]
	public class ConsolidadoController : ControllerBase, IControllers<ConsolidadoModel>
	{
		public int ClienteID { get { return int.Parse(User.FindFirst(a => a.Type == "clienteid").Value); } }

		public int? UsuarioID
		{
			get
			{
				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Any())
					if (User.FindFirst(c => c.Type == ClaimTypes.GroupSid).Value != "5")
						return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return new Nullable<int>();
			}
		}

		readonly IRepository<ConsolidadoModel> repository = null;

		public ConsolidadoController(IRepository<ConsolidadoModel> repos)
		{
			repository = repos;
		}

		[HttpGet("arquivo/down/{arquivo}")]
		[NivelPermissao(1, PaginaID = 137, SubPaginaID = 0)]
		public async Task<IActionResult> DownloadArquivo(string arquivo)
		{
			try
			{

				string mimetype = null;
				switch (Path.GetExtension(arquivo).ToLower())
				{
					case ".txt":
						mimetype = "text/plain";
						break;

					case ".csv":
						mimetype = "text/csv";
						break;
					case ".zip":
						mimetype = " application/zip";
						break;
				}

				using (var mem = new MemoryStream())
				{
					var stream = await Util.DownloadFileS3("moneoup", arquivo, ClienteID);
					await stream.CopyToAsync(mem);

					Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
					Response.Headers.Add("Content-Disposition", $"attachment;filename={arquivo}");
					return File(mem.ToArray(), mimetype);
				}

			}
			catch (Exception err)
			{
				var msg = (err.InnerException ?? err).Message;
				if (msg == "The specified key does not exist.")
					msg = "Arquivo expirado ou inexistente";
				else
					if (msg.Contains("Amazon.Runtime.Internal.HttpErrorResponseException"))
					msg = "arquivo corrompido";

				return BadRequest($"{arquivo} - {msg}");
			}
		}

		[HttpPost("arquivos")]
		[NivelPermissao(1, PaginaID = 137, SubPaginaID = 0)]
		public async Task<IActionResult> RelatorioArquivosAsync([FromBody] ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
				var dados = await new ConsolidadoRepository().RelatorioArquivosAsync(c, ClienteID, UsuarioID);

				if (!dados.Any())
					return NoContent();

				b.Result = dados.Select(a => new
				{
					quantidade = a.Quantidade,
					datadia = a.DataDia,
					expiracao = a.DataDia.AddDays(90),
					expirado = a.DataDia.AddDays(90) < DateTime.Now.Date,
					arquivo = a.Arquivo,
					carteira = a.Carteira,
					registros = a.Registros,
					paginas = a.Paginas,
					quantidadetotal = a.QuantidadeTotal
				});

				b.Itens = dados.Count();
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


		[HttpPost("invalidos")]
		[NivelPermissao(1, PaginaID = 136, SubPaginaID = 0)]
		public async Task<IActionResult> RelatorioInvalidosAsync([FromBody] ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<ConsolidadoInvalidosModel>>() { Start = DateTime.Now };

			try
			{
				var dados = await new ConsolidadoRepository().RelatorioInvalidosAsync(c, ClienteID, UsuarioID);


				if (!dados.Any())
					return NoContent();
				//throw new Exception($"Sem dados para consulta do dia {c.DataInicial.ToString("dd/MM/yyyy")} ao dia {c.DataFinal.ToString("dd/MM/yyyy")}");


				b.Result = dados;
				b.Itens = dados.Count();
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

		[HttpPost("carteiras/down")]
		[NivelPermissao(1, PaginaID = 133, SubPaginaID = 96)]
		public async Task<IActionResult> CarteirasDown([FromBody] ConsolidadoModel c)
		{

			var dados = await new ConsolidadoRepository().Carteiras(c, ClienteID, UsuarioID);
			var sb = new StringBuilder();
			var sbConsolidado = new StringBuilder();


			sb.Append("CARTEIRA;ENVIADAS;DATA\r\n");
			foreach (var a in dados)
				sb.AppendFormat("{0};{1:N0};{2:dd/MM/yyyy}\r\n", a.Carteira, a.Enviados, a.DataDia);

			int totalRegistros = dados.Sum(a => a.Enviados);

			sbConsolidado.Append("CARTEIRA;REGISTROS;PERCENTUAL\r\n");
			foreach (var item in dados.GroupBy(a => new { Carteira = a.Carteira }, (a, m) => new { Carteira = a.Carteira, Enviados = m }))
				sbConsolidado.AppendFormat("{0};{1:N0};{2:N2}\r\n", item.Carteira, item.Enviados.Sum(k => k.Enviados), (decimal)item.Enviados.Sum(k => k.Enviados) / (decimal)totalRegistros * 100);


			var files = new Dictionary<string, byte[]>() { };

			files.Add("detalhado.csv", Util.EncoderDefaultFiles.GetBytes(sb.ToString()));
			files.Add("consolidado.csv", Util.EncoderDefaultFiles.GetBytes(sbConsolidado.ToString()));


			Response.Headers[HeaderNames.ContentEncoding] = "utf-8";
			Response.Headers.Add("Content-Disposition", $"attachment;filename=carteiras.zip");
			return File(await Util.ZipFiles(files), "application/x-zip-compressed");
		}



		[HttpPost("carteiras/")]
		[NivelPermissao(1, PaginaID = 133, SubPaginaID = 96)]
		public async Task<IActionResult> Carteiras([FromBody] ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
				var dados = await new ConsolidadoRepository().Carteiras(c, ClienteID, UsuarioID);


				if (!dados.Any() || dados == null)
					return NoContent();

				b.Result = dados.Select(a => new
				{
					enviados = a.Enviados,
					datadia = a.DataDia,
					carteira = a.Carteira,
					carteiraid = a.CarteiraID,
					registros = a.Registros,
					paginas = a.Paginas
				});
				b.Itens = dados.Count();
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

		[HttpPost("status/{status:int}")]
		[NivelPermissao(1, PaginaID = 132, SubPaginaID = 95)]
		public async Task<IActionResult> ConsolidadoByStatus([FromBody]ConsolidadoModel c, byte status)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{
				var dados = await new ConsolidadoRepository().ConsolidadoByStatus(c, (byte)status, ClienteID, UsuarioID);

				if (dados == null || !dados.Any())
					return NoContent();

				b.Result = new
				{
					paginas = dados.ElementAt(0).Paginas,
					registros = dados.ElementAt(0).Registros,
					dados = dados.Select(a => new
					{
						arquivo = a.Arquivo.Arquivo,
						dataenviar = a.DataEnviar,
						carteira = a.Carteira.Carteira,
						quantidade = a.Quantidade
					})
				};
				b.Itens = dados.Count();



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



		[HttpPost("especializado/download/")]
		[NivelPermissao(1, PaginaID = 135, SubPaginaID = 0)]
		public async Task<IActionResult> DownloadEspecializado([FromBody] ConsolidadoModel c)
		{
			var dados = await new ConsolidadoRepository().DownEspecializado(c, ClienteID, UsuarioID);

			var sb = new StringBuilder();

			sb.Append("ENTREGUES;REJEITADAS;EXPIRADAS;ENVIADAS;CANCELADAS;ERROS;SUSPENSAS;ARQUIVO;CARTEIRA;SPGRANDE;SPCAPITAL;DEMAISDDD;VALIDADE;LOTE;FORNECEDOR;DATAENVIAR;USUARIO;TIPOCAMPANHA\r\n");
			foreach (var a in dados)
			{
				sb.AppendFormat("{0:N0};{1:N0};{2:N0};{3:N0};{4:N0};{5:N0};{6:N0};{7};{8};{9:N0};{10:N0};{11:N0};{12:dd/MM/yyyy};{13};{14};{15};{16};{17}\r\n",
							  a.Entregues,
							  a.Excluidas,
							  a.Expiradas,
							  a.Enviados,
							  a.Canceladas,
							  a.Erros,
							  a.Suspensos,//6
							  a.Arquivo,
							  a.Carteira,
							  a.SpGrande,//9
							  a.SpCapital,
							  a.DemaisDDD,
							  a.Validade,//12
							  a.Codigo,
							  a.FornecedorNome,
							  a.DataEnviar,
							  a.UsuarioNome,
							  a.TipoCampanha ?? string.Empty);
			}

			using (var mem = new MemoryStream(Util.EncoderDefaultFiles.GetBytes(sb.ToString())))
			{
				Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
				Response.Headers.Add("Content-Disposition", $"attachment;filename=especializado.zip");
				return File(await mem.ToZip("especializado.csv"), "application/x-zip-compressed");
			}
		}

		[HttpPost("especializado/{carteiraid:int?}")]
		[NivelPermissao(1, PaginaID = 135, SubPaginaID = 0)]
		public async Task<IActionResult> Especializado([FromBody] ConsolidadoModel c, int? carteiraid)
		{

			//Scheduler.NewThread.Schedule(.Schedule(() => UrlShoortnerGoogle(campanhas));

			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var dados = await new ConsolidadoRepository().Especializado(c, ClienteID, UsuarioID);

				if (dados == null || !dados.Any())
					return NoContent();


				b.Result = new
				{
					paginas = dados.ElementAt(0).Paginas,
					registros = dados.ElementAt(0).Registros,
					dados = dados.Select(a => new
					{
						arquivo = a.Arquivo,
						carteira = a.Carteira,
						spgrande = a.SpGrande,
						spcapital = a.SpCapital,
						demaisddd = a.DemaisDDD,
						validade = a.Validade,
						enviados = a.Enviados,
						invalidos = a.CelularInvalido,
						excluidas = a.Excluidas,
						codigo = a.Codigo,
						celularinvalido = a.CelularInvalido,
						nomeusuario = a.UsuarioNome,
						fornecedor = a.FornecedorNome,
						datadia = a.DataDia
					})
				};
				b.Itens = dados.Count();



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


		[HttpPost("envios/")]
		[NivelPermissao(1, PaginaID = 132, SubPaginaID = 94)]
		public async Task<IActionResult> Envios([FromBody]ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

			try
			{

				var dados = await new ConsolidadoRepository().Consolidado(c, ClienteID, UsuarioID);

				if (!dados.Item1.Any() && !dados.Item2.Any())
					return NoContent();//throw new Exception($"sem dados pra consulta correspondente ao período de {c.DataInicial.Value.ToString("dd/MM/yyyy")} a {c.DataFinal.Value.ToString("dd/MM/yyyy")}");

                var dadosAgrupadosDia = dados.Item2.Select(l => new
                {
                    datadia = l.DataDia,
                    enviadas = l.Enviadas,
                    naoenviadas = l.NaoEnviadas,
                    total = l.Entregues + l.Expiradas + l.Excluidas + l.Erros + l.Suspensos + l.Canceladas + l.Enviados                    
                });

                b.Result = new
                {
                    //paginas = totalPaginas,
                    registros = dados.Item1.ElementAt(0).Registros,
                    enviadas = dados.Item1.Sum(a => a.Enviadas),
                    naoenviadas = dados.Item1.Sum(a => a.NaoEnviadas),
                    total = dados.Item1.Sum(a => a.Enviadas) + dados.Item1.Sum(a => a.NaoEnviadas),
                    consolidados = dadosAgrupadosDia
                };

				b.Itens = dadosAgrupadosDia.Count();
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


		public Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<ConsolidadoModel> t)
		{
			throw new NotImplementedException();
		}

		[HttpPost("update/")]
		[NivelPermissao(1, PaginaID = 135, SubPaginaID = 0)]
		public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<ConsolidadoModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<ConsolidadoModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				await repository.Update(t, ClienteID, null);
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

		public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<ConsolidadoModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAll()
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

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] ConsolidadoModel t)
		{
			throw new NotImplementedException();
		}
	}
}
