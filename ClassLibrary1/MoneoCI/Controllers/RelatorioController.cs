using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MoneoCI.Repository;
using Atributos;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize(Roles = "Usuario,Cliente,AdminOnly")]
    public class RelatorioController : ControllerBase
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
        [HttpGet("carteiras/")]
        [NivelPermissao(1, PaginaID = 120, SubPaginaID = 85)]
        public async Task<IActionResult> Carteiras() => await new UtilController().GenericCall<CarteiraModel>(ClienteID, UsuarioID, new CarteiraRepository(), new CarteiraModel() { Cliente = new ClienteModel() { ClienteID = ClienteID }, OrigemChamada = OrigemChamadaEnums.RELATORIO });


        [HttpPost("down/cancelados/")]
        public async Task<IActionResult> DownArquivoInvalido([FromBody] ConsolidadoModel c)
        {
            var dados = await new ConsolidadoRepository().DownCancelados(c, ClienteID, UsuarioID);

            if (!dados.Any())
                throw new Exception("Sem dados");


            var sb = new StringBuilder();
            sb.Append("CELULAR;IDCLIENTE;CARTEIRA;ARQUIVO;TEXTO;DATAENVIAR\r\n");

            foreach (var a in dados)
                sb.AppendFormat("{0};{1};{2};{3};{4};{5}\r\n",
              a.Celular,
              a.IDCliente,
              a.Carteira.Carteira,
              a.Arquivo.Arquivo,
              a.Texto,
              a.DataEnviar);

            using (var mem = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString())))
            {
                Response.Headers[HeaderNames.ContentEncoding] = "utf-7";
                Response.Headers.Add("Content-Disposition", $"attachment;filename=cancelados.zip");
                return File(await mem.ToZip("cancelados.csv"), "application/x-zip-compressed");
            }

        }
        readonly object _locker = new object();


        [HttpPost("down/2")]
        [NivelPermissao(1, PaginaID = 132, SubPaginaID = 94)]
        public async Task<IActionResult> DownArquivoEnviado(int statusenvio, [FromBody] ConsolidadoModel c)
        {
            var tipo = StatusEnvioEnums.ENVIADOS;
            var files = new Dictionary<string, byte[]>() { };

            var sbConsolidado = new StringBuilder();

            var dados = (await new ConsolidadoRepository().DownEspecializado(c, ClienteID, UsuarioID)).ToList();
            
            var lista = dados.GroupBy(a => new { Data = a.DataEnviar.Date, Carteira = a.Carteira }, (a, b) => new
            {
                Enviadas = b.Sum(k => k.Entregues) + b.Sum(k => k.Excluidas) + b.Sum(k => k.Expiradas) + b.Sum(k => k.Enviados),
                NaoEnviadas = b.Sum(k => k.Suspensos) + b.Sum(k => k.Erros) + b.Sum(k => k.Canceladas),
                Carteira = a.Carteira,
                Data = a.Data
            }).ToList();


            sbConsolidado.Append("CARTEIRA;DATA;ENVIADAS;NAOENVIADAS;TOTAL\r\n");
            foreach (var item in lista)
                sbConsolidado.AppendFormat("{0};{1:dd/MM/yyyy};{2:N0};{3:N0};{4:N0}\r\n", item.Carteira, item.Data, item.Enviadas, item.NaoEnviadas, item.Enviadas + item.NaoEnviadas);
            
            files.Add($"envios.csv", Util.EncoderDefaultFiles.GetBytes(sbConsolidado.ToString()));

            Response.Headers[HeaderNames.ContentEncoding] = "utf-8";
            Response.Headers.Add("Content-Disposition", $"attachment;filename={tipo}.zip");
            return File(await Util.ZipFiles(files), "application/x-zip-compressed");

        }

        [HttpPost("down/5")]
        [NivelPermissao(1, PaginaID = 132, SubPaginaID = 95)]
        public async Task<IActionResult> DownArquivoCancelado([FromBody] ConsolidadoModel c)
        {
            var tipo = StatusEnvioEnums.CANCELADOS;
            var files = new Dictionary<string, byte[]>() { };

            var sbConsolidado = new StringBuilder();

            var detalhado = (await new CampanhaRepository().DetalhadoGenerico(new CampanhaModel()
            {
                Cliente = new ClienteModel() { ClienteID = ClienteID },
                Usuario = UsuarioID.HasValue ? new UsuarioModel() { UsuarioID = UsuarioID.Value } : null,
                StatusEnvio = 5,
                DataInicial = c.DataInicial,
                DataFinal = c.DataFinal.Value.Date.AddMinutes(1439),
                CarteiraList = c.CarteiraList
            })).ToList();


            if (!detalhado.Any())
                return NoContent();

            sbConsolidado.Append("DATA;CARTEIRA;ARQUIVO;QUANTIDADE\r\n");
            foreach (var item in detalhado.GroupBy(a => new { Arquivo = a.Arquivo.Arquivo, Carteira = a.Carteira.Carteira, Data = a.DataEnviar }, (a, b) => new { Arquivo = a.Arquivo, Carteira = a.Carteira, Data = a.Data, Canceladas = b.Count() }))
                sbConsolidado.AppendFormat("{0:dd/MM/yyyy HH:mm};{1};{2};{3}\r\n", item.Data, item.Carteira, item.Arquivo, item.Canceladas);

            files.Add($"cancelados.csv", Util.EncoderDefaultFiles.GetBytes(sbConsolidado.ToString()));

            Response.Headers[HeaderNames.ContentEncoding] = "utf-8";
            Response.Headers.Add("Content-Disposition", $"attachment;filename={tipo}.zip");
            return File(await Util.ZipFiles(files), "application/x-zip-compressed");

        }

        public Task<IActionResult> Search(string s)
        {
            throw new NotImplementedException();
        }
    }
}
