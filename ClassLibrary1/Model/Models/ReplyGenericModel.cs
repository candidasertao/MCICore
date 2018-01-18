using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{

	public  class ReplyGenericModel
	{
		public string messageId { get; set; }
		public string reference { get; set; }
		public string message { get; set; }
		public DateTime received { get; set; }
		public string from { get; set; }
		public string accountId { get; set; }
		public string accountName { get; set; }
		public DateTime sentOriginal { get; set; }
		public string messageOriginal { get; set; }
		public string mailingName { get; set; }
		public string mailingId { get; set; }

	}
	
}
