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

    	public void Dispose()
    	{
    		server.Stop();
        }

        protected PayPalHttp.HttpClient Client()
        {
            return new PayPalHttp.HttpClient(new TestEnvironment(server.Ports[0]));
        }
    }
}
