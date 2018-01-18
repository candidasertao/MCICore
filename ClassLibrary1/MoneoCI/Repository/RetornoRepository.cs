using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneoCI.Repository
{
    public class RetornoRepository : IRepository<RetornoModel>
    {
        DALRetorno dal;

        public Task Add(IEnumerable<RetornoModel> r, int c, int? u)
        {
			dal = new DALRetorno();
			return dal.AdicionarItens(r, c, u);
        }

        public Task AddByApi(IEnumerable<RetornoModel> r)
        {
			dal = new DALRetorno();
			return dal.AdicionarItens(r, 0, null);
		}

        public Task<RetornoModel> FindById(RetornoModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RetornoModel>> GetAll(RetornoModel t, int? u)
        {
            throw new NotImplementedException();
        }
        public Task<(IEnumerable<RetornoModel>, IEnumerable<RetornoModel>)> GetAllTuple(RetornoModel t, int? u)
        {
            dal = new DALRetorno();
            return dal.ObterTodosTupla(t, u);
        }

        public Task AtualizaRetornoNimura(RetornoModel r)
        {
            dal = new DALRetorno();
            return dal.AtualizaRetornoIoPeople(r);
        }

        public Task<IEnumerable<RetornoModel>> DashBoard(int c)
        {
            dal = new DALRetorno();
            return dal.DashBoard(c);
        }
        public Task<IEnumerable<ClassificacaoIOModel>> ClassificacaoIOPeople()
        {
            dal = new DALRetorno();
            return dal.ClassificacaoIOPeople();
        }
        public Task Remove(IEnumerable<RetornoModel> r, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RetornoModel>> Search(RetornoModel g, string s, int? u)
        {
            throw new NotImplementedException();
        }

        public Task Update(IEnumerable<RetornoModel> r, int c, int? u)
        {
            dal = new DALRetorno();
            return dal.AtualizaItens(r, c, u);
        }
        public Task<IEnumerable<RetornoModel>> RetornosAPI(RetornoModel r)
        {
            dal = new DALRetorno();
            return dal.RetornosAPI(r);

        }
        public Task<IEnumerable<RetornoModel>> GetAllPaginado(RetornoModel t, int? u)
        {
            throw new NotImplementedException();
        }
    }
}
