using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class PrefixoRepository
	{
		DALPrefixo dal;

		public Task Add(IEnumerable<PrefixoModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<PrefixoModel> FindById(PrefixoModel t, int? u)
		{
			throw new NotImplementedException();
		}

		

		public Task Remove(IEnumerable<PrefixoModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<PrefixoModel>> Search(PrefixoModel g, string s, int? u)
		{
			throw new NotImplementedException();
		}

		public Task Update(IEnumerable<PrefixoModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
