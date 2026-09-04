if (args.Length != 5)
{
    throw new ArgumentException(
        "Expected protocol data directory, protocol output, block input, block output, and hash output paths.");
}

if (ProtocolDataGenerator.Generate(args[0], args[1], args[4]))
{
    BlockIdentifierGenerator.Generate(args[2], args[3]);
}
