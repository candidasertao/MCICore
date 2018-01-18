using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Fornecedor.Pontal
{
	public class PontalModel
	{
		public string id { get; set; }
		public string to { get; set; }
		public string message { get; set; }
		public DateTime schedule { get; set; }
		public string reference { get; set; }
		public int status { get; set; }
		public string statusDescription { get; set; }
		public string account { get; set; }
		public string type { get; set; }
	}

	public class PontalRoot
	{
		public string id { get; set; }
		public IEnumerable<PontalModel> messages { get; set; }
	}

	public class reply : ReplyGenericModel { }

	public class retorno
	{
		public string type { get; set; }
		public IEnumerable<reply> replies { get; set; }
	}
}
