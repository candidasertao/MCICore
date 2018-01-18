using MoneoCIData.DAL;
using MoneoCIModel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoneoTaskEnvio
{
    class Program
    {
        public static int TaskDelay = 10000;

        static SemaphoreSlim semaphore = new SemaphoreSlim(2);

        public static ConcurrentDictionary<int, ConcurrentDictionary<long, CampanhaModel>> FilaFornecedor { get; set; }

        static ManualResetEventSlim Wait { get; set; }

        static async Task InicioFila()
        {
            Console.WriteLine($" ---------------- ");
            Console.WriteLine($"| Buscando Filas |");
            Console.WriteLine($" ---------------- ");
            
            var camps = await new DALCampanha().CampanhasPendentes();

            if (camps.Any())
            {
                await Task.WhenAll(camps.GroupBy(a => new { a.Fornecedor.FornecedorID}, (a, b) => new { FornecedorID = a.FornecedorID, Campanhas = b })
                                        .ToObservable()
                                        .ForEachAsync(a =>
                                        {
                                            var f = new ConcurrentDictionary<long, CampanhaModel>();

                                            if (FilaFornecedor.TryGetValue(a.FornecedorID, out f))
                                            {
                                                a.Campanhas.ToObservable().ForEachAsync(c =>
                                                {
                                                    f.TryAdd(c.CampanhaID, c);
                                                });
                                            }
                                            else
                                            {
                                                f = new ConcurrentDictionary<long, CampanhaModel>();
                                                a.Campanhas.ToObservable().ForEachAsync(c =>
                                                {
                                                    f.TryAdd(c.CampanhaID, c);
                                                });

                                                FilaFornecedor.TryAdd(a.FornecedorID, f);
                                            }                        
                                        }));     
            }
            Wait.Set();
        }

        static void StartFornecedores()
        {
            new Conectta();

            new Pontal();
        }
        
        public static async Task Atualizar(object o)
        {
            semaphore.Wait();

            try
            {                                
                var f = (Tuple<int, IEnumerable<CampanhaModel>>)o;
                
                Console.WriteLine($"Inicio Atualizando Registros Fornecedor:{f.Item1} Quantidade:{f.Item2.Count()}");
                
                await new DALCampanha().UpdateFilaCampanha(f.Item2);

                ConcurrentDictionary<long, CampanhaModel> fornecedor = null;
                
                FilaFornecedor.TryRemove(f.Item1, out fornecedor);

                await Log.Delete(f.Item1);
                
                Console.WriteLine($"Fim Atualizando Registros Fornecedor:{f.Item1} Quantidade:{f.Item2.Count()}");                
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            semaphore.Release();
        }

        static async Task Atrasado()
        {
            var c = await Log.Read();

            if (c.Any())
            {
                await new DALCampanha().UpdateFilaCampanha(c);

                await Log.DeleteAll();
            }
        }

        static async Task MainAsync(string[] args)
        {
            Wait = new ManualResetEventSlim(false);

            FilaFornecedor = new ConcurrentDictionary<int, ConcurrentDictionary<long, CampanhaModel>>() { };

            await Atrasado();

            await InicioFila();

            StartFornecedores();

            await Task.Factory.StartNew(() =>
                    Observable.Interval(TimeSpan.FromSeconds(15)).Subscribe(x =>
                    {
                        Wait.Reset();
                        #pragma warning disable 4014
                        InicioFila();
                        #pragma warning restore 4014
                        Wait.Wait();
                    })
                , TaskCreationOptions.LongRunning);

            Console.ReadLine();
        }
        
        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
    }
}