using System;
using Godot;
using Godot.Collections;

public class NpcPoinitingGun: TriggerBase
{
    [Export] public string npcPath;
    [Export] public float delayTimer;
    private NpcWithWeapons npc;

    private float tempTimer;
    private int step;

    public override void _Ready()
    {
        SetProcess(false);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }
    
    public override void _Process(float delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        step = 2;
        _on_activate_trigger();
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

    private  void WaitDelayTimer()
    {
        step = 1;
        tempTimer = delayTimer;
        SetProcess(true);
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        saveData["tempTimer"] = tempTimer;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.Contains("tempTimer"))
        {
            tempTimer = Convert.ToSingle(data["tempTimer"]);
        }
        
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }
}