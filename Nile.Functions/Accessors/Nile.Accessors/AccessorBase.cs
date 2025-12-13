using Microsoft.Extensions.Logging;
using Nile.Common;
using Nile.Accessors.Paging;
using System.Text;
using System.Text.Json;
using Nile.Common.InternalDTOs;

namespace Nile.Accessors
{
    public abstract class AccessorBase : ServiceContractBase
    {
        public AccessorBase(ILogger logger) : base(logger)
        {
        }


        protected static bool DecodePagingToken<T>(string? pagingToken, out T? token) where T : PagingTokenBase
        {
            var bytes = Convert.FromBase64String(pagingToken);
            var json = Encoding.Default.GetString(bytes);

            try
            {
                token = JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception)
            {
                token = null;
            }
            return token is not null;
        }

        protected static string EncodePagingToken<T>(T token) where T : PagingTokenBase
        {
            var json = JsonSerializer.Serialize(token);
            var bytes = Encoding.Default.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}