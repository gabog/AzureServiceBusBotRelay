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
                var request = MakeHttpRequestMessageFrom(context.IncomingRequest, null, _config.BufferRequestContent);
                HttpResponseMessage response;
                Console.WriteLine("...calling {0}...", request.RequestUri);
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
                Console.WriteLine(ex);
                Console.WriteLine("");
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
                Console.WriteLine("In InvokleAsync:");
                var context = WebOperationContext.Current;
                Stream s = null;
                if (msg.Properties.TryGetValue("WebBodyFormatMessageProperty", out var value))
                {
                    if (value is WebBodyFormatMessageProperty prop && (prop.Format == WebContentFormat.Json || prop.Format == WebContentFormat.Raw))
                    {
                        s = StreamMessageHelper.GetStream(msg);
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
                    s = ms;
                }
                var request = MakeHttpRequestMessageFrom(context.IncomingRequest, s, _config.BufferRequestContent);
                HttpResponseMessage response;
                Console.WriteLine("...calling {0}", request.RequestUri);
                using (var client = new HttpClient())
                {
                    response = await client.SendAsync(request, CancellationToken.None);
                }
                Console.WriteLine("...and back {0:N0} ms...", DateTime.Now.Subtract(ti0).TotalMilliseconds);

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
                Console.WriteLine(ex);
                Console.WriteLine("");
                throw;
            }
        }

        private HttpRequestMessage MakeHttpRequestMessageFrom(IncomingWebRequestContext oreq, Stream body, bool bufferBody)
        {
            var mappedUri = new Uri(oreq.UriTemplateMatch.RequestUri.ToString().Replace(_config.SBAddress.ToString(), _config.TargetAddress.ToString()));
            var nreq = new HttpRequestMessage(new HttpMethod(oreq.Method), mappedUri);
            foreach (var name in oreq.Headers.AllKeys.Where(name => !_httpContentHeaders.Contains(name)))
            {
                if (name == "Host")
                {
                    nreq.Headers.TryAddWithoutValidation(name, _config.TargetAddress.Host);
                }
                else
                {
                    nreq.Headers.TryAddWithoutValidation(name, oreq.Headers.Get(name));
                }
            }
            if (body != null)
            {
                if (bufferBody)
                {
                    var ms = new MemoryStream();
                    body.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    nreq.Content = new StreamContent(ms);
                }
                else
                {
                    nreq.Content = new StreamContent(body);
                }

                foreach (var name in oreq.Headers.AllKeys.Where(name => _httpContentHeaders.Contains(name)))
                {
                    nreq.Content.Headers.TryAddWithoutValidation(name, oreq.Headers.Get(name));
                }
            }
            return nreq;
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