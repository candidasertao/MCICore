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
using MoneoCIModel.Models.Fornecedor;

namespace MoneoTaskEnvio
{
    public class Zenvia
    {
        string Username = "";
        string Password = "";
        string url = ""; //"https://api-rest.zenvia360.com.br/services/send-sms-multiple";

        public async Task<IEnumerable<CampanhaModel>> Enviar(IEnumerable<CampanhaModel> l)
        {
            try
            {
                await l.GroupBy(a => new { a.Cliente.ClienteID, a.Fornecedor.FornecedorID }, (a, b) => new { dados = a, Campanhas = b })
                    .ToObservable()
                    .ForEachAsync(async e =>
                    {
                        await EnviarLote(e.dados.FornecedorID, e.dados.ClienteID, e.Campanhas);
                    });

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }
                
        private async Task<IEnumerable<CampanhaModel>> EnviarLote(int f, int c, IEnumerable<CampanhaModel> l)
        {
            try
            {
                var body = "{{\"sendSmsMultiRequest\":{{ \"aggregateId\": \"\",\"sendSmsRequestList\": [ {0} ]}}}}";

                var smss = new List<string>();

                var login = l.Where(k => !string.IsNullOrEmpty(k.Fornecedor.Login.Username) && !string.IsNullOrEmpty(k.Fornecedor.Login.Password)).Select(k => k.Fornecedor.Login).ElementAt(0);

                Username = login.Username;
                Password = login.Password;

                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))                
                    throw new Exception("Permissões não configuradas");
                
                await l.ToObservable()
                        .Where(k => k.StatusEnvio == 1)
                        .ForEachAsync(e =>
                        {
                            smss.Add($"{{\"to\":\"55{e.Celular}\",\"schedule\":\"2017-08-02T00:00:00\",\"msg\":\"{e.Texto}\",\"callbackOption\":\"FINAL\",\"id\":\"{e.CampanhaID}\"}}");
                        });

                var envio = string.Format(body, string.Join(",", smss));
                var agora = DateTime.Now;
                
                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/json");
                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"))));
                request.AddParameter("application/json", envio, ParameterType.RequestBody);

                var tcs = new TaskCompletionSource<ZenviaModel.Root>();

                client.ExecuteAsync(request, response =>
                {
                    tcs.SetResult(JsonConvert.DeserializeObject<ZenviaModel.Root>(response.Content));
                });

                var r = await tcs.Task;

                await ProcessarRetorno(l, r, agora);

                await Log.Save(f, c, l);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }        

        async Task<IEnumerable<CampanhaModel>> ProcessarRetorno(IEnumerable<CampanhaModel> l, ZenviaModel.Root r, DateTime d)
        {
            try
            {
                var i = 0;
                var c = r.sendSmsMultiResponse.sendSmsResponseList;
                
                await l.ToObservable()
                        .Where(k => k.StatusEnvio == 1)
                        .ForEachAsync(e =>
                        {
                            var code = c[i].statusCode;

                            if (int.Parse(code) <= 3)
                                e.StatusEnvio = 2;
                            else
                                e.StatusEnvio = 3;

                            e.DataEnviar = d;

                            i++;
                        });
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }
    }
}
