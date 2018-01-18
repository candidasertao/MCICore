using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class ArquivoCampanhaModel
    {
		public int ArquivoID { get; set; }
		public string Arquivo { get; set; }
		public ClienteModel Cliente { get; set; }
		public UsuarioModel Usuario { get; set; }
		public DateTime Data { get; set; }

		public ArquivoCampanhaModel()
		{

		}
		public ArquivoCampanhaModel(string _arquivo)
		{
			this.Arquivo = _arquivo;
		}
	}
}
