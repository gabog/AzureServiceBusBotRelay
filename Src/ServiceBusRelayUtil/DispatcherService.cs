using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceBus.Web;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace GaboG.ServiceBusRelayUtil
{
    [ServiceContract(Namespace = "http://samples.microsoft.com/ServiceModel/Relay/")]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class DispatcherService
    {
        private static readonly HashSet<string> _httpContentHeaders = new HashSet<string>
        {
            "Allow",
            "Content-Encoding",
            "Content-Language",
            "Content-Length",
            "Content-Location",
            "Content-MD5",
            "Content-Range",
            "Content-Type",
            "Expires",
            "Last-Modified"
        };

        private readonly ServiceBusRelayUtilConfig _config;

        public DispatcherService(ServiceBusRelayUtilConfig config)
        {
            _config = config;
        }

        [WebGet(UriTemplate = "*")]
        [OperationContract(AsyncPattern = true)]
        public async Task<Message> GetAsync()
        {
            try
            {
                var ti0 = DateTime.Now;
                Console.WriteLine("In GetAsync:");
                var context = WebOperationContext.Current;
                var request = BuildForwardedRequest(context, null);
                Console.WriteLine("...calling {0}...", request.RequestUri);
                HttpResponseMessage response;
                using (var client = new HttpClient())
                {
                    response = await client.SendAsync(request, CancellationToken.None);
                }

                Console.WriteLine("...and back {0:N0} ms...", DateTime.Now.Subtract(ti0).TotalMilliseconds);
                Console.WriteLine("");

                Console.WriteLine("...reading and creating response...");
                CopyHttpResponseMessageToOutgoingResponse(response, context.OutgoingResponse);
                var stream = response.Content != null ? await response.Content.ReadAsStreamAsync() : null;
                var message = StreamMessageHelper.CreateMessage(MessageVersion.None, "GETRESPONSE", stream ?? new MemoryStream());
                Console.WriteLine("...and done (total time: {0:N0} ms).", DateTime.Now.Subtract(ti0).TotalMilliseconds);
                Console.WriteLine("");
                return message;
            }
            catch (Exception ex)
            {
                WriteException(ex);
                throw;
            }
        }

        [WebInvoke(UriTemplate = "*", Method = "*")]
        [OperationContract(AsyncPattern = true)]
        public async Task<Message> InvokeAsync(Message msg)
        {
            try
            {
                var ti0 = DateTime.Now;
                WriteFlowerLine();
                Console.WriteLine("In InvokeAsync:");
                var context = WebOperationContext.Current;
                var request = BuildForwardedRequest(context, msg);
                Console.WriteLine("...calling {0}", request.RequestUri);
                HttpResponseMessage response;
                using (var client = new HttpClient())
                {
                    response = await client.SendAsync(request, CancellationToken.None);
                }

                Console.WriteLine("...and done {0:N0} ms...", DateTime.Now.Subtract(ti0).TotalMilliseconds);

                Console.WriteLine("...reading and creating response...");
                CopyHttpResponseMessageToOutgoingResponse(response, context.OutgoingResponse);
                var stream = response.Content != null ? await response.Content.ReadAsStreamAsync() : null;
                var message = StreamMessageHelper.CreateMessage(MessageVersion.None, "GETRESPONSE", stream ?? new MemoryStream());
                Console.WriteLine("...and done (total time: {0:N0} ms).", DateTime.Now.Subtract(ti0).TotalMilliseconds);
                return message;
            }
            catch (Exception ex)
            {
                WriteException(ex);
                throw;
            }
        }

        private HttpRequestMessage BuildForwardedRequest(WebOperationContext context, Message msg)
        {
            var incomingRequest = context.IncomingRequest;

            var mappedUri = new Uri(incomingRequest.UriTemplateMatch.RequestUri.ToString().Replace(_config.SBAddress.ToString(), _config.TargetAddress.ToString()));
            var newRequest = new HttpRequestMessage(new HttpMethod(incomingRequest.Method), mappedUri);

            // Copy headers
            foreach (var name in incomingRequest.Headers.AllKeys.Where(name => !_httpContentHeaders.Contains(name)))
            {
                newRequest.Headers.TryAddWithoutValidation(name, name == "Host" ? _config.TargetAddress.Host : incomingRequest.Headers.Get(name));
            }

            if (msg != null)
            {
                Stream messageStream = null;
                if (msg.Properties.TryGetValue("WebBodyFormatMessageProperty", out var value))
                {
                    if (value is WebBodyFormatMessageProperty prop && (prop.Format == WebContentFormat.Json || prop.Format == WebContentFormat.Raw))
                    {
                        messageStream = StreamMessageHelper.GetStream(msg);
                    }
                }
                else
                {
                    var ms = new MemoryStream();
                    using (var xw = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
                    {
                        msg.WriteBodyContents(xw);
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    messageStream = ms;
                }

                if (messageStream != null)
                {
                    if (_config.BufferRequestContent)
                    {
                        var ms1 = new MemoryStream();
                        messageStream.CopyTo(ms1);
                        ms1.Seek(0, SeekOrigin.Begin);
                        newRequest.Content = new StreamContent(ms1);
                    }
                    else
                    {
                        var ms1 = new MemoryStream();
                        messageStream.CopyTo(ms1);
                        ms1.Seek(0, SeekOrigin.Begin);

                        var debugMs = new MemoryStream();
                        ms1.CopyTo(debugMs);
                        debugMs.Seek(0, SeekOrigin.Begin);

                        var result = Encoding.UTF8.GetString(debugMs.ToArray());
                        WriteJsonObject(result);

                        ms1.Seek(0, SeekOrigin.Begin);
                        newRequest.Content = new StreamContent(ms1);
                    }

                    foreach (var name in incomingRequest.Headers.AllKeys.Where(name => _httpContentHeaders.Contains(name)))
                    {
                        newRequest.Content.Headers.TryAddWithoutValidation(name, incomingRequest.Headers.Get(name));
                    }
                }
            }

            return newRequest;
        }

        private static void WriteException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.WriteLine("");
            Console.ResetColor();
        }

        private static void WriteJsonObject(string result)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            var s = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            dynamic o = JsonConvert.DeserializeObject(result);
            var formatted = JsonConvert.SerializeObject(o, s);
            Console.WriteLine(formatted);
            Console.ResetColor();
        }

        private static void WriteFlowerLine()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\n=> {0:MM/dd/yyyy hh:mm:ss.fff tt} {1}", DateTime.Now, new string('*', 80));
            Console.ResetColor();
        }

        private static void CopyHttpResponseMessageToOutgoingResponse(HttpResponseMessage response, OutgoingWebResponseContext outgoingResponse)
        {
            outgoingResponse.StatusCode = response.StatusCode;
            outgoingResponse.StatusDescription = response.ReasonPhrase;
            if (response.Content == null)
            {
                outgoingResponse.SuppressEntityBody = true;
            }

            foreach (var kvp in response.Headers)
            {
                foreach (var value in kvp.Value)
                {
                    outgoingResponse.Headers.Add(kvp.Key, value);
                }
            }

            if (response.Content != null)
            {
                foreach (var kvp in response.Content.Headers)
                {
                    foreach (var value in kvp.Value)
                    {
                        outgoingResponse.Headers.Add(kvp.Key, value);
                    }
                }
            }
        }
    }
}