using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public interface IRepository<T>
	{
		Task<IEnumerable<T>> GetAllPaginado(T t, int? u);
		Task<IEnumerable<T>> GetAll(T t, int? u);
		Task<IEnumerable<T>> Search(T g, string s, int? u);
		Task<T> FindById(T t, int? u);
		Task Add(IEnumerable<T> r,int c, int? u);
		Task Update(IEnumerable<T> r, int c, int? u);
		Task Remove(IEnumerable<T> r, int c, int? u);
	}
}
