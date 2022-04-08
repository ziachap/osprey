namespace Osprey.Http
{
    public class HttpMessage<T, Y>
    {
        public string Endpoint { get; set; }
        public HttpVerb RequestType { get; set; }
        public NodeInfo Sender { get; set; }
        public T Payload { get; set; }
    }

    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }
}