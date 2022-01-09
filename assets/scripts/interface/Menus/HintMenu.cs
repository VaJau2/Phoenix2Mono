using Godot;
using Godot.Collections;

public class HintMenu: Control, IMenu
{
    [Export] private Array<NodePath> canceledHintsPaths;
    private Array<HintTrigger> canceledHints;
    public bool mustBeClosed => true;

    public string hintMessage;
    
    private Label hintLabel;
    private Label hintHeader;
    private Label hintOkLabel;
    private Label hintSkipLabel;
    
    public override void _Ready()
    {
        SetProcess(false);
        hintLabel     = GetNode<Label>("back/text");
        hintHeader    = GetNode<Label>("back/header");
        hintOkLabel   = GetNode<Label>("back/ok");
        hintSkipLabel = GetNode<Label>("back/skip");
        
        canceledHints = new Array<HintTrigger>();
        foreach (var hintPath in canceledHintsPaths)
        {
            var hint = GetNodeOrNull<HintTrigger>(hintPath);
            if (IsInstanceValid(hint))
            {
                canceledHints.Add(hint);
            }
        }
        
        MenuBase.LoadColorForChildren(this);
        
    }
    
    public override void _Process(float _delta)
    {
        if (Input.IsActionJustPressed("use"))
        {
            StartClosingMenu();
        }
        
        if (Input.IsActionJustPressed("ui_shift"))
        {
            StartClosingMenu();
            foreach (var hint in canceledHints)
            {
                if (IsInstanceValid(hint))
                {
                    hint.SetActive(false);
                }
            }
        }
    }

    private async void StartClosingMenu()
    {
        SetProcess(false);
        await ToSignal(GetTree(), "idle_frame");
        Global.Get().SetPause(this, false, false);
        MenuManager.CloseMenu(this);
    }
    
    private string getKeysMessage(string tempHintCode, string hintSection = null)
    {
        var message = InterfaceLang.GetPhrase("inGame", hintSection ?? "hintsModal", tempHintCode);
        if (message == null) return null;
        if (message.Contains("#"))
        {
            message = HintTrigger.ReplaceKeys(message);
        }
        return message;
    }
    
    private void loadInterfaceLanguage()
    {
        hintLabel.Text     = hintMessage;
        hintHeader.Text    = getKeysMessage("header");
        hintOkLabel.Text   = getKeysMessage("ok");
        hintSkipLabel.Text = getKeysMessage("skip");
    }

    public void OpenMenu()
    {
        loadInterfaceLanguage();
        Visible = true;
        Global.Get().SetPause(this, true, false);
        SetProcess(true);
    }

    public void CloseMenu()
    {
        Visible = false;
    }
}