namespace Basalt.Core.Commands.Vanilla;

/// <summary>
/// Registers all default (vanilla) commands with the registry.
/// </summary>
public static class DefaultCommands {
    public static void Register(CommandRegistry registry) {
        registry.Register(StatusCommand.Definition);
        registry.Register(ClearCommand.Definition);
        registry.Register(EnchantCommand.Definition);
        registry.Register(XpCommand.Definition);
        registry.Register(GamemodeCommand.Definition);
        registry.Register(GiveCommand.Definition);
        registry.Register(OpCommand.Definition);
        registry.Register(DeopCommand.Definition);
        registry.Register(ListCommand.Definition);
        registry.Register(SummonCommand.Definition);
        registry.Register(TpCommand.Definition);
        registry.Register(PluginsCommand.Definition);
        registry.Register(WorldsCommand.Definition);
        registry.Register(SaveCommand.Definition);
        registry.Register(SetWorldSpawnCommand.Definition);
        registry.Register(FillCommand.Definition);
        registry.Register(SetBlockCommand.Definition);
        registry.Register(KillCommand.Definition);
        registry.Register(BanCommand.Definition);
        registry.Register(UnBanCommand.Definition);
        registry.Register(KickCommand.Definition);
    }
}
