using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Atributos;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
    [Authorize(Roles = "Usuario,Cliente,AdminOnly")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class RetornoController : ControllerBase, IControllers<RetornoModel>
    {
        public int ClienteID { get { return int.Parse(User.FindFirst(a => a.Type == "clienteid").Value); } }

        //public int ClienteID { get { return 1; } }

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

        readonly IRepository<RetornoModel> repository = null;

        public RetornoController(IRepository<RetornoModel> repos)
        {
            repository = repos;
        }


        public Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<RetornoModel> t)
        {
            throw new NotImplementedException();
        }

        [HttpPost("update/")]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<RetornoModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<IEnumerable<RetornoModel>>() { Start = DateTime.Now };
            try
            {
                await repository.Update(t, ClienteID, UsuarioID);
                b.End = DateTime.Now;
                b.Itens = t.Count();
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

        public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<RetornoModel> t)
        {
            throw new NotImplementedException();
        }

        [HttpGet("dashboard/")]
        public async Task<IActionResult> DashBoard()
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
            try
            {
                var dados = await new RetornoRepository().DashBoard(ClienteID);
                
                if (dados == null || !dados.Any())
                    return NoContent();

                b.Result = new
                {
                    classificacoes = dados.Select(a => new { Classificacao = a.ClassificacaoIOPeople, Quantidade = a.Quantidade }),
                    registros = dados.Sum(k => k.Quantidade)
                };

                b.End = DateTime.Now;
                b.Itens = dados.Count();
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
        [HttpPost("get/p")]
        [NivelPermissao(1, PaginaID = 131, SubPaginaID = 92)]
        public async Task<IActionResult> GetAll([FromBody]RetornoModel r)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
            try
            {
                r.ClienteID = ClienteID;

                var dados = await new RetornoRepository().GetAllTuple(r, UsuarioID);

                if (!dados.Item1.Any() && !dados.Item2.Any())
                    return NoContent();


                b.Result = new
                {
                    paginas = dados.Item2.Sum(a => a.Quantidade) / r.Registros,
                    registros = dados.Item2.Sum(a => a.Quantidade),
                    datainicial = r.DataInicial,
                    classificacoes = dados.Item2.OrderByDescending(a => a.Quantidade).Take(3),
                    datafinal = r.DataFinal,

                    retornos = dados.Item1.Select(n => new
                    {
                        celular = n.Celular,
                        classificacaoio = n.ClassificacaoIOPeople,
                        score = n.Score,
                        retornocliente = n.RetornoCliente,
                        dataretorno = n.DataRetorno,
                        idcliente = n.IDCliente,
                        texto = n.Texto,
                        datadia = n.DataRetorno.Date,
                        codigo = n.Codigo
                    })
                };

                b.Itens = dados.Item1.Sum(a => a.Quantidade);
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

        [HttpGet("classificacao/")]
        public async Task<IActionResult> Classificacao()
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<IEnumerable<ClassificacaoIOModel>>() { Start = DateTime.Now };
            try
            {
                
                b.Result = await new RetornoRepository().ClassificacaoIOPeople();
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

        [HttpPost("get/")]
        [NivelPermissao(1, PaginaID = 131, SubPaginaID = 92)]
        public async Task<IActionResult> GetAllPaginadoAsync([FromBody]RetornoModel r)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
            try
            {
                r.ClienteID = ClienteID;



                var dados = await new RetornoRepository().GetAllTuple(r, UsuarioID);

                if (!dados.Item1.Any() && !dados.Item2.Any())
                    return NoContent();


                b.Result = new
                {
                    paginas = dados.Item2.Sum(a => a.Quantidade) / r.Registros,
                    registros = dados.Item2.Sum(a => a.Quantidade),
                    datainicial = r.DataInicial,
                    classificacoes = dados.Item2.OrderByDescending(a => a.Quantidade).Take(3),
                    datafinal = r.DataFinal,
                    retornos = dados.Item1.Select(n => new
                    {
                        celular = n.Celular,
                        classificacaoio = n.ClassificacaoIOPeople,
                        score = n.Score,
                        retornocliente = n.RetornoCliente,
                        dataretorno = n.DataRetorno,
                        idcliente = n.IDCliente,
                        texto = n.Texto,
                        datadia = n.DataRetorno.Date,
                        codigo = n.Codigo,
                        carteira=n.Carteira,
                        arquivo=n.Arquivo
                        
                        
                    })
                };

                b.Itens = dados.Item1.Sum(a => a.Quantidade);
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

        [NivelPermissao(1, PaginaID = 131, SubPaginaID = 93)]
        public async Task<IActionResult> GetAll()
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<IEnumerable<RetornoModel>>() { Start = DateTime.Now };
            try
            {
                b.Result = await repository.GetAll(new RetornoModel() { ClienteID = ClienteID }, UsuarioID);

                if (b.Result != null || !b.Result.Any())
                    return NoContent();

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


    }
}
