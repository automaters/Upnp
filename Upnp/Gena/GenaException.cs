using System;
using System.Net;

namespace Upnp.Gena
{
    public class GenaException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public GenaException(HttpStatusCode code)
        {
            StatusCode = code;
        }
    }
    
}
