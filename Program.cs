using System;
using static System.Console;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Specialized;
using oobi;

namespace HttpHostTest
{
    class Program
    {
        static void Main(string[] args) {
            List<string> prefixes = new List<string>();
            prefixes.Add("http://+:80/");
            prefixes.Add("https://+:443/");

            oobi.HttpHost host = new oobi.HttpHost(prefixes);

            Write("Starting server");
            host.StartHostAsync(Message_GetRequest);
            do {
                Write(".");
            } while (host.isStarting == true);
            WriteLine("OK!");
            WriteLine("HTTP and HTTPS enabled.");
            WriteLine();

            bool shouldExitInputLoop = false;
            do {
                WriteLine();

                //change user input prefix foreground color based on server status
                ConsoleColor oldForeColor = ForegroundColor;
                switch (host.IsRunning) {
                    case true:
                        ForegroundColor = ConsoleColor.Green;
                        break;
                    case false:
                        ForegroundColor = ConsoleColor.Red;
                        break;
                }
                Write("HttpHost >");
                ForegroundColor = oldForeColor;

                //accept user input while server operations process in the background
                string Input = ReadLine();

                //demonstrates how to start the HttpHost
                if (Input.ToLower() == "start") {
                    if (!host.IsRunning) {
                        Write("Starting Server");
                        host.StartHostAsync(Message_GetRequest);
                        do {
                            Write(".");
                        } while (host.isStarting == true);
                        WriteLine("OK!");
                        WriteLine();
                    }
                    else { WriteLine("Already Running."); }
                }
                //demonstrates how to stop the HttpHost
                if (Input.ToLower() == "stop") {
                    if (host.IsRunning) {
                        host.StopHost();
                        WriteLine("Server has stopped.");
                    }
                    else { WriteLine("Already Stopped."); }
                }

                //close the application instance.
                if (Input.ToLower() == "exit") {
                    host.StopHost();
                    shouldExitInputLoop = true;
                }

                //demonstrates restarting the instance.
                if (Input.ToLower() == "restart") {
                    Write("Host Stopping...");
                    host.StopHost();
                    WriteLine("OK!");
                    Write("Starting Server");
                    host.StartHostAsync(Message_GetRequest);
                    do {
                        Write(".");
                    } while (host.isStarting == true);
                    WriteLine("OK!");
                    WriteLine();
                }

                //demonstrates adding prefixes on-the-fly while server is running.
                if (Input.ToLower() == "enablessl") {
                    try { host.HttpListener.Prefixes.Add("https://+:443/"); WriteLine("OK!"); } catch (Exception ex) { WriteLine(ex.ToString()); }
                }
                if (Input.ToLower() == "disablessl") {
                    try { host.HttpListener.Prefixes.Remove("https://+:443/"); WriteLine("OK!"); } catch (Exception ex) { WriteLine(ex.ToString()); }
                }

                //demonstrates changing the response delegate while the server is running.
                if (Input.ToLower() == "switch") {
                    if (host.GenerateMessageFunctionDelegate == Message_GetListing) {
                        host.GenerateMessageFunctionDelegate = Message_GetRequest;
                        WriteLine("Switched response mode to list request details.");
                        continue;
                    }
                    if (host.GenerateMessageFunctionDelegate == Message_GetRequest) {
                        host.GenerateMessageFunctionDelegate = Message_GetListing;
                        WriteLine("Switched response mode to list directories and files contained within the request URL.");
                        continue;
                    }
                }
            } while (!shouldExitInputLoop);

            Environment.Exit(1);
        }


        private static string Message_GetListing(HttpHost.StateInfo scene) {
            XDocument output = XDocument.Parse("<root></root>");

            output.Root.Add(new XElement("datetime", DateTime.Now.ToString()));
            output.Root.Add(new XElement("directoryListing", null));
            foreach (string x in System.IO.Directory.GetDirectories(URLDecode(scene.Request.RawUrl.ToString().Replace("/", "\\")), "*", System.IO.SearchOption.TopDirectoryOnly)) {
                output.Root.Element("directoryListing").Add(new XElement("directory", x));
            }
            foreach (string x in System.IO.Directory.GetFiles(URLDecode(scene.Request.RawUrl.ToString().Replace("/", "\\")), "*", System.IO.SearchOption.TopDirectoryOnly)) {
                XElement file = new XElement("file");
                System.IO.FileInfo info = new System.IO.FileInfo(x);

                file.Add(new XElement("name", info.Name));
                file.Add(new XElement("created", info.CreationTime.ToString()));
                file.Add(new XElement("modified", info.LastWriteTime.ToString()));
                file.Add(new XElement("size", GetFileSizeFromByteLength(info.Length)));


                output.Root.Element("directoryListing").Add(file);
            }


            scene.Response.ContentType = "text/xml";
            return output.ToString();
        }

        private static string Message_GetRequest(oobi.HttpHost.StateInfo scene) {
            XDocument output = XDocument.Parse("<root></root>");
            
            output.Root.Add(new XElement("datetime", DateTime.Now.ToString()));


            output.Root.Add(new XElement("URL", scene.Request.Url.OriginalString));
            output.Root.Add(new XElement("Raw", scene.Request.RawUrl));
            output.Root.Add(new XElement("Local", scene.Request.Url.LocalPath));


            output.Root.Add(new XElement("headers"));
            foreach (string headerKey in scene.Request.Headers.Keys) {

                output.Root.Element("headers").Add(new XElement("header", scene.Request.Headers.Get(headerKey), new XAttribute("name", headerKey)));
            }


            output.Root.Add(new XElement("body"));
            System.IO.StreamReader inputReader = new System.IO.StreamReader(scene.Request.InputStream);
            string EvalRequest = inputReader.ReadToEnd();
            output.Root.Element("body").Value = EvalRequest;
            
            
            scene.Response.ContentType = "text/xml";
            
            return output.ToString();
        }




        /// <summary>
        /// Convert counts of bytes into a closest converted size unit string.
        /// </summary>
        /// <param name="Length">Length of the file in bytes.</param>
        /// <param name="Precision">Number of decimal places to round the file size to (Default is 3 decimal places).</param>
        /// <returns>Converted size of file with size label (B, KB, MB, GB, or TB).</returns>
        private static string GetFileSizeFromByteLength(long Length, int Precision = 3) {
            NumberFormatInfo setPrec = new NumberFormatInfo();
            setPrec.NumberDecimalDigits = Precision;


            if (Length < 1024) {
                return Length + " B";
            } else if (Length >= 1024 && Length < 1048576) {
                return (Length / (decimal)1024).ToString("N", setPrec) + " KB";
            } else if (Length >= 1048576 && Length < 1073741824) {
                return (Length / (decimal)1048576).ToString("N", setPrec) + " MB";
            } else if (Length >= 1073741824 && Length < 1099511627776) {
                return (Length / (decimal)1073741824).ToString("N", setPrec) + " GB";
            } else if (Length >= 1099511627776) {
                return (Length / (decimal)1099511627776).ToString("N", setPrec) + " TB";
            }
            return ((decimal)Length).ToString("N", setPrec) + " B";


        }

        /// <summary>
        /// Shorthand for System.Net.WebUtility.UrlDecode method.
        /// </summary>
        /// <param name="txt">Raw URL to be converted into plain text with symbols.</param>
        /// <returns></returns>
        private static string URLDecode(string txt) {
            return System.Net.WebUtility.UrlDecode(txt);
        }

    }
}
