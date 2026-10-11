using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Network;

public sealed class BedrockEncryption : IDisposable {
    private const int MaximumBlockBatch = 1024;
    private readonly Lock sync = new();
    private readonly byte[] key;
    private readonly Aes aes = Aes.Create();
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly byte[] encryptKeystream = new byte[16];
    private readonly byte[] decryptKeystream = new byte[16];
    private ulong encryptCounter;
    private ulong decryptCounter;
    private uint encryptBlockCounter = 2;
    private uint decryptBlockCounter = 2;
    private int encryptKeystreamOffset = 16;
    private int decryptKeystreamOffset = 16;
    private int disposed;

    public bool Enabled;

    internal BedrockEncryption(byte[] key) {
        this.key = key;
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
    }

    public static BedrockEncryption Create(ECParameters clientPublicKey, out string handshakeToken) {
        using ProfilerZone zone = Profiler.BeginZone("Encryption.Create");
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using ECDiffieHellman serverAgreement = ECDiffieHellman.Create(signingKey.ExportParameters(true));
        using ECDiffieHellman clientAgreement = ECDiffieHellman.Create(clientPublicKey);

        byte[] sharedSecret = serverAgreement.DeriveRawSecretAgreement(clientAgreement.PublicKey);
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] derivationInput = GC.AllocateUninitializedArray<byte>(salt.Length + sharedSecret.Length);
        salt.CopyTo(derivationInput, 0);
        sharedSecret.CopyTo(derivationInput, salt.Length);
        byte[] key = SHA256.HashData(derivationInput);
        CryptographicOperations.ZeroMemory(sharedSecret);
        CryptographicOperations.ZeroMemory(derivationInput);

