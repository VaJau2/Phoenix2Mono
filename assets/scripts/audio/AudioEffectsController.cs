using System;
using Godot;
using System.Collections.Generic;
using Godot.Collections;

using Array = Godot.Collections.Array;
using Phoenix2Mono.assets.scripts.audio.factories;

public partial class AudioEffectsController : Node, ISavable
{
    private List<string> effectsKeys = new();
    private Player player;

    private Array<string> savedCodes = new();
    
    public override async void _Ready()
    {
        player = GetParent<Player>();
        player.WearItem += OnClothWear;
        player.UnwearItem += RemoveEffects;
        player.ChangeView += OnChangeView;
        
        await ToSignal(GetTree(), "idle_frame");
        
        AddEffects(player.Inventory.cloth);
    }

    public override void _ExitTree()
    {
        for (int i = AudioServer.GetBusEffectCount((int)AudioBus.Master) - 1; i >= 0; i--)
        {
            AudioServer.RemoveBusEffect((int)AudioBus.Master, i);
        }

        effectsKeys.Clear();
        base._ExitTree();
    }

    public void AddEffects(string code)
    {
        if (Enum.TryParse(code, out FactoryType factoryType))
        {
            var factory = FactoriesManager.GetFactory(factoryType);
            var dictionary = factory.CreateEffects();

            foreach (var key in dictionary.Keys)
            {
                if (effectsKeys.Contains(key)) return;
                AudioServer.AddBusEffect((int)AudioBus.Master, dictionary[key]);
                effectsKeys.Add(key);
            }
            
            savedCodes.Add(code);
        }
    }

    public void RemoveEffects(string code)
    {
        if (Enum.TryParse(code, out FactoryType factoryType))
        {
            var factory = FactoriesManager.GetFactory(factoryType);
            var keys = factory.GetKeys();

            foreach (var key in keys)
            {
                var index = effectsKeys.IndexOf(key);

                AudioServer.RemoveBusEffect((int)AudioBus.Master, index);
                effectsKeys.Remove(key);
            }

            savedCodes.Remove(code);
        }
    }

    private void OnClothWear(string clothCode)
    {
        AddEffects(clothCode);
        if (player.ThirdView) OnChangeView(true);
    }

    private void OnChangeView(bool toThird)
    {
        if (Enum.TryParse(player.Inventory.cloth, out FactoryType factoryType))
        {
            var factory = FactoriesManager.GetFactory(factoryType);
            var keys = factory.GetKeys();

            foreach (var key in keys)
            {
                var index = effectsKeys.IndexOf(key);
                AudioServer.SetBusEffectEnabled((int)AudioBus.Master, index, !toThird);
            }
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "codes", savedCodes }
        };
    }

    public void LoadData(Dictionary data)
    {
        var codes = (Array) data["codes"];
        
        foreach (var codeObj in codes)
        {
            var code = codeObj.ToString();
            savedCodes.Add(code);
            AddEffects(code);
        }
        
        if (player.ThirdView) OnChangeView(true);
    }
}
