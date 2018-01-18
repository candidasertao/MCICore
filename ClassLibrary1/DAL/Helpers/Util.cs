using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FluentValidation;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Utils;
using DAL;
using Helpers;
using DTO;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace System.Linq
{
	public static class Util
	{



		public static Dictionary<int, IEnumerable<T>> TakeGroup<T>(this IEnumerable<T> s, int q) where T : class
		{
			int quantByLote = (int)Math.Ceiling((decimal)s.Count() / (decimal)q);
			Dictionary<int, IEnumerable<T>> listagem = new Dictionary<int, IEnumerable<T>>() { };
			IEnumerable<T> itens = new T[] { };

			int i = 0;
			do
			{
				itens = s.Take(quantByLote).ToList();
				listagem.Add(i, itens);
				s = s.Skip(quantByLote);
				i++;
			} while (s.Count() > 0);

			return listagem;
		}


		public static DateTime DateTimeMinuteInterval(this DateTime d)
		{

			DateTime dataatual = DateTime.Parse(d.ToString("dd/MM/yyyy HH:mm"));
			return dataatual.AddMinutes(-(dataatual.Minute % 5));
		}

		static async Task<byte[]> DataObj(string filepath)
		{
			using (var moneologo = File.OpenRead(filepath))
			{
				using (var mem = new MemoryStream())
				{
					await moneologo.CopyToAsync(mem);
					return mem.ToArray();
				}
			}
		}
		static string EmbedImage(ref BodyBuilder body, string img, string pathimg, byte[] dados = null)
		{
			byte[] _b = new byte[] { };

			if (dados != null)
				_b = dados;
			else
				_b = DataObj(pathimg).GetAwaiter().GetResult();

			var dado = body.LinkedResources.Add(img, _b);
			dado.ContentId = MimeUtils.GenerateMessageId();
			return dado.ContentId;
		}

		public static string NormalizeCell(this string cell)
		{
			if (cell.StartsWith("55") && (cell.Length == 13 || cell.Length == 12))
				cell = cell.Substring(2);



			return cell;

		}

		public static Match NumberIsValid(string c) => Regex.Match(c, "^(55)?([11|12|13|14|15|16|17|18|19|21|22|24|27|28|81|82|83|84|85|86|87|88|89|91|92|93|94|95|96|97|98|99|31|32|33|34|35|37|38|79|71|73|74|75|77|79|61|62|63|64|65|66|67|68|69|41|42|43|44|45|46|47|48|49|51|52|53|54]{2})([\\d]{8,9})$", RegexOptions.Compiled);


		public async static Task<T> CacheFactory<T>(IMemoryCache cache, string chave, IHostingEnvironment host, DateTimeOffset? offset = null)
		{
			T _item;
			var _offset = offset ?? DateTimeOffset.MaxValue;

			if (!cache.TryGetValue(chave, out _item))
				switch (chave)
				{
					case "nextel":
						cache.Set<List<PrefixoModel>>(chave, File.ReadLines($"{host.ContentRootPath}\\Nextel.csv").Select(a => new PrefixoModel()
						{
							Operadora = OperadorasEnums.NEXTEL,
							Prefixo = int.Parse(a)
						}).ToList(), _offset);
						break;
					case "prefixos":
						var prefixos = await new DALPrefixo().ObterTodos();
						cache.Set<List<PrefixoModel>>(chave, prefixos.ToList(), _offset);
						break;
					case "quarentena":
						cache.Set<IEnumerable<decimal>>(chave, (await new DALCampanha().RetornaRejeitados()).ToHashSetEx(), _offset);
						break;
				}

			return (T)cache.Get(chave);
		}
		
		public static decimal NormalizeCell(this decimal cell)
		{
			var _cell = cell.ToString();
			if (_cell.StartsWith("55") && (_cell.Length == 13 || _cell.Length == 12))
				_cell = _cell.Substring(2);



			return decimal.Parse(_cell);

		}

		public static decimal InsereNonoDigito(this string s) => s.Length == 6 ? decimal.Parse(s.Insert(2, "9")) : decimal.Parse(s);


		public static DateTime DateTimeNoSecond(this DateTime d) => DateTime.Parse(d.ToString("dd/MM/yyyy HH:mm"));

		public static string ToShortDateTime(this DateTime d) => d.ToString("dd/MM/yyyy");

		public static async Task SendEmailAsync(IEnumerable<EmailViewModel> email, string subject, string message, bool ishtml, TipoEmail tipo, byte[] dados = null)
		{
			var emailMessage = new MimeMessage();
			var body = new BodyBuilder();

			foreach (var e in email)
				emailMessage.To.Add(string.IsNullOrEmpty(e.Nome) ? new MailboxAddress(e.Email) : new MailboxAddress(e.Nome, e.Email));

			if (ishtml)
			{
				message = message.Replace("#logo", string.Format(@"src=""cid:{0}""", EmbedImage(ref body, "moneo.png", $"{Directory.GetCurrentDirectory()}\\Imgs\\moneo.png")));

				switch (tipo)
				{
					case TipoEmail.OUTROS:
						break;
					case TipoEmail.LIMITECARTEIRA:
						emailMessage.To.Clear();
						emailMessage.To.Add(new MailboxAddress("Candida", "candida@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Pedro Mira", "pedro.mira@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Flavia Ramalho", "flavia.ramalho@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Ricardo Beck", "ricardo@conecttasoftwares.com.br"));
						break;
					case TipoEmail.ENVIOSMS:
						emailMessage.To.Clear();
						emailMessage.To.Add(new MailboxAddress("Candida", "candida@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Pedro Mira", "pedro.mira@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Flavia Ramalho", "flavia.ramalho@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Ricardo Beck", "ricardo@conecttasoftwares.com.br"));
						message = message.Replace("#arquivo", string.Format(@"src=""cid:{0}""", EmbedImage(ref body, "arquivos.png", $"{Directory.GetCurrentDirectory()}\\Imgs\\arquivos.png")));
						break;
					case TipoEmail.REQUISICAOCARTEIRA:
						break;
					case TipoEmail.NOVOUSUARIO:
						break;
					case TipoEmail.NOVOCADASTRO:
						break;
					case TipoEmail.RESETSENHA:
						break;
					case TipoEmail.RELATORIOANALITICO:
						emailMessage.To.Clear();
						emailMessage.To.Add(new MailboxAddress("Candida", "candida@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Pedro Mira", "pedro.mira@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Flavia Ramalho", "flavia.ramalho@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Ricardo Beck", "ricardo@conecttasoftwares.com.br"));

						break;
					case TipoEmail.ANALITICORETORNO:
						emailMessage.To.Clear();
						emailMessage.To.Add(new MailboxAddress("Candida", "candida@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Pedro Mira", "pedro.mira@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Flavia Ramalho", "flavia.ramalho@conecttasoftwares.com.br"));
						emailMessage.To.Add(new MailboxAddress("Ricardo Beck", "ricardo@conecttasoftwares.com.br"));
						message = message.Replace("#grafico", string.Format(@"src=""cid:{0}""", EmbedImage(ref body, "grafico.png", null, dados)));
						break;
					default:
						break;
				}
				body.HtmlBody = message;
			}
			else
				body.TextBody = message;

			emailMessage.From.Add(new MailboxAddress("Moneo SI", "sms@conecttasms.com.br"));


			emailMessage.Subject = subject;
			emailMessage.Body = body.ToMessageBody();

			using (var client = new SmtpClient())
			{
				await client.ConnectAsync(Configuration["MailServer"], int.Parse(Configuration["Porta"]), false).ConfigureAwait(false);
				await client.AuthenticateAsync(new NetworkCredential(Configuration["Login"], Configuration["Senha"])).ConfigureAwait(false);
				await client.SendAsync(emailMessage).ConfigureAwait(false);
				await client.DisconnectAsync(true).ConfigureAwait(false);
			}
		}

		public static IEnumerable<string[]> ListaCelulares(byte[] r)
		{
			using (var mem = new MemoryStream(r))
			{
				using (StreamReader reader = new StreamReader(mem, Encoding.ASCII, true))
				{
					while (reader.Peek() >= 0)
						yield return reader.ReadLine().Trim().TrimEnd(';').Split(";".ToCharArray());
				}
			}
		}

		public static IEnumerable<string[]> ListaCelulares(Stream r)
		{
			r.Position = 0L;
			using (StreamReader reader = new StreamReader(r, Encoding.ASCII, true))
			{
				while (reader.Peek() >= 0)
					yield return reader.ReadLine().Trim().TrimEnd(';').Split(";".ToCharArray());
			}
		}
		public static IEnumerable<string> ListaLinhas(Stream r)
		{
			r.Position = 0L;
			using (StreamReader reader = new StreamReader(r, Encoding.ASCII, true))
			{
				while (reader.Peek() >= 0)
					yield return reader.ReadLine().Trim();
			}

		}
		public static async Task<(bool, string)> ValidaRequisicao<T>(AbstractValidator<T> _validator, T o)
		{
			var validator = await _validator.ValidateAsync(o);
			return (validator.IsValid,
				validator.Errors.Any() ? validator.Errors.Select(a => a.ErrorMessage).Aggregate((a, b) => $"{a},{b}") : string.Empty);

		}

		public static bool ValidarCPF(string cpf)
		{
			int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
			int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
			string tempCpf;
			string digito;
			int soma;
			int resto;

			cpf = cpf.Trim();
			cpf = cpf.Replace(".", "").Replace("-", "");

			if (cpf.Length != 11)
				return false;

			tempCpf = cpf.Substring(0, 9);
			soma = 0;
			for (int i = 0; i < 9; i++)
				soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

			resto = soma % 11;
			if (resto < 2)
				resto = 0;
			else
				resto = 11 - resto;

			digito = resto.ToString();

			tempCpf = tempCpf + digito;

			soma = 0;
			for (int i = 0; i < 10; i++)
				soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

			resto = soma % 11;
			if (resto < 2)
				resto = 0;
			else
				resto = 11 - resto;

			digito = digito + resto.ToString();

			return cpf.EndsWith(digito);
		}

		public static bool ValidaCnpj(string cnpj)
		{
			int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
			int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
			int soma;
			int resto;
			string digito;
			string tempCnpj;

			cnpj = cnpj.Trim();
			cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");

			if (cnpj.Length != 14)
				return false;

			tempCnpj = cnpj.Substring(0, 12);

			soma = 0;
			for (int i = 0; i < 12; i++)
				soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

			resto = (soma % 11);
			if (resto < 2)
				resto = 0;
			else
				resto = 11 - resto;

			digito = resto.ToString();

			tempCnpj = tempCnpj + digito;
			soma = 0;
			for (int i = 0; i < 13; i++)
				soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

			resto = (soma % 11);
			if (resto < 2)
				resto = 0;
			else
				resto = 11 - resto;

			digito = digito + resto.ToString();

			return cnpj.EndsWith(digito);
		}

		static Dictionary<string, int> AcentoDic
		{
			get
			{
				return "ç,Ç,á,é,í,ó,ú,ý,Á,É,Í,Ó,Ú,Ý,à,è,ì,ò,ù,À,È,Ì,Ò,Ù,ã,õ,ñ,ä,ë,ï,ö,ü,ÿ,Ä,Ë,Ï,Ö,Ü,Ã,Õ,Ñ,â,ê,î,ô,û,Â,Ê,Î,Ô,Û".Split(",".ToCharArray())
				.Select((a, b) => new { indice = b, caract = a })
				.ToDictionary(b => b.caract, a => a.indice);
			}
		}


		static Dictionary<int, string> NoAcentoDic
		{
			get
			{
				return "c,C,a,e,i,o,u,y,A,E,I,O,U,Y,a,e,i,o,u,A,E,I,O,U,a,o,n,a,e,i,o,u,y,A,E,I,O,U,A,O,N,a,e,i,o,u,A,E,I,O,U"
				.Split(",".ToCharArray())
				.Select((a, b) => new { indice = b, caract = a })
				.ToDictionary(a => a.indice, b => b.caract);
			}
		}

		static Regex regCharsAcento = new Regex("[çÇáéíóúýÁÉÍÓÚÝàèìòùÀÈÌÒÙãõñäëïöüÿÄËÏÖÜÃÕÑâêîôûÂÊÎÔÛ]", RegexOptions.Compiled | RegexOptions.Multiline);



		static decimal InsereNonoDigito(decimal c)
		{
			var _cell = c.ToString();
			if (_cell.Substring(2, 1).Equals("9") && _cell.Length > 10)
				return decimal.Parse(_cell);

			return decimal.Parse(_cell.Insert(2, "9"));
		}

		#region AMAZON AWS

		#region S3
		public async static Task DeleteFileAamazon(string filename, string bucketname)
		{
			using (var cliente = new AmazonS3Client(Configuration["ChaveAWS"], Configuration["SenhaAWS"], Amazon.RegionEndpoint.USEast1))
			{
				await cliente.DeleteObjectAsync(new DeleteObjectRequest() { BucketName = bucketname, Key = filename });
			}
		}
		public static async Task<Stream> DownloadFileS3(string bucketname, string arquivo, int clienteid)
		{
			using (TransferUtility fileTransferUtility = new TransferUtility(Configuration["ChaveAWS"], Configuration["SenhaAWS"], Amazon.RegionEndpoint.USEast1))
			{
				return await fileTransferUtility.OpenStreamAsync("moneoup", clienteid > 0 ? $"{clienteid}/{arquivo}" : arquivo);
			}
		}
		public async static Task UploadAamazon(byte[] data, string filename, string bucketname)
		{
			using (var mem = new MemoryStream(data))
			{

				using (TransferUtility fileTransferUtility = new TransferUtility(Configuration["ChaveAWS"], Configuration["SenhaAWS"], Amazon.RegionEndpoint.USEast1))
				{

					var fileTransfer = new TransferUtilityUploadRequest
					{
						BucketName = bucketname,
						InputStream = mem,
						Key = filename

					};


					await fileTransferUtility.UploadAsync(fileTransfer);
				}
			}
		}
		#endregion

		#endregion

		public async static Task<byte[]> Decompress(this byte[] data)
		{
			byte[] buffer;
			using (var mem = new MemoryStream())
			{
				await mem.WriteAsync(data, 0, data.Length);
				mem.Position = 0L;
				MemoryStream memoryStream2 = new MemoryStream();
				DeflateStream deflateStream = new DeflateStream((Stream)mem, CompressionMode.Decompress);
				await deflateStream.CopyToAsync((Stream)memoryStream2);
				buffer = new byte[memoryStream2.Length];
				memoryStream2.Position = 0L;
				await memoryStream2.ReadAsync(buffer, 0, buffer.Length);
				mem.Dispose();
				memoryStream2.Dispose();
				deflateStream.Dispose();

			}
			return buffer;
		}

		public async static Task<byte[]> Compress(this byte[] data)
		{
			byte[] buffer;
			using (var mem = new MemoryStream())
			{
				DeflateStream deflateStream = new DeflateStream((Stream)mem, CompressionMode.Compress, true);
				await deflateStream.WriteAsync(data, 0, data.Length);
				deflateStream.Dispose();
				buffer = new byte[mem.Length];
				mem.Position = 0L;
				await mem.ReadAsync(buffer, 0, buffer.Length);
				mem.Dispose();
				deflateStream.Dispose();
			}
			return buffer;
		}

		public static IEnumerable<CampanhaModelDTO> NonoDigito(this IEnumerable<CampanhaModelDTO> c, IEnumerable<PrefixoModel> nextel)
		{
			return (from a in c
					join b in nextel on a.Celular.ToPrefixo() equals b.Prefixo into ps
					from b in ps.DefaultIfEmpty()
					select new CampanhaModelDTO()
					{
						Celular = b == null ? InsereNonoDigito(a.Celular) : a.Celular,
						Texto = a.Texto,
						IDCliente = a.IDCliente,
						DataEnviar = a.DataEnviar,
						TipoCampanha = a.TipoCampanha,
						Carteira = a.Carteira,
						Operadora = b == null ? OperadorasEnums.DESCONHECIDA : OperadorasEnums.NEXTEL
					})
					.Distinct(new CompareObject<CampanhaModelDTO>(
						(a, b) => a.Celular == b.Celular && a.Texto == b.Texto,
						i => (i.Celular.GetHashCode() ^ i.Texto.GetHashCode()).GetHashCode()));
		}
		public static (IEnumerable<CampanhaModel>, int) NonoDigito(this IEnumerable<CampanhaModel> c, IEnumerable<PrefixoModel> nextel)
		{
			var dados1 = (from a in c
						  join b in nextel on a.Celular.ToPrefixo() equals b.Prefixo into ps
						  from b in ps.DefaultIfEmpty()
						  select new CampanhaModel()
						  {
							  Celular = b == null ? InsereNonoDigito(a.Celular) : a.Celular,
							  Texto = a.Texto,
							  IDCliente = a.IDCliente,
							  Arquivo = a.Arquivo,
							  DataEnviar = a.DataEnviar,
							  Cliente = a.Cliente,
							  Variaveis = a.Variaveis,
							  Usuario = a.Usuario,
							  TipoCampanha = a.TipoCampanha,
							  ArquivoZip = a.ArquivoZip,
							  Carteira = a.Carteira,
							  Operadora = b == null ? OperadorasEnums.DESCONHECIDA : OperadorasEnums.NEXTEL
						  })
					.Distinct(new CompareObject<CampanhaModel>(
						CompareItemCampanha(), CampanhaHashCode()));

			return (dados1, c.Count() - dados1.Count());
		}



		public static Func<CampanhaModel, int> CampanhaHashCode() => i => !string.IsNullOrEmpty(i.Texto) ? (i.Celular.GetHashCode() ^ i.Texto.GetHashCode()) : i.Celular.GetHashCode();

		public static Func<CampanhaModel, CampanhaModel, bool> CompareItemCampanha() => (a, b) => !string.IsNullOrEmpty(a.Texto) ? a.Celular == b.Celular && a.Texto == b.Texto : a.Celular == b.Celular;


		static Regex regGSM = new Regex("[@£$¥èéùìòÇ\nØø\rÅå\u0394_\u03a6\u0393\u039b\u03a9\u03a0\u03a8\u03a3\u0398\u039e€ÆæßÉ !\"#¤%&()*\\+-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà]+", RegexOptions.Compiled);

		public static Regex RegexEmail = new Regex(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\\x01-\\x08\\x0b\\x0c\x0e-\\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])", RegexOptions.Compiled);

		static Regex regNumero = new Regex("\\d+", RegexOptions.Compiled);


		public static string CleanInvalidCaracteres(this String s)
		{
			if (regNumero.IsMatch(s))
			{
				s = regNumero.Match(s).Value;
				if (s.Length >= 10 && s.Length <= 13)
					return s;
				else
					return null;
			}
			return null;

		}

		public static string ToAlphabetGSM(this string message)
		{
			var sb = new StringBuilder();

			var valor = message;
			foreach (var item in regGSM.Matches(message).Cast<Match>())
				sb.AppendFormat("{0} ", item.Value);


			return sb.ToString().Trim();
		}

		public static string NoSimbols(this string s) => Regex.Replace(s, "[\\s-_,.]", string.Empty);

		public static string NoAcento(this string s)
		{
			var valor = s;

			if (!string.IsNullOrEmpty(s))
				foreach (var item in regCharsAcento.Matches(s).Cast<Match>())
					valor = valor.Replace(item.Value, NoAcentoDic[AcentoDic[item.Value]]);
			return valor;
		}

		public async static Task<byte[]> ToArrayByte(this Stream s)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream m = new MemoryStream())
			{
				int read;
				while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
					await m.WriteAsync(buffer, 0, read);

				return m.ToArray();
			}
		}
		public async static Task<byte[]> ToUnGzip(this byte[] s)
		{
			using (var _mem = new MemoryStream(s))
			{
				using (GZipStream g = new GZipStream(_mem, CompressionMode.Decompress))
				{
					using (var _memfinal = new MemoryStream())
					{
						await g.CopyToAsync(_memfinal);
						return _memfinal.ToArray();
					}
				}
			}
		}
		public async static Task<Dictionary<string, IEnumerable<byte>>> ToUnZip(this byte[] s)
		{
			var lista = new Dictionary<string, IEnumerable<byte>>() { };
			using (var _mem = new MemoryStream(s))
			{
				using (ZipArchive zip = new ZipArchive(_mem, ZipArchiveMode.Read))
				{
					foreach (var a in zip.Entries)
					{
						using (var mem = new MemoryStream())
						{
							await a.Open().CopyToAsync(mem);
							mem.Position = 0L;
							lista.Add(a.Name, await mem.ToArrayByte());
						}
					}
				}
			}
			return lista;
		}
		public async static Task<byte[]> ZipFiles(Dictionary<string, byte[]> files)
		{
			using (var mem = new MemoryStream()) //stream final para o zip
			{
				using (var arquivo = new ZipArchive(mem, ZipArchiveMode.Create, true))
				{
					foreach (var item in files)
					{
						var _file = arquivo.CreateEntry(item.Key, CompressionLevel.Optimal);
						using (var entryFile = _file.Open())
						{
							var _b = item.Value;
							await entryFile.WriteAsync(_b, 0, _b.Count());
						}
					}
				}
				mem.Seek(0, SeekOrigin.Begin);
				return await mem.ToArrayByte();
			}
		}
		public async static Task<byte[]> ToZip(this Stream s, string file)
		{
			using (var mem = new MemoryStream()) //stream final para o zip
			{
				using (var arquivo = new ZipArchive(mem, ZipArchiveMode.Create, true))
				{
					var _file = arquivo.CreateEntry(file, CompressionLevel.Optimal);
					using (var entryFile = _file.Open())
					{
						s.Position = 0L;
						var _b = await s.ToArrayByte();
						await entryFile.WriteAsync(_b, 0, _b.Count());
					}
				}
				mem.Seek(0, SeekOrigin.Begin);
				return await mem.ToArrayByte();
			}
		}
		public static HashSet<T> ToHashSetEx<T>(this IEnumerable<T> s)
		{
			return new HashSet<T>(s);
		}
		public static int ToPrefixo(this decimal s)
		{
			var _s = s.ToString();
			return int.Parse(_s.ToString().Remove(_s.Length - 4));

		}
		public async static Task<Dictionary<string, IEnumerable<byte>>> ToUnZip(this Stream s)
		{
			s.Position = 0L;
			var lista = new Dictionary<string, IEnumerable<byte>>() { };
			using (ZipArchive zip = new ZipArchive(s, ZipArchiveMode.Read))
			{
				foreach (var a in zip.Entries)
				{
					using (var mem = new MemoryStream())
					{
						await a.Open().CopyToAsync(mem);
						mem.Position = 0L;
						lista.Add(a.Name, mem.ToArray());
					}
				}
			}
			return lista;
		}

		public const int TIMEOUTEXECUTE = 888;

		public static IConfigurationRoot Configuration
		{
			get
			{
				return new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
					.Build();
			}
		}

		public static ReportDeliveryEnums Report(byte statusenvio, byte? statusreport)
		{
			var report = ReportDeliveryEnums.ENVIADA;

			switch (statusenvio)
			{
				case 2:
					if (statusreport.HasValue)
						report = ((ReportDeliveryEnums)Enum.Parse(typeof(ReportDeliveryEnums), statusreport.Value.ToString()));
					break;
				case 0: report = ReportDeliveryEnums.AGENDADA; break;
				case 1: report = ReportDeliveryEnums.ENVIANDO; break;
				case 3: report = ReportDeliveryEnums.ERRO; break;
				case 5: report = ReportDeliveryEnums.CANCELADA; break;
				case 4: report = ReportDeliveryEnums.SUSPENSA; break;
			}

			return report;
		}


		public static string ConnString=> Configuration.GetConnectionString("DefaultConnection");


		public static string EnumDescription(this Enum valor)
		{
			FieldInfo fio = valor.GetType().GetField(valor.ToString());
			DescriptionAttribute[] atributos = (DescriptionAttribute[])fio.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (atributos.Length > 0)
				return atributos[0].Description;
			else
				return valor.ToString();
		}

		public static decimal Percentual(decimal? d, decimal? i)
		{
			if (d.HasValue && d.Value > 0 && i.HasValue && i.Value > 0)
				return ((d.Value * 100) / i.Value);
			else
				return 0;
		}

        public static decimal Match(string o, string d)
        {
            var _o = o.ToCharArray();
            var _d = d.ToCharArray();

            Array.Sort(_o);
            Array.Sort(_d);

            List<bool> lmatch = new List<bool>();

            for (var i = 0; i < _o.Length; i++)
            {
                if (_d.Length > i)
                    lmatch.Add(_o[i].Equals(_d[i]));
                else
                    lmatch.Add(false);
            }

            if (_d.Length > _o.Length)
            {
                var diferenca = _d.Length - _o.Length;

                for (var i = 0; i < diferenca; i++)
                    lmatch.Add(false);
            }

            var match = Percentual(lmatch.Where(k => k).Count(), lmatch.Count());

            return match;
        }

        public static string GetKeyEncodingToken
        {
            get { return "#m0n30c!&c0n3ctt@"; }
        }

        public static Encoding EncoderDefaultFiles
        {
            get { return Encoding.GetEncoding(1252); }
        }
    }
}
