using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class NotificacaoLimiteCarteiraModel:BaseEntity
    {
		public int Limite { get; set; }
		public int DiaInicio { get; set; }
		public IEnumerable<EmailViewModel> Gestores { get; set; }
		public ClienteModel Cliente { get; set; }
		public CarteiraModel Carteira { get; set; }
		public decimal PercentualUso { get; set; }
		public int PorcentagemAviso { get; set; }
	}
}
