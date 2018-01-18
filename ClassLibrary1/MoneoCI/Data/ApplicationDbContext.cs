﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models;

namespace MoneoCI.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
		
		public ApplicationDbContext(DbContextOptions options)
			  : base(options) { }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Customize the ASP.NET Identity model and override the defaults if needed.
			// For example, you can rename the ASP.NET Identity table names and more.
			// Add your customizations after calling base.OnModelCreating(builder);
		}

		private static bool _databaseChecked;
		// The following code creates the database and schema if they don't exist.
		// This is a temporary workaround since deploying database through EF migrations is
		// not yet supported in this release.
		// Please see this http://go.microsoft.com/fwlink/?LinkID=615859 for more information on how to do deploy the database
		// when publishing your application.
		public void EnsureDatabaseCreated()
		{
			if (!_databaseChecked)
			{
				_databaseChecked = true;
				this.Database.EnsureCreated();
			}
		}
		//public DbSet<Application> Applications { get; set; }
	}
}
