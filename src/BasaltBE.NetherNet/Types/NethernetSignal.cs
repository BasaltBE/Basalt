using System.Globalization;
using System.Text;

namespace BasaltBE.NetherNet.Types;

public readonly record struct NethernetSignal(string Type, ulong ConnectionId, string Data, string NetworkId = "") {
    public static NethernetSignal Offer(ulong id, string data) => new("CONNECTREQUEST", id, data);
    public static NethernetSignal Answer(ulong id, string data) => new("CONNECTRESPONSE", id, data);
    public static NethernetSignal Candidate(ulong id, string data) => new("CANDIDATEADD", id, data);
    public static NethernetSignal Error(ulong id, int code) => new("CONNECTERROR", id, code.ToString(CultureInfo.InvariantCulture));

    public byte[] Encode() {
        if (Type is not ("CONNECTREQUEST" or "CONNECTRESPONSE" or "CANDIDATEADD" or "CONNECTERROR"))
            throw new FormatException("Unsupported NetherNet signal type.");
        return Encoding.UTF8.GetBytes($"{Type} {ConnectionId.ToString(CultureInfo.InvariantCulture)} {Data}");
    }

    public static bool TryDecode(ReadOnlySpan<byte> source, string networkId, out NethernetSignal signal) {
        signal = default;
        int first = source.IndexOf((byte)' ');
        if (first <= 0) return false;
        int secondOffset = source[(first + 1)..].IndexOf((byte)' ');
        if (secondOffset <= 0) return false;
        int second = first + secondOffset + 1;
        if (!ulong.TryParse(Encoding.ASCII.GetString(source[(first + 1)..second]), NumberStyles.None, CultureInfo.InvariantCulture, out ulong id)) return false;
        string type = Encoding.ASCII.GetString(source[..first]);
        if (type is not ("CONNECTREQUEST" or "CONNECTRESPONSE" or "CANDIDATEADD" or "CONNECTERROR")) return false;
        signal = new NethernetSignal(type, id, Encoding.UTF8.GetString(source[(second + 1)..]), networkId);
        return true;
    }
}

