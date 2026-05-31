namespace Basalt.Server.Commands;

public class Command
{
    /// <summary>
    /// The name of the command.
    /// For bedrock edition it must be lowercase!
    /// </summary>
    public string Name;

    /// <summary>
    /// A description of the command.
    /// </summary>
    public string Description;

    /// <summary>
    /// A list of aliases for the command.
    /// </summary>
    public List<string> Aliases = new();

    /// <summary>
    /// A list of string permissions where at least one is required to execute the command.
    /// </summary>
    public List<string> Permissions = new();

    public List<SubCommand> SubCommands = new();

    public CommandOverload Overload = new();

    public Command
    (
        string name,
        string description,
        string[] aliases,
        string[] permissions
    )
    {
        Name = name;
        Description = description;
        Aliases = new List<string>(aliases);
        Permissions = new List<string>(permissions);
    }

    public Command
    (
        string name,
        string description
    )
    {
        Name = name;
        Description = description;
        Aliases = new List<string>();
        Permissions = new List<string>();
    }

    public void AddAlias(string alias)
    {
        Aliases.Add(alias);
    }

    public void AddPermission(string permission)
    {
        Permissions.Add(permission);
    }

    public void AddPermissions(IEnumerable<string> permissions)
    {
        Permissions.AddRange(permissions);
    }

    public void AddAliases(IEnumerable<string> aliases)
    {
        Aliases.AddRange(aliases);
    }

    public void AddSubCommand(SubCommand subCommand)
    {
        SubCommands.Add(subCommand);
    }

    public CommandOverload CreateOverload()
    {
        Overload = new CommandOverload();
        return Overload;
    }

    public virtual CommandResult Execute(CommandExecutionState state)
    {
        return CommandResult.Empty(false);
    }
}







