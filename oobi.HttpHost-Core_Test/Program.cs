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

            //create the HttpHost class instance
            oobi.HttpHostCore.HttpHost host = new oobi.HttpHostCore.HttpHost();

            //define URL prefixes for the HttpHost to bind to
            host.PrefixBindings.Add("http://+:80/");
            host.PrefixBindings.Add("https://+:443/MyApp/");

            //start the server asynchronously (you can do other things on this thread while the listener is starting).
            Write("Starting server");
            host.StartHostAsync(Message_GetRequest);
            do
            {
                Write(".");
            } while (host.IsStarting == true);
            WriteLine("OK!");

            //while the listener is running, your main thread can still do work.
            do
            {
                // simulate other application work here
                string input = ReadLine();
                WriteLine(input);
            } while (host.IsRunning == true);

        }

        //This function is called on each incoming request. Each incoming request is designated it's own thread.
        private static string Message_GetRequest(oobi.HttpHostCore.Components.StateInfo scene)
        {

            var datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            WriteLine($"[{datetime}] REQUEST: {scene.Request.HttpMethod}: {scene.Request.RawUrl}");

            return ("<h1>It Works!</h1>");

        }
    }
}
