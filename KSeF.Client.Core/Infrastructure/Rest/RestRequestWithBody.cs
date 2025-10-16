﻿using System;
using System.Collections.Generic;
using System.Net.Http;

namespace KSeF.Client.Core.Infrastructure.Rest
{
    public sealed class RestRequest<TBody> : IRestRequestWithBody<TBody>
    {
        public string Path { get; private set; }
        public HttpMethod Method { get; private set; }
        public string AccessToken { get; private set; }
        public string ContentType { get; private set; }
        public string Accept { get; private set; }
        public IDictionary<string, string> Headers { get; private set; }
        public IDictionary<string, string> Query { get; private set; }

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);
        public TimeSpan? Timeout { get; private set; }
        public string ApiVersion { get; private set; }

        public TBody Body { get; private set; }

        public static RestRequest<TBody> New(string path, HttpMethod method, TBody body, RestContentType contentType = RestContentType.Json)
        {
            if (object.Equals(body, null)) throw new ArgumentNullException(nameof(body));
            RestRequest<TBody> r = new RestRequest<TBody>(path, method, body);
            r.ContentType = contentType.ToMime();
            return r;
        }

        private RestRequest(string path, HttpMethod method, TBody body)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (method == null) throw new ArgumentNullException(nameof(method));

            Path = path;
            Method = method;
            Body = body;

            ContentType = RestContentType.Json.ToMime();
            Accept = null;
            AccessToken = null;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Timeout = DefaultTimeout;
            ApiVersion = null;
        }

        public RestRequest<TBody> AddAccessToken(string accessToken) { AccessToken = accessToken; return this; }
        public RestRequest<TBody> AddHeader(string name, string value)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(Headers, StringComparer.OrdinalIgnoreCase);
            d[name] = value; Headers = d; return this;
        }
        public RestRequest<TBody> AddQueryParameter(string name, string value)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(Query, StringComparer.OrdinalIgnoreCase);
            d[name] = value; Query = d; return this;
        }
        public RestRequest<TBody> WithAccept(string accept) { Accept = accept; return this; }
        public RestRequest<TBody> WithTimeout(TimeSpan timeout) { Timeout = timeout; return this; }
        public RestRequest<TBody> WithContentType(string contentType) { ContentType = contentType; return this; }
        public RestRequest<TBody> WithApiVersion(string apiVersion) { ApiVersion = apiVersion; return this; }
    }
}
