﻿using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public class TokenAuthOptions
	{
			public string Path { get; set; } = "/token";
			public string Issuer { get; set; }
			public string Audience { get; set; }
			public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);
			public SigningCredentials SigningCredentials { get; set; }
	}
}