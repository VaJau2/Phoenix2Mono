using Godot;
using Godot.Collections;

//Сабменю настроек сложности
public class DifficultySubmenu : SubmenuBase
{
    public const float DEFAULT_SLIDERS_VALUE = 1f;
    public const bool DEFAULT_DEATH_SCREEN = true;
    
    Global global = Global.Get();
    
    private Button defaultButton;

    private Dictionary<string, Label> labels;
    private Dictionary<string, Label> numbers;
    private Dictionary<string, Slider> sliders;

    private Button dealthButton;
    
    public override void LoadSubmenu(SettingsMenu parent)
    {
        base.LoadSubmenu(parent);
        defaultButton = GetNode<Button>("default");
        dealthButton = GetNode<Button>("death_button");
        
        labels = new Dictionary<string, Label>
        {
            {"player_damage", GetNode<Label>("player_damage_label")},
            {"npc_damage", GetNode<Label>("npc_damage_label")},
            {"npc_aggressive", GetNode<Label>("npc_aggressive_label")},
            {"npc_accuracy", GetNode<Label>("npc_accuracy_label")},
            {"inflation", GetNode<Label>("inflation_label")},
            {"death_screen", GetNode<Label>("death_screen")},
        };
        numbers = new Dictionary<string, Label>
        {
            {"player_damage", GetNode<Label>("player_damage_number")},
            {"npc_damage", GetNode<Label>("npc_damage_number")},
            {"npc_aggressive", GetNode<Label>("npc_aggressive_number")},
            {"npc_accuracy", GetNode<Label>("npc_accuracy_number")},
            {"inflation", GetNode<Label>("inflation_number")},
        };
        sliders = new Dictionary<string, Slider>
        {
            {"player_damage", GetNode<Slider>("player_damage_slider")},
            {"npc_damage", GetNode<Slider>("npc_damage_slider")},
            {"npc_aggressive", GetNode<Slider>("npc_aggressive_slider")},
            {"npc_accuracy", GetNode<Slider>("npc_accuracy_slider")},
            {"inflation", GetNode<Slider>("inflation_slider")},
        };
    }
    
    private void UpdateSliderNumber(string sliderCode)
    {
        numbers[sliderCode].Text = sliders[sliderCode].Value.ToString("F");
    }

    private void SetSettingFloat(string code, float value)
    {
        switch (code)
        {
            case "player_damage": 
                global.Settings.playerDamage = value;
                break;
            case "npc_damage": 
                global.Settings.npcDamage = value;
                break;
            case "npc_aggressive": 
                global.Settings.npcAggressive = value;
                break;
            case "npc_accuracy": 
                global.Settings.npcAccuracy = value;
                break;
            case "inflation": 
                global.Settings.inflation = value;
                break;
        }
    }

    private float GetSettingFloat(string code)
    {
        switch (code)
        {
            case "player_damage":  return global.Settings.playerDamage;
            case "npc_damage":     return global.Settings.npcDamage;
            case "npc_aggressive": return global.Settings.npcAggressive;
            case "npc_accuracy":   return global.Settings.npcAccuracy;
            case "inflation":      return global.Settings.inflation;
            default: return 0f;
        }
    }
    
    public override void LoadInterfaceLanguage()
    {
        defaultButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "default");
        SettingsMenu.LoadOnOffText(dealthButton, global.Settings.deathScreen);
        foreach (var labelCode in labels.Keys)
        {
            labels[labelCode].Text = InterfaceLang.GetPhrase("settingsMenu", "labels", labelCode);
            if (!sliders.ContainsKey(labelCode)) continue;
            
            sliders[labelCode].Value = GetSettingFloat(labelCode);
            UpdateSliderNumber(labelCode);
        }
    }

    public void _on_default_pressed()
    {
        parentMenu.SoundClick();
        
        foreach (var sliderCode in sliders.Keys)
        {
            sliders[sliderCode].Value = DEFAULT_SLIDERS_VALUE;
            _on_slider_value_changed(DEFAULT_SLIDERS_VALUE, sliderCode);
        }
        
        global.Settings.deathScreen = DEFAULT_DEATH_SCREEN;
    }
    
    public void _on_slider_value_changed(float value, string sliderCode)
    {
        UpdateSliderNumber(sliderCode);
        SetSettingFloat(sliderCode, value);
    }
    
    public void _on_death_button_pressed()
    {
        parentMenu.SoundClick();
        var deathScreen = global.Settings.deathScreen;
        global.Settings.deathScreen = !deathScreen;
        SettingsMenu.LoadOnOffText(dealthButton, !deathScreen);
    }
}
