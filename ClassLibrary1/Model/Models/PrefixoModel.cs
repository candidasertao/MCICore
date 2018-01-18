using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	
	public class PrefixoModel
    {
		public int Prefixo { get; set; }
		public OperadorasEnums Operadora { get; set; }
		public int OperadoraID { get; set; }
		public string OperadoraNome { get; set; }
	}
}
