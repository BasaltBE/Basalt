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
        public readonly ArrayPool<byte> FramesPool = ArrayPool<byte>.Create(
            maxArrayLength: 2048,
            maxArraysPerBucket: 4096
        );
        public event Action<NetworkConnection>? OnConnected;
        public event Action<NetworkConnection>? OnDisconnected;
        public event Action<NetworkConnection, ReadOnlyMemory<byte>>? OnMessage;

        public ulong ServerGuid = unchecked((ulong)Random.Shared.NextInt64());
        public string Advertisement = "MCPE;Basalt;924;1.21.90;0;10;03124212345;Bedrock level;Survival;1;19132;19133;";
        public bool EnableCookies = true; // all dat is temporary, we can move it to options or som else

        private readonly byte[] CookieSecret = RandomNumberGenerator.GetBytes(32);
        private Socket? Listener;

        public async ValueTask Start()
        {
            byte[] buffer = new byte[2048];
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Listener.Bind(new IPEndPoint(IPAddress.Any, 19132));
            SocketAddress recieve = new(AddressFamily.InterNetwork);
            while (true)
            {
                int received = await Listener.ReceiveFromAsync(buffer, SocketFlags.None, recieve);
                if (received > 0)
                    RecieveFrom(recieve, buffer.AsSpan(0, received));
            }
        }

        public void Listen(int port = 19132)
        {
            /*
            Listener.Bind(new IPEndPoint(IPAddress.Any, port));

            byte[] Frame = FramesPool.Rent(2048);


            try
            {
                while (true)
                {
                    EndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int Length = Listener.ReceiveFrom(Frame, ref RemoteEndPoint);
                    RecieveFrom(RemoteEndPoint, Frame.AsSpan(0, Length));
                }
            }
            finally
            {
                FramesPool.Return(Frame);
            }*/
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
                default:
                {
                    Console.WriteLine($"Unhandled packet 0x{PacketId:X2} from {endpoint} ({message.Length} bytes)");
                    break;
                }
            }
        }

        private void HandleFrameSet(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            try
            {
                FrameSet frameSet = FrameSet.Deserialize(message);
                foreach (Frame frame in frameSet.Frames)
                {
                    string payloadHex = Convert.ToHexString(frame.Buffer);
                    if (payloadHex.Length > 256)
                    {
                        payloadHex = payloadHex[..256] + "...";
                    }

                    Console.WriteLine($"Frame Buffer from {endpoint}: {payloadHex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid FrameSet from {endpoint}: {ex.Message}");
            }
        }

        private void HandleUnconnectedPing(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            UnconnectedPing Ping = UnconnectedPing.Deserialize(message);
            UnconnectedPong Pong = new(Ping.Time, ServerGuid, Advertisement);
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
            ushort MTU = (ushort)Math.Clamp(message.Length + 28, 576, 1492);
            uint? Cookie = null;
            if (EnableCookies /* && endpoint is IPEndPoint IpEndPoint*/)
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

            Console.WriteLine($"OpenConnectionRequestOne from {endpoint} protocol={ProtocolVersion} mtu={MTU}");
        }

        private void HandleOpenConnectionRequestTwo(SocketAddress endpoint, ReadOnlySpan<byte> message)
        {
            OpenConnectionRequestTwo Request = OpenConnectionRequestTwo.Deserialize(message);

            if (EnableCookies)
            {
                if (!Request.Cookie.HasValue || !ConnectionCookie.Validate(endpoint, CookieSecret, Request.Cookie.Value))
                {
                    Console.WriteLine($"Dropped OpenConnectionRequestTwo invalid cookie from {endpoint}");
                    return;
                }
                //return;
            }

            OpenConnectionReplyTwo Reply = new((long)ServerGuid, endpoint, Request.MTU, false);

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

            Console.WriteLine($"OpenConnectionRequestTwo from {endpoint} mtu={Request.MTU} guid={Request.ClientId}");
        }

    }
}
