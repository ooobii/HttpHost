using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static System.Console;

namespace HttpHost_Core_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            oobi.HttpHostCore.HttpHost host = new oobi.HttpHostCore.HttpHost();
            host.PrefixBindings.Add("http://+:80/");
            host.PrefixBindings.Add("https://+:443/MyApp/");

            Write("Starting server");
            host.StartHostAsync(Message_GetRequest);
            do
            {
                Write(".");
                System.Threading.Thread.Sleep(20);
            } while (host.IsStarting == true);
            WriteLine("OK!");

            do
            {
                // simulate other application work here
                string input = ReadLine();
                WriteLine(input);
            } while (host.IsRunning == true);

            ReadLine();
        }

        private static string Message_GetRequest(oobi.HttpHostCore.Components.StateInfo scene)
        {
            WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fff") + "] REQUEST: " + scene.Request.HttpMethod + ": " + scene.Request.RawUrl);
            scene.Response.ContentType = "application/json";
            return ("{ \"name\": \"Matthew Wendel\" }");
        }
    }
}
