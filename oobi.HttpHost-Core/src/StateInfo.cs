using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace oobi.HttpHostCore.Components
{
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
        public object Argument;

        public DateTime CreationTime;

        public StateInfo(ref HttpListener http, ref HttpListenerContext context,
                               ref HttpListenerRequest request, ref HttpListenerResponse response,
                               ref object argument)
        {
            Listener = http;
            Context = context;
            Request = request;
            Response = response;
            Argument = argument;

            CreationTime = DateTime.Now;
        }
    }

}
