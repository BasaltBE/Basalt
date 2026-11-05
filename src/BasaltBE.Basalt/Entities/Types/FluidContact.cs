using System.Numerics;

namespace BasaltBE.Basalt.Entities.Types;

public struct FluidContact {
    public float WaterDepth;
    public float LavaDepth;
    public bool HeadInWater;
    public bool HeadInLava;
    public bool Fire;
    public bool SoulFire;
    public Vector3 WaterFlow;
    public Vector3 LavaFlow;
}
