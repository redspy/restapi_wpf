using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace httpserver.Models
{
    public class RequestContext
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string RawBody { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParams { get; set; }

        public T DeserializeBody<T>()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(RawBody);
        }
    }
}
