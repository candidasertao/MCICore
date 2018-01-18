using DAL;
using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class UsuarioRepository : IRepository<UsuarioModel>
	{
		DALUsuario dal;

		public Task<int> Add(UsuarioModel r, int c, int? u)
		{
			dal = new DALUsuario();
			return dal.AddUser(r, c, u);
		}
		public Task<IEnumerable<UsuarioModel>> RegravaTodosUsuarios()
		{
			dal = new DALUsuario();
			return dal.RegravaTodosUsuarios();
		}

		public Task<UsuarioModel> FindById(UsuarioModel t, int? u)
		{
			dal = new DALUsuario();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<UsuarioModel>> GetAll(UsuarioModel t, int? u)
		{
			dal = new DALUsuario();
			return dal.ObterTodosAsync(t, u);
		}

		public Task Remove(IEnumerable<UsuarioModel> r, int c, int? u)
		{
			dal = new DALUsuario();
            return dal.ExcluirItensAsync(r, c, u);
        }
		public Task<int> ExcluirItensAsyncAfected(IEnumerable<UsuarioModel> r, int c, int? u)
		{
			dal = new DALUsuario();
			return dal.ExcluirItensAsyncAfected(r, c, u);
		}
		
		public Task<IEnumerable<UsuarioModel>> Search(UsuarioModel g, string s, int? u)
		{
			dal = new DALUsuario();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task<UsuarioModel> UsuarioByLoginUser(UsuarioModel u)
		{
			dal = new DALUsuario();
			return dal.UsuarioByLoginName(u);
		}

		public Task<IEnumerable<CarteiraModel>> BuscaCarteirasByUsuarioID(UsuarioModel g, int c)
		{
			dal = new DALUsuario();
			return dal.BuscaCarteirasByUsuarioID(g, c);
		}
		public Task<IEnumerable<UsuarioModel>> ObterTodosByCarteiraID(UsuarioModel g)
		{
			dal = new DALUsuario();
			return dal.ObterTodosByCarteiraID(g);
		}
		public Task Update(IEnumerable<UsuarioModel> r, int c, int? u)
		{
			dal = new DALUsuario();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public ValueTask<int> RenovaSaldoUsuario(UsuarioModel u, int c)
		{
			dal = new DALUsuario();
			return dal.RenovaSaldoUsuarioAsync(u, c);
		}

		public Task<int> AlteraSaldoUsuarioEnvio(UsuarioModel u, int c)
		{
			dal = new DALUsuario();
			return dal.AlteraSaldoUsuarioEnvio(u, c, u.Cota.Value);
		}
		public ValueTask<string> BuscaByEmail(UsuarioModel u, int c)
		{
			dal = new DALUsuario();
			return dal.BuscaByEmail(u, c);
		}
		public Task<(int, bool)> SaldoUsuario(int c, int u)
		{
			dal = new DALUsuario();
			return dal.SaldoUsuario(c, u);
		}

		public Task Add(IEnumerable<UsuarioModel> r, int c, int? u)
		{
			throw new NotImplementedException();
		}



		public Task<IEnumerable<UsuarioModel>> GetAllPaginado(UsuarioModel t, int? u)
		{
			dal = new DALUsuario();
			return dal.ObterTodosPaginadoAsync(t, u);
		}

        public Task<dynamic> DadosCadastrais(int u)
        {
            dal = new DALUsuario();
            return dal.DadosCadastrais(u);
        }
    }
}
