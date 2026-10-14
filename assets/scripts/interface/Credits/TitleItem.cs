using Godot;

public class TitleItem : Label
{
    [Export] private bool isMoveTitle;
    [Export] private string key;

    public override void _Ready()
    {
        if (!string.IsNullOrEmpty(key))
        {
            Text = InterfaceLang.GetPhrase("credits", "credits", key);
        }
    }

    public string GetAnimationName()
    {
        return isMoveTitle ? "Move Title" : "Show Title";
    }
}
