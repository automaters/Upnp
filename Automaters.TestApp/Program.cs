using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automaters.Discovery.Ssdp;
using System.Net;
using Automaters.Core.Net;

namespace Automaters.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var announcer = new SsdpAnnouncer())
            {
                announcer.MaxAge = 3;
                announcer.Start();

                SsdpSearch search = new SsdpSearch();
                search.SearchType = "upnp:rootdevice";

                // Attach our events for async search
                search.ResultFound += new EventHandler<Automaters.Core.EventArgs<SsdpMessage>>(search_ResultFound);
                search.SearchComplete += new EventHandler(search_SearchComplete);

                Console.WriteLine("Performing asynchronous search:");
                //search.SearchAsync();
                search.SearchAsync(Protocol.DiscoveryEndpoints.IPv4, Protocol.DiscoveryEndpoints.Broadcast);

                // Wait for our async search to complete before doing the synchronous search
                search.WaitForSearch();
                search.ResultFound -= search_ResultFound;
                search.SearchComplete -= search_SearchComplete;

                Console.WriteLine();
                Console.WriteLine("Performing synchronous search:");
                search.Search(Protocol.DiscoveryEndpoints.IPv4, Protocol.DiscoveryEndpoints.Broadcast).ForEach(msg =>
                {
                    Console.WriteLine(msg.Location);
                });

                Console.ReadLine();
            }
        }

        static void search_SearchComplete(object sender, EventArgs e)
        {
            Console.WriteLine("Search is complete!");
        }

        static void search_ResultFound(object sender, Automaters.Core.EventArgs<SsdpMessage> e)
        {
            Console.WriteLine(e.Value.Location);
        }
    }
}
