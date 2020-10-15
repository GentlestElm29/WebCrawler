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

            for(int i = 0; i < numHops + 1; i++)
            {
                string previousURL = currentURL;
                // Console.WriteLine(currentURL);
                // Console.WriteLine("Current Hop: " + i);
                currentURL = getNextHop(currentURL, visitedURLs);
                if(currentURL == null)
                {
                    Console.WriteLine("No more accessible references");
                    currentURL = previousURL;
                    break;
                }
            }
            
            Console.WriteLine(currentURL);
            Console.WriteLine(getLastHopHTML(currentURL));
        }

        static string getNextHop(string url, Dictionary<string, int> visitedURLs){
            using(var client = new HttpClient(new HttpClientHandler {AutomaticDecompression = System.Net.DecompressionMethods.GZip | DecompressionMethods.Deflate}))
            {
                // client.BaseAddress = new Uri(url);
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
                            if(!(visitedURLs.ContainsKey(aLine)))
                            {
                                // 400 is skipped next
                                // is regex better than string parsing?
                                // Backtracking?
                                visitedURLs.Add(aLine, 1);
                                //Console.WriteLine(aLine);
                                int linkStart = aLine.IndexOf("href=\"") + 6;
                                int linkEnd = aLine.IndexOf("\"", linkStart);
                                string link = aLine.Substring(linkStart, linkEnd - linkStart);
                                //Console.WriteLine("link: " + link);
                                if(link.Contains("http://") || link.Contains("https://"))
                                {
                                    return link;
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return null;
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