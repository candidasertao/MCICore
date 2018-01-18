using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	public class ErroResultModel
	{
		[JsonProperty("quantidade", NullValueHandling = NullValueHandling.Ignore)]
		public int Quantidade { get; set; }
		[JsonProperty("errotipo", NullValueHandling = NullValueHandling.Ignore)]
		public TiposInvalidosEnums ErroTipo { get; set; }

		public override string ToString()
		{
			return $"{ErroTipo.ToString()}-{Quantidade}";
		}
	}
	public class ResultModel
	{
		[JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
		public int Total { get; set; }
		[JsonProperty("validos", NullValueHandling = NullValueHandling.Ignore)]
		public int Validos { get; set; }
		[JsonProperty("erros", NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<ErroResultModel> Erros { get; set; }
	}
}

//{
//    "total": 100,
//    "validos": 12,
//    "erro": {
//        "errotipo": "SEMDADOS",
//        "desricao": "teste"
//    },
//    "invalidos": [
//        {
//            "celularinvalido": 15
//        },
//        {
//            "balcklist": 15
//        },
//        {
//            "fitrado": 15
//        },
//        {
//            "acima160caracteres": 15
//        },
//        {
//            "higienizado": 15
//        }
//    ]
//}
