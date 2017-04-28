using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Slalom.Stacks.Services;
using Slalom.Stacks.Services.Messaging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Functions
{
    public class Function
    {
        public static Stack Stack;

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req)
        {
            var path = req.RequestUri.PathAndQuery.Split('?')[0].Trim('/');
            if (path.StartsWith("api"))
            {
                path = path.Substring(3).Trim('/');
            }
            var registry = Stack.GetServices();
            var endPoint = registry.Find(path);
            if (endPoint != null)
            {
                using (var stream = new MemoryStream())
                {
                    await req.Content.CopyToAsync(stream);

                    var content = Encoding.UTF8.GetString(stream.ToArray());
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        content = null;
                    }
                    var result = await Stack.Send(path, content);
                    return HandleResult(result);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage HandleResult(MessageResult result)
        {
            if (result.ValidationErrors.Any(e => e.Type == ValidationType.Input))
            {
                return Respond(result.ValidationErrors, HttpStatusCode.BadRequest);
            }
            if (result.ValidationErrors.Any(e => e.Type == ValidationType.Security))
            {
                return Respond(result.ValidationErrors, HttpStatusCode.Unauthorized);
            }
            if (result.ValidationErrors.Any())
            {
                return Respond(result.ValidationErrors, HttpStatusCode.Conflict);
            }
            if (!result.IsSuccessful)
            {
                var message = "An unhandled exception was raised on the server.  Please try again.  " + result.CorrelationId;
                return Respond(message, HttpStatusCode.InternalServerError);
            }
            if (result.Response != null)
            {
                return Respond(result.Response, HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private static HttpResponseMessage Respond(object content, HttpStatusCode statusCode)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var message = new HttpResponseMessage(statusCode);
            message.Content = new StringContent(JsonConvert.SerializeObject(content, settings), Encoding.UTF8, "application/json");
            return message;
        }
    }
}