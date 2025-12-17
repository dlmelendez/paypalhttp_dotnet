using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayPalHttp
{
    public partial class TextSerializer : ISerializer
    {
        private const string RegExPattern = "^text/.*$";
        private static readonly Regex _pattern = ContentTypeRegEx();

        public async Task<object> DecodeAsync(HttpContent content, Type responseType)
        {
            return await content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<HttpContent> EncodeAsync(HttpRequest request)
        {
            return await Task.FromResult(new StringContent(request.Body.ToString())).ConfigureAwait(false);
        }

        public Regex GetContentRegEx()
        {
            return _pattern;
        }

        public string GetContentTypeRegexPattern()
        {
            return RegExPattern;
        }

        [GeneratedRegex(RegExPattern, RegexOptions.Compiled)]
        private static partial Regex ContentTypeRegEx();
    }
}
