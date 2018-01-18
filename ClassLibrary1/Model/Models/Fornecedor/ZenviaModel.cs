using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Fornecedor
{
	public class ZenviaModel
	{
		public class ZenviaJson
		{
			[JsonProperty("callbackmorequest", NullValueHandling = NullValueHandling.Ignore)]
			public callbackMoRequest CallBackMoRequest { get; set; }
			[JsonProperty("callbackMtRequest", NullValueHandling = NullValueHandling.Ignore)]
			public callbackMtRequest CallbackMtRequest { get; set; }
		}
		public class callbackMoRequest
		{
			public string id { get; set; }
			public string mobile { get; set; }
			public string shortCode { get; set; }
			public string account { get; set; }
			public string body { get; set; }
			public DateTime received { get; set; }
			public string correlatedMessageSmsId { get; set; }
		}
		public class callbackMtRequest
		{
			public string status { get; set; }
			public string statusMessage { get; set; }
			public string statusDetail { get; set; }
			public string statusDetailMessage { get; set; }
			public string id { get; set; }
			public DateTime received { get; set; }
			public string mobileOperatorName { get; set; }
		}
		public class sendSmsMultiResponse
		{
			public string statusCode { get; set; }
			public string statusDescription { get; set; }
			public string detailCode { get; set; }
			public string detailDescription { get; set; }
		}

        public class Root
        {
            public Root1 sendSmsMultiResponse { get; set; }
        }

        public class Root1
        {
            public List<Root2> sendSmsResponseList { get; set; }
        }

        public class Root2
        {
            public string statusCode { get; set; }
            public string statusDescription { get; set; }
            public string detailCode { get; set; }
            public string detailDescription { get; set; }
        }
    }
}
