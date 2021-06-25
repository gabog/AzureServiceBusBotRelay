using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace Microsoft.ServiceBusBotRelay.Core
{
    public static class Logger
    {

        /// <summary>
        /// Logs the request's starting time
        /// </summary>
        /// <param name="startTimeUtc"></param>
        public static void LogRequest(DateTime startTimeUtc)
        {
            var stopTimeUtc = DateTime.UtcNow;
            //var buffer = new StringBuilder();
            //buffer.Append($"{startTimeUtc.ToString("s", CultureInfo.InvariantCulture)}, ");
            //buffer.Append($"\"{context.Request.HttpMethod} {context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)}\", ");
            //buffer.Append($"{(int)context.Response.StatusCode}, ");
            //buffer.Append($"{(int)stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds}");
            //Console.WriteLine(buffer);

            Console.WriteLine("...and back {0:N0} ms...", stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds);
            Console.WriteLine("");
        }


        /// <summary>
        /// Logs the request activity
        /// </summary>
        /// <param name="requestMessage"></param>
        public static void LogRequestActivity(HttpRequestMessage requestMessage)
        {
            if (requestMessage.Content == null) return;
            var content = requestMessage.Content.ReadAsStringAsync().Result;
            Console.ForegroundColor = ConsoleColor.Yellow;

            var formatted = content;
            if (IsValidJson(formatted))
            {
                var s = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };

                dynamic o = JsonConvert.DeserializeObject(content);
                formatted = JsonConvert.SerializeObject(o, s);
            }

            Console.WriteLine(formatted);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs the exception
        /// </summary>
        /// <param name="ex"></param>
        public static void LogException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.WriteLine("");
            Console.ResetColor();
        }


        /// <summary>
        /// Validates the Json string
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((!strInput.StartsWith("{") || !strInput.EndsWith("}")) && (!strInput.StartsWith("[") || !strInput.EndsWith("]")))
            {
                return false;
            }

            try
            {
                JToken.Parse(strInput);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
