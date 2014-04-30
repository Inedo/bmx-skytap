using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Caching;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;
using Inedo.Web.Handlers;

namespace Inedo.BuildMasterExtensions.Skytap
{
    internal static class Ajax
    {
        [AjaxMethod]
        public static object GetConfigurations(string token)
        {
            var rawToken = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(token), null, DataProtectionScope.LocalMachine));
            var parts = rawToken.Split(new[] { ':' }, 3);

            var cache = HttpContext.Current.Cache;
            var cachedValues = cache.Get(parts[0]);

            if (cachedValues == null)
            {
                var client = new SkytapClient(parts[1], parts[2]);
                cachedValues = client
                    .ListConfigurations()
                    .Select(c => new { id = Uri.EscapeDataString(c.Id) + "&" + Uri.EscapeDataString(c.Name), text = c.Name })
                    .ToList();

                cache.Add(parts[0], cachedValues, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration, CacheItemPriority.Low, null);
            }

            return cachedValues;
        }

        [AjaxMethod]
        public static object GetTemplates(string token)
        {
            var rawToken = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(token), null, DataProtectionScope.LocalMachine));
            var parts = rawToken.Split(new[] { ':' }, 3);

            var cache = HttpContext.Current.Cache;
            var cachedValues = cache.Get(parts[0]);

            if (cachedValues == null)
            {
                var client = new SkytapClient(parts[1], parts[2]);
                cachedValues = client
                    .ListTemplates()
                    .Select(t => new { id = Uri.EscapeDataString(t.Id) + "&" + Uri.EscapeDataString(t.Name), text = t.Name })
                    .ToList();

                cache.Add(parts[0], cachedValues, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration, CacheItemPriority.Low, null);
            }

            return cachedValues;
        }
    }
}
