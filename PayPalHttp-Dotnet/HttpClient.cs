﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PayPalHttp
{
    public class HttpClient
    {               
        private readonly System.Net.Http.HttpClient _client;
        private readonly List<IInjector> _injectors = [];
        protected TimeSpan _timeout = TimeSpan.FromMinutes(5); //5 minute http pool default timeout
        protected readonly IEnvironment _environment;

        private static readonly ConcurrentDictionary<string, System.Net.Http.HttpClient> ClientDictionary = new();

        public Encoder Encoder { get; private set; }


        public HttpClient(IEnvironment environment)
        {
            _environment = environment;
            Encoder = new Encoder();
            _client = GetHttpClient(environment.BaseUrl());
        }

        protected virtual SocketsHttpHandler GetHttpSocketHandler()
        {
            return new SocketsHttpHandler() {  PooledConnectionLifetime = _timeout };
        }

        protected virtual System.Net.Http.HttpClient GetHttpClient(string baseUrl)
        {
            return ClientDictionary.GetOrAdd(baseUrl.ToLower(), (bUrl) => {
                var client = new System.Net.Http.HttpClient(GetHttpSocketHandler())
                {
                    BaseAddress = new Uri(baseUrl)
                };
                client.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());

                return client;
            });
        }

        protected virtual string GetUserAgent()
        {
            return "PayPalHttp-Dotnet HTTP/1.1";
        }

        public void AddInjector(IInjector injector)
        {
            if (injector != null)
            {
                _injectors.Add(injector);
            }
        }

        public void SetConnectTimeout(TimeSpan timeout)
        {
            _client.Timeout = _timeout = timeout;
        }

        public virtual async Task<HttpResponse> Execute<T>(T req) where T: HttpRequest
        {
            var request = req.Clone<T>();

            foreach (var injector in _injectors) {
                request = await injector.InjectAsync(request).ConfigureAwait(false);
            }

            request.RequestUri = new Uri(_environment.BaseUrl() + request.Path);

            if (request.Body != null)
            {
                request.Content = await Encoder.SerializeRequestAsync(request).ConfigureAwait(false);
            }

			var response = await _client.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                object responseBody = null;
                if (response.Content.Headers.ContentType != null)
                {
                    responseBody = await Encoder.DeserializeResponseAsync(response.Content, request.ResponseType).ConfigureAwait(false);
                }
                return new HttpResponse(response.Headers, response.StatusCode, responseBody);
            }
            else
            {
				var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				throw new HttpException(response.StatusCode, response.Headers, responseBody);
            }
        }

        public virtual async Task<HttpResponseMessage> ExecuteRaw<T>(T req) where T : HttpRequest
        {
            var request = req.Clone<T>();

            foreach (var injector in _injectors)
            {
                request = await injector.InjectAsync(request).ConfigureAwait(false);
            }

            request.RequestUri = new Uri(_environment.BaseUrl() + request.Path);

            if (request.Body != null)
            {
                request.Content = await Encoder.SerializeRequestAsync(request).ConfigureAwait(false);
            }

            return await _client.SendAsync(request).ConfigureAwait(false);           
        }
    }
}
