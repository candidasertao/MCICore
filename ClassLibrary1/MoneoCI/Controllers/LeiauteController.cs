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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize(Roles = "Usuario,Cliente,AdminOnly")]
    public class LeiauteController : ControllerBase, IControllers<LeiauteModel>
    {
        const int PAGINAID = 124;
        const int SUBPAGINAID = 0;

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

        readonly IRepository<LeiauteModel> repository = null;

        public LeiauteController(IRepository<LeiauteModel> repos)
        {
            repository = repos;
        }

        Regex regVariavel = new Regex("#[0-9A-Za-z]+$", RegexOptions.Compiled);

        [HttpPut("add/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<LeiauteModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<LeiauteModel>() { Start = DateTime.Now, Itens = t.Count() };

            try
            {
                int contador = 0;
                foreach (var item in t)
                {
                    foreach (var _i in item.LeiauteVariaveis)
                    {
                        _i.Variavel = string.IsNullOrEmpty(_i.Variavel) ? $"#ignorar{contador++}" : _i.Variavel;


                        if (!regVariavel.IsMatch(_i.Variavel))
                            throw new Exception($"Variável {_i.Variavel} inválida");
                    }

                    if (!item.LeiauteVariaveis.Any(a => a.Variavel == "#numero"))
                        throw new Exception("Variável #numero não constante na listagem");
                }


                await repository.Add(t, ClienteID, UsuarioID);
                b.End = DateTime.Now;
                res = Ok(b);
            }
            catch (Exception err)
            {
                b.End = DateTime.Now;
                b.Error = (err.InnerException ?? err).Message;


                if (b.Error.Contains("IX_LAYOUT_UNIQUE_CLIENTEID_NOM"))
                    b.Error = $"Padrão com o nome {t.ElementAt(0).Nome} já existente no sistema";
                if (b.Error.Contains("IX_VARIAVEL_LAYOUT"))
                    b.Error = $"Item de variável já existente";
                else if (b.Error.Contains("IX_IDCOLUNA_LAYOUT"))
                    b.Error = "ID de coluna duplicado";

                res = BadRequest(b);
            }
            return res;
        }
        [HttpPost("definipadrao/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> DefiniPadraoAsync([FromBody] LeiauteModel t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<LeiauteModel>() { Start = DateTime.Now, Itens = 1 };


            try
            {
                await new LeiauteRepository().DefiniPadraoAsync(t, ClienteID, UsuarioID);
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

        [HttpPost("update/")]
        [NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<LeiauteModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<LeiauteModel>() { Start = DateTime.Now, Itens = t.Count() };


            try
            {

                int contador = 0;
                foreach (var item in t)
                {
                    foreach (var _i in item.LeiauteVariaveis)
                    {
                        _i.Variavel = string.IsNullOrEmpty(_i.Variavel) ? $"#ignorar{contador++}" : _i.Variavel;

                        if (!regVariavel.IsMatch(_i.Variavel))
                            throw new Exception($"Variável {_i.Variavel} inválida");
                    }

                    if (!item.LeiauteVariaveis.Any(a => a.Variavel == "#numero"))
                        throw new Exception("Variável #numero não constante na listagem");
                }


                await repository.Update(t, ClienteID, null);
                b.End = DateTime.Now;
                res = Ok(b);
            }
            catch (Exception err)
            {
                b.End = DateTime.Now;
                b.Error = (err.InnerException ?? err).Message;

                if (b.Error.Contains("IX_VARIAVEL_LAYOUT"))
                    b.Error = $"Número já existente na blacklist: {t.ElementAt(0).Nome}";
                else if (b.Error.Contains("IX_IDCOLUNA_LAYOUT"))
                    b.Error = "ID de coluna duplicado";


                res = BadRequest(b);
            }
            return res;
        }



        [HttpDelete("delete/")]
        [NivelPermissao(3, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<LeiauteModel> t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<LeiauteModel>() { Start = DateTime.Now, Itens = t.Count() };


            try
            {
                await repository.Remove(t, ClienteID, UsuarioID);
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

        public Task<IActionResult> GetAll()
        {
            throw new NotImplementedException();
        }

        [HttpPost("get/p/")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetAllPaginadoAsync([FromBody] LeiauteModel t)
        {
            IActionResult res = null;
            var b = new BaseEntityDTO<IEnumerable<LeiauteModel>>() { Start = DateTime.Now };

            try
            {
                if (t.Registros == 0)
                    throw new Exception("o campo Registro precisa de valor maior que 0");

                t.Cliente = new ClienteModel() { ClienteID = ClienteID };

                var leiautes = await repository.GetAllPaginado(t, UsuarioID);

                if (leiautes == null || !leiautes.Any())
                    return NoContent();


                if (leiautes.Any())
                {
                    if (t.PaginaAtual.HasValue)
                    {
                        if (t.PaginaAtual.Value == 0)
                            t.PaginaAtual = 1;
                    }
                    else
                        t.PaginaAtual = 1;

                    b.Result = leiautes.Select(a => new LeiauteModel()
                    {
                        LeiauteID = a.LeiauteID,
                        Nome = a.Nome,
                        Data = a.Data,
                        Visivel = a.Visivel,
                        Padrao = a.Padrao,
                        IsEspecial = a.IsEspecial,
                        LeiauteVariaveis = a.LeiauteVariaveis,
                        Registros = leiautes.Count(),
                        Paginas = leiautes.Count() / t.Registros
                    })
                    .Skip((t.PaginaAtual.Value - 1) * t.Registros)
                    .Take(t.Registros);
                }
                else
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

        [HttpGet("get/{id:int}")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> GetByIDAsync(int id)
        {
            IActionResult res = null;

            var b = new BaseEntityDTO<LeiauteModel>() { Start = DateTime.Now };

            try
            {
                b.Result = await repository.FindById(new LeiauteModel() { LeiauteID = id, Cliente = new ClienteModel() { ClienteID = ClienteID } }, UsuarioID);
                if (b.Result != null)
                    b.Itens = 1;

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
        [HttpGet("get/l")]
        [NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
        public async Task<IActionResult> Leiautes()
        {
            IActionResult res = null;

            var b = new BaseEntityDTO<IEnumerable<LeiauteModel>>() { Start = DateTime.Now };

            try
            {
                b.Result = await new LeiauteRepository().ListaLayouts(ClienteID, UsuarioID);

                if (!b.Result.Any())
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



        public Task<IActionResult> Search(string s)
        {
            throw new NotImplementedException();
        }
    }
}
