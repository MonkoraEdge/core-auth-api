namespace MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate
{
    public static class HeaderName
    {
        // Add at least one constant to avoid empty class diagnostic
        public const string Authorization = "Authorization";
        public const string CorrelationId = "X-Correlation-ID";
        public const string TraceId = "X-Trace-ID";
        public const string SpanId = "X-Span-ID";
        public const string RequestId = "X-Request-ID";
        public const string UserAgent = "User-Agent";
        public const string ContentType = "Content-Type";
        public const string Accept = "Accept";
        public const string AcceptLanguage = "Accept-Language";
        public const string CacheControl = "Cache-Control";
        public const string IfNoneMatch = "If-None-Match";
        public const string IfModifiedSince = "If-Modified-Since";
        public const string XForwardedFor = "X-Forwarded-For";
        public const string XForwardedHost = "X-Forwarded-Host";
        public const string XForwardedProto = "X-Forwarded-Proto";  
        public const string XRealIp = "X-Real-IP";        
    }
}
