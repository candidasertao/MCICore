using MoneoCIData.DAL;
using MoneoCIModel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboEnviosMoneo
{

	public static class TimeSpanExtensions
	{
		public static DateTime DateTimeMinuteInterval(this DateTime d)
		{
			var dataatual = DateTime.Parse(d.ToString("dd/MM/yyyy HH:mm"));
			return dataatual.AddMinutes(-(dataatual.Minute % 5));
		}
		public static TimeSpan RoundToNearestMinutes(this TimeSpan input, int minutes)
		{
			var totalMinutes = (int)(input + new TimeSpan(0, minutes / 2, 0)).TotalMinutes;

			return new TimeSpan(0, totalMinutes - totalMinutes % minutes, 0);
		}
	}
	public class Program
	{

		static ManualResetEventSlim Wait { get; set; }
		static async Task InicioFila()
		{
			Console.WriteLine($"Buscando campanhas");


			var camps = await new DALCampanha().CampanhasPendentes();
			var campsNew = new ConcurrentDictionary<long, CampanhaModel>() { };

			

			if (camps.Any())
			{
				await camps.GroupBy(a => a.Fornecedor.FornecedorID, (a, b) => new { FornecedorID = a, Camps = b })
					.ToObservable()
					.ForEachAsync(a =>
					{
						var campsOld = new ConcurrentDictionary<long, CampanhaModel>() { };

						if (Colecoes.FilaFornecedor.TryGetValue(a.FornecedorID, out campsOld))
						{
							campsNew = campsOld;

							foreach (var item in a.Camps)
								if (campsNew.TryAdd(item.CampanhaID, item))
									Colecoes.Fornecedores[a.FornecedorID].Campanhas.Enqueue(item); //se positivo adiciona na fila concorrente

							Colecoes.FilaFornecedor.TryUpdate(a.FornecedorID, campsNew, campsOld);
						}
						else
						{
							foreach (var item in a.Camps)
								if (campsNew.TryAdd(item.CampanhaID, item))
									Colecoes.Fornecedores[a.FornecedorID].Campanhas.Enqueue(item);

							Colecoes.FilaFornecedor.TryAdd(a.FornecedorID, campsNew);
						}
					});
			}
			Wait.Set();
		}
		static Random Rnd = new Random();

		static async Task MainAsync(string[] args)
		{
			Colecoes.CampanhasEnviadas = new ConcurrentDictionary<long, CampanhaModel>() { };
			UpdateCampanhas();
			Wait = new ManualResetEventSlim(false);
			Colecoes.CampBlock = new BlockingCollection<CampanhaModel>(1000);
			Colecoes.Fornecedores = new Dictionary<int, FornecedorMod>() { };

			foreach (var item in await new DALFornecedor().FornecedoresAtivosEnvio())
				Colecoes.Fornecedores.Add(item.FornecedorID, new FornecedorMod(1, Rnd.Next(100, 500)));


			Colecoes.FilaFornecedor = new ConcurrentDictionary<int, ConcurrentDictionary<long, CampanhaModel>>() { };

			await InicioFila();
			await Task.Factory.StartNew(() =>
					Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe(x =>
					{
						Wait.Reset();
#pragma warning disable 4014
						InicioFila();
#pragma warning restore 4014
						Wait.Wait();
					})
				, TaskCreationOptions.LongRunning);

			Console.ReadLine();
		}

		public static async void UpdateCampanhas()
		{
			var camps = new List<CampanhaModel>() { };
			var camp = new CampanhaModel() { };

			await Task.Factory.StartNew(() =>
								Observable.Interval(TimeSpan.FromSeconds(30)).Subscribe(async x =>
								{
									foreach (var item in Colecoes.CampanhasEnviadas.Values)
									{
										Colecoes.CampanhasEnviadas.TryRemove(item.CampanhaID, out camp);
										camps.Add(camp);
									}
									if (camps.Any())
									{
										Console.WriteLine("atualizando campanhas");

										await new DALCampanha().CampanhasLoteNovo(camps);
										camps.Clear();
									}
								})
							, TaskCreationOptions.LongRunning);
		}
		public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

	}
}
