using Microsoft.Extensions.Caching.Memory;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class Cache
    {
        readonly static IMemoryCache _cache;

        public static List<PrefixoModel> GetPrefixoNextel()
        {
            try
            {

            }
            catch (Exception err)
            {

            }

            return _cache.Get<List<PrefixoModel>>("nextel");
        }

        public static List<PrefixoModel> GetPrefixoModel()
        {
            try
            {

            }
            catch (Exception err)
            {

            }

            return _cache.Get<List<PrefixoModel>>("prefixos");
        }

        public async static Task<HashSet<decimal>> GetFiltrados()
        {
            try
            {
                using (var mem = new MemoryStream())
                {
                    var stream = await Util.DownloadFileS3("moneoup", "rejeitados.zip", 0);
                    await stream.CopyToAsync(mem);
                    var unziped = await mem.ToUnZip();
                    var linhas = Util.ListaCelulares(unziped["rejeitados.csv"].ToArray());
                    _cache.Set("quarentena", linhas.Select(a => decimal.Parse(a.ElementAt(0))).ToList());
                }
            }
            catch (Exception err)
            {

            }

            return _cache.Get<IEnumerable<decimal>>("quarentena").ToHashSetEx();
        }
    }
}
