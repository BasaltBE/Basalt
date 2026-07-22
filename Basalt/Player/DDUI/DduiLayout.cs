namespace Basalt.Core.DDUI;

internal sealed class DduiLayout {
    int _count;

    public DduiProperty Property;

    public DduiLayout(DataDrivenScreen screen) {
        Property = DduiProperty.Object("layout");
        Property.Parent = screen.Root;
    }

    public void Add(DduiProperty property) {
        property.Name = _count.ToString();
        Property.Set(property);
        _count++;
        Property.Set(DduiProperty.Long("length", _count));
    }
}
