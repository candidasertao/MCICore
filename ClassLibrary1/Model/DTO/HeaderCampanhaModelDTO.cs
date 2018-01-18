using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DTO
{
  public  class HeaderCampanhaModel:BaseEntity
    {
		[JsonProperty("campanhas", Required = Required.Always)]
		public IEnumerable<CampanhaModel> Campanhas { get; set; }
	}
}
