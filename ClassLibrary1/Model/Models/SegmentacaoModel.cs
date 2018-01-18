using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class SegmentacaoModel
    {
		[JsonProperty("segmentacaoid", NullValueHandling = NullValueHandling.Ignore)]
		public int SegmentacaoID { get; set; }
		[JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
		public string Nome { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
		[JsonProperty("carteiras", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CarteiraModel> Carteiras { get; set; }

	
	}
}
