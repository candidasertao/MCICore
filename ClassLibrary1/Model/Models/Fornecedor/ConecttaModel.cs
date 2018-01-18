using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	public class ConecttaModel:BaseEntity
	{
		public string id { get; set; }
		public string status { get; set; }
		public string statuscode { get; set; }
		public DateTime datareport { get; set; }
	}
	public class RetornoConectta : ConecttaModel
	{
		public string retorno { get; set; }
		public DateTime dataretorno { get; set; }
		
	}    
}
