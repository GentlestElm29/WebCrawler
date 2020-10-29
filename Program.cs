using System;
using System.Collections.Generic;
//using System.Linq;
using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace WebCrawler
{
    class Program
    {
        // Main Function
        // When running on command line, 
        // user executes like this: WebCrawler.exe {url} {numHops}
        static void Main(string[] args)
        {
            // If there are less than two arguments, we print out the expected arguments
            if (args.Length < 2)
            {
                Console.WriteLine("WebCrawler.exe {url} {numHops}");
                return;
            }

            // If user enters correct input, we take the two parameters as intended
            int numHops = int.Parse(args[1]);
            String currentURL = args[0];

            // This stores all visited urls so that we don't visit the same one twice
            Dictionary<string, int> visitedURLs = new Dictionary<string, int>();

            // If we don't find enough urls with the number of hops given, 
            // We print out the current url and the HTML of that url along with the message of not finding the link 
            // at the intended hop amount
            if (!getNextHop(currentURL, numHops, 0, visitedURLs))
            {
                Console.WriteLine("Didn't find the last Hop link");
                Console.WriteLine(currentURL);
                Console.WriteLine(getLastHopHTML(currentURL));
            }
        }

        // This is a recursive method to get all urls and store them into the dictionary
        // Parameters:
        // string url: url to start
        // int maxHops: the max number of hops
        // int currentHops: how many hops have been executed
        // Dictionary<string, int> visitedURLs: to store all visited urls so that we don't visit them again
        static Boolean getNextHop(string url, int maxHops, int currentHops, Dictionary<string, int> visitedURLs)
        {
            // End condition of recursive method,
            // calls the function to print out the HTML of the last link
            if (currentHops > maxHops)
            {
                Console.WriteLine("Final URL: " + url);
                Console.WriteLine(getLastHopHTML(url));
                return true;
            }

            // Accessing the HTTP response from the website
            // Assumption: As state in class by Professor Dimpsey, 300 errors are handled by C# automatically
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                // retrieves the HTTP response
                HttpResponseMessage response = client.GetAsync(url).Result;
                // Gets the status code of the response
                // This checks to see if response was in range of 200 to 299
                if (!response.IsSuccessStatusCode)
                {
                    int statusCode = (int)response.StatusCode;

                    // if error code is in range of 400 to 499
                    // we will return false so that we can backtrack to previous link
                    if (statusCode >= 400 && statusCode < 500)
                    {
                        Console.WriteLine("Error " + statusCode);
                        return false;
                    }
                    // if error code is in range of 500 to 599
                    // we will retry for a max of three attempts
                    // if any of those three attempts work, we will proceed normally
                    // if none of the three attempts work, we will backtrack
                    else if (statusCode >= 500 && statusCode < 600)
                    {
                        Console.WriteLine("Error " + statusCode);
                        bool is500RetrySucceed = false;

                        for (int i = 0; i < 3; i++)
                        {
                            HttpResponseMessage retryResponse = client.GetAsync(url).Result;

                            statusCode = (int)retryResponse.StatusCode;
                            if (statusCode < 500 && statusCode >= 600)
                            {
                                is500RetrySucceed = true;
                                break;
                            }
                        }

                        if (!is500RetrySucceed)
                        {
                            return false;
                        }
                    }
                }

                // This retrieves the HTML of the page

                string result = response.Content.ReadAsStringAsync().Result;
                int currentPosition = 0;

                // This is a continous while loop since we don't always know the length of the HTML of the page
                while (true)
                {
                    // We retrieve the line of HTML
                    int linkStartIndex = result.IndexOf("href=\"", currentPosition);

                    if (linkStartIndex == -1)
                    {
                        break;
                    }

                    // We keep going through the HTML until we find the line containing href 
                    // Find the link start and end index to get the link inside href
                    int linkStart = 6 + linkStartIndex;
                    int linkEnd = result.IndexOf("\"", linkStart);
                    string link = result.Substring(linkStart, linkEnd - linkStart);
                        
                    currentPosition = linkEnd + 1;

                    // Find the line that contains http://
                    if (link.Contains("http://") || link.Contains("https://"))
                    {
                        // This call is to clean up the backslash as we treat both urls with or without backslash the same
                        link = cleanupUrl(link);

                        // If url isn't in dictionary, meaning we haven't visited, then we will enter the url
                        if (!visitedURLs.ContainsKey(link))
                        {
                            // Console.WriteLine("Current Hop: " + currentHops);
                            // Console.WriteLine("Current URL: " + link);
                            // upon visiting, we will add the link into dictionary to not visit it again
                            visitedURLs.Add(link, 1);

                            // Recursive call through the urls to hop as many times as needed
                            if (getNextHop(link, maxHops, currentHops + 1, visitedURLs))
                            {
                                //Console.WriteLine("Visited: " + link);
                                return true;
                            }
                            // If we get to a dead end, we remove link and backtrack
                            else if (!getNextHop(link, maxHops, currentHops + 1, visitedURLs))
                            {
                                visitedURLs.Remove(link);
                            }
                        }
                    }
                }

                // Get out of recursive call if we reach end of while loop
                return false;
            }
        }

        // Prints out the HTML of the last url
        static string getLastHopHTML(string url)
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        // Cleans up the url to remove backslashes to keep urls consistent as per specs
        static string cleanupUrl(string url)
        {
            //Console.WriteLine(url.Length);
            if (url.Substring(url.Length - 2, 1) == "/")
            {
                return url.Substring(0, url.Length - 1);
            }

            return url;
        }
    }
}