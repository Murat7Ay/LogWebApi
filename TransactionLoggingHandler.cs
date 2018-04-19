using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;

namespace com.Telekurye.WebServices.LogWebApi.Handler
{

    public class TransactionLoggingHandler : DelegatingHandler,ISaveTracer
    {
        private readonly int _integrationId;


        public TransactionLoggingHandler()
        {
            
        }

        public TransactionLoggingHandler(int integrationId)
        {
            _integrationId = integrationId;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            //Start the watch
            var stopwatch = Stopwatch.StartNew();

            //Machine name 
            var machineName = Environment.MachineName;

            //Release Version
            var releaseVersion = Assembly.GetExecutingAssembly().GetName().Version;

            //version ??
            var version = request.Version;

            //IpAddress
            var ipAddress = GetClientIp(request);
            
            //Read the request body
            var requestMessageBytes = await request.Content.ReadAsByteArrayAsync();

            //Decode the message bytes to string and log them
            var requestContentMessage = Encoding.UTF8.GetString(requestMessageBytes);
            
            //Request Content Headers
            var requestContentHeaders = request.Content.Headers.Where(x => x.Value != null && x.Value.Any());

            //Log the request Headers
            var requestHeaders = request.Headers.Where(x => x.Value != null && x.Value.Any());

            //Request Method
            var requestMethod = request.Method.ToString();
            
            #region RequestURI
            //Request uri
            var requestUri = new
            {
                request.RequestUri.AbsolutePath,	 //Gets the absolute path of the URI.

                request.RequestUri.AbsoluteUri,	 //Gets the absolute URI.

                request.RequestUri.Authority,	 //Gets the Domain Name System (DNS) host name or IP address and the port number for a server.

                request.RequestUri.DnsSafeHost,	 //Gets an unescaped host name that is safe to use for DNS resolution.

                request.RequestUri.Fragment,	 //Gets the escaped URI fragment.

                request.RequestUri.Host,	 //Gets the host component of this instance.

                request.RequestUri.HostNameType,	 //Gets the type of the host name specified in the URI.

                request.RequestUri.IsAbsoluteUri,	 //Gets whether the Uri instance is absolute.

                request.RequestUri.IsDefaultPort,	 //Gets whether the port value of the URI is the default for this scheme.

                request.RequestUri.IsFile,	 //Gets a value indicating whether the specified Uri is a file URI.

                request.RequestUri.IsLoopback,	 //Gets whether the specified Uri references the local host.

                request.RequestUri.IsUnc,	 //Gets whether the specified Uri is a universal naming convention (UNC) path.

                request.RequestUri.LocalPath,	 //Gets a local operating-system representation of a file name.

                request.RequestUri.OriginalString,	 //Gets the original URI string that was passed to the Uri constructor.

                request.RequestUri.PathAndQuery,	 //Gets the AbsolutePath and Query properties separated by a question mark (?).

                request.RequestUri.Port,	 //Gets the port number of this URI.

                request.RequestUri.Query,	 //Gets any query information included in the specified URI.

                request.RequestUri.Scheme,	 //Gets the scheme name for this URI.

                request.RequestUri.Segments,	 //Gets an array containing the path segments that make up the specified URI.

                request.RequestUri.UserEscaped,	 //Indicates that the URI string was completely escaped before the Uri instance was created.

                request.RequestUri.UserInfo	 //Gets the user name, password, or other user-specific information associated with the specified URI.
            };
            #endregion

            //Release the request to the controller and read the response
            var response = await base.SendAsync(request, cancellationToken);

            //stop watch
            stopwatch.Stop();

            response.Headers.Add("Correlation-GUID",Guid.NewGuid().ToString());
            //Log the response status
            var responseHttpStatusCode = response.StatusCode.ToString();

            //Log the response body
            byte[] responseMessageBytes = { };

            if (response.Content != null)
                responseMessageBytes = await response.Content.ReadAsByteArrayAsync();

            var responseBody = Encoding.UTF8.GetString(responseMessageBytes);

            //Response content headers
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> responseContentHeaders = null;
            if (response.Content != null)
            {
                responseContentHeaders = response.Content.Headers.Where(x => x.Value != null && x.Value.Any());
            }

            //Log the Response Headers
            var responseHeaders = response.Headers.Where(x=>x.Value!=null&&x.Value.Any());

            //Response version
            var responseVersion = response.Version;

            //Response reason phrase
            var responseReasonPhrase =response.ReasonPhrase;

            var finalModel = new
            {
                IntegrationId = _integrationId,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                MachineName = machineName,
                IpAddress =ipAddress,
                ReleaseVersion = releaseVersion,
                Request = new
                {
                    Uri = requestUri,
                    Method = requestMethod,
                    Headers = requestHeaders,
                    ContentHeaders = requestContentHeaders,
                    Content = JsonConvert.DeserializeObject(requestContentMessage),
                    Version = version
                },
                Response = new
                {
                    StatusCode = responseHttpStatusCode,
                    Headers = responseHeaders,
                    Content = JsonConvert.DeserializeObject(responseBody),
                    ContentHeaders = responseContentHeaders,
                    Version = responseVersion,
                    ReasonPhrase = responseReasonPhrase
                }

            };

            Log(finalModel);

            

            return response;

        }

