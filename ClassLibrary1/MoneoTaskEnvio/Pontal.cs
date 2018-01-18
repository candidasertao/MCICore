using RestSharp;
using System;
using System.Collections.Generic;
using ConecttaManagerData;
using System.Text;
using System.Threading.Tasks;
using MoneoCIData;
using MoneoCIModel.Models;
using System.Reactive.Linq;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using MoneoCIModel.Models.Fornecedor.Pontal;
using System.Collections.Concurrent;

namespace MoneoTaskEnvio
{
    public class Pontal
    {
        string url = "https://sms-api-pointer.pontaltech.com.br/v1/multiple-sms";
        string urlCallbak = "www.moneoci.com.br/api/f/report/eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmb3JuZWNlZG9yaWQiOiIxOSIsImp0aSI6IjQ5NmQzMWM3LWVhMmYtNGU4Ny04NTliLTI3MWRjMGFmZDdmNyIsImF1ZCI6IkZvcm5lY2Vkb3JBUEkifQ.QOS9IYHxgjXKpkSwMC_HJAmUR9b15ZvPDDIRJ1xN1tw";

        public Pontal()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Enviar().Wait();

                    Thread.Sleep(Program.TaskDelay);
                }

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task Enviar()
        {
            try
            {
                var l = new ConcurrentDictionary<long, CampanhaModel>();

                Program.FilaFornecedor.TryGetValue((int)FornecedorEnum.Pontal, out l);

                if (l != null && l.Values.Any())
                {
                    Console.WriteLine($"Preparando Envios Fornecedor:{FornecedorEnum.Pontal} Quantidade:{l.Count()}");

                    await Task.WhenAll(

                        l.Values.GroupBy(a => new { a.Cliente.ClienteID, a.Fornecedor.FornecedorID }, (a, b) => new { ClienteID = a.ClienteID, FornecedorID = a.FornecedorID, Campanhas = b })
                            .Select(e => EnviarLote(e.FornecedorID, e.ClienteID, e.Campanhas))
                        );

                    var t = new Tuple<int, IEnumerable<CampanhaModel>>((int)FornecedorEnum.Pontal, l.Values) { };
                    await Program.Atualizar(t);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        private async Task<IEnumerable<CampanhaModel>> EnviarLote(int f, int c, IEnumerable<CampanhaModel> l)
        {
            try
            {
                if (l.Where(k => k.StatusEnvio == 1).Any())
                {
                    var smss = new List<string>();

                    var login = l.Where(k => !string.IsNullOrEmpty(k.Fornecedor.Login.Username) && !string.IsNullOrEmpty(k.Fornecedor.Login.Password)).Select(k => k.Fornecedor.Login).ElementAt(0);

                    if (string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
                        throw new Exception("Permissões não configuradas");

                    await Task.WhenAll(l.ToObservable()
                                        .Where(k => k.StatusEnvio == 1)
                                        .ForEachAsync(e =>
                                        {
                                            smss.Add($"{{\"to\":\"{e.Celular}\", \"message\":\"{e.Texto}\", \"schedule\":\"2017-09-25T00:00:00.000Z\",\"reference\":\"{e.CampanhaID}\"}}");
                                        }));

                    var envio = string.Join(",", smss);
                    var agora = DateTime.Now;

                    var body = $"{{\"urlCallback\": \"{urlCallbak}\", \"messages\":[{envio}]}}";

                    var client = new RestClient(url);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/json");
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login.Username}:{login.Password}"))));
                    request.AddParameter("application/json", body, ParameterType.RequestBody);

                    var tcs = new TaskCompletionSource<IRestResponse>();

                    client.ExecuteAsync(request, response =>
                    {
                        tcs.SetResult(response);
                    });

                    var r = await tcs.Task;

                    if (r.StatusCode != HttpStatusCode.OK)
                        throw new Exception(r.Content);

                    var obj = JsonConvert.DeserializeObject<PontalRoot>(r.Content);

                    await ProcessarRetorno(l, obj.messages, agora);

                    await Log.Save(f, c, l);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }

        private async Task<IEnumerable<CampanhaModel>> ProcessarRetorno(IEnumerable<CampanhaModel> l, IEnumerable<PontalModel> r, DateTime d)
        {
            try
            {
                await Task.WhenAll(l.ToObservable()
                                    .Where(k => k.StatusEnvio == 1)
                                    .ForEachAsync(e =>
                                    {
                                        var code = r.Where(k => long.Parse(k.reference) == e.CampanhaID).Select(k => k.status).FirstOrDefault();

                                        switch (code)
                                        {
                                            case 0:
                                            case 1:
                                            case 2:
                                            case 3:
                                            case 5:
                                            case 12: //não entregue tarifado
                                            case 13: //expirado tarifado
                                                e.StatusEnvio = 2;
                                                break;
                                                
                                            case 4:
                                            case 6:
                                            case 9:
                                            case 10:
                                                e.TipoErroApi = TipoRetornoErroApiEnum.ERRO;
                                                e.StatusEnvio = 3;
                                                break;
                                                
                                            case 7:
                                                e.TipoErroApi = TipoRetornoErroApiEnum.BLACKLIST;
                                                e.StatusEnvio = 3;
                                                break;

                                            case 8:
                                            case 11: //sem orcamento
                                                e.TipoErroApi = TipoRetornoErroApiEnum.INVALIDO;
                                                e.StatusEnvio = 3;
                                                break;
                                                
                                            case 14: //invalid base
                                                e.TipoErroApi = TipoRetornoErroApiEnum.BLOQUEADO;
                                                e.StatusEnvio = 3;
                                                break;
                                        }

                                        e.DataEnviar = d;
                                    }));
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }
    }
}
