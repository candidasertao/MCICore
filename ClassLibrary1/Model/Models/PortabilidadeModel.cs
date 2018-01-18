using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class PortabilidadeModel
    {
		public OperadorasEnums Operadora { get; set; }
		public decimal Celular { get; set; }
		public int CodigoOperadora { get; set; }
		public string NomeOperadora { get; set; }

		public override bool Equals(object obj)
		{
			PortabilidadeModel p = obj as PortabilidadeModel;
			return p != null && p.Celular == this.Celular;
		}
		public override int GetHashCode()
		{
			return this.Celular.GetHashCode();
		}
	}
}
