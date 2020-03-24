﻿using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace oobi
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
            public List<string> HostBindingPrefixes {
                get {try { return _listener.Prefixes.ToList<string>();
                    } catch { return _prefixes; }
                    }
                set {
                    try {
                        _listener.Prefixes.Clear();
                        foreach (string i in value) {
                            _listener.Prefixes.Add(i);
                        }
                        _prefixes = value;
                    } catch (Exception ex) { throw ex; }
                }
            }
            private List<string> _prefixes;
            //------
            /// <summary>
            /// The HttpListener base object this class is operating upon.
            /// </summary>
            public HttpListener HttpListener {
                    get { return _listener; }
            }
            private HttpListener _listener;
            //------
            /// <summary>
            /// The function delegate that is responsible for processing incoming requests to the server.
            /// </summary>
            public Func<StateInfo, string> GenerateMessageFunctionDelegate;
            //------
            /// <summary>
            /// Determines if the System.Net.HttpListener class is actively listening.
            /// </summary>
            public bool IsRunning {
                get {
                    if (_starting == false) { return _listener.IsListening; } else { return false; }
                }
            }
            //------
            /// <summary>
            /// Determines if the asynchronous operations are still waiting for the listener to start.
            /// Will return false if running or stopped, but true if start signal was received but the listener has not started yet.
            /// </summary>
            public bool isStarting {
                get { return _starting; }
            }
        #endregion

        #region "Container Classes"
            private class ServerArgumentContainer
            {
                public List<object> AdditionalArguments;

                public ServerArgumentContainer(List<object> CustomObjectArguments) {
                    AdditionalArguments = CustomObjectArguments;
                }
            }   
            private class AsyncCallbackContainer
            {
                public ServerArgumentContainer ServerArgumentContainer;
                public HttpListener HttpListener;

                public AsyncCallbackContainer(ref HttpListener listener, ref ServerArgumentContainer arguments) {
                    HttpListener = listener;
                    ServerArgumentContainer = arguments;
                }
            }

            /// <summary>
            /// Passed to the 'GenerateMessage' method containing the listener instance, request, context, response, and additional object references.
            /// The string returned from the 'GenerateMessage' function is sent as the final transmission through the responding sockets before closure.
            /// </summary>
            public class StateInfo
            {
                public HttpListener Listener;
                public HttpListenerContext Context;
                public HttpListenerRequest Request;
                public HttpListenerResponse Response;
                public List<object> AdditionalArguments;

                public DateTime CreationTime;

                public StateInfo(ref HttpListener http, ref HttpListenerContext context,
                                       ref HttpListenerRequest request, ref HttpListenerResponse response,
                                       ref List<object> Arguments) {
                    Listener = http;
                    Context = context;
                    Request = request;
                    Response = response;
                    AdditionalArguments = Arguments;

                    CreationTime = DateTime.Now;
                }
            }
        #endregion

        #region "Private Objects"
            private Thread _work;
            private bool _workPendStop = false;
            private bool _starting = false;
        #endregion

        

        /// <summary>
        /// Create a new instance of an HttpHost, for handling your (a)synchronous HTTP requests.
        /// </summary>
        /// <param name="BindingPrefixes">A List of URI compatible strings (wild-cards allowed) for the System.Net.HttpListener to bind to.</param>
        /// <param name="GenerateMessage">The string function res</param>
        public HttpHost(List<string> BindingPrefixes) {
            _prefixes = BindingPrefixes;
        }


        #region "Synchronous Operations"
            /// <summary>
            /// Begin accepting and processing incoming connections received on the bound prefixes (synchronously).
            /// </summary>
            public void StartHost(Func<StateInfo, string> GenerateMessage, List<object> args = null) {
                _listener = new HttpListener();
                foreach (string Itm in _prefixes) {
                    _listener.Prefixes.Add(Itm);
                }

                _listener.Start();

                do {
                    HttpListenerContext context = _listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    try {
                        StateInfo scenario = new StateInfo(ref _listener, ref context, ref request, ref response, ref args);

                        string message = GenerateMessage(scenario); ;
                        byte[] buff = System.Text.Encoding.UTF8.GetBytes(message);

                        System.IO.Stream output = response.OutputStream;
                        output.Write(buff, 0, buff.Length);
                        output.Flush(); output.Close();

                    } catch (Exception ex) {
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
            public void StartHostAsync(Func<StateInfo, string> GenerateMessage, List<object> args = null) {
                GenerateMessageFunctionDelegate = GenerateMessage;
                _listener = new HttpListener();
                foreach (string Itm in _prefixes) {
                    _listener.Prefixes.Add(Itm);
                }
            
                _starting = true;

                var customArguments = new ServerArgumentContainer(args);
                _work = new Thread(new ParameterizedThreadStart(WaitForNextRecievedContext));
            
                _work.Start(customArguments);
            }
            private void WaitForNextRecievedContext(object args = null) {
                var arguments = args as ServerArgumentContainer;

                _listener.Start(); 
                _starting = false;
                do {

                    AsyncCallbackContainer container = new AsyncCallbackContainer(ref _listener, ref arguments);

                    IAsyncResult result = _listener.BeginGetContext(new AsyncCallback(HandleRecievedContext), container);
                    result.AsyncWaitHandle.WaitOne();

                } while (_listener.IsListening == true);
            }
            private void HandleRecievedContext(IAsyncResult result) {
                if (_listener.IsListening == true && _workPendStop != true) {
                    AsyncCallbackContainer container = result.AsyncState as AsyncCallbackContainer;
                    HttpListener http = container.HttpListener;
                    HttpListenerContext context = http.EndGetContext(result);
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    try {
                        byte[] buff = System.Text.Encoding.UTF8.GetBytes(this.GenerateMessageFunctionDelegate(new StateInfo(ref http, 
                                                                                                                            ref context, 
                                                                                                                            ref request, 
                                                                                                                            ref response, 
                                                                                                                            ref container.ServerArgumentContainer.AdditionalArguments)
                                                                                                              )
                                                                         );

                        System.IO.Stream output = response.OutputStream;
                        output.Write(buff, 0, buff.Length);
                        output.Flush(); output.Close();                   
                    
                    } catch (Exception ex) {
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
        public void StopHost() {
            _workPendStop = true;

            if (_work.IsAlive) { _work.Abort(); }
            if (_listener.IsListening == true) { _listener.Stop(); }

            _workPendStop = false;
        }

    }
}
