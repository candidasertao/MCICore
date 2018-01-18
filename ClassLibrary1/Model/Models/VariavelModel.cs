using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
   public  class VariavelModel
    {
		public int IDColuna { get; set; }
		public string Variavel { get; set; }
		public string Valor { get; set; }

		public VariavelModel(int idcoluna, string variavel, string valor)
		{
			this.IDColuna = idcoluna;
			this.Variavel = variavel;
			this.Valor = valor;
		}
	}
}
