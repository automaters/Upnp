using System;
using System.Net;
using System.Text;

namespace Upnp.Gena
{
    class Protocol
    {

        private static string OSString = String.Format("{0}/{1}", Environment.OSVersion.Platform, Environment.OSVersion.Version);

        private static string SubscribeRequest = 
            "SUBSCRIBE {0} HTTP/1.1\r\n" +
            "HOST: {1}:{2}\r\n" +
            "CALLBACK: {3}\r\n" +
            "NT: upnp:event\r\n" +
            "TIMEOUT: {4}\r\n" +
            "\r\n";

        private static string SubscribeResponse = 
            "HTTP/1.1 200 OK\r\n" +
            "DATE: {0}\r\n" +
            "SERVER: {1} UPnP/1.1 {2}\r\n" +
            "SID: uuid:{3}\r\n" + 
            "CONTENT-LENGTH: 0\r\n" +
            "TIMEOUT: {4}\r\n" +
            "\r\n";


        private static string RenewalRequest =
            "SUBSCRIBE {0} HTTP/1.1\r\n" +
            "HOST: {1}:{2}\r\n" +
            "SID: uuid:{3}\r\n" +
            "TIMEOUT: {4}\r\n" +
            "\r\n";

        private static string UnsubscribeRequest = "";
        private static string UnsubscribeResponse = "";


        private static string NotifyRequest =
            "NOTIFY {0} HTTP/1.1\r\n" +
            "HOST: {1}:{2}\r\n" +
            "CONTENT-TYPE: text/xml; charset=\"utf-8\"\r\n" +
            "NT: upnp:event\r\n" +
            "NTS: upnp:propchange\r\n" +
            "SID: uuid:{3}\r\n" +
            "SEQ: {4}\r\n" +
            "CONTENT-LENGTH: {5}\r\n" +
            "\r\n" + 
            "{6}";

        /// <summary>
        /// Creates the subscribe request.
        /// </summary>
        /// <param name="eventUrl">The event URL.</param>
        /// <param name="host">The host.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="callbacks">The callbacks.</param>
        /// <returns></returns>
        public static string CreateSubscribeRequest(string eventUrl, IPEndPoint host, TimeSpan timeout, params Uri[] callbacks)
        {
            string timeoutString = GetTimeoutString(timeout);

            var sb = new StringBuilder();

            foreach (var callback in callbacks)
            {
                sb.AppendFormat("<{0}>, ", callback);
            }

            if (sb.Length > 0)
                sb.Length -= 2;

            return string.Format(SubscribeRequest, eventUrl, host.Address, host.Port, sb, timeoutString);
        }

        private static string GetTimeoutString(TimeSpan timeout)
        {
            string timeoutString;
            if (timeout == TimeSpan.MaxValue) //infinite
            {
                timeoutString = "Infinite";
            }
            else
            {
                timeoutString = "Second-" + timeout.TotalSeconds;
            }
            return timeoutString;
        }


        public static string CreateSubscribeResponse(string userAgent, string subscribeId, TimeSpan timeout)
        {
            return string.Format(SubscribeResponse, DateTime.Now.ToString("r"), OSString, userAgent, subscribeId, GetTimeoutString(timeout));
        }

        public static string CreateNotify(Uri callbackUrl, string subscriptionId, string sequenceId, string xml)
        {
            return string.Format(NotifyRequest, callbackUrl.PathAndQuery, callbackUrl.Host, callbackUrl.Port, subscriptionId, sequenceId, xml.Length, xml);
        }
    }
}
