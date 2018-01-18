using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
	public static class Emails
	{

		/// <summary>
		/// Gera o html do e-mail a ser enviado ao cliente
		/// </summary>
		/// <param name="campanhasFinal"> Lista contendo as campanhas</param>
		/// <returns></returns>
		public static string EmailGestoresEnvio(IEnumerable<CampanhaModel> campanhasFinal)
		{
			var sb = new StringBuilder();

			sb.Append(@"<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
										<tr>
											<td valign=""top"" style="" padding-bottom:5rem;"">
												<!-- head -->
												<table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
													<tr>
														<td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo"" #logo></a></td>
													</tr>
												</table>");

			foreach (var item in campanhasFinal.GroupBy(k => k.Carteira.Carteira, (k, l) => new { Carteira = k, Campanhas = l })) //agrupando por carteira
			{
				sb.AppendFormat(@"<h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">{0}</h1>", item.Carteira);

				var arquivosGroup = item.Campanhas.GroupBy(k => k.Arquivo.Arquivo, (k, l) => new { Arquivo = k, Camps = l });

				foreach (var _item in arquivosGroup) //agrupando por arquivo
				{
					sb.AppendFormat(@"<!-- repeat arquivo --><h2 style=""margin: 24px; font-weight: 200; color:rgba(154,154,154,1); font-size:16px;""><img style=""vertical-align: middle;"" #arquivo><span style=""margin-left: 24px"">{0}</span></h2>", _item.Arquivo);
					sb.Append(@"<!-- content --><div style=""padding: 24px; text-align: center;"">
																						<table width=""100%"" style=""border-collapse: collapse; color: #7f7f7f; text-align: left;"">
																							<!-- table head -->
																							<tr style=""border-bottom:1px solid rgba(38,50,56,.15); font-size:24px; color:#ff8a09; font-weight:400;"">
																								<td style=""font-size:50%; padding:12px 0 12px 50px;"">Lote</td>
																								<td style=""font-size:50%; padding:12px 0 12px 50px;"">Data</td>
																								<td style=""font-size:50%; padding:12px 0 12px 50px;"">Hora</td>
																								<td style=""font-size:50%; padding:12px 50px 12px 0; text-align:right;"">Quantidade</td>
																							</tr>");

					foreach (var lotes in _item.Camps
					.GroupBy(k => k.DataEnviar, (k, l) => new { DataEnviar = k, Lotes = l })
					.OrderBy(k => k.DataEnviar).Select((k, l) => new { DataEnviar = k.DataEnviar, Lote = l+1, Lotes = k.Lotes })) //agrupando por lotes
					{
						sb.AppendFormat(@"<!-- tr body / repeat carteiras --><tr style=""font-size:14px; border-bottom:1px solid rgba(38,50,56,.15)"">
																								<td style=""padding:12px 0 12px 50px;"">{2:N0}</td>
																								<td style=""padding:12px 0 12px 50px;"">{0:dd/MM/yyyy}</td>
																								<td style=""padding:12px 0 12px 50px;"">{0:HH:mm}</td>
																								<td style=""padding:12px 50px 12px 0; text-align:right;"">{1:N0}</td>
																							</tr>", lotes.DataEnviar, lotes.Lotes.Count(), lotes.Lote);
					}
					sb.AppendFormat(@"<tr style=""font-size:14px; color:#ff8a09;"">
                                <td style=""padding:12px 0 12px 50px;"" colspan=""3"">TOTAL</td>
                                <td style=""padding:12px 50px 12px 0; text-align:right;"">{0:N0}</td>
                            </tr>", _item.Camps.Count());
					sb.Append(@"</table></div>");
				}


			}




			sb.Append(@"</td></tr></table>");

			return sb.ToString();
		}

		/// <summary>
		/// Email de requisição de relatório de carteira
		/// </summary>
		/// <param name="url">url que aponta pra download da carteira</param>
		/// <returns></returns>
		public static string EmailRequisicaoCarteira(string url)
		{
			return string.Format(@"
			<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style=""padding-bottom:5rem; color:rgba(99,99,99,1);"">
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td></td>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo"" #logo></a></td>
                        </tr>
                    </table>
                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">O processamento do seu relatório foi finalizado.</h1>
                    <div style=""padding: 0 24px;"">
                        <p>Você pode acessá-lo <b>através desse link</b>.<br>
                        <a href=""{0}"" style=""color: #ff6f00"" target=""_blank"">{0}</a></p>
                    </div>
                </td>
            </tr>
        </table>", url);
		}

		/// <summary>
		/// envia o HTML de novo usuário
		/// </summary>
		/// <param name="login">login do usuário</param>
		/// <param name="ident">URL de identificação</param>
		/// <returns></returns>
		public static string NovoUsuario(string login, string ident)
		{
			return string.Format(@"<DOCUMENT html>
			<html>
				<head>
					<meta charset=""utf-8"">
				</head>
				<body style=""height: 100%; background-color: #efefef; font-family: Helvetica, Tahoma, Verdana, Arial, sans-serif"">
				<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style=""padding-bottom:5rem; color:rgba(99,99,99,1);"">
                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td></td>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo"" #logo></a></td>
                        </tr>
                    </table>
                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Olá, seja bem-vindo.</h1>

                    <!-- content -->
                    <div style=""padding: 0 24px;"">
                        <p>Seu login de acesso é: <span style=""color: #ff6f00;"">{1}</span></p>
                        <p>Acesse o link abaixo para <b>criar sua senha de acesso.</b><br>
                        <a href=""{0}"" style=""color: #ff6f00"" target=""_blank"">{0}</a></p>
                    </div>
                </td>
            </tr>
        </table></body></html>", ident, login);
		}

		public static string NovoCadastro()
		{
			return @"<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style=""padding-bottom:5rem; color:rgba(99,99,99,1);"">

                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td></td>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo"" #logo></a></td>
                        </tr>
                    </table>

                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Obrigado por seu interesse</h1>

                    <!-- content -->
                    <div style=""padding: 0 24px;"">
                        <p>Logo entraremos em contato para habilitá-lo.</p>
                    </div>

                </td>
            </tr>
        </table>";

		}


		public static string NotificaLimiteCarteira(string carteira, decimal percentual)
		{
			return string.Format(@"<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style=""padding-bottom:5rem; color:rgba(99,99,99,1);"">
                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td></td>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo""  #logo></a></td>
                        </tr>
                    </table>
                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Notificação de limite de carteira</h1>
                    <!-- content -->
                    <div style=""padding: 0 24px;"">
                        <p>A carteira <b style=""color: #ff6f00;"">{0}</b> chegou ao limite de <b>{1:N0}%</b>.</p>
                    </div>
                </td>
            </tr>
        </table>", carteira, percentual);

		}

		/// <summary>
		/// Redefine a senha do usuário
		/// </summary>
		/// <param name="_url">URL de acesso</param>
		/// <returns></returns>
		public static string RedefinicaoSenha(string _url)
		{
			return string.Format(@"
				<table width=""620"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style=""padding-bottom:5rem; color:rgba(99,99,99,1);"">
                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td></td>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo"" #logo></a></td>
                        </tr>
                    </table>
                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Foi solicitado a redefinição de acesso.</h1>
                    <!-- content -->
                    <div style=""padding: 0 24px;"">
                        <p>Acesse o link abaixo para cadastrar sua nova senha.<br>
                        <a href=""{0}"" style=""color: #ff6f00"" target=""_blank"">{0}</a></p>
                    </div>
                </td>
            </tr>
        </table>", _url);
		}

		static bool IsPalindrome(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return false;
			bool LocalIsPalindrome(string target)
			{
				target = target.Trim();  // Start by removing any surrounding whitespace.
				if (target.Length <= 1) return true;
				else
				{
					return char.ToLower(target[0]) ==
					  char.ToLower(target[target.Length - 1]) &&
					  LocalIsPalindrome(
						target.Substring(1, target.Length - 2));
				}
			}
			return LocalIsPalindrome(text);
		}

		public static string AnaliticoConsolidado(IEnumerable<ConsolidadoModel> c)
		{
			var sb = new StringBuilder();
			
			sb.Append(@"<table width=""960"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style="" padding-bottom:5rem;"">
                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo""  #logo></a></td>
                        </tr>
                    </table>

                    <h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Relatório de Entrega</h1>

                    <!-- content | entrega -->
                    <div style=""padding: 0 24px 24px; text-align: center;"">

                        <table width=""100%"" style=""border-collapse: collapse; color: #7f7f7f; text-align: left;"">
                            <!-- table head -->
                            <tr style=""border-bottom:1px solid rgba(38,50,56,.15); font-size:24px; color:#ff8a09; font-weight:400;"">
                                <td style=""font-size:50%; padding:12px 6px 12px 0;"">Carteira</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Entregues</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Enviados</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Expirados</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Excluídos</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Erros</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Suspensos</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Cancelados</td>
                                <td style=""font-size:50%; padding:12px 6px; text-align:right;"">Inválidos</td>
                                <td style=""font-size:50%; padding:12px 0 12px 6px; text-align:right;"">Total</td>
                            </tr>");

			foreach (var item in c.GroupBy(a => a.Carteira, (a, b) => new { Carteira = a, Consolidados = b }))
			{
				sb.AppendFormat(@"<!-- tr body / repeat carteiras -->
                            <tr style=""font-size:13px; border-bottom:1px solid rgba(38,50,56,.15)"">
                                <td style=""padding:12px 6px 12px 0; display: block; width:200px; overflow: hidden; white-space: nowrap; text-overflow: ellipsis;"">{0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{1:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{2:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{3:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{4:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{5:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{6:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{7:N0}</td>
                                <td style=""padding:12px 6px; text-align:right;"">{8:N0}</td>
                                <td style=""padding:12px 0 12px 6px; text-align:right;"">{9:N0}</td>
                            </tr>", item.Carteira,
							item.Consolidados.Sum(a => a.Entregues),
							item.Consolidados.Sum(a => a.Enviados),
							item.Consolidados.Sum(a => a.Expiradas),
							item.Consolidados.Sum(a => a.Excluidas),
							item.Consolidados.Sum(a => a.Erros),
							item.Consolidados.Sum(a => a.Suspensos),
							item.Consolidados.Sum(a => a.Canceladas),
							item.Consolidados.Sum(a => a.Invalidos),
							item.Consolidados.Sum(a => a.Entregues) + item.Consolidados.Sum(a => a.Enviados) + item.Consolidados.Sum(a => a.Expiradas) + item.Consolidados.Sum(a => a.Excluidas) + item.Consolidados.Sum(a => a.Erros) + item.Consolidados.Sum(a => a.Suspensos) + item.Consolidados.Sum(a => a.Canceladas) + item.Consolidados.Sum(a => a.Invalidos));
			}

			//sb.AppendFormat(@"
   //                         <!-- tr foot / total -->
   //                         <tr style=""font-size:13px; color:#ff8a09; border-bottom:1px solid rgba(38,50,56,.15)"">
   //                             <td style=""padding:12px 6px 12px 0;""></td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{0:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{1:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{2:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{3:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{4:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{5:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{6:N0}</td>
   //                             <td style=""padding:12px 6px; text-align:right;"">{7:N0}</td>
   //                             <td style=""padding:12px 0 12px 6px; text-align:right;"">{8:N0}</td>
   //                         </tr>
   //                     </table>",
			//						c.Sum(a => a.Entregues),
			//						c.Sum(a => a.Enviados),
			//						c.Sum(a => a.Expiradas),
			//						c.Sum(a => a.Excluidas),
			//						c.Sum(a => a.Erros),
			//						c.Sum(a => a.Suspensos),
			//						c.Sum(a => a.Canceladas),
			//						c.Sum(a => a.Invalidos),
			//						c.Sum(a => a.Entregues) + c.Sum(a => a.Enviados) + c.Sum(a => a.Expiradas) + c.Sum(a => a.Excluidas) + c.Sum(a => a.Erros) + c.Sum(a => a.Suspensos) + c.Sum(a => a.Canceladas) + c.Sum(a => a.Invalidos));

			sb.Append(@"</div></td></tr></table>");

			return sb.ToString();
		}

		public static async Task<Tuple<string, byte[]>> AnaliticoRetorno(IEnumerable<ConsolidadoModel> c)
		{
			//(string, string, string) pathData = (DirectoryName: @"\\test\unc\path\to", FileName: "something", Extension: ".ext");


			#region montagem do gráfico
			var dados = c.GroupBy(a => a.Hora, (a, b) => new
			{
				Hora = a,
				Campanhas = b,
				Total = b.Sum(j=>j.Recebidas)
			}).OrderBy(a => a.Hora);


			//71997214831
			var minmax = $"0,{dados.Max(a => a.Total)}";

			int valorInicial = dados.Max(a => a.Total);
			int fator = Convert.ToInt32(valorInicial * 0.2);

			var dados1 = Enumerable.Range(7, 15);
			var dados2 = dados;

			var result = (from a in dados1
						  join b in dados2 on a equals b.Hora into ps
						  from b in ps.DefaultIfEmpty()
						  select new { Dado = b == null ? 0 : b.Total })
						  .Select(a => a.Dado.ToString()).Aggregate((a, b) => $"{a},{b}");


			string _url = string.Format("https://chart.googleapis.com/chart?cht=bvs&chs=880x220&chco=ff8a09&chxt=x,y&chbh=30,25,50&chxr=1,0,{0}&chds={1}&chl=7h|8h|9h|10h|11h|12h|13h|14h|15h|16h|17h|18h|19h|20h|21h&chd=t:{2}", valorInicial - (valorInicial % 1000), minmax, result);

			WebRequest myReq = WebRequest.Create(_url);

			myReq.Method = "GET";
			myReq.ContentType = "image/png";

			byte[] _b = new byte[] { };

			using (var myResp = await myReq.GetResponseAsync())
			{
				using (var mem = new MemoryStream())
				{
					await myResp
					.GetResponseStream()
					.CopyToAsync(mem);
					_b = mem.ToArray();
				}
			}
			#endregion

			var sb = new StringBuilder();


			sb.Append(@"<table width=""960"" style=""margin: 0 auto; background-color: #fff; border-collapse:collapse"" border=""0"">
            <tr>
                <td valign=""top"" style="" padding-bottom:5rem;"">
                    <!-- head -->
                    <table width=""100%"" height=""60"" style=""border-collapse:collapse;"">
                        <tr>
                            <td style=""text-align:right; height: 60px; padding: 0 24px;""><a href=""http://moneosi.com.br"" target=""_blank""><img style=""margin: 20px 0 20px;"" alt=""Moneo""  #logo></a></td>
                        </tr>
                    </table><h1 style=""margin: 24px; font-weight: 200; color:rgba(99,99,99,1); font-size:24px;"">Relatório de Retorno</h1>

                    <!-- content | retorno -->
                    <div style=""padding: 24px; text-align: center;"">
                        
                        <img style=""margin-bottom: 24px;"" alt=""Relatório de retorno"" #grafico"">

                        <table width=""100%"" style=""border-collapse: collapse; color: #7f7f7f; text-align: left;"">
                            <!-- table head -->
                            <tr style=""border-bottom:1px solid rgba(38,50,56,.15); font-size:24px; color:#ff8a09; font-weight:400;"">
                                <td style=""font-size:50%; padding:12px 0 12px 50px;"">Carteira</td>
                                <td style=""font-size:50%; padding:12px 50px 12px 0; text-align:right;"">Quantidade</td>
                            </tr>");

			foreach (var item in c.GroupBy(a=>a.Carteira, (a,b)=>new {Carteira=a, Campanhas=b }))
				sb.AppendFormat(@"<tr style=""font-size:14px; border-bottom:1px solid rgba(38,50,56,.15)""><td style=""padding:12px 0 12px 50px;"">{0}</td><td style=""padding:12px 50px 12px 0; text-align:right;"">{1:N0}</td></tr>", item.Carteira, item.Campanhas.Sum(k=>k.Recebidas));

			sb.AppendFormat(@"<!-- tr foot / total -->
                            <tr style=""font-size:14px; color:#ff8a09;""><td style=""padding:12px 0 12px 50px;"">TOTAL</td><td style=""padding:12px 50px 12px 0; text-align:right;"">{0:N0}</td></tr>
                        </table>
                    </div>
                </td>
            </tr>
        </table>
", c.Sum(a => a.Recebidas));

			return new Tuple<string, byte[]>(sb.ToString(), _b);
		}

	}
}
