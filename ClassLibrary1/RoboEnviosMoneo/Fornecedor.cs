using MoneoCIModel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoboEnviosMoneo
{
	public class FornecedorMod
	{
		public ConcurrentQueue<CampanhaModel> Campanhas { get; set; }
		public int NumeroPortas { get; set; }
		public int FornecedorID { get; set; }
		public Random Rnd { get; set; }

		public FornecedorMod(int fornecedorid, int portas)
		{
			this.Rnd = new Random();
			this.FornecedorID = fornecedorid;
			this.Campanhas = new ConcurrentQueue<CampanhaModel>() { };

			Task.Run(() =>
			{
				var ts = new List<Task>() { };
				foreach (var item in Enumerable.Range(1, portas))
					ts.Add(Porta(item));

				Console.WriteLine($"Inicializando fornecedor: {fornecedorid} portas de envio: {ts.Count}");

			});
		}
		Task Porta(int _porta)
		{
			return Task.Factory.StartNew((k) =>
			{
				
				PortaCOMModel porta = new PortaCOMModel(_porta, "COM", FornecedorID);
				var camp = new CampanhaModel();
				while (true)
				{
					Thread.Sleep(Rnd.Next(3000, 5000));
					if (Campanhas.TryDequeue(out camp))
					{
						porta.Wait.Reset();
						porta.WriteSMS(camp);
						porta.Wait.Wait();
					}
					
				}
			}, CancellationToken.None, TaskCreationOptions.LongRunning);
		}
	}
}
