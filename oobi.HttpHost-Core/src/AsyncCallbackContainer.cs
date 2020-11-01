using System;
using System.Collections.Generic;
using System.Net;
using System.Text;


namespace oobi.HttpHostCore.Components
{

    internal class AsyncCallbackContainer
    {
        public object Argument;
        public HttpListener HttpListener;

        public AsyncCallbackContainer(ref HttpListener listener, ref object argument)
        {
            HttpListener = listener;
            Argument = argument;
        }
    }


}
