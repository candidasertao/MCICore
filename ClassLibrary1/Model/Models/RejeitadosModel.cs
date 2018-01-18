using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
	public class RejeitadosModel
	{

		public decimal Celular { get; set; }
		public DateTime Data { get; set; }

		public override bool Equals(object obj)
		{
			RejeitadosModel p = obj as RejeitadosModel;
			return p != null && p.Celular == this.Celular;
		}
		public override int GetHashCode() => this.Celular.GetHashCode();
	}
}
