﻿
namespace pandapache.src.RequestHandling
{
    public class HttpContext
    {
        public Request Request {  get; set; }
        public HttpResponse Response { get; set; }
        public HttpContext(Request request, HttpResponse response) 
        {
            Request = request;
            Response = response;
        }
    }
}
