using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet
{
    public class NetworkServer
    {
        public RaknetServerOptions Options { get; }
        public readonly ArrayPool<byte> FramesPool = ArrayPool<byte>.Create(
            maxArrayLength: 2048,
            maxArraysPerBucket: 4096
        );
        public event Action<NetworkConnection>? OnConnected;
        public event Action<NetworkConnection>? OnDisconnected;
        public event Action<NetworkConnection, ReadOnlyMemory<byte>>? OnMessage;

        public ulong ServerGuid = unchecked((ulong)Random.Shared.NextInt64());

        private readonly byte[] CookieSecret = RandomNumberGenerator.GetBytes(32);
        private readonly Dictionary<EndpointKey, NetworkServerConnection> Connections = [];
        private Socket? Listener;

        public NetworkServer(RaknetServerOptions options = default)
        {
            Options = options.Equals(default(RaknetServerOptions)) ? new RaknetServerOptions() : options;
        }

        public async ValueTask Start()
        {
            byte[] buffer = new byte[2048];
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                try
                {
                    Listener.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
                }
                catch { }
            }
            Listener.Bind(new IPEndPoint(IPAddress.Any, 19132));
            _ = RunTickLoop();
            SocketAddress recieve = new(AddressFamily.InterNetwork);
            while (true)
            {
                try
                {
                    int received = await Listener.ReceiveFromAsync(buffer, SocketFlags.None, recieve);
                    if (received > 0)
                        RecieveFrom(recieve, buffer.AsSpan(0, received));
                }
                catch (Exception)
                {
                }
            }
        }
        public void RecieveFrom(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            if (message.Length < 1 || Listener is null)
            {
                return;
            }

            byte PacketId = message[0];
            switch (PacketId)
            {
                case UnconnectedPing.PacketId:
                {
                    HandleUnconnectedPing(endpoint, message);
                    break;
                }
                case OpenConnectionRequestOne.PacketId:
                {
                    HandleOpenConnectionRequestOne(endpoint, message);
                    break;
                }
                case OpenConnectionRequestTwo.PacketId:
                {
                    HandleOpenConnectionRequestTwo(endpoint, message);
                    break;
                }
                case >= 0x80 and <= 0x8d:
                {
                    HandleFrameSet(endpoint, message);
                    break;
                }
                case Ack.PacketId:
                {
                    HandleAck(endpoint, message);
                    break;
                }
                case Nack.PacketId:
                {
                    HandleNack(endpoint, message);
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        private void HandleFrameSet(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            try
            {
                FrameSet frameSet = FrameSet.Deserialize(message);
                EndpointKey endpointKey = new(endpoint);
                if (Connections.TryGetValue(endpointKey, out NetworkServerConnection? connection))
                {
                    connection.HandleFrameSet(frameSet);
                    return;
                }
            }
            catch (Exception)
            {
            }
        }

        private void HandleAck(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            EndpointKey key = new(endpoint);
            if (!Connections.TryGetValue(key, out NetworkServerConnection? connection))
            {
                return;
            }

            Ack ack = Ack.Deserialize(message);
            connection.HandleAck(ack);
        }

        private void HandleNack(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            EndpointKey key = new(endpoint);
            if (!Connections.TryGetValue(key, out NetworkServerConnection? connection))
            {
                return;
            }

            Nack nack = Nack.Deserialize(message);
            connection.HandleNack(nack);
        }

        private void HandleUnconnectedPing(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            UnconnectedPing Ping = UnconnectedPing.Deserialize(message);
            UnconnectedPong Pong = new(Ping.Time, ServerGuid, Options.Advertisement);
            byte[] ReplyFrame = FramesPool.Rent(2048);
            try
            {
                int ReplyLength = UnconnectedPong.Serialize(Pong, ReplyFrame);
                Listener!.SendTo(ReplyFrame.AsSpan(0, ReplyLength), SocketFlags.None, endpoint);
            }
            finally
            {
                FramesPool.Return(ReplyFrame);
            }
        }

        private void HandleOpenConnectionRequestOne(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            if (message.Length < 1 + Magic.MAGIC_LENGTH + 1)
            {
                return;
            }

            byte ProtocolVersion = message[1 + Magic.MAGIC_LENGTH];
            ushort MTU = (ushort)Math.Clamp(message.Length + 28, (int)(ushort)576, (int)Options.MaxMtu);
            uint? Cookie = null;
            if (Options.EnableCookies /* && endpoint is IPEndPoint IpEndPoint*/)
            {
                Cookie = ConnectionCookie.Create(endpoint, CookieSecret);
            }

            OpenConnectionReplyOne Reply = new((long)ServerGuid, Cookie, MTU);
            byte[] ReplyFrame = FramesPool.Rent(2048);
            try
            {
                int ReplyLength = OpenConnectionReplyOne.Serialize(Reply, ReplyFrame);
                Listener!.SendTo(ReplyFrame.AsSpan(0, ReplyLength), SocketFlags.None, endpoint);
            }
            finally
            {
                FramesPool.Return(ReplyFrame);
            }
        }

        private void HandleOpenConnectionRequestTwo(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            OpenConnectionRequestTwo Request = OpenConnectionRequestTwo.Deserialize(message);

            if (Options.EnableCookies)
            {
                if (!Request.Cookie.HasValue || !ConnectionCookie.Validate(endpoint, CookieSecret, Request.Cookie.Value))
                {
                    return;
                }
                //return;
            }

            ushort selectedMtu = Math.Clamp(Request.MTU, (ushort)576, Options.MaxMtu);
            OpenConnectionReplyTwo Reply = new((long)ServerGuid, endpoint, selectedMtu, false);

            byte[] ReplyFrame = FramesPool.Rent(2048);
            try
            {
                int ReplyLength = OpenConnectionReplyTwo.Serialize(Reply, ReplyFrame);
                Listener!.SendTo(ReplyFrame.AsSpan(0, ReplyLength), SocketFlags.None, endpoint);
            }
            finally
            {
                FramesPool.Return(ReplyFrame);
            }

            SocketAddress connectionEndpoint = CloneEndpoint(endpoint);
            EndpointKey connectionKey = new(connectionEndpoint);
            if (Connections.ContainsKey(connectionKey))
            {
                return;
            }

            NetworkServerConnection connection = new(Listener!, connectionEndpoint, Request.ClientId, selectedMtu);
            connection.Connected += connected => OnConnected?.Invoke(connected);
            connection.Disconnected += disconnected =>
            {
                if (disconnected is NetworkServerConnection serverConnection)
                {
                    Connections.Remove(new EndpointKey(serverConnection.Endpoint));
                }

                OnDisconnected?.Invoke(disconnected);
            };
            connection.Message += (source, payload) => OnMessage?.Invoke(source, payload);
            if (Connections.Count >= Options.MaxConnections)
            {
                return;
            }

            Connections[connectionKey] = connection;
        }

        private async Task RunTickLoop()
        {
            PeriodicTimer timer = new(TimeSpan.FromMilliseconds(50));
            while (await timer.WaitForNextTickAsync())
            {
                long now = Environment.TickCount64;
                foreach (NetworkServerConnection connection in Connections.Values)
                {
                    try
                    {
                        connection.Tick(now);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static SocketAddress CloneEndpoint(SocketAddress endpoint)
        {
            SocketAddress clone = new(endpoint.Family, endpoint.Size);
            for (int i = 0; i < endpoint.Size; i++)
            {
                clone[i] = endpoint[i];
            }

            return clone;
        }

        private readonly struct EndpointKey : IEquatable<EndpointKey>
        {
            private readonly AddressFamily family;
            private readonly byte[] bytes;

            public EndpointKey(SocketAddress endpoint)
            {
                family = endpoint.Family;
                bytes = endpoint.Buffer.Span.Slice(0, endpoint.Size).ToArray();
            }

            public bool Equals(EndpointKey other)
            {
                if (family != other.family || bytes.Length != other.bytes.Length)
                {
                    return false;
                }

                return bytes.AsSpan().SequenceEqual(other.bytes);
            }

            public override bool Equals(object? obj)
            {
                return obj is EndpointKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();
                hash.Add((int)family);
                hash.AddBytes(bytes);
                return hash.ToHashCode();
            }
        }

    }
}
