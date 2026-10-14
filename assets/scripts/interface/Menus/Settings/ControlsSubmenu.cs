using Godot;
using Godot.Collections;

//Сабменю настроек управления
public class ControlsSubmenu : SubmenuBase
{
    Global global = Global.Get();
    
    private Button defaultButton;
    private Dictionary<string, Label> controlLabels;
    
    private TextureRect changeIcon;
    private Texture backupIcon;
    private string tempAction = "";

    private Array<TextureRect> selectedIconList = new Array<TextureRect>();
    private TextureRect selectedIcon;
    private Vector2 speedSize = new Vector2(0.1f, 0.1f);

    [Signal]
    public delegate void ChangeControlEvent();
    
    public override void _Process(float delta)
    {
        SelectIcon();
    }

    private void SelectIcon()
    {
        if (selectedIconList.Count > 0)
        {
            for (int i = 0; i < selectedIconList.Count; i++)
            {
                if (selectedIconList[i] == selectedIcon)
                {
                    if (selectedIconList[i].RectScale.x < 1) selectedIconList[i].RectScale += speedSize;
                }
                else
                {
                    if (selectedIconList[i].RectScale.x > 0.6f) selectedIconList[i].RectScale -= speedSize;
                    else selectedIconList.Remove(selectedIconList[i]);
                }
            }
        }
        else SetProcess(false);
    }

    public override void LoadSubmenu(SettingsMenu parent)
    {
        base.LoadSubmenu(parent);
        defaultButton = GetNode<Button>("default");
        controlLabels = new Dictionary<string, Label>()
        {
            {"forward", GetNode<Label>("keyForwardLabel")},
            {"back", GetNode<Label>("keyBackLabel")},
            {"left", GetNode<Label>("keyLeftLabel")},
            {"right", GetNode<Label>("keyRightLabel")},
            {"jump", GetNode<Label>("keyJumpLabel")},
            {"run", GetNode<Label>("keyRunLabel")},
            {"use", GetNode<Label>("keyUseLabel")},
            {"sit", GetNode<Label>("keyCrouchLabel")},
            {"dash", GetNode<Label>("keyDashLabel")},
            {"choseHit", GetNode<Label>("keyLegsLabel")},
            {"changeView", GetNode<Label>("keyCameraLabel")},
            {"seeTasks", GetNode<Label>("keyTaskLabel")},
            {"inventory", GetNode<Label>("keyInventoryLabel")},
            {"autoheal", GetNode<Label>("keyAutohealLabel")},
            {"quicksave", GetNode<Label>("keyQuicksaveLabel")},
            {"quickload", GetNode<Label>("keyQuickloadLabel")},
        };
    }

    public override void LoadInterfaceLanguage()
    {
        defaultButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "default");
        foreach (string key in controlLabels.Keys)
        {
            controlLabels[key].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", key);
        }
        
        LoadControlButtons();
        LoadRaceLabel();
    }
    
    public void LoadRaceLabel()
    {
        switch (global.playerRace)
        {
            case Race.Earthpony:
                controlLabels["jump"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "jump");
                controlLabels["run"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "run");
                controlLabels["dash"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "dash");
                break;

            case Race.Pegasus:
                controlLabels["jump"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "fly");
                controlLabels["run"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "descent");
                controlLabels["dash"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "useless");
                break;

            case Race.Unicorn:
                controlLabels["jump"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "jump");
                controlLabels["run"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "shield");
                controlLabels["dash"].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", "teleport");
                break;
        }
    }

    private TextureRect GetIcon(string action)
    {
        return action switch
        {
            "ui_up" => GetNode<TextureRect>("keyForwardIcon"),
            "ui_down" => GetNode<TextureRect>("keyBackIcon"),
            "ui_left" => GetNode<TextureRect>("keyLeftIcon"),
            "ui_right" => GetNode<TextureRect>("keyRightIcon"),
            "jump" => GetNode<TextureRect>("keyJumpIcon"),
            "ui_shift" => GetNode<TextureRect>("keyRunIcon"),
            "use" => GetNode<TextureRect>("keyUseIcon"),
            "crouch" => GetNode<TextureRect>("keyCrouchIcon"),
            "dash" => GetNode<TextureRect>("keyDashIcon"),
            "legsHit" => GetNode<TextureRect>("keyLegsIcon"),
            "changeView" => GetNode<TextureRect>("keyCameraIcon"),
            "task" => GetNode<TextureRect>("keyTaskIcon"),
            "inventory" => GetNode<TextureRect>("keyInventoryIcon"),
            "autoheal" => GetNode<TextureRect>("keyAutohealIcon"),
            "ui_quicksave" => GetNode<TextureRect>("keyQuicksaveIcon"),
            "ui_quickload" => GetNode<TextureRect>("keyQuickloadIcon"),
            _ => null
        };
    }
    
    private void LoadControlButtons()
    {
        foreach(var action in global.Settings.controlActions) {
            var actions = InputMap.GetActionList(action);
            if (!(actions[0] is InputEventKey eventKey)) continue;
            var key = OS.GetScancodeString(eventKey.Scancode);
            var icon = GetIcon(action);
            WriteKeyToEdit(key, icon);
        }
    }

    private void WriteKeyToEdit(string key, TextureRect icon)
    {
        icon.Texture = GD.Load<Texture>("res://assets/textures/interface/icons/buttons/" + key + ".png");
        EmitSignal(nameof(ChangeControlEvent));
    }

    private void CancelControlEdit()
    {
        changeIcon.Texture = backupIcon;
    }
    
    public void _on_default_pressed()
    {
        InputMap.LoadFromGlobals();
        foreach(var action in global.Settings.controlActions) 
        {
            var actions = InputMap.GetActionList(action);
            if (!(actions[0] is InputEventKey eventKey)) continue;
            var key = OS.GetScancodeString(eventKey.Scancode);
            var icon = GetIcon(action);
            WriteKeyToEdit(key, icon);
        }
    }
    
    public void _on_controls_mouse_entered(string editName, string phrase)
    {
        parentMenu._on_mouse_entered("controls", phrase);

        selectedIcon = GetNode<TextureRect>(editName);
        selectedIconList.Add(selectedIcon);
        SetProcess(true);
    }

    public void _on_controls_mouse_exited()
    {
        parentMenu._on_mouse_exited();

        selectedIcon = null;
        SetProcess(true);
    }

    public void _on_controls_gui_input(InputEvent @event, string action)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            tempAction = action;

            if (changeIcon != null && backupIcon != null)
            {
                changeIcon.Texture = backupIcon;
            }

            changeIcon = GetIcon(action);
            backupIcon = changeIcon.Texture;
            changeIcon.Texture = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        if (changeIcon == null) return;
        if (!(@event is InputEventKey eventKey)) return;
        if (!eventKey.Pressed) return;
        
        if (eventKey.Scancode == (uint)KeyList.Escape) 
        {
            CancelControlEdit();
        } 
        else 
        {
            InputMap.ActionEraseEvents(tempAction);
            InputMap.ActionAddEvent(tempAction, eventKey);
            var key = OS.GetScancodeString(eventKey.Scancode);
            WriteKeyToEdit(key, changeIcon);
        }

        tempAction = null;
        backupIcon = null;
        changeIcon = null;
    }
}
