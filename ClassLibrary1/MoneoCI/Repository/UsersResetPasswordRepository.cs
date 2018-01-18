using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;
using ConecttaManagerData.DAL;

namespace MoneoCI.Repository
{
    public class UsersResetPasswordRepository:IRepository<UsersResetPasswordModel>
	{
		DALUsersResetPassword dal;


		public Task Add(IEnumerable<UsersResetPasswordModel> r, int c, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.AdicionarItensAsync(r, c, u);
		}

		public Task<UsersResetPasswordModel> FindById(UsersResetPasswordModel t, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.BuscarItemByIDAsync(t, u);
		}

		public Task<IEnumerable<UsersResetPasswordModel>> GetAll(UsersResetPasswordModel t, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.ObterTodosAsync(t, u);
		}

		public Task<IEnumerable<UsersResetPasswordModel>> GetAllPaginado(UsersResetPasswordModel t, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.ObterTodosPaginadoAsync(t, u);
		}

		public Task Remove(IEnumerable<UsersResetPasswordModel> r, int c, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.ExcluirItensAsync(r, c, u);
		}

		public Task<IEnumerable<UsersResetPasswordModel>> Search(UsersResetPasswordModel g, string s, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.BuscarItensAsync(g, s, u);
		}

		public Task Update(IEnumerable<UsersResetPasswordModel> r, int c, int? u)
		{
			dal = new DALUsersResetPassword();
			return dal.AtualizaItensAsync(r, c, u);
		}
	}
}
