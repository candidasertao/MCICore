using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	interface IControllers<T> where T : class
	{
		Task<IActionResult> GetByIDAsync(int id);
		Task<IActionResult> GetAll();
		Task<IActionResult> GetAllPaginadoAsync([FromBody] T t);
		Task<IActionResult> Search(string s);
		Task<IActionResult> AdicionaItemAsync([FromBody] IEnumerable<T> t);
		Task<IActionResult> AtualizaItemAsync([FromBody] IEnumerable<T> t);
		Task<IActionResult> ExcluirItemAsync([FromBody] IEnumerable<T> t);
	}
}
