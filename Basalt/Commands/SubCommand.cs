namespace Basalt.Commands;

public class SubCommand : Command
{
    public SubCommand
    (
        string name,
        string description
    ) : base(name, description) { }
}
