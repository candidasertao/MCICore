using MoneoCIModel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoboEnviosMoneo
{
    public static class Colecoes
    {
		public static Dictionary<int, FornecedorMod> Fornecedores { get; set; }

		public static ConcurrentDictionary<int, ConcurrentDictionary<long, CampanhaModel>> FilaFornecedor { get; set; }

		public static BlockingCollection<CampanhaModel> CampBlock  { get; set; }

		public static ConcurrentDictionary<long, CampanhaModel> CampanhasEnviadas { get; set; }
	}
}
