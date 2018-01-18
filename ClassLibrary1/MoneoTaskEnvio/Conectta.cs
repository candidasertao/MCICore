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
using System.Collections.Concurrent;

namespace MoneoTaskEnvio
{
    public class Conectta
    {
        string url = "http://localhost:59528/api/conectta/enviarsms";

        public Conectta()
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

                Program.FilaFornecedor.TryGetValue((int)FornecedorEnum.Conectta, out l);

                if (l != null && l.Values.Any())
                {
                    Console.WriteLine($"Preparando Envios Fornecedor:{FornecedorEnum.Conectta} Quantidade:{l.Count()}");

                    await Task.WhenAll(

                        l.Values.GroupBy(a => new { a.Cliente.ClienteID, a.Fornecedor.FornecedorID }, (a, b) => new { ClienteID = a.ClienteID, FornecedorID = a.FornecedorID, Campanhas = b })
                            .Select(e => EnviarLote(e.FornecedorID, e.ClienteID, e.Campanhas))
                        );
                    
                    var t = new Tuple<int, IEnumerable<CampanhaModel>>((int)FornecedorEnum.Conectta, l.Values) { };
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
                                            smss.Add($"{{\"to\":\"{e.Celular}\", \"msg\":\"{e.Texto}\", \"type\":\"0\",\"id\":\"{e.CampanhaID}\"}}");
                                        }));

                    var envio = string.Join(",", smss);
                    var agora = DateTime.Now;

                    var client = new RestClient(url);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("content-type", "application/json");
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login.Username}:{login.Password}"))));
                    request.AddParameter("application/json", $"[{envio}]", ParameterType.RequestBody);

                    var tcs = new TaskCompletionSource<IRestResponse>();

                    client.ExecuteAsync(request, response =>
                    {
                        tcs.SetResult(response);
                    });

                    var r = await tcs.Task;

                    if (r.StatusCode == HttpStatusCode.BadRequest)
                        throw new Exception(r.Content);

                    var obj = JsonConvert.DeserializeObject<IEnumerable<ConecttaModel>>(r.Content);

                    await ProcessarRetorno(l, obj, agora);

                    await Log.Save(f, c, l);                    
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }

        private async Task<IEnumerable<CampanhaModel>> ProcessarRetorno(IEnumerable<CampanhaModel> l, IEnumerable<ConecttaModel> r, DateTime d)
        {
            try
            {
                await Task.WhenAll(l.ToObservable()
                                    .Where(k => k.StatusEnvio == 1)
                                    .ForEachAsync(e =>
                                    {
                                        var code = r.Where(k => long.Parse(k.id) == e.CampanhaID).Select(k => k.statuscode).FirstOrDefault();

                                        switch (code)
                                        {
                                            case "0":
                                            case "1":
                                            case "2":
                                            case "3":
                                                e.StatusEnvio = 2;
                                                break;

                                            case "4":
                                                //blacklist
                                                e.TipoErroApi = TipoRetornoErroApiEnum.BLACKLIST;
                                                e.StatusEnvio = 3;
                                                break;

                                            case "5":
                                                //número invalido
                                                e.TipoErroApi = TipoRetornoErroApiEnum.NUMEROINVALIDO;
                                                e.StatusEnvio = 3;
                                                break;

                                            case "6":
                                                //bloqueado
                                                e.TipoErroApi = TipoRetornoErroApiEnum.BLOQUEADO;
                                                e.StatusEnvio = 3;
                                                break;

                                            case "7":
                                                //invalido
                                                e.TipoErroApi = TipoRetornoErroApiEnum.INVALIDO;
                                                e.StatusEnvio = 3;
                                                break;

                                            case "8":
                                                //erro
                                                e.TipoErroApi = TipoRetornoErroApiEnum.ERRO;
                                                e.StatusEnvio = 3;
                                                break;

                                            case "9":
                                                //duplicado
                                                e.TipoErroApi = TipoRetornoErroApiEnum.DUBPLICADO;
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
