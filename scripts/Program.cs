if (args.Length != 4)
{
    throw new ArgumentException(
        "Expected protocol data directory, protocol output, block input, and block output paths.");
}

ProtocolDataGenerator.Generate(args[0], args[1]);
BlockIdentifierGenerator.Generate(args[2], args[3]);
