using Newtonsoft.Json;
using System;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{


	public class CampanhaInvalidos
	{
		[JsonProperty("acima160caracteres", NullValueHandling = NullValueHandling.Ignore)]
		public int Acima160Caracteres { get; set; }
		[JsonProperty("higienizado", NullValueHandling = NullValueHandling.Ignore)]
		public int Higienizado { get; set; }
		[JsonProperty("blacklist", NullValueHandling = NullValueHandling.Ignore)]
		public int Blacklist { get; set; }
		[JsonProperty("celularinvalido", NullValueHandling = NullValueHandling.Ignore)]
		public int CelularInvalido { get; set; }
		[JsonProperty("filtrado", NullValueHandling = NullValueHandling.Ignore)]
		public int Filtrado { get; set; }
		[JsonProperty("forapadrao", NullValueHandling = NullValueHandling.Ignore)]
		public int ForaPadrao { get; set; }
		[JsonProperty("duplicados", NullValueHandling = NullValueHandling.Ignore)]
		public int Duplicados { get; set; }


	}

	public class CampanhaListagemResult
	{
		public decimal Celular { get; set; }
		public string Texto { get; set; }
	}



	public class CampanhaResultModel
	{
		[JsonProperty("mensageminvalida", NullValueHandling = NullValueHandling.Ignore)]
		public bool MensagemInvalida { get; set; }

		[JsonProperty("tipocampanha", NullValueHandling = NullValueHandling.Ignore)]
		public TipoCampanhaModel TipoCampanha { get; set; }
		[JsonProperty("arquivoforapadrao", NullValueHandling = NullValueHandling.Ignore)]
		public bool ArquivoForaPadrao { get; set; }
		[JsonProperty("leiaute", NullValueHandling = NullValueHandling.Ignore)]
		public LeiauteModel Leiaute { get; set; }
		[JsonProperty("mensagem", NullValueHandling = NullValueHandling.Ignore)]
		public string Mensagem { get; set; }
		[JsonProperty("variaveis", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<string> Variaveis { get; set; }
		[JsonProperty("ismsgempty", NullValueHandling = NullValueHandling.Ignore)]
		public bool IsMsgEmpty { get; set; }
		[JsonProperty("codigosession", NullValueHandling = NullValueHandling.Ignore)]
		public int CodigoSession { get; set; }
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		public int ID { get; set; }
		[JsonProperty("fornecedor", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<FornecedorModel> Fornecedor { get; set; }
		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string Arquivo { get; set; }
		[JsonProperty("arquivos", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<string> Arquivos { get; set; }
		[JsonProperty("registros", NullValueHandling = NullValueHandling.Ignore)]
		public int Registros { get; set; }
		[JsonProperty("registrosvalidos", NullValueHandling = NullValueHandling.Ignore)]
		public int RegistrosValidos { get; set; }
		[JsonProperty("totalinvalidos", NullValueHandling = NullValueHandling.Ignore)]
		public int TotalInvalidos { get; set; }
		[JsonProperty("situacao", NullValueHandling = NullValueHandling.Ignore)]
		public string Situacao { get; set; }
		[JsonProperty("invalidos", NullValueHandling = NullValueHandling.Ignore)]
		public CampanhaInvalidos Invalidos { get; set; }
		[JsonProperty("carteira", NullValueHandling = NullValueHandling.Ignore)]
		public CarteiraModel Carteira { get; set; }
		[JsonProperty("intervalo", NullValueHandling = NullValueHandling.Ignore)]
		public int Intervalo { get; set; }
		[JsonProperty("dataenviar", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime DataEnviar { get; set; }
		[JsonProperty("filebytes", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<byte> FileBytes { get; set; }
		[JsonProperty("campanhalista", NullValueHandling = NullValueHandling.Ignore)]
		public List<CampanhaModel> CampanhasLista { get; set; }
		[JsonProperty("campanhas", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CampanhaListagemResult> Campanhas { get; set; }
		[JsonProperty("campanhagridlotes", NullValueHandling = NullValueHandling.Ignore)]
		public List<CampanhaGridLotesModel> GridCampanhasLote { get; set; }
		[JsonProperty("campanhainvalida", NullValueHandling = NullValueHandling.Ignore)]
		public List<CampanhaModel> CampanhaInvalida { get; set; }
		[JsonProperty("tipocampanhaid", NullValueHandling = NullValueHandling.Ignore)]
		public int TipoCampanhaID { get; set; }
		[JsonProperty("permitirloteatrasado", NullValueHandling = NullValueHandling.Ignore)]
		public bool PermitirLoteAtrasado { get; set; }
		[JsonProperty("idarquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string IDArquivo { get; set; }
		[JsonProperty("gestores", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<GestorModel> Gestores { get; set; }

		public CampanhaResultModel()
		{
			GridCampanhasLote = new List<CampanhaGridLotesModel>() { };

		}
	}
}
