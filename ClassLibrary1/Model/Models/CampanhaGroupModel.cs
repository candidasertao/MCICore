using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class CampanhaGroupModel
    {
		[Key]
		public int Codigo { get; set; }
		public DateTime DataEnviar { get; set; }
		public int Quantidade { get; set; }
		public CarteiraModel Carteira { get; set; }
		public string Arquivo { get; set; }
	}
}
