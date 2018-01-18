using System;
using System.Collections.Generic;
using System.Text;
using ConecttaManagerData;
using System.Linq;
using System.IO;
using MoneoCIModel.Models;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace MoneoTaskEnvio
{
    public class Log
    {
        static string path = Util.Configuration["PathLog"];

        public static async Task Save(int fornecedor, int cliente, IEnumerable<CampanhaModel> l)
        {
            var filePath = string.Format(@"{0}\fornecedor_{1}_cliente_{2}.txt", path, fornecedor, cliente);

            var file = File.Create(filePath);

            var write = new StreamWriter(file, Encoding.UTF8);
            write.WriteLine(string.Format("campanhaid;fornecedorid;clienteid;status;dataenviofornecedor"));

            var formato = @"{0};{1};{2};{3};{4}";

            await Task.WhenAll(l.ToObservable()
                                .ForEachAsync(c =>
                                {
                                    write.WriteLine(string.Format(formato, c.CampanhaID, fornecedor, cliente, c.StatusEnvio, c.DataEnviar));
                                }));

            write.Flush();
            write.Dispose();
        }

        public static async Task Delete(int fornecedor)
        {
            var dir = new DirectoryInfo(path);

            await Task.WhenAll(dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(f => f.Name.StartsWith(string.Format("fornecedor_{0}", fornecedor)))
                                .ToObservable()
                                    .ForEachAsync(f =>
                                    {
                                        f.Delete();
                                    }));
        }

        public static async Task DeleteAll()
        {
            var dir = new DirectoryInfo(path);

            await Task.WhenAll(dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(f => f.Name.StartsWith("fornecedor_"))
                                .ToObservable()
                                    .ForEachAsync(f =>
                                    {
                                        f.Delete();
                                    }));
        }

        public static async Task<IEnumerable<CampanhaModel>> Read()
        {
            var dir = new DirectoryInfo(path);

            var files = new List<string>();

            await Task.WhenAll(dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(f => f.Name.StartsWith("fornecedor_"))
                                .ToObservable()
                                .ForEachAsync(f =>
                                {
                                    files.AddRange(File.ReadAllLines(f.FullName).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("campanhaid")));
                                }));

            var campanhas = new List<CampanhaModel>();

            var provide = new CultureInfo("pt-BR");

            await Task.WhenAll(files.ToObservable()
                                    .ForEachAsync(l =>
                                    {
                                        var c = l.Split(';');
                                        var d = DateTime.ParseExact(c.ElementAt(4), "dd/MM/yyyy HH:mm:ss", provide);
                                        campanhas.Add(new CampanhaModel
                                        {
                                            CampanhaID = long.Parse(c.ElementAt(0)),
                                            StatusEnvio = int.Parse(c.ElementAt(3)),
                                            DataEnviar = d
                                        });
                                    }));

            return campanhas;
        }
    }
}
