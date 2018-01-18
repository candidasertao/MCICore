using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	public class SessionDataModel
    {
		public string Guid { get; set; }
		public string Key { get; set; }
		public byte[] Value { get; set; }
		public DateTime Data { get; set; }
		public bool IsPadraoEnvio { get; set; }
		public int Codigo { get; set; }

		public SessionDataModel(){}

		public SessionDataModel(string guid)
		{
			Guid = guid;
		}
	}
}
