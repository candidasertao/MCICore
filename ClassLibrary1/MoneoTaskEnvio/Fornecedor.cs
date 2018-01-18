using MoneoCIModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneoTaskEnvio
{
    public class Fornecedor
    {
        public async Task<IEnumerable<CampanhaModel>> Enviar(IEnumerable<CampanhaModel> l)
        {
            try
            {
                await l.GroupBy(a => new { a.Cliente.ClienteID, a.Fornecedor.FornecedorID }, (a, b) => new { dados = a, Campanhas = b })
                    .ToObservable()
                    .ForEachAsync(async e =>
                    {
                        await EnviarLoteDev(e.dados.FornecedorID, e.dados.ClienteID, e.Campanhas);
                    });

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }

        private async Task<IEnumerable<CampanhaModel>> EnviarLoteDev(int f, int c, IEnumerable<CampanhaModel> l)
        {
            try
            {
                Random rnd = new Random();

                var agora = DateTime.Now;

                foreach (var item in l.Where(k => k.StatusEnvio == 1))
                {
                    item.DataEnviar = agora;
                    item.StatusEnvio = 2;
                }

                await Log.Save(f, c, l);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            return l;
        }
    }
}
