using Godot;
using Godot.Collections;

public class RandomItems : Node
{
    [Export]
    public int maxItemsCount = 5;
    //в списке лежат и патроны тоже
    [Export]
    public Array<string> itemCodes = new Array<string>();
    //здесь определяется их количество
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();
}
