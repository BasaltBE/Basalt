namespace BasaltBE.NetherNet.Types;

public interface IServerIdentity : IDisposable {
    string AddAssertion(string answerSdp);
}

