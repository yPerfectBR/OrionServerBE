namespace Orion.Protocol.Registry;

/// <summary>
/// Bedrock runtime block state hashes (protocol 975) for Orion curated blocks.
/// </summary>
public static class BedrockBlockStates
{
    public const int Air = -604749536;
    public const int Bedrock = -173245189;
    public const int Dirt = -2108756090;
    public const int GrassBlock = -567203660;
    public const int Barrier = 951810905;
    public const int StructureVoid = 1150271535;

    public static bool IsRuntimeIdSolid(int runtimeId) =>
        runtimeId != Air && runtimeId != StructureVoid;
}
