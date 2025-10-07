namespace Ion.Windows;

public sealed class ExifTag(int id, string name, string description, string value = "")
{
    public int Id { get; private set; } = id;

    public string Description { get; private set; } = description;

    public string Name { get; private set; } = name;

    public string Value { get; set; } = value;

    public override string ToString() => string.Format("{0} ({1}) = {2}", Description, Name, Value);
}