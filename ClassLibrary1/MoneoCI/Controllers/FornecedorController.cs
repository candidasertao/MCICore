using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Helpers;
using Atributos;
using DTO;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	class ComparativoFornecedor
	{
		[JsonProperty("envios", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<Consolidado> Envios { get; set; }
		public string Fornecedor { get; set; }
		public double TempoMedio { get; set; }
	}
	class Consolidado
	{
		public DateTime DataDia { get; set; }
		public double TempoMedio { get; set; }
		public IEnumerable<ConsolidadoModel> Envios { get; set; }
	}

	[Produces("application/json")]
	[Route("api/[controller]")]
	[Authorize]
	public class FornecedorController : ControllerBase, IControllers<FornecedorModel>
	{
		const int PAGINAID = 125;
		const int SUBPAGINAID = 0;

		readonly IRepository<FornecedorModel> repository = null;
		readonly SignInManager<IdentityUser> _signInManager;
		readonly UserManager<IdentityUser> _userManager;
		readonly RoleManager<IdentityRole> _roleManager;



		public FornecedorController(IRepository<FornecedorModel> repos,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager)
		{
			repository = repos;
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
		}

		public int ClienteID { get { return int.Parse(User.FindFirst(a => a.Type == "clienteid").Value); } }
                
        public int? UsuarioID
		{
			get
			{
				IdentityUser j = new IdentityUser();

				

				var result = User.Claims.Where(a => a.Type == "usuarioid");
				if (result.Count() > 0)
					return new Nullable<int>(int.Parse(result.ElementAt(0).Value));

				return new Nullable<int>();
			}
		}

		[HttpPost("comparativo/")]
		[NivelPermissao(1, PaginaID = 134, SubPaginaID = 99)]
		public async Task<IActionResult> ComparativoFornecedor([FromBody] ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{

				var dados = (await new ConsolidadoRepository().ComparativoFornecedor(c, ClienteID, UsuarioID)).ToList();

				if (dados == null || !dados.Any())
					return NoContent();

				var rnd = new Random();

				var diario = dados.GroupBy(a => a.DataDia, (a, m) => new { DataDia = a, enviadas = m.Sum(k => k.Enviadas) }).ToList();

				var _dados = dados.GroupBy(a => a.DataDia, (a, m) => new { datadia = a, campanhas = m }).OrderBy(a => a.datadia).ToList();

				var l = new List<ComparativoFornecedor>() { };

				foreach (var item in dados.GroupBy(a => a.FornecedorNome, (a, m) => new { fornecedor = a, envios = m }).ToList())
					l.Add(new ComparativoFornecedor()
					{
						Fornecedor = item.fornecedor,
						Envios = from a in _dados
								 join _b in item.envios.GroupBy(k => k.DataDia, (k, m) => new { datadia = k, envios = m }).ToList() on a.datadia equals _b.datadia into ps
								 from _b in ps.DefaultIfEmpty()
								 select new Consolidado
								 {
									 DataDia = a.datadia,
									 Envios = _b == null ? new ConsolidadoModel[] { } : _b.envios,
									 TempoMedio = _b == null ? default(double) : _b.envios.Average(k => k.Atraso.TotalSeconds)
								 }
					});


				b.Result = new
				{
					totalpordia = _dados,
					comparativo = l.Select(a => new
					{
						nome = a.Fornecedor,
						envios = new
						{
							totaldeenvios = a.Envios.Select(k => new
							{
								datadia = k.DataDia,
								enviadas = k.Envios.Sum(j => j.Enviadas)
							}),
							taxaentrega = a.Envios.Select(m => new
							{
								datadia = m.DataDia,
								taxaentrega = m.Envios.Sum(k => k.Entregues) / Enviadas(m.Envios.Sum(k => k.Enviadas)) * 100
							}),
							tempomedioentrega = a.Envios.Select(m => new
							{
								datadia = m.DataDia,
								taxa = m.TempoMedio
							}),
						},
						distribuicao = a.Envios.Select(m => new
						{
							datadia = m.DataDia,
							envios = m.Envios.Sum(k => (decimal)k.Enviadas) / Enviadas(diario.Where(k => k.DataDia == m.DataDia).Sum(k => k.enviadas)) * (decimal)100
						}),
						ratinhos = a.Envios.Select(m => new
						{
							datadia = m.DataDia,
							efetividade = rnd.Next(70, 100)
						})

					})};



				b.End = DateTime.Now;
				b.Itens = dados.Count;
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
		decimal Enviadas(int enviadas)
		{
			if (enviadas == 0)
				enviadas = 1;

			return (decimal)enviadas;
		}

		[AllowAnonymous]
		[HttpPut("add/")]
		public async Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<FornecedorModel> t)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<FornecedorModel>() { Start = DateTime.Now, Itens = t.Count() };

			try
			{
				var f = t.ElementAt(0);


				foreach (var item in f.Contatos)
					if (!string.IsNullOrEmpty(item.Email))
					{
						if (!Util.RegexEmail.IsMatch(item.Email))
							throw new Exception($"Email {item.Email} com formato inválido");

						if (_userManager.FindByEmailAsync(item.Email).GetAwaiter().GetResult() != null)
							throw new Exception($"Email {item.Email} já existente no sistema");
					}


				if (f.CPFCNPJ.Length == 11)
				{
					if (!Util.ValidarCPF(f.CPFCNPJ))
						throw new Exception($"Erro no CPF {f.CPFCNPJ} informado");
				}
				else if (f.CPFCNPJ.Length == 14)
					if (!Util.ValidaCnpj(f.CPFCNPJ))
						throw new Exception($"Erro no CNPJ {f.CPFCNPJ} informado");



				//checando existência de usuário
				var _userExistent = await _userManager.FindByNameAsync(f.CPFCNPJ);

				if (_userExistent != null)
					throw new Exception($"usuário com login: {f.CPFCNPJ} já existente");



				var senha = Uteis.GeraSenha();

				//montagem do usuário
				var user = new IdentityUser { UserName = f.CPFCNPJ, Email = f.Contatos.ElementAt(0).Email, LockoutEnabled = true, EmailConfirmed = true };
				var fornecedorid = await new FornecedorRepository().AddItem(f, 0, null);



				//criando o usuário
				var criacaoUser = await _userManager.CreateAsync(user, senha);


				if (criacaoUser.Succeeded)
				{
					user = await _userManager.FindByNameAsync(f.CPFCNPJ);

					//adicionando as claims
					var _resultClaim = await _userManager.AddClaimsAsync(user, new List<Claim> {
					new Claim(JwtClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "Fornecedor"),
					new Claim("fornecedorid", fornecedorid.ToString())
					});

					//adicionando o usuário a role
					await _userManager.AddToRoleAsync(user, "Fornecedor");

					await Util.SendEmailAsync(new EmailViewModel[] { new EmailViewModel(user.Email) }, "Novo Cadastro Moneo", Emails.NovoCadastro(), true, TipoEmail.NOVOCADASTRO);


					b.End = DateTime.Now;
					res = Ok(b);
				}
			}
			catch (Exception err)
			{
				b.End = DateTime.Now;
				b.Error = (err.InnerException ?? err).Message;
				res = BadRequest(b);
			}
			return res;
		}

		public Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<FornecedorModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<FornecedorModel> t)
		{
			throw new NotImplementedException();
		}


		[HttpGet("get/")]
		public async Task<IActionResult> GetAll()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var dados = await new ClienteRepository().FornecedoresCliente(new ClienteModel() { ClienteID = ClienteID }, UsuarioID);

				if (dados == null || !dados.Any())
					return NoContent();

				b.Result = dados.Select(a => new
				{
					nome = a.Nome,
					fornecedorid = a.FornecedorID,
					statusfornecedor = (byte)a.StatusFornecedor,
					distribuicao = a.Distribuicao,
					capacidade5min = a.Capacidade5M,
					capacidadetotal = a.CapacidadeTotal,
					statusoperacionalfornecedor = (byte)a.StatusOperacionalFornecedor
				});


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



		public Task<IActionResult> GetByIDAsync(int id)
		{
			throw new NotImplementedException();
		}

		[HttpGet("monitoria/")]
		[NivelPermissao(1, PaginaID = 130, SubPaginaID = 0)]
		public async Task<IActionResult> Monitoria()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var tuple = await new FornecedorRepository().Monitoria(ClienteID);

				var dados = tuple.Item1.ToList();
				var _agendamentofornecedor = tuple.Item2.ToList();


				if (dados == null || !dados.Any())
					return NoContent();

				var horarios = dados.GroupBy(a => a.DataEnviar.Hour, (a, m) => new { Horario = a, Total = m.Sum(k => k.Quantidade) }).OrderBy(a => a.Horario);




				var fornecedores = dados.GroupBy(k => new { k.Nome, k.FornecedorID }, (a, m) => new { a, m }).Where(a => a.m.Sum(l => l.Quantidade) > 0).Select(a => new
				{

					nome = new { nome = a.a.Nome, fornecedorid = a.a.FornecedorID },
					distribuicao = new
					{
						agendamentofornecedor = _agendamentofornecedor.Where(o => o.FornecedorID == a.a.FornecedorID).GroupBy(g => g.Hora, (g, o) => new
						{
							quantidade = o.Sum(x => x.Quantidade),
							hora = g
						}),
						atual = a.m.GroupBy(k => k.DataEnviar.Hour, (n, o) => new
						{
							Hora = n,
							total = o.Sum(k => k.Quantidade)
						})
						.Join(dados.GroupBy(n => n.DataEnviar.Hour, (n, o) => new
						{
							hora = n,
							total = o.Sum(k => k.Quantidade)
						}), n => n.Hora, o => o.hora, (o, n) => new { hora = o.Hora, totalhora = o.total == 0 ? 0 : Math.Round(((decimal)o.total / (decimal)n.total) * 100) }).OrderByDescending(k => k.hora).Select(s => s.totalhora).First(),

						maxima = a.m.GroupBy(k => k.DataEnviar.Hour, (n, o) => new
						{
							Hora = n,
							total = o.Sum(k => k.Quantidade)
						})
						.Join(dados.GroupBy(n => n.DataEnviar.Hour, (n, o) => new
						{
							hora = n,
							total = o.Sum(k => k.Quantidade)
						}), n => n.Hora, o => o.hora, (o, n) => new { totalhora = o.total == 0 ? 0 : Math.Round(((decimal)o.total / (decimal)n.total) * 100) }).Max(k => k.totalhora),
						minima = a.m.GroupBy(k => k.DataEnviar.Hour, (n, o) => new
						{
							Hora = n,
							Total = o.Sum(k => k.Quantidade)
						})
						.Join(dados.GroupBy(n => n.DataEnviar.Hour, (n, o) => new
						{
							Hora = n,
							Total = o.Sum(k => k.Quantidade)
						}), n => n.Hora, o => o.Hora, (o, n) => new
						{
							TotalHora = o.Total == 0 ? 0 : Math.Round(((decimal)o.Total / (decimal)n.Total) * 100)
						}).Min(k => k.TotalHora),

						//tudo que for menor igual a 8
						inicial = (decimal)a.m.Where(k => k.DataEnviar.Hour <= 8).Sum(k => k.Quantidade) == 0 ? 0 : (decimal)a.m.Where(k => k.DataEnviar.Hour <= 8).Sum(k => k.Quantidade) / (decimal)dados.Where(k => k.DataEnviar.Hour <= 8).Sum(k => k.Quantidade) * 100,

						horahora = a.m.GroupBy(k => k.DataEnviar.Hour, (n, o) => new
						{
							hora = n,
							total = o.Sum(k => k.Quantidade),
							share = (decimal)o.Sum(k => k.Quantidade) == 0 ? 0 : (decimal)o.Sum(k => k.Quantidade) / (decimal)horarios.Where(k => k.Horario == n).ElementAt(0).Total * 100
						}),

						//media de todo volume atual de envios do dia
						distribuicao = _agendamentofornecedor.Where(k => k.FornecedorID == a.a.FornecedorID).Select(k => k.Quantidade).Sum() == 0 ? 0 : (decimal)((_agendamentofornecedor.Where(k => k.FornecedorID == a.a.FornecedorID).Select(k => k.Quantidade).Sum() * 100) / (decimal)_agendamentofornecedor.Select(k => k.Quantidade).Sum())
					},
					envios = new
					{
						agendados = a.m.Where(k => k.StatusEnvio == 0).Sum(k => k.Quantidade),
						eficiencia = 0,
						entrega = TimeSpan.FromSeconds(a.m.Where(k => k.Atraso.HasValue).Average(k => k.Atraso.Value)),
						capacidade = a.m.ElementAt(0).Capacidade,
						consumo = a.m.Sum(k => k.Quantidade),
						consumoporcentagem = (decimal)a.m.Where(k => k.StatusEnvio == 2).Sum(k => k.Quantidade) == 0 ? 0 : (decimal)a.m.Where(k => k.StatusEnvio == 2).Sum(k => k.Quantidade) / (decimal)(a.m.Sum(k => k.Quantidade)) * 100,
						horahora = _agendamentofornecedor.Where(o => o.FornecedorID == a.a.FornecedorID).GroupBy(g => g.Hora, (g, o) => new
						{
							hora = g,
							total = a.m.Sum(k => k.Quantidade),
							agendados = o.Sum(k => k.Quantidade),
							enviados = a.m.Where(k => k.StatusEnvio == 2 && k.DataEnviar.Hour == g).Sum(k => k.Quantidade)
						})
					}
				});

				b.Result = fornecedores;
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

		[HttpGet("dashboard/")]
		public async Task<IActionResult> DashBoard()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<FornecedorModel>>() { Start = DateTime.Now };
			try
			{

				b.Result = await new FornecedorRepository().DashBoard(ClienteID, UsuarioID);

				if (!b.Result.Any())
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

		[HttpGet("get/itens")]
		[NivelPermissao(1, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> GetAllExistentes()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<bool>() { Start = DateTime.Now };

			try
			{
				var dados = await new FornecedorRepository().FornecedoresCliente(ClienteID);
				b.Result = dados > 0;
				b.End = DateTime.Now;
				b.Itens = dados;
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

		[HttpPost("relatorio/")]
		[NivelPermissao(1, PaginaID = 134, SubPaginaID = 98)]
		public async Task<IActionResult> Relatorio([FromBody]ConsolidadoModel c)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var dados = await new FornecedorRepository().Relatorio(c, ClienteID, UsuarioID);

				if (!dados.item1.Any() || !dados.item3.Any())
					return NoContent();


				b.Result = new
				{
					detalhamento = new
					{
						atraso = dados.f.Atraso,
						totalenvios = dados.Item1.Sum(a => a.Enviadas),
						enviados = dados.Item1.Sum(a => a.Enviados),
						entregues = dados.Item1.Sum(a => a.Entregues),
						expirada = dados.Item1.Sum(a => a.Expiradas),
						excluidos = dados.Item1.Sum(a => a.Excluidas),
						erros = dados.Item1.Sum(a => a.Erros),
						grafico = dados.Item1.GroupBy(a => a.DataDia, (a, m) => new { datadia = a, enviados = m.Sum(n => n.Enviados) }).OrderBy(a => a.datadia)
					},
					retornos = new
					{
						classificacoes = dados.Item2.GroupBy(a => new { a.ClassificacaoID, a.Classificacao }, (a, m) => new { classificacao = a.Classificacao, quantidade = m.Sum(k => k.Quantidade) }).OrderByDescending(k => k.quantidade),
						registros = dados.Item2.Sum(a => a.Quantidade)
					},
					discadora = new
					{
						ligacoes = 10000,
						emespera = 4000,
						fechamento = 9000
					},
					ratinhosmoneo = new
					{
						esperados = 1000000,
						entregues = 999878
					},

					distribuicao = new
					{

						distribuicao = dados.f,
						enviadostotal = dados.Item3.Select(a => new
						{
							enviados = a.Enviados,
							datadia = a.DataDia
						}),
						enviadosfornecedor = dados.Item1.GroupBy(a => a.DataDia, (a, m) => new
						{
							enviados = m.Sum(k => k.Enviados),
							datadia = a
						}),
						media = from a in dados.Item3
								join m in dados.Item1.GroupBy(a => a.DataDia, (a, m) => new
								{
									enviados = m.Sum(k => k.Enviados),
									datadia = a
								}) on a.DataDia equals m.datadia into ps
								from m in ps.DefaultIfEmpty()
								orderby a.DataDia
								select new
								{
									datadia = a.DataDia,
									mediauso = m == null ? 0 : m.enviados == 0 && a.Enviados == 0 ? 0 : (decimal)m.enviados / (decimal)a.Enviados * 100
								},
						mediaunica = (from x in dados.Item3
									  join m in dados.Item1.GroupBy(a => a.DataDia, (a, m) => new
									  {
										  enviados = m.Sum(k => k.Enviados),
										  datadia = a
									  }) on x.DataDia equals m.datadia into ps
									  from m in ps.DefaultIfEmpty()
									  select new
									  {
										  datadia = x.DataDia,
										  mediauso = m == null ? 0 : m.enviados == 0 && x.Enviados == 0 ? 0 : (decimal)m.enviados / (decimal)x.Enviados * 100
									  }).Average(a => a.mediauso)

					}

				};
				b.End = DateTime.Now;
				b.Itens = dados.Item1.Count() + dados.Item2.Count();
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

		[HttpPost("redistribuilotes/{carteiraid:int}/{arquivoid:int}")]
		public async Task<IActionResult> RedistribuiLotesFornecedores([FromBody]IEnumerable<FornecedorMinModel> f, int arquivoid, int carteiraid)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<int>() { Start = DateTime.Now };
			try
			{
				if (f.Sum(a => a.Distribuicao) != 100)
					throw new Exception("A soma da distribuição não corresponde a 100%");


				b.Result = await new FornecedorRepository().RedistribuiLotes(f, arquivoid, carteiraid, ClienteID, UsuarioID);
				b.End = DateTime.Now;
				b.Itens = b.Result;
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

		[HttpPost("atualizafornecedor/")]
		[NivelPermissao(2, PaginaID = PAGINAID, SubPaginaID = SUBPAGINAID)]
		public async Task<IActionResult> AtualizaStatusFornecedorCliente([FromBody] IEnumerable<FornecedorModel> f)
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<FornecedorModel>() { Start = DateTime.Now, Itens = f.Count() };

			try
			{

				await new FornecedorRepository().AtualizaStatusFornecedorCliente(f, ClienteID);
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

		[HttpGet("cadastrocliente/")]
		[NivelPermissao(1, PaginaID = 125, SubPaginaID = 0)]
        //[NivelPermissao(1, PaginaID = 134, SubPaginaID = 98)]
        public async Task<IActionResult> Fornececedorescadastro()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<dynamic>() { Start = DateTime.Now };
			try
			{
				var dados = await new FornecedorRepository().FornececedoresCadastro(ClienteID);


				if (dados == null || !dados.Any())
					return NoContent();



				b.Result = new
				{
					ativos = dados.Where(a => a.StatusFornecedor == StatusFornecedorEnums.ATIVO),
					inativo = dados.Where(a => a.StatusFornecedor == StatusFornecedorEnums.INATIVO),
					pendente = dados.Where(a => a.StatusFornecedor == StatusFornecedorEnums.PENDENTE || a.StatusFornecedor == StatusFornecedorEnums.INTEGRADO),
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

		[HttpGet("redistribuicao/")]
		public async Task<IActionResult> Redistribuicao()
		{
			IActionResult res = null;
			var b = new BaseEntityDTO<IEnumerable<FornecedorModel>>() { Start = DateTime.Now };
			try
			{


				b.Result = await new FornecedorRepository().Redistribuicao(ClienteID);

				if (b.Result == null || !b.Result.Any())
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

		public Task<IActionResult> Search(string s)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginado(int pagesize, int pagina)
		{
			throw new NotImplementedException();
		}

		public Task<IActionResult> GetAllPaginadoAsync([FromBody] FornecedorModel t)
		{
			throw new NotImplementedException();
		}
	}
}
