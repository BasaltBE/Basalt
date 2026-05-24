using System.Security.Cryptography;

namespace Basalt.Waterfall.Auth;

public static class KeyPairGenerator
{
    public static CachedKeyPair GenerateKeyPair()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        
        var publicKey = Convert.ToBase64String(ecdh.PublicKey.ExportSubjectPublicKeyInfo());
        var privateKey = Convert.ToBase64String(ecdh.ExportECPrivateKey());

        return new CachedKeyPair
        {
            PublicKey = publicKey,
            PrivateKey = privateKey
        };
    }
}
