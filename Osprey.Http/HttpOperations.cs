using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Osprey.Logging;

namespace Osprey.Http
{
    /*
	public interface IHttp
	{
		Task<T> Get<T>(string url, CancellationToken cancellationToken);
		Task<T> Post<TBody, T>(string url, TBody data, CancellationToken cancellationToken);
		Task<T> Put<TBody, T>(string url, TBody data, CancellationToken cancellationToken);
		Task<T> Delete<T>(string url, CancellationToken cancellationToken);
	}*/

	public static class HttpOperations // TODO: Give this a better name
	{
		private static HttpClient Client()
		{
			return new HttpClient();
		}

		public static async Task<T> Get<T>(string url, CancellationToken cancellationToken)
		{
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers = { { "Accept", "application/json" } }
            };

            var response = await Send<T>(message, cancellationToken);

            return response;
        }

		public static async Task<T> Post<TBody, T>(string url, TBody data, CancellationToken cancellationToken)
        {
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StreamContent(ToStream(data)),
                Headers = { { "Accept", "application/json" } }
            };

            var response = await Send<T>(message, cancellationToken);

            return response;
        }

		public static async Task<T> Put<TBody, T>(string url, TBody data, CancellationToken cancellationToken)
        {
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri(url),
                Content = new StreamContent(ToStream(data)),
                Headers = { { "Accept", "application/json" } }
            };

            var response = await Send<T>(message, cancellationToken);

            return response;
        }

		public static async Task<T> Delete<T>(string url, CancellationToken cancellationToken)
        {
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers = { { "Accept", "application/json" } }
            };

            var response = await Send<T>(message, cancellationToken);

            return response;
        }

        private static async Task<T> Send<T>(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            using (var client = Client())
            {
                var requestTask = client.SendAsync(message, cancellationToken);

                HttpResponseMessage response;
                try
                {
                    response = await requestTask;
                }
                catch (HttpRequestException ex)
                {
                    OspreyLog.Warn(ex.ToString());
                    throw;
                }
                catch (Exception ex)
                {
                    OspreyLog.Warn(ex.ToString());
                    throw;
                }

                var content = await response.Content.ReadAsStringAsync();

                var deserialized = Osprey.Network.Serializer.Deserialize<T>(content);

                return deserialized;
            }
        }

        private static Stream ToStream(object data)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
            return stream;
        }
    }
}
