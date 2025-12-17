using System;
using WireMock.Server;

namespace PayPalHttp.Tests
{

	public class TestEnvironment(int port, bool useSSL = false) : PayPalHttp.IEnvironment
	{
		public string BaseUrl()
        {
            var scheme = useSSL ? "https" : "http";
            return scheme + "://localhost:" + port;
		}
	}

    public class TestHarness: IDisposable
    {
        protected WireMockServer server;

		public TestHarness()
        {
			server = WireMockServer.Start();
    	}

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
    		server.Stop();
        }

        protected PayPalHttp.HttpClient Client()
        {
            return new PayPalHttp.HttpClient(new TestEnvironment(server.Ports[0]));
        }
    }
}
