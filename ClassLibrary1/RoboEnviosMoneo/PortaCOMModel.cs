using MoneoCIData.DAL;
using RoboEnviosMoneo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoneoCIModel.Models
{
	public class PortaCOMModel
	{
		public ManualResetEventSlim Wait { get; set; }
		public int FornecedorID { get; set; }
		public string Porta { get; set; }
		public int PortaNumero { get; set; }
		public PortaCOMModel(int porta, string name, int fornecedorid)
		{
			Wait = new ManualResetEventSlim(false);
			PortaNumero = porta;
			Porta = name;
			this.FornecedorID = fornecedorid;
		}

		Random rnd = new Random();

		public  void WriteSMS(CampanhaModel c)
		{
			Thread.Sleep(rnd.Next(2000, 2000));
			c.StatusEnvio = 2;
			c.DataEnviar = DateTime.Now;

			Colecoes.CampanhasEnviadas.TryAdd(c.CampanhaID, c);
			this.Wait.Set();
			Console.WriteLine($"Sucesso {this.ToString()} CampanhaID {c.CampanhaID}");
		}

		public override string ToString()
		{
			return $"{Porta}{PortaNumero}-{FornecedorID}";
		}
	}
}
