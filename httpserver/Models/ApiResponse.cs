using System.Web.Script.Serialization;

namespace httpserver.Models
{
    public class ApiResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json; charset=utf-8";
        public string Body { get; set; } = "{}";

        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public static ApiResponse Ok(object payload) =>
            new ApiResponse { StatusCode = 200, Body = _serializer.Serialize(payload) };

        public static ApiResponse Created(object payload) =>
            new ApiResponse { StatusCode = 201, Body = _serializer.Serialize(payload) };

        public static ApiResponse NotFound(string message) =>
            new ApiResponse { StatusCode = 404, Body = _serializer.Serialize(new { error = message }) };

        public static ApiResponse BadRequest(string message) =>
            new ApiResponse { StatusCode = 400, Body = _serializer.Serialize(new { error = message }) };

        public static ApiResponse Error(string message) =>
            new ApiResponse { StatusCode = 500, Body = _serializer.Serialize(new { error = message }) };
    }
}
