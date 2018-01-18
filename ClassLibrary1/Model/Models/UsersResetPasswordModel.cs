using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
   public class UsersResetPasswordModel:BaseEntity
    {
		public ClienteModel Cliente { get; set; }
		public UsuarioModel Usuario { get; set; }
		public bool SenhaTrocada { get; set; }
		public int Codigo { get; set; }
		public string LoginUser { get; set; }
	}
}
