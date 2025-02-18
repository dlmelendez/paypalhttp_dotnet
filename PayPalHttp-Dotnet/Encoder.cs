﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PayPalHttp
{
    public class Encoder
    {
        private static readonly Dictionary<string, ISerializer> DefaultSerializers = [];

        private readonly Dictionary<string, ISerializer> _serializerLookup;

        static Encoder()
        {
            RegisterSerializer(new JsonSerializer(), DefaultSerializers);
            RegisterSerializer(new TextSerializer(), DefaultSerializers);
            RegisterSerializer(new MultipartSerializer(), DefaultSerializers);
            RegisterSerializer(new FormEncodedSerializer(), DefaultSerializers);
        }

        public Encoder()
        {
            _serializerLookup = new Dictionary<string, ISerializer>(DefaultSerializers);
        }

        private static void RegisterSerializer(ISerializer serializer, Dictionary<string, ISerializer> serializerLookup)
        {
            if (serializer != null)
            {
                serializerLookup[serializer.GetContentTypeRegexPattern()] = serializer;
            }
        }

        public void RegisterSerializer(ISerializer serializer)
        {
            RegisterSerializer(serializer, _serializerLookup);
        }

        public async Task<HttpContent> SerializeRequestAsync(HttpRequest request)
        {
            if (request.ContentType == null)
            {
                throw new IOException("HttpRequest did not have content-type header set");
            }

            request.ContentType = request.ContentType.ToLower();
            
            ISerializer serializer = GetSerializer(request.ContentType) ?? throw new IOException($"Unable to serialize request with Content-Type {request.ContentType}. Supported encodings are {GetSupportedContentTypes()}");
            var content = await serializer.EncodeAsync(request).ConfigureAwait(false);

            if ("gzip".Equals(request.ContentEncoding))
            {
                var source = await content.ReadAsStringAsync().ConfigureAwait(false);
                content = new ByteArrayContent(await GzipAsync(source).ConfigureAwait(false));
            }

            return content;
        }

        public async Task<object> DeserializeResponseAsync(HttpContent content, Type responseType)
        {
            if (content.Headers.ContentType == null)
            {
                throw new IOException("HTTP response did not have content-type header set");
            }
            var contentType = content.Headers.ContentType.ToString();
            contentType = contentType.ToLower();
            ISerializer serializer = GetSerializer(contentType) ?? throw new IOException($"Unable to deserialize response with Content-Type {contentType}. Supported encodings are {GetSupportedContentTypes()}");
            var contentEncoding = content.Headers.ContentEncoding.FirstOrDefault();

            if ("gzip".Equals(contentEncoding))
            {
                var buf = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
                content = new StringContent(await GunzipAsync(buf).ConfigureAwait(false), Encoding.UTF8);
            }

            return await serializer.DecodeAsync(content, responseType).ConfigureAwait(false);
        }

        private ISerializer GetSerializer(string contentType)
        {
            return _serializerLookup.Values.FirstOrDefault(f => f.GetContentRegEx().Match(contentType).Success);
        }

        private string GetSupportedContentTypes()
        {
            return String.Join(", ", _serializerLookup.Keys);
        }

        private static async Task<byte[]> GzipAsync(string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);

            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                await msi.CopyToAsync(gs).ConfigureAwait(false);
            }

            return mso.ToArray();
        }

        private static async Task<string> GunzipAsync(byte[] source)
        {
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(new MemoryStream(source), CompressionMode.Decompress))
            {
                await gs.CopyToAsync(mso).ConfigureAwait(false);
            }

            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}
