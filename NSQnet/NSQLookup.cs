﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NSQnet.Extensions;

namespace NSQnet
{
    public class NSQLookup
    {
        public NSQLookup() { }
        public NSQLookup(String hostname)
        {
            this.Hostname = hostname;
        }

        public NSQLookup(String hostname, Int32 port)
        {
            this.Hostname = hostname;
            this.Port = port;
        }

        /// <summary>
        /// The Hostname to connect to.
        /// </summary>
        public String Hostname { get; set; }

        /// <summary>
        /// The Port number to connect to.
        /// </summary>
        public Int32 Port { get; set; }

        protected HttpWebRequest _getHttpRequest(String absolutePath, object parameters = null)
        {
            if (parameters != null)
                return _getHttpRequestImpl(absolutePath, parameters.ToDynamic());
            else
                return _getHttpRequestImpl(absolutePath, null);
        }

        protected HttpWebRequest _getHttpRequestImpl(String absolutePath, IDictionary<String, Object> parameters)
        {
            if (String.IsNullOrWhiteSpace(this.Hostname))
                throw new ArgumentException("Hostname must be set.", "Hostname");

            if (this.Port == default(Int16))
                throw new ArgumentException("Port must be set.", "Port");

            if (String.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentException("Cannot be null or whitespace.", "absolutePath");

            absolutePath = absolutePath.StartsWith("/") ? absolutePath.Substring(1, absolutePath.Length - 1) : absolutePath;

            Uri targetUri = null;

            if(parameters != null)
                targetUri = new Uri(String.Format("http://{0}:{1}/{2}?{3}", Hostname, Port, absolutePath, _dictToQS(parameters)));
            else
                targetUri = new Uri(String.Format("http://{0}:{1}/{2}", Hostname, Port, absolutePath));

            return (HttpWebRequest)WebRequest.Create(targetUri);
        }

        protected async Task<String> _getHttpResponseBody(String absolutePath, object parameters)
        {
            if (null != parameters && !(parameters is IDictionary<String, Object>))
                parameters = parameters.ToDynamic();

            var request = _getHttpRequestImpl(absolutePath, parameters as IDictionary<String, Object>);
            var response = await request.GetResponseAsync() as HttpWebResponse;
            return response.GetResponseStream().ReadAll();
        }

        /// <summary>
        /// Ping the server.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public Boolean Ping()
        {
            return PingAsync().Result;
        }

        /// <summary>
        /// Pings the server asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<Boolean> PingAsync()
        {
            var request = _getHttpRequest("/ping");
            var response = await request.GetResponseAsync() as HttpWebResponse;
            String responseBody = response.GetResponseStream().ReadAll();
            return response.StatusCode == HttpStatusCode.OK && responseBody.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }

        public IEnumerable<NSQProducer> ProducersForTopic(String topicName)
        {
            return ProducersForTopicAsync(topicName).Result;
        }

        public async Task<IEnumerable<NSQProducer>> ProducersForTopicAsync(String topicName)
        {
            var responseBody = await _getHttpResponseBody("/lookup", new { topic = topicName });

            dynamic responseData = _getDataFromResponse(responseBody);
            IEnumerable<Object> values = responseData.producers;
            if (values != null && values.Any())
                return values.Select(_ => Utility.Reflection.MarshallAs<NSQProducer>(_ as IDictionary<String, Object>));
            else
                return new List<NSQProducer>();
        }

        public IEnumerable<String> Topics()
        {
            return TopicsAsync().Result;
        }

        public async Task<IEnumerable<String>> TopicsAsync()
        {
            var responseBody = await _getHttpResponseBody("/topics", null);
            dynamic responseData = _getDataFromResponse(responseBody);
            IEnumerable<object> list = responseData.topics;

            if (list != null && list.Any())
                return list.Select(_ => _.ToString());
            else
                return new List<String>();
        }

        public IEnumerable<String> ChannelsForTopic(String topicName)
        {
            return ChannelsForTopicAsync(topicName).Result;
        }

        public async Task<IEnumerable<String>> ChannelsForTopicAsync(String topicName)
        {
            var responseBody = await _getHttpResponseBody("/channels", null);
            dynamic responseData = _getDataFromResponse(responseBody);
            IEnumerable<object> list = responseData.channels;

            if (list != null && list.Any())
                return list.Select(_ => _.ToString());
            else
                return new List<String>();
        }

        public IEnumerable<NSQProducer> Nodes()
        {
            return NodesAsync().Result;
        }

        public async Task<IEnumerable<NSQProducer>> NodesAsync()
        {
            var responseBody = await _getHttpResponseBody("/nodes", null);
            dynamic responseData = _getDataFromResponse(responseBody);
            IEnumerable<Object> values = responseData.producers;
            if (values != null && values.Any())
                return values.Select(_ => Utility.Reflection.MarshallAs<NSQProducer>(_ as IDictionary<String, Object>));
            else
                return new List<NSQProducer>();
        }

        public Boolean DeleteTopic(String topicName)
        {
            return DeleteTopicAsync(topicName).Result;
        }

        public async Task<Boolean> DeleteTopicAsync(String topicName)
        {
            var request = _getHttpRequest("/delete_topic", new { topic = topicName });
            var response = await request.GetResponseAsync() as HttpWebResponse;
            String responseBody = response.GetResponseStream().ReadAll();
            return response.StatusCode == HttpStatusCode.OK && responseBody.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }

        public Boolean DeleteChannel(String channelName, String nodeName)
        {
            return DeleteChannelAsync(channelName, nodeName).Result;
        }

        public async Task<Boolean> DeleteChannelAsync(String channelName, String nodeName)
        {
            var request = _getHttpRequest("/delete_channel", new { channel = channelName, node = nodeName });
            var response = await request.GetResponseAsync() as HttpWebResponse;
            String responseBody = response.GetResponseStream().ReadAll();
            return response.StatusCode == HttpStatusCode.OK && responseBody.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }

        public Boolean TombstoneProducer(String topicName, String nodeName)
        {
            return TombstoneProducerAsync(topicName, nodeName).Result;
        }

        public async Task<Boolean> TombstoneProducerAsync(String topicName, String nodeName)
        {
            var request = _getHttpRequest("/tombstone_topic_producer", new { channel = topicName, node = nodeName });
            var response = await request.GetResponseAsync() as HttpWebResponse;
            String responseBody = response.GetResponseStream().ReadAll();
            return response.StatusCode == HttpStatusCode.OK && responseBody.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }

        public NSQServerInfo Info()
        {
            return InfoAsync().Result;
        }

        public async Task<NSQServerInfo> InfoAsync()
        {
            var responseBody = await _getHttpResponseBody("/info", null);
            IDictionary<String, Object> data = _getDataFromResponse(responseBody);
            return Utility.Reflection.MarshallAs<NSQServerInfo>(data);
        }

        protected static String _dictToQS(IDictionary<String, Object> parameters)
        {
            return String.Join("&", parameters.Select(_ => String.Format("{0}={1}", _.Key, Uri.EscapeDataString(_.Value.ToString()))));
        }

        protected static dynamic _getDataFromResponse(String responseBody)
        {
            dynamic resp = JsonSerializer.Current.DeserializeObject(responseBody);
            return resp.data;
        }
    }

    public class NSQProducer
    {
        public String Address { get; set; }
        public String Hostname { get; set; }
        public String BroadcastAddress { get; set; }
        public Int64 TCP_Port { get; set; }
        public Int64 Http_Port { get; set; }
        public String Version { get; set; }
    }

    public struct NSQServerInfo
    {
        String Version { get; set; }
    }
}
