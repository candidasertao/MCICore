using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DTO;
using Models;
using MoneoCI.Repository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using MoneoCI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MoneoCI.Controllers
{
    [Authorize(Roles = "Fornecedor")]
    [Produces("application/json")]
    [Route("api/AdministrativoFornecedor")]
    public class AdministrativoFornecedorController : Controller
    {
        public int FornecedorID { get { return int.Parse(User.FindFirst(a => a.Type == "fornecedorid").Value); } }

        [HttpPost("cliente/get/p")]
        public async Task<IActionResult> GetAllPaginadoFornecedorClienteAsync([FromBody] FornecedorClienteModel t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                if (t == null)
                    throw new Exception("Requisição inválida");

                var r = await new FornecedorRepository().GetAllPaginadoFornecedorClienteAsync(t, FornecedorID);

                if (r == null || !r.Any())
                    return NoContent();

                var hoje = DateTime.Now.Date;

                b.Result = r.GroupBy(k => new { k.Fornecedor.FornecedorID, k.Fornecedor.Nome }, (a, k) => new
                {
                    clientes = k.Select(x => new
                    {
                        codigo = x.Codigo,
                        cliente = new
                        {
                            clienteid = x.Cliente.ClienteID,
                            nome = x.Cliente.Nome,
                            email = x.Cliente.Email,
                            telefone = x.Cliente.Telefone,
                            cnpj = x.Cliente.CNPJ
                        },                         
                        isintegrado = x.isIntegrado,
                        usuario = x.Usuario,
                        senha = x.Senha,
                        capacidade = x.Capacidade,
                        envio5min = x.Envio5min,
                        tipo = x.Tipo.ToString(),
                        tipocodigo = (int?)x.Tipo,
                        statusoperacional = x.StatusOperacional,
                        token = x.Token,
                        capacidadeextra = x.Capacidades.Where(extra => extra.DataInicial <= hoje).Sum(e => e.Capacidade),
                        capacidades = x.Capacidades.Select(e => new
                        {
                            codigo = e.Codigo,
                            capacidade = e.Capacidade,
                            datainicial = e.DataInicial,
                            datafinal = e.DataFinal,
                            ativo = e.DataInicial <= hoje
                        })
                    }),                    
                    registros = k.ElementAt(0).Registros
                }).ElementAt(0);

                b.End = DateTime.Now;
                b.Itens = r.Count();
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

        [HttpPost("cliente/update/")]
        public async Task<IActionResult> AtualizarFornecedorClienteAsync([FromBody] FornecedorClienteModel t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<FornecedorClienteModel>() { Start = DateTime.Now };

            try
            {
                if (t == null)
                    throw new Exception("Requisição inválida");

                var hoje = DateTime.Now.Date;

                if (t.Capacidade.HasValue
                        && t.Envio5min.HasValue
                        && t.Tipo.HasValue
                        && (!string.IsNullOrEmpty(t.Usuario) && !string.IsNullOrEmpty(t.Senha) || !string.IsNullOrEmpty(t.Token)))
                    t.StatusFornecedor = 2;
                else
                    t.StatusFornecedor = 3;

                await new FornecedorRepository().AtualizarFornecedorClienteAsync(t, FornecedorID);

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

        [HttpPut("capacidade/add/")]
        public async Task<IActionResult> AdicionarFornecedorCapacidadeExtraAsync([FromBody] IEnumerable<FornecedorCapacidadeExtraModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<IEnumerable<FornecedorCapacidadeExtraModel>>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");

                var hoje = DateTime.Now.Date;

                foreach(var o in t)
                {
                    if (o.Capacidade == 0 || o.ClienteID == 0)
                        throw new Exception("Requisição inválida");

                    if (!o.DataInicial.HasValue || !o.DataFinal.HasValue )
                        throw new Exception("Data inicial e ou final não informada(s)");

                    if (o.DataInicial < hoje || o.DataFinal < hoje)
                        throw new Exception("As datas devem ser maior ou igual a hoje");

                    if (o.DataInicial > o.DataFinal)
                        throw new Exception($"A data final { o.DataFinal.Value.ToString("dd/MM/yyyy") } deve ser maior ou igual a data inicial { o.DataInicial.Value.ToString("dd/MM/yyyy") }");
                }

                await new FornecedorRepository().AdicionarFornecedorCapacidadeExtraAsync(t, FornecedorID);
                                
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

        [HttpDelete("capacidade/delete/")]
        public async Task<IActionResult> RemoverFornecedorCapacidadeExtraAsync([FromBody] IEnumerable<FornecedorCapacidadeExtraModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<FornecedorCapacidadeExtraModel>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");

                foreach (var o in t)
                {
                    if (o.Codigo == 0)
                        throw new Exception("Requisição inválida");
                }
                
                await new FornecedorRepository().RemoverFornecedorCapacidadeExtraAsync(t, FornecedorID);

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

        [HttpGet("monitoria/")]
        public async Task<IActionResult> Monitoria()
        {

            IActionResult res = null;
            var b = new BaseEntityDTO<FornecedorMonitoria>() { Start = DateTime.Now };

            try
            {
                var result = await new FornecedorRepository().MonitoriaFornecedor(FornecedorID);

                if (result == null)
                    return NoContent();

                b.Result = result;

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
    
        [HttpGet("previsto/")]
        public async Task<IActionResult> Previsto()
        {

            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                var result = await new FornecedorRepository().Previsto(FornecedorID);

                if (result == null)
                    return NoContent();

                b.Result = result;

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
        
        [HttpPut("servico/add/")]
        public async Task<IActionResult> AdicionarFornecedorServico([FromBody] IEnumerable<FornecedorServicoModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");

                var check = await new FornecedorRepository().isPodeAgendarInterrupcaoServico(FornecedorID, null);

                if (!check)
                    throw new Exception("Serviço suspenso por prazo indefinido. Reative para realizar um agendamento.");

                var hoje = DateTime.Now;

                foreach (var o in t)
                {
                    if (!o.isImediato && o.DataInicio < hoje || (o.DataFim.HasValue && o.DataFim < hoje))
                        throw new Exception("As datas devem ser maior ou igual a hoje.");

                    if (!o.isImediato && !o.DataFim.HasValue)
                        throw new Exception("A data fim não informada.");

                    if (!o.isImediato && o.DataFim.HasValue && o.DataInicio > o.DataFim)
                        throw new Exception($"A data final { o.DataFim.Value.ToString("dd/MM/yyyy hh:mm") } deve ser maior que a data inicial { o.DataInicio.ToString("dd/MM/yyyy hh:mm") }.");

                    if (o.isImediato)
                        o.DataInicio = DateTime.Now;
                }

                await new FornecedorRepository().AdicionarFornecedorServico(FornecedorID, t);
                
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

        [HttpPost("servico/update/")]
        public async Task<IActionResult> AtualizarFornecedorServico([FromBody] IEnumerable<FornecedorServicoModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");

                var hoje = DateTime.Now;

                foreach (var o in t)
                {
                    if (o.Id == 0)
                        throw new Exception("Agendamento sem identificador.");

                    if (!o.isImediato && o.DataInicio < hoje || (o.DataFim.HasValue && o.DataFim < hoje))
                        throw new Exception("As datas devem ser maior ou igual a hoje.");

                    if (!o.isImediato && !o.DataFim.HasValue)
                        throw new Exception("A data fim não informada.");

                    if (!o.isImediato && o.DataFim.HasValue && o.DataInicio > o.DataFim)
                        throw new Exception($"A data final { o.DataFim.Value.ToString("dd/MM/yyyy hh:mm") } deve ser maior que a data inicial { o.DataInicio.ToString("dd/MM/yyyy hh:mm") }.");
                    
                    var check = await new FornecedorRepository().isPodeAgendarInterrupcaoServico(FornecedorID, o.Id);

                    if (!check)
                        throw new Exception("Serviço suspenso por prazo indefinido. Reative para realizar um agendamento.");
                }

                await new FornecedorRepository().AtualizarFornecedorServico(FornecedorID, t);

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
        
        [HttpPost("servico/finalizar")]
        public async Task<IActionResult> FinalizarFornecedorServico([FromBody] IEnumerable<FornecedorServicoModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");
                
                foreach (var o in t)
                {
                    if (o.Id == 0)
                        throw new Exception("Agendamento sem identificador.");                    
                }

                await new FornecedorRepository().FinalizarFornecedorServico(FornecedorID, t);

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

        [HttpDelete("servico/delete")]
        public async Task<IActionResult> ExcluirFornecedorServico([FromBody] IEnumerable<FornecedorServicoModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };

            try
            {
                if (t == null || !t.Any())
                    throw new Exception("Requisição inválida");

                foreach (var o in t)
                {
                    if (o.Id == 0)
                        throw new Exception("Agendamento sem identificador.");
                }

                await new FornecedorRepository().ExcluirFornecedorServico(FornecedorID, t);

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

        [HttpGet("get/token/")]
        public IActionResult GetToken()
        {
            var id = FornecedorID;
            var audience = "FornecedorAPI";

            string guidapi = Guid.NewGuid().ToString();

            List<Claim> claim = new List<Claim>() { };
            claim.Add(new Claim("fornecedorid", id.ToString()));
            claim.Add(new Claim(JwtRegisteredClaimNames.Jti, guidapi));
            
            new FornecedorRepository().AtualizaAPIKey(new FornecedorModel()
            {
                FornecedorID = id,
                ApiKey = guidapi
            });

            var _options = new TokenProviderOptions
            {
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Util.GetKeyEncodingToken)), SecurityAlgorithms.HmacSha256)
            };
            
            var _jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claim,
                notBefore: DateTime.Now,
                signingCredentials: _options.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(_jwt);

            return Ok(new { Token = encodedJwt });
        }
    }
}