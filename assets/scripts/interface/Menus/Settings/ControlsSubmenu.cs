using Godot;
using Godot.Collections;

//Сабменю настроек управления
public class ControlsSubmenu : SubmenuBase
{
    Global global = Global.Get();
    
    private Button defaultButton;
    private Dictionary<string, Label> controlLabels;
    
    private Label tempEdit;
    private ColorRect tempEditBack;
    private string tempAction = "";
    
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
    }
    
    private Label GetControlEdit(string action) 
    {
        switch(action) {
            case "ui_up": return GetNode<Label>("forwardBack/edit");
            case "ui_down": return GetNode<Label>("backBack/edit");
            case "ui_left": return GetNode<Label>("leftBack/edit");
            case "ui_right": return GetNode<Label>("rightBack/edit");
            case "jump": return GetNode<Label>("jumpBack/edit");
            case "ui_shift": return GetNode<Label>("runBack/edit");
            case "use": return GetNode<Label>("useBack/edit");
            case "crouch": return GetNode<Label>("crouchBack/edit");
            case "dash": return GetNode<Label>("dashBack/edit");
            case "legsHit": return GetNode<Label>("legsBack/edit");
            case "changeView": return GetNode<Label>("cameraBack/edit");
            case "task": return GetNode<Label>("taskBack/edit");
            case "inventory": return GetNode<Label>("inventoryBack/edit");
            case "autoheal": return GetNode<Label>("autohealBack/edit");
            case "ui_quicksave": return GetNode<Label>("quicksaveBack/edit");
            case "ui_quickload": return GetNode<Label>("quickloadBack/edit");
        }
        return null;
    }
    
    private void LoadControlButtons()
    {
        foreach(var action in global.Settings.controlActions) {
            var actions = InputMap.GetActionList(action);
            if (!(actions[0] is InputEventKey eventKey)) continue;
            var key = OS.GetScancodeString(eventKey.Scancode);
            var edit = GetControlEdit(action);
            WriteKeyToEdit(key, edit);
        }
    }

    private void WriteKeyToEdit(string key, Label edit)
    {
        edit.Text = "["  + key.Capitalize() + "]";
    }

    private void CancelControlEdit()
    {
        if (tempAction == "") return;
        SetEditOn(tempEditBack, false);
        var key = Global.GetKeyName(tempAction);
        if (IsInstanceValid(tempEdit)) {
            WriteKeyToEdit(key, tempEdit);
        }
        tempAction = "";
        tempEdit = null;
    }
    
    private void SetEditOn(ColorRect editBack, bool on)
    {
        var editButton = editBack.GetNode<Label>("edit");
        if (on) {
            Color tempColor = editBack.Color;
            tempColor.a = 1;
            editBack.Color = tempColor;

            editButton.Modulate = Colors.Black;
            tempEditBack = editBack;
        } else {
            Color tempColor = editBack.Color;
            tempColor.a = 0;
            editBack.Color = tempColor;

            editButton.Modulate = Colors.White;
            tempEditBack = null;
        }
    }
    
    public void _on_default_pressed()
    {
        InputMap.LoadFromGlobals();
        foreach(var action in global.Settings.controlActions) 
        {
            var actions = InputMap.GetActionList(action);
            if (!(actions[0] is InputEventKey eventKey)) continue;
            var key = OS.GetScancodeString(eventKey.Scancode);
            var edit = GetControlEdit(action);
            WriteKeyToEdit(key, edit);
        }
    }
    
    public void _on_controls_mouse_entered(string editName, string section, string phrase)
    {
        if (tempEdit != null) return;
        parentMenu._on_mouse_entered(section, phrase);
        var editBack = GetNode<ColorRect>(editName);
        SetEditOn(editBack, true);
    }

    public void _on_controls_mouse_exited()
    {
        if (tempEdit != null || tempEditBack == null) return;
        parentMenu._on_mouse_exited();
        SetEditOn(tempEditBack, false);
    }

    public void _on_controls_gui_input(InputEvent @event, string action)
    {
        if (tempEdit != null) return;
        if (!(@event is InputEventMouseButton mouseEv)) return;
        if (!mouseEv.Pressed) return;
        CancelControlEdit();
        tempEdit = tempEditBack.GetNode<Label>("edit");
        tempEdit.Text = "[            ]";
        tempAction = action;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        if (tempEdit == null) return;
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
            WriteKeyToEdit(key, tempEdit);
            tempAction = "";
            SetEditOn(tempEditBack, false);
            tempEdit = null;
        }
    }
}