        try {
            string publicKey = Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo());
            string header = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "ES384", x5u = publicKey }));
            string payload = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { salt = Convert.ToBase64String(salt) }));
            string signingInput = $"{header}.{payload}";
            byte[] signature = signingKey.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA384,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            handshakeToken = $"{signingInput}.{Base64Url.Encode(signature)}";
            CryptographicOperations.ZeroMemory(signature);
            return new BedrockEncryption(key);
        } catch {
            CryptographicOperations.ZeroMemory(key);
            throw;
        }
    }

    public byte[] Encrypt(ReadOnlySpan<byte> payload) {
        byte[] ciphertext = GC.AllocateUninitializedArray<byte>(payload.Length + 8);
        Encrypt(payload, ciphertext);
        return ciphertext;
    }

    public int Encrypt(ReadOnlySpan<byte> payload, Span<byte> ciphertext) {
        using ProfilerZone zone = Profiler.BeginZone("Encryption.Encrypt");
        int ciphertextLength = checked(payload.Length + 8);
        if (ciphertext.Length < ciphertextLength)
            throw new ArgumentException("Ciphertext buffer is too small.", nameof(ciphertext));
        if (payload.Overlaps(ciphertext[..ciphertextLength]))
            throw new ArgumentException("Payload and ciphertext buffers must not overlap.", nameof(ciphertext));

        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            if (encryptCounter == ulong.MaxValue)
                throw new CryptographicException("Bedrock encryption packet counter is exhausted.");

            Span<byte> checksum = stackalloc byte[8];
            WriteChecksum(encryptCounter++, payload, checksum);
            Transform(payload, ciphertext[..payload.Length], ref encryptBlockCounter, encryptKeystream, ref encryptKeystreamOffset);
            Transform(checksum, ciphertext[payload.Length..], ref encryptBlockCounter, encryptKeystream, ref encryptKeystreamOffset);
            return ciphertextLength;
        }
    }

    public byte[] Decrypt(ReadOnlySpan<byte> payload) {
        if (payload.Length < 9) throw new InvalidDataException("Encrypted Bedrock packet is too short.");
        byte[] plaintext = GC.AllocateUninitializedArray<byte>(payload.Length - 8);
        Decrypt(payload, plaintext);
        return plaintext;
    }

    public int Decrypt(ReadOnlySpan<byte> payload, Span<byte> plaintext) {
        using ProfilerZone zone = Profiler.BeginZone("Encryption.Decrypt");
        if (payload.Length < 9) throw new InvalidDataException("Encrypted Bedrock packet is too short.");
        int plaintextLength = payload.Length - 8;
        if (plaintext.Length < plaintextLength)
            throw new ArgumentException("Plaintext buffer is too small.", nameof(plaintext));
        if (payload.Overlaps(plaintext[..plaintextLength]))
            throw new ArgumentException("Ciphertext and plaintext buffers must not overlap.", nameof(plaintext));

        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            if (decryptCounter == ulong.MaxValue)
                throw new CryptographicException("Bedrock decryption packet counter is exhausted.");

            Span<byte> receivedChecksum = stackalloc byte[8];
            Span<byte> expectedChecksum = stackalloc byte[8];
            Transform(payload[..plaintextLength], plaintext[..plaintextLength], ref decryptBlockCounter, decryptKeystream, ref decryptKeystreamOffset);
            Transform(payload[plaintextLength..], receivedChecksum, ref decryptBlockCounter, decryptKeystream, ref decryptKeystreamOffset);
            WriteChecksum(decryptCounter++, plaintext[..plaintextLength], expectedChecksum);
            if (!CryptographicOperations.FixedTimeEquals(expectedChecksum, receivedChecksum))
                throw new CryptographicException("Encrypted Bedrock packet checksum is invalid.");
            return plaintextLength;
        }
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        aes.Dispose();
        hash.Dispose();
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(encryptKeystream);
        CryptographicOperations.ZeroMemory(decryptKeystream);
    }

    private void Transform(
        ReadOnlySpan<byte> input,
        Span<byte> output,
        ref uint blockCounter,
        Span<byte> keystream,
        ref int keystreamOffset
    ) {
        using ProfilerZone zone = Profiler.BeginZone("Encryption.Transform");
        int offset = 0;
        if (keystreamOffset < keystream.Length) {
            int count = Math.Min(input.Length, keystream.Length - keystreamOffset);
            for (int index = 0; index < count; index++)
                output[index] = (byte)(input[index] ^ keystream[keystreamOffset + index]);
            offset = count;
            keystreamOffset += count;
        }

        int fullBlocks = (input.Length - offset) / keystream.Length;
        if (fullBlocks != 0) {
            byte[]? rentedScratch = null;
            Span<byte> scratch = fullBlocks <= 64
                ? stackalloc byte[fullBlocks * keystream.Length * 2]
                : (rentedScratch = ArrayPool<byte>.Shared.Rent(MaximumBlockBatch * keystream.Length * 2));
            int scratchLength = 0;
            try {
                int processedBlocks = 0;
                while (processedBlocks < fullBlocks) {
                    int blockCount = Math.Min(fullBlocks - processedBlocks, MaximumBlockBatch);
                    if ((ulong)blockCount > uint.MaxValue - blockCounter)
                        throw new CryptographicException("Bedrock encryption block counter is exhausted.");

                    int byteCount = blockCount * keystream.Length;
                    Span<byte> counters = scratch[..byteCount];
                    Span<byte> streams = scratch.Slice(byteCount, byteCount);
                    scratchLength = Math.Max(scratchLength, byteCount * 2);
                    for (int block = 0; block < blockCount; block++) {
                        Span<byte> counterBlock = counters.Slice(block * keystream.Length, keystream.Length);
                        key.AsSpan(0, 12).CopyTo(counterBlock);
                        BinaryPrimitives.WriteUInt32BigEndian(counterBlock[12..], blockCounter++);
                    }

                    aes.EncryptEcb(counters, streams, PaddingMode.None);
                    int chunkLength = byteCount;
                    for (int index = 0; index < chunkLength; index++)
                        output[offset + index] = (byte)(input[offset + index] ^ streams[index]);
                    offset += chunkLength;
                    processedBlocks += blockCount;
                }
            }
            finally {
                CryptographicOperations.ZeroMemory(scratch[..scratchLength]);
                if (rentedScratch is not null)
                    ArrayPool<byte>.Shared.Return(rentedScratch);
            }
        }

        if (offset < input.Length) {
            if (keystreamOffset == keystream.Length) {
                if (blockCounter == uint.MaxValue)
                    throw new CryptographicException("Bedrock encryption block counter is exhausted.");
                Span<byte> counterBlock = stackalloc byte[16];
                key.AsSpan(0, 12).CopyTo(counterBlock);
                BinaryPrimitives.WriteUInt32BigEndian(counterBlock[12..], blockCounter++);
                aes.EncryptEcb(counterBlock, keystream, PaddingMode.None);
                keystreamOffset = 0;
            }

            int count = input.Length - offset;
            for (int index = 0; index < count; index++)
                output[offset + index] = (byte)(input[offset + index] ^ keystream[keystreamOffset + index]);
            keystreamOffset += count;
        }
    }

    private void WriteChecksum(ulong counter, ReadOnlySpan<byte> payload, Span<byte> checksum) {
        using ProfilerZone zone = Profiler.BeginZone("Encryption.Checksum");
        Span<byte> counterBytes = stackalloc byte[sizeof(ulong)];
        Span<byte> digest = stackalloc byte[32];
        BinaryPrimitives.WriteUInt64LittleEndian(counterBytes, counter);
        hash.AppendData(counterBytes);
        hash.AppendData(payload);
        hash.AppendData(key);
        if (hash.GetHashAndReset(digest) != digest.Length)
            throw new CryptographicException("Bedrock encryption checksum could not be created.");
        digest[..checksum.Length].CopyTo(checksum);
    }
}
