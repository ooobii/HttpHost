using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace oobi.HttpHostCore
{


    /// <summary>
    /// An HttpListener wrapper that utilizes asynchronous HTTP request hosting services.
    /// </summary>
    public class HttpHost
    {
        #region "Properties"



        /// <summary>
        /// The URI prefixes that the HttpListener will bind to (in List(Of String) format instead of URI collection).
        /// </summary>
        public List<string> PrefixBindings
        {
            get
            {
                if (_listener != null) { return _listener.Prefixes.ToList<string>(); } else { return _prefixes; }
            }
            set
            {
                _prefixes = value;
                try
                {
                    _listener.Prefixes.Clear();
                    foreach (string i in value)
                    {
                        _listener.Prefixes.Add(i);
                    }
                }
                catch { }
            }
        }
        private List<string> _prefixes = new List<string>();



        /// <summary>
        /// The HttpListener base object this class is operating upon.
        /// </summary>
        public HttpListener HttpListener
        {
            get { return _listener; }
        }
        private HttpListener _listener;



        /// <summary>
        /// The function delegate that is responsible for processing incoming requests to the server.
        /// </summary>
        public Func<oobi.HttpHostCore.Components.StateInfo, string> GenerateMessageFunctionDelegate;



        /// <summary>
        /// Determines if the System.Net.HttpListener class is actively listening.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                if (_starting == false) { return _listener.IsListening; } else { return false; }
            }
        }



        /// <summary>
        /// Determines if the asynchronous operations are still waiting for the listener to start.
        /// Will return false if running or stopped, but true if start signal was received but the listener has not started yet.
        /// </summary>
        public bool IsStarting
        {
            get { return _starting; }
        }



        #endregion

        #region "Container Classes"
        

        
        #endregion

        #region "Private Objects"
        private Thread _work;
        private bool _starting = false;
        #endregion



        /// <summary>
        /// Create a new instance of an HttpHost, for handling your (a)synchronous HTTP requests.
        /// </summary>
        /// <param name="BindingPrefixes">A List of URI compatible strings (wild-cards allowed) for the System.Net.HttpListener to bind to.</param>
        /// <param name="GenerateMessage">The string function res</param>
        public HttpHost()
        {
        }


        #region "Synchronous Operations"
        /// <summary>
        /// Begin accepting and processing incoming connections received on the bound prefixes (synchronously).
        /// </summary>
        public void StartHost(Func<Components.StateInfo, string> GenerateMessage, object args = null)
        {
            _listener = new HttpListener();
            foreach (string Itm in _prefixes)
            {
                _listener.Prefixes.Add(Itm);
            }

            _listener.Start();

            do
            {
                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                try
                {
                    Components.StateInfo scenario = new Components.StateInfo(ref _listener, ref context, ref request, ref response, ref args);

                    string message = GenerateMessage(scenario); ;
                    byte[] buff = System.Text.Encoding.UTF8.GetBytes(message);

                    System.IO.Stream output = response.OutputStream;
                    output.Write(buff, 0, buff.Length);
                    output.Flush(); output.Close();

                }
                catch (Exception ex)
                {
                    response.StatusCode = 500;
                    response.StatusDescription = "InternalServerException";
                    response.ContentType = "text/javascript";
                    response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(ex.ToString()), 0, System.Text.Encoding.UTF8.GetBytes(ex.ToString()).Length);
                    response.OutputStream.Flush(); response.OutputStream.Close();

                }

            } while (_listener.IsListening == true);
        }
        #endregion

        #region "Asynchronous Operations"
        /// <summary>
        /// Begin accepting and processing incoming connections received on the bound prefixes asynchronously.
        /// </summary>
        public void StartHostAsync(Func<Components.StateInfo, string> GenerateMessage, object args = null)
        {
            GenerateMessageFunctionDelegate = GenerateMessage;
            _listener = new HttpListener();
            foreach (string Itm in _prefixes)
            {
                _listener.Prefixes.Add(Itm);
            }

            _starting = true;
            _work = new Thread(new ParameterizedThreadStart(WaitForNextRecievedContext));

            _work.Start(args);
        }
        private void WaitForNextRecievedContext(object args = null)
        {
            _listener.Start();
            _starting = false;
            do
            {

                Components.AsyncCallbackContainer container = new Components.AsyncCallbackContainer(ref _listener, ref args);

                IAsyncResult result = _listener.BeginGetContext(new AsyncCallback(HandleRecievedContext), container);
                result.AsyncWaitHandle.WaitOne();

            } while (_listener.IsListening == true);

        }
        private void HandleRecievedContext(IAsyncResult result)
        {
            if (_listener.IsListening == true)
            {
                Components.AsyncCallbackContainer container = result.AsyncState as Components.AsyncCallbackContainer;
                HttpListener http = container.HttpListener;
                HttpListenerContext context = http.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                try
                {
                    byte[] buff = System.Text.Encoding.UTF8.GetBytes(this.GenerateMessageFunctionDelegate(new Components.StateInfo(ref http,
                                                                                                                                   ref context,
                                                                                                                                   ref request,
                                                                                                                                   ref response,
                                                                                                                                   ref container.Argument)
                                                                                                          )
                                                                    );
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buff, 0, buff.Length);
                    output.Flush(); output.Close();

                }
                catch (Exception ex)
                {
                    response.StatusCode = 500;
                    response.StatusDescription = "InternalServerException";
                    response.ContentType = "text/javascript";
                    response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(ex.ToString()), 0, System.Text.Encoding.UTF8.GetBytes(ex.ToString()).Length);
                    response.OutputStream.Flush(); response.OutputStream.Close();

                }
            }
        }
        #endregion



        /// <summary>
        /// Stop the worker thread responsible for handling incoming requests, and stop listening to any network traffic.
        /// </summary>
        public void StopHost()
        {
            this._listener.Stop();
        }

    }
}