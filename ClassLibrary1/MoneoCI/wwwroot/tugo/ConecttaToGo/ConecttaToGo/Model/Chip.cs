using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConecttaToGo.Model
{
    class Chip
    {
        public virtual string Numero { get; set; }

        public virtual string SenhaToGo { get; set; }

        public virtual string Username
        {
            get
            {
                return "55" + Numero;
            }
        }

        public virtual string AuthorizationToGO
        {
            get
            {
                return "Basic "+ System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(this.Username + ":" + this.SenhaToGo));
            }
        }

        public virtual string SenhaWs { get; set; }
    }
}
