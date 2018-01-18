using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
	public class GrupoUsuariosRepository : IRepository<GrupoUsuariosModel>
	{
		DALGrupoUsuario dal;

		public Task Add(IEnumerable<GrupoUsuariosModel> r, int c, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<GrupoUsuariosModel> FindById(GrupoUsuariosModel t, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.BuscarItemByIDAsync(t, u);
		}
		public Task<GrupoUsuariosModel> PaginasPermissao(GrupoUsuariosModel t, int c)
		{
			dal = new DALGrupoUsuario();
			return dal.PaginasPermissao(t, c);
		}
		public Task<IEnumerable<GrupoUsuariosModel>> GetAll(GrupoUsuariosModel t, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.ObterTodosAsync(t, u);
		}
		public ValueTask<byte> PermissaoPagina(int grupousuarioid, int? subpaginaid, int paginaid, int c)
		{
			dal = new DALGrupoUsuario();
			return dal.PermissaoPaginaAsync(grupousuarioid, subpaginaid, paginaid, c);
		}
		public Task Remove(IEnumerable<GrupoUsuariosModel> r, int c, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<GrupoUsuariosModel>> Search(GrupoUsuariosModel g, string s, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<GrupoUsuariosModel> r, int c, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.AtualizaItensAsync(r, c, u);
		}

		public Task AtualizaSaldoGrupo(GrupoUsuariosModel r, int c, int? u)
		{
			dal = new DALGrupoUsuario();
			return dal.AtualizaSaldoGrupo(r, c, u);
		}
		public Task AtualizaPermissaoPaginaAsync(IEnumerable<GrupoUsuarioPaginas> r, int c)
		{
			dal = new DALGrupoUsuario();
			return dal.AtualizaPermissaoPaginaAsync(r, c);
		}

		public Task<IEnumerable<PaginaModel>> RetornaPaginas(int clienteid)
		{
			dal = new DALGrupoUsuario();
			return dal.RetornaPaginas(clienteid);
		}
		public Task<IEnumerable<GrupoUsuariosModel>> ListaGrupos(int c)
		{
			dal = new DALGrupoUsuario();
			return dal.ListaGrupos(c);
		}
		public Task<IEnumerable<GrupoUsuariosModel>> GetAllPaginado(GrupoUsuariosModel t, int? u)
		{
			throw new NotImplementedException();
		}
	}
}
