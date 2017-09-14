using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a client used to connect to a network time server
    /// </summary>
    public class NtpClient : IDisposable
    {
        readonly UdpClient _udpClient;

        public TimeSpan Timeout
        {
            get => TimeSpan.FromMilliseconds(_udpClient.Client.ReceiveTimeout);
            set
            {
                if (value < TimeSpan.FromMilliseconds(1))
                    throw new ArgumentOutOfRangeException();
                _udpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates new NtpClient from server endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint of the remote NTP server</param>
        public NtpClient(IPEndPoint endpoint)
        {
            _udpClient = new UdpClient {Client = {ReceiveTimeout = 15000}};
            _udpClient.Connect(endpoint);
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates new NtpClient from server's IP address and optional port
        /// </summary>
        /// <param name="address">IP address of remote NTP server</param>
        /// <param name="port">Port of remote NTP server. Default is 123.</param>
        public NtpClient(IPAddress address, int port = 123) : this(new IPEndPoint(address, port)) { }

        /// <inheritdoc />
        /// <summary>
        /// Creates new NtpClient from server's host name and optional port
        /// </summary>
        /// <param name="host">The hostname of the NTP server to connect to</param>
        /// <param name="port">The port of the NTP server to connect to</param>
        public NtpClient(string host, int port = 123) : this(Dns.GetHostAddresses(host).First(), port) { }

        /// <inheritdoc />
        public void Dispose() { _udpClient.Close(); }

        /// <summary>
        /// Queries the NTP server and returns correction offset
        /// </summary>
        /// <returns>
        /// Time that should be added to local time to synchronize it with NTP server
        /// </returns>
        public TimeSpan GetCorrectionOffset() { return Query().CorrectionOffset; }

        /// <summary>
        /// Queries NTP server with configurable NTP packet
        /// </summary>
        /// <param name="request">NTP packet to use when querying the network time server </param>
        /// <returns>The response from the NTP server</returns>
        public NtpPacket Query(NtpPacket request)
        {
            _udpClient.Send(request.Bytes, request.Bytes.Length);
            IPEndPoint remote = null;
            var response = new NtpPacket(_udpClient.Receive(ref remote))
            {
                OriginTimestamp = request.OriginTimestamp,
                DestinationTimestamp = DateTime.UtcNow
            };
            return response;
        }

        /// <summary>
        /// Queries NTP server with default options
        /// </summary>
        /// <returns>NTP packet returned from the server</returns>
        public NtpPacket Query() { return Query(new NtpPacket { OriginTimestamp = DateTime.UtcNow }); }
    }
}