        private string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            if (!request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                return HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            var prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
            return prop.Address;

        }
        private void Log(string s)
        {
            using (var writetext = new StreamWriter(@"c:\apideneme\rrLog.txt", true))
            {
                writetext.WriteLine(_integrationId);
                writetext.WriteLine(s);
            }
        }

        public void Log(object o)
        {
            var jsonObject = JsonConvert.SerializeObject(o, Formatting.Indented);
            //save to whatever you want
        }
    }

    

    public class TraceModel
    {
        public TraceModel(Version releaseVersion, string machineName, HttpResponseMessage response, Stopwatch stopwatch, string ipAddress, string requestContent)
        {
            MachineName = machineName;
            ReleaseVersion = releaseVersion;
            Request = new RequestModel();
            Request.Content = requestContent;
            //Request.Properties = response.RequestMessage.Properties;
            Request.Headers = response.RequestMessage.Headers.ToList();
            Request.Uri = response.RequestMessage.RequestUri.ToString();
            Request.Method = response.RequestMessage.Method.ToString();
            Request.IpAddress = ipAddress;
            Request.ContentHeaders = response.RequestMessage.Content.Headers.ToList();
            TimeStamp = DateTime.Now;
            ResponseTime = stopwatch.ElapsedMilliseconds;
            Response = new ResponseModel();
            if (response.Content != null)
                Response.Content = response.Content.ReadAsStringAsync().Result;
            Response.Headers = response.Headers.ToList();
            Response.Version = response.Version;
            Response.StatusCode = (int)response.StatusCode;
            Response.ReasonPhrase = response.ReasonPhrase;
            Response.ContentHeaders = response.Content.Headers.ToList();
        }
        public long ResponseTime { get; set; }
        public string MachineName { get; set; }
        public Version ReleaseVersion { get; set; }
        public RequestModel Request { get; set; }
        public ResponseModel Response { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public interface ISaveTracer
    {
        void Log(object o);
        
    }
    public class ResponseModel
    {
        public Version Version { get; set; }
        public int StatusCode { get; set; }
        public List<KeyValuePair<string, IEnumerable<string>>> ContentHeaders { get; set; }
        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }
        public string Content { get; set; }
        public string ReasonPhrase { get; set; }
    }

    public class RequestModel
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        //public int UserId { get; set; }
        public string IpAddress { get; set; }

        public List<KeyValuePair<string, IEnumerable<string>>> ContentHeaders { get; set; }

        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }

        public string Content { get; set; }

        //public IDictionary<string,object> Properties { get; set; }

        public Version Version { get; set; }
    }
}
