using Godot;

public partial class LabelLang : Label
{
    [Export] private string phraseCode;
    
    public override void _Ready()
    {
        Text = InterfaceLang.GetPhrase("inGame", "labels", phraseCode);
    }
}
