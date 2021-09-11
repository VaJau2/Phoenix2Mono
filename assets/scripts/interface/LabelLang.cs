using Godot;
using System;

public class LabelLang : Label
{
    [Export] private string phraseCode;
    
    public override void _Ready()
    {
        Text = InterfaceLang.GetPhrase("inGame", "labels", phraseCode);
    }
}
