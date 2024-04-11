using Godot;
using Godot.Collections;

public partial class HintMenu: Control, IMenu
{
    [Export] private Array<HintTrigger> canceledHints;
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

        MenuBase.LoadColorForChildren(this);
    }
    
    public override void _Process(double _delta)
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
        await ToSignal(GetTree(), "process_frame");
        Global.Get().SetPause(this, false, false);
        MenuManager.CloseMenu(this);
    }
    
    private string GetKeysMessage(string tempHintCode, string hintSection = null)
    {
        return InterfaceLang.GetPhrase("inGame", hintSection ?? "hintsModal", tempHintCode);
    }
    
    private void loadInterfaceLanguage()
    {
        hintLabel.Text     = hintMessage;
        hintHeader.Text    = GetKeysMessage("header");
        hintOkLabel.Text   = GetKeysMessage("ok");
        hintSkipLabel.Text = GetKeysMessage("skip");
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