using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Automaters.Discovery.Gena
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
