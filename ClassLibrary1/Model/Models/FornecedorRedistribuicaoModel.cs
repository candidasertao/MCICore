using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class FornecedorRedistribuicaoModel:BaseEntity
    {
		[JsonProperty("dataenviarlist", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<DateTime> DataEnviarList { get; set; }

		[JsonProperty("fornecedores", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<FornecedorModel> Fornecedores { get; set; }
	}
}
