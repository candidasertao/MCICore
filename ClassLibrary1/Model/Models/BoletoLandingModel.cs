using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class BoletoLandingModel
    {
			public string ArquivoRetorno { get; set; }
			public string LinhaDigitavel { get; set; }
			public DateTime DataDocumento { get; set; }
			public DateTime Vencimento { get; set; }
			public string NumeroDocumento { get; set; }
			public string Aceite { get; set; }
			public string AgenciaConta { get; set; }
			public int Quantidade { get; set; }
			public string Carteira { get; set; }
			public decimal? ValorDocumento { get; set; }
			public decimal? Desconto { get; set; }
			public decimal? MoraMulta { get; set; }
			public string LocalPagamento { get; set; }
			public decimal? OutrosAcrescimos { get; set; }
			public string Beneficiario { get; set; }
			public string NossoNumero { get; set; }
			public string EspecieDocumento { get; set; }
			public Tuple<bool, string> CpfCnpj { get; set; }
			public string Endereco { get; set; }
			public string Cep { get; set; }
			public string UF { get; set; }
			public decimal? ValorCobrado { get; set; }
			public string Cidade { get; set; }
			public DateTime DataProcessamento { get; set; }
			public string ValorMoeda { get; set; }
			public string Instrucoes { get; set; }
			public string Contrato { get; set; }
			public decimal? ValorTotal { get; set; }
			public string Planos { get; set; }
			public int Parcelas { get; set; }
			public string Pagador { get; set; }
			public decimal? ValorDocumentoBoleto { get; set; }
			public string MD5Shar { get; set; }
			public int Conta { get; set; }
			public string Guid { get; set; }

	}
}
