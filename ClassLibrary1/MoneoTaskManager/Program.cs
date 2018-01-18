using Microsoft.Extensions.Configuration;
using MoneoCIData;
using MoneoCIData.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoTaskManager
{
    public class Program
    {


        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {

			//await new DALPortabilidade().ProcessaPortabilidade();



			if (args.Any())
            {
                switch (args.ElementAt(0))
                {
                    case "/PORTABILIDADE":
                        await new DALPortabilidade().GetPortabilidade();
                        break;
					case "/AGENDADAS":
                        await new DALCampanha().AtualizaCampanhasAgendadas();
                        break;
                    case "/CONSOLIDADAS":
                        await new DALCampanha().GeraConsolidados();
                        break;
                    case "/NOTIFICAGESTOR":
                        await new DALCarteira().LimiteCarteira();
                        break;
                    case "/RELATORIOANALITICO":
                        await new DALCampanha().RelatorioAnalitico(DateTime.Now.Date.AddDays(-1));
                        break;
                    case "/RELATORIORETORNO":
                        await new DALCampanha().AnaliticoRetorno(DateTime.Now.Date.AddDays(-1));
                        //await new DALCampanha().AnaliticoRetorno(DateTime.Parse("7/8/2017"));
                        break;
                    case "/LIMPARSESSAO":
                        await new DALCampanha().Limpeza();
                        break;
                }
            }
            Environment.Exit(0);
        }
    }
}
