using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CampanhaRetornoModel: ValidacaoDeSessao
	{
		public FornecedorModel Fornecedor { get; set; }
		public CampanhaModel Campanha { get; set; }
		public string Retorno { get; set; }
		public DateTime DataRetorno { get; set; }
		
		public DateTime DataGravacao { get; set; }
		[Key]
		public int Codigo { get; set; }
	}
}
