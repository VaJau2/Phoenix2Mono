using System;
using Godot;
using Godot.Collections;

public partial class NpcPoinitingGun: TriggerBase
{
    [Export] public string npcPath;
    [Export] public float delayTimer;
    private NpcWithWeapons npc;

    private double tempTimer;
    private int step;

    public override void _Ready()
    {
        SetProcess(false);
        if (IsActive)
        {
            OnActivateTrigger();
        }
    }
    
    public override void _Process(double delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        step = 2;
        OnActivateTrigger();
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
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
        
        base.OnActivateTrigger();
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
        
        if (data.TryGetValue("tempTimer", out var timerValue))
        {
            tempTimer = timerValue.AsSingle();
        }

        step = data["step"].AsInt16();
        if (step > 0)
        {
            OnActivateTrigger();
        }
    }
}