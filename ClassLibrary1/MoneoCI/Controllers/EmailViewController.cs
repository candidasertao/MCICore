using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneoCI.Repository;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Controllers
{
	[Produces("application/json")]
	[Route("api/[controller]")]
	public class EmailViewController:ControllerBase
    {
		readonly IRepository<EmailViewModel> repository = null;

		public EmailViewController(IRepository<EmailViewModel> repos)
		{
			repository = repos;
		}

		public Task<EmailViewModel> AdicionaItem(string t)
		{
			throw new NotImplementedException();
		}

		[HttpPost]
		[AllowAnonymous]
		[Route("add/")]
		public async Task<IActionResult> AdicionaItem([FromBody] IEnumerable<EmailViewModel> t)
		{
			//if (!ModelState.IsValid)
			//	return BadRequest();

		//	await repository.Add(t);
			//GestorRepository g = new GestorRepository();
			//t.Start= DateTime.Now;
			//await g.Adicionar(t);
			//t.End = DateTime.Now;
			
			throw new NotImplementedException();
		}

		public Task AdicionaItens(IEnumerable<EmailViewModel> t)
		{
			throw new NotImplementedException();
		}

		public Task AtualizaItem(EmailViewModel t)
		{
			throw new NotImplementedException();
		}

		public Task AtualizaItens(IEnumerable<EmailViewModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<EmailViewModel>> BuscaItens(EmailViewModel t)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItem(EmailViewModel t)
		{
			throw new NotImplementedException();
		}

		public Task ExcluirItens(IEnumerable<EmailViewModel> t)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<EmailViewModel>> ObterItens()
		{
			throw new NotImplementedException();
		}
	}
}
