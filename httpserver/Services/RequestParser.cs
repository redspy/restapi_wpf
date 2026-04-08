using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using httpserver.Models;

namespace httpserver.Services
{
    public class RequestParser
    {
        public RequestContext Parse(HttpListenerRequest req)
        {
            string body = "";
            if (req.HasEntityBody)
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    body = reader.ReadToEnd();

            var queryParams = new Dictionary<string, string>();
            foreach (string key in req.QueryString.AllKeys)
                if (key != null)
                    queryParams[key] = req.QueryString[key];

            string path = req.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            if (string.IsNullOrEmpty(path)) path = "/";

            return new RequestContext
            {
                Method = req.HttpMethod.ToUpperInvariant(),
                Path = path,
                RawBody = body,
                Headers = req.Headers.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => req.Headers[k]),
                QueryParams = queryParams
            };
        }
    }
}
