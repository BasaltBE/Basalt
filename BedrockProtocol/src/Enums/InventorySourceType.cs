namespace Basalt.BedrockProtocol.Enums;

public enum InventorySourceType : uint {
    ContainerInventory,
    GlobalInventory,
    WorldInteraction,
    CreativeInventory,
    NonImplementedFeature = 99999
}
