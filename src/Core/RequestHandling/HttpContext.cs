﻿using pandapache.src;

namespace PandApache3.src.Core.RequestHandling
{
    public class HttpContext
    {
        public Request Request { get; set; }
        public HttpResponse Response { get; set; }
        public bool isAuth { get; set; } = false;

        public HttpContext(Request request, HttpResponse response)
        {
            Request = request;
            Response = response;
        }
    }
}
