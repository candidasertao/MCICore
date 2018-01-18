using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
  public  class IOPeopleModel
    {
		public IEnumerable<ReplyGenericModel> replies { get; set; }
		public IOPeopleModel(IEnumerable<ReplyGenericModel> r) => replies = r;
	}
}
