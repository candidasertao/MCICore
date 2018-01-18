using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{

	public class FileUploadModel
	{
		public string FileName { get; set; }
		public byte[] Linhas { get; set; }
		public string ArquivoPadrao { get; set; }
		public bool IsPadrao { get; set; }
		public int ID { get; set; }
		public Dictionary<string, IEnumerable<byte>> FilesInZip { get; set; }
		public CarteiraModel Carteira { get; set; }
		public TipoCampanhaModel TipoCampanha { get; set; }
		public int Registros { get; set; }
		public bool IsZiped { get; set; }

		public FileUploadModel(string arquivo, byte[] l, string arquivopadrao, bool ispadrao)
		{
			FileName = arquivo;
			Linhas = l;
			ArquivoPadrao = arquivopadrao;
			IsPadrao = ispadrao;
		}
	}


	public class FileUp:BaseEntity
	{
		public IEnumerable<GestorModel> Gestores { get; set; }
		public string ArquivoPadrao { get; set; }
		public string FileZip { get; set; }
		public bool IsZiped { get; set; }
		public bool ForaPadrao { get; set; }
		public byte[] Linhas { get; set; }
		public LeiauteModel Leiaute { get; set; }
		public CarteiraModel Carteira { get; set; }
		public TipoCampanhaModel TipoCampanha { get; set; }
	}

	public class FileUploadMultiplesModel
	{
		public string Arquivo { get; set; }
		public bool IsZiped { get; set; }
		public bool UploadPadrao { get; set; }
		public int Registros { get; set; }
		public IEnumerable<PadraoPostagensModel> ArquivosUp { get; set; }
		public Dictionary<string, IEnumerable<byte>> Arquivos { get; set; }

		public FileUploadMultiplesModel()=>Arquivos = new Dictionary<string, IEnumerable<byte>>() { };
		
		
	}
}
