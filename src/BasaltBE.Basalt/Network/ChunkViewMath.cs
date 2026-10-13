namespace BasaltBE.Basalt.Network;

internal static class ChunkViewMath {
    internal const int MaxBedrockViewDistance = 96;

    internal static int SquareToCircle(int renderDistance) => renderDistance <= 0
        ? 1
        : (int)Math.Ceiling((renderDistance + 1) * Math.Sqrt(2.0));

    internal static int MaxChebyshevForClientCircle(int clientMaxCircle) {
        int limit = Math.Clamp(clientMaxCircle, 1, MaxBedrockViewDistance);
        int radius = 0;

        while (radius < limit && SquareToCircle(radius + 1) <= limit)
            radius++;

        return radius;
    }

    internal static uint PublisherRadiusBlocks(int renderDistance) =>
        (uint)(Math.Clamp(renderDistance, 1, MaxBedrockViewDistance) << 4);
}
