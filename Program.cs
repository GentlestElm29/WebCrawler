using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("WebCrawler.exe {url} {numHops}");
                return;
            }
            
            int numHops = int.Parse(args[1]);
            String currentURL = args[0]; //"http://courses.washington.edu/css342/dimpsey";
            Dictionary<string, int> visitedURLs = new Dictionary<string, int>();
            
            if(!getNextHop(currentURL, numHops, 0, visitedURLs))
            {
                Console.WriteLine("Didn't find max hop link");
                Console.WriteLine(currentURL);
                Console.WriteLine(getLastHopHTML(currentURL));
            }
        }

        static Boolean getNextHop(string url, int maxHops, int currentHops, Dictionary<string, int> visitedURLs){
            if(currentHops >= maxHops)
            {
                Console.WriteLine(getLastHopHTML(url));
                return true;
            }
            
            using(var client = new HttpClient(new HttpClientHandler {AutomaticDecompression = System.Net.DecompressionMethods.GZip | DecompressionMethods.Deflate}))
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;

                StringReader strReader = new StringReader(result);
                string aLine = "";

                while(true)
                {
                    aLine = strReader.ReadLine();
                    if(aLine != null)
                    {
                        if(aLine.Contains("href"))
                        {
                            int linkStart = aLine.IndexOf("href=\"") + 6;
                            int linkEnd = aLine.IndexOf("\"", linkStart);
                            string link = aLine.Substring(linkStart, linkEnd - linkStart);
                            if(link.Contains("http://") || link.Contains("https://"))
                            {
                                if(!visitedURLs.ContainsKey(link))
                                {
                                    Console.WriteLine("Current Hop: " + currentHops);
                                    Console.WriteLine("Current URL: " + link);
                                    visitedURLs.Add(link, 1);
                                    if(getNextHop(link, maxHops, currentHops + 1, visitedURLs))
                                    {
                                        Console.WriteLine("Visited: " + link);
                                        return true;
                                    }
                                    else if(!getNextHop(link, maxHops, currentHops + 1, visitedURLs))
                                    {
                                        visitedURLs.Remove(link);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return false;
            }
        }

        static string getLastHopHTML(string url){
            using(var client = new HttpClient(new HttpClientHandler {AutomaticDecompression = System.Net.DecompressionMethods.GZip | DecompressionMethods.Deflate}))
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}