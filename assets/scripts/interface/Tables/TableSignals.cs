using Godot;

public class TableSignals : ScrollContainer
{
    [Signal]
    public delegate void TableButtonPressed(string fileName);
}
