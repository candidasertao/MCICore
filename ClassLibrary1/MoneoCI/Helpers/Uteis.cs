
using Amazon.S3.Transfer;
using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using DAL;
using Helpers;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace System.Linq
{
	//public class Teste: IEnlistmentNotification
	//{ }


	public class PasswordGeneratorSettings
	{
		const string LOWERCASE_CHARACTERS = "abcdefghijklmnopqrstuvwxyz";
		const string UPPERCASE_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string NUMERIC_CHARACTERS = "0123456789";
		const string SPECIAL_CHARACTERS = @"!#$%&*@\";
		const int PASSWORD_LENGTH_MIN = 8;
		const int PASSWORD_LENGTH_MAX = 128;

		public bool IncludeLowercase { get; set; }
		public bool IncludeUppercase { get; set; }
		public bool IncludeNumbers { get; set; }
		public bool IncludeSpecial { get; set; }
		public int PasswordLength { get; set; }
		public string CharacterSet { get; set; }
		public int MaximumAttempts { get; set; }

		public PasswordGeneratorSettings(bool includeLowercase, bool includeUppercase, bool includeNumbers, bool includeSpecial, int passwordLength)
		{
			IncludeLowercase = includeLowercase;
			IncludeUppercase = includeUppercase;
			IncludeNumbers = includeNumbers;
			IncludeSpecial = includeSpecial;
			PasswordLength = passwordLength;

			StringBuilder characterSet = new StringBuilder();

			if (includeLowercase)
			{
				characterSet.Append(LOWERCASE_CHARACTERS);
			}

			if (includeUppercase)
			{
				characterSet.Append(UPPERCASE_CHARACTERS);
			}

			if (includeNumbers)
			{
				characterSet.Append(NUMERIC_CHARACTERS);
			}

			if (includeSpecial)
			{
				characterSet.Append(SPECIAL_CHARACTERS);
			}

			CharacterSet = characterSet.ToString();
		}

		public bool IsValidLength()
		{
			return PasswordLength >= PASSWORD_LENGTH_MIN && PasswordLength <= PASSWORD_LENGTH_MAX;
		}

		public string LengthErrorMessage()
		{
			return string.Format("Password length must be between {0} and {1} characters", PASSWORD_LENGTH_MIN, PASSWORD_LENGTH_MAX);
		}
	}


	public static class PasswordGenerator
	{

		/// <summary>
		/// Generates a random password based on the rules passed in the settings parameter
		/// </summary>
		/// <param name="settings">Password generator settings object</param>
		/// <returns>Password or try again</returns>
		public static string GeneratePassword(PasswordGeneratorSettings settings)
		{
			const int MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS = 2;
			char[] password = new char[settings.PasswordLength];
			int characterSetLength = settings.CharacterSet.Length;

			Random random = new Random();
			for (int characterPosition = 0; characterPosition < settings.PasswordLength; characterPosition++)
			{
				password[characterPosition] = settings.CharacterSet[random.Next(characterSetLength - 1)];

				bool moreThanTwoIdenticalInARow =
					characterPosition > MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS
					&& password[characterPosition] == password[characterPosition - 1]
					&& password[characterPosition - 1] == password[characterPosition - 2];

				if (moreThanTwoIdenticalInARow)
					characterPosition--;
			}

			return string.Join(null, password);
		}


		/// <summary>
		/// When you give it a password and some settings, it validates the password against the settings.
		/// </summary>
		/// <param name="settings">Password settings</param>
		/// <param name="password">Password to test</param>
		/// <returns>True or False to say if the password is valid or not</returns>
		public static bool PasswordIsValid(PasswordGeneratorSettings settings, string password)
		{
			const string REGEX_LOWERCASE = @"[a-z]";
			const string REGEX_UPPERCASE = @"[A-Z]";
			const string REGEX_NUMERIC = @"[\d]";
			const string REGEX_SPECIAL = @"([!#$%&*@\\])+";

			bool lowerCaseIsValid = !settings.IncludeLowercase || (settings.IncludeLowercase && Regex.IsMatch(password, REGEX_LOWERCASE));
			bool upperCaseIsValid = !settings.IncludeUppercase || (settings.IncludeUppercase && Regex.IsMatch(password, REGEX_UPPERCASE));
			bool numericIsValid = !settings.IncludeNumbers || (settings.IncludeNumbers && Regex.IsMatch(password, REGEX_NUMERIC));
			bool symbolsAreValid = !settings.IncludeSpecial || (settings.IncludeSpecial && Regex.IsMatch(password, REGEX_SPECIAL));

			return lowerCaseIsValid && upperCaseIsValid && numericIsValid && symbolsAreValid;
		}
	}

	public static class Uteis
	{

		//public static byte[] Serialize<T>(this T thisObj)
		//{
		//	return MessagePackSerializer.Serialize<T>(thisObj);
		//}

		//public static T Deserialize<T>(this byte[] bytes)
		//{
		//	return MessagePackSerializer.Deserialize<T>(bytes);
		//}



			





		

		public static Task<byte[]> ToObjectToByteArray<T>(this T obj)
		{
			return Task
				.Factory
				.StartNew(() => Encoding
				.UTF7
				.GetBytes(JsonConvert.SerializeObject(obj)));
		}

		public static Task<T> ByteArrayToOject<T>(this byte[] obj)
		{
			return Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(Encoding.UTF7.GetString(obj)));
		}

		public async static Task GravaArquivo(Stream arquivo, string bucketname)
		{
			using (TransferUtility fileTransferUtility = new TransferUtility(Util.Configuration["ChaveAWS"], Util.Configuration["SenhaAWS"], Amazon.RegionEndpoint.USEast1))
			{
				var fileTransfer = new TransferUtilityUploadRequest { BucketName = bucketname, InputStream = arquivo };
				await fileTransferUtility.UploadAsync(fileTransfer);
			}
		}

		public static string GeraSenha()
		{

			const int MAXIMUM_PASSWORD_ATTEMPTS = 10000;
			bool includeLowercase = true;
			bool includeUppercase = true;
			bool includeNumeric = true;
			bool includeSpecial = false;
			int lengthOfPassword = 8;

			PasswordGeneratorSettings settings = new PasswordGeneratorSettings(includeLowercase, includeUppercase, includeNumeric, includeSpecial, lengthOfPassword);
			string password;
			if (!settings.IsValidLength())
			{
				password = settings.LengthErrorMessage();
			}
			else
			{
				int passwordAttempts = 0;
				do
				{
					password = PasswordGenerator.GeneratePassword(settings);
					passwordAttempts++;
				}
				while (passwordAttempts < MAXIMUM_PASSWORD_ATTEMPTS && !PasswordGenerator.PasswordIsValid(settings, password));
				password = PasswordGenerator.PasswordIsValid(settings, password) ? password : "Try again";
			}

			if (password == "Try Again")
				GeraSenha();

			return password;
		}

		public static Task<T> GetComplexData<T>(this ISession session, string key)
		{
			var data = session.GetString(key);

			if (data == null)
				return Task.Factory.StartNew<T>(() => default(T));

			return Task.Factory.StartNew<T>(() => JsonConvert.DeserializeObject<T>(data));
		}

		public static Task SetComplexData(this ISession session, string key, object value)
		{


			return Task.Factory.StartNew(() => session.SetString(key, JsonConvert.SerializeObject(value)));
		}

		public static Task<T> FromArrayToT<T>(this byte[] arr)
		{

			return Task
				.Factory
				.StartNew(() => JsonConvert
				.DeserializeObject<T>(Encoding
				.UTF7
				.GetString(arr)));

		}

		public static Task<IEnumerable<PrefixoModel>> CargaPrefixos()
		{
			DALPrefixo dal = new DALPrefixo();
			return dal.ObterTodos();
		}





		public static IEnumerable<CampanhaGridLotesModel> TakeGroup(this IEnumerable<CampanhaModel> s, int q, DateTime data, int intervalo)
		{
			var quantByLote = (int)Math.Ceiling((decimal)s.Count() / (decimal)q);
			var listagem = new List<CampanhaGridLotesModel>() { };
			var dataLote = new DateTime();

			var lote = 1;

			do
			{
				var itens = s.Take(quantByLote).ToList();

				if (lote == 1)
					dataLote = data;
				else
					dataLote = dataLote.AddMinutes(intervalo);

				if (dataLote.Hour >= 22 && dataLote.Hour <= 23)
					dataLote = dataLote.AddHours(10);

				listagem.Add(new CampanhaGridLotesModel()
				{
					DataEnviar = dataLote,
					Intervalos = intervalo,
					Data = dataLote.ToString("dd/MM/yyyy"),
					Hora = dataLote.ToString("HH:mm"),
					Lote = lote,
					Quantidade = itens.Count
				});

				s = s.Skip(quantByLote);

				lote++;

			} while (s.Count() > 0);

			return listagem;
		}


	}
}


