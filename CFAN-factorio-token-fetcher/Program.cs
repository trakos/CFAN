using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CFAN_factorio_token_fetcher
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Write("Username:");
            string username = Console.ReadLine();

            Console.Write("Password:");
            string password = ReadPassword();

            try
            {
                string token = FactorioComAuthClient.fetchToken(username, password);
                Console.Write(token);
            }
            catch (WebException e)
            {
                HttpWebResponse response = e.Response as HttpWebResponse;
                if (response != null)
                {
                    Console.WriteLine("HTTP error: " + (int)response.StatusCode);
                    Console.Write(StreamToString(response.GetResponseStream()));
                }
                else
                {
                    throw;
                }
            }

            Console.ReadLine();
        }

        public static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        static string ReadPassword(char mask = '*')
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            System.Console.WriteLine();

            return new string(pass.Reverse().ToArray());
        }
    }
}
