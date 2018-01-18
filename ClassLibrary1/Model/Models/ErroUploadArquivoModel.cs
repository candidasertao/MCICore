using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class ErroUploadArquivoModel:BaseEntity
    {
		[JsonProperty("arquivo", NullValueHandling = NullValueHandling.Ignore)]
		public string Arquivo { get; set; }
		[JsonProperty("tipoerrouplaod", NullValueHandling = NullValueHandling.Ignore)]
		public TipoErroUploadArquivoEnum TipoErroUpload { get; set; }
		[JsonProperty("mensagem", NullValueHandling = NullValueHandling.Ignore)]
		public string Mensagem { get; set; }

		public ErroUploadArquivoModel(string a, TipoErroUploadArquivoEnum t, string m)
		{
			this.Arquivo = a;
			this.TipoErroUpload = t;
			this.Mensagem = m;
		}
	}
}
