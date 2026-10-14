using Godot;

public partial class TableSignals : ScrollContainer
{
    [Signal]
    public delegate void TableButtonPressed(string fileName);
}
