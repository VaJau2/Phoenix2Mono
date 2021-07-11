using System;
using Godot;
using Godot.Collections;

public class NpcPoinitingGun: TriggerBase
{
    [Export] public string npcPath;
    [Export] public float delayTimer;
    private NpcWithWeapons npc;
    
    private SavableTimers timers;
    private int step;

    public override void _Ready()
    {
        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        if (npc == null)
        {
            npc = GetNode<NpcWithWeapons>(npcPath);
        }
        
        switch (step)
        {
            case 0:
            case 1:
                WaitDelayTimer();
                return;
            case 2:
                npc?.weapons.SetWeapon(true);
                break;
        }
        
        base._on_activate_trigger();
    }

    private async void WaitDelayTimer()
    {
        step = 1;
        if (!(delayTimer > 0)) return;
        while (timers.CheckTimer(Name + "_timer", delayTimer))
        {
            await ToSignal(GetTree(), "idle_frame");
        }

        step = 2;
        _on_activate_trigger();
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }
}