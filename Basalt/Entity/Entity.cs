namespace Basalt.Entity;

public class Entity
{
    public readonly string Identifier;

    public Entity(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(identifier));
        }

        Identifier = identifier;
    }
}
