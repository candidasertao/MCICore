using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class BlackListModel:BaseEntity
    {
		[JsonProperty("blacklistid", NullValueHandling = NullValueHandling.Ignore)]
		public int BlacklistID { get; set; }
		[JsonProperty("celular", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Celular { get; set; }
		[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime Data { get; set; }
		[JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
		public ClienteModel Cliente { get; set; }
	}
}
