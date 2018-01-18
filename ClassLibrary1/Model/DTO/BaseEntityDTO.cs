using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DTO
{
    public class BaseEntityDTO<T>
    {
		[JsonProperty("start")]
		public DateTime Start { get; set; }
		[JsonProperty("end")]
		public DateTime End { get; set; }
		[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
		public T Result { get; set; }
		[JsonProperty("itens")]
		public int Itens { get; set; }
		[JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
		public string Error { get; set; }
		[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
		public string Token { get; set; }
		[JsonProperty("observacao", NullValueHandling = NullValueHandling.Ignore)]
		public dynamic Observacao { get; set; }


	}
}
