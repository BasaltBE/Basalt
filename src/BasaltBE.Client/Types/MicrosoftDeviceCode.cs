namespace BasaltBE.Client;

public readonly record struct MicrosoftDeviceCode(
    string UserCode,
    Uri VerificationUri,
    string Message
);
