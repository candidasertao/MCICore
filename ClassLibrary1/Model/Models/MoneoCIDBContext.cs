using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public  class MoneoCIDBContext: DbContext
	{
		public MoneoCIDBContext(DbContextOptions<MoneoCIDBContext> options): base(options)
        { }

		public DbSet<ClienteModel> Clientes { get; set; }
		public DbSet<CampanhaModel> Campanhas { get; set; }
		public DbSet<FornecedorModel> Fornecedor { get; set; }
	}
}
