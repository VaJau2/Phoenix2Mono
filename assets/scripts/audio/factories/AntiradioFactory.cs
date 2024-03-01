using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public partial class AntiradioFactory : IAudioEffectsFactory
{
    private const string key = "antiradio";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var notchFilter = new AudioEffectNotchFilter();
        notchFilter.Db = AudioEffectFilter.FilterDB.Filter12Db;
        notchFilter.Resonance = 0.5f;
        notchFilter.CutoffHz = 4000;

        return new Dictionary<string, AudioEffect>
        {
            {key, notchFilter}
        };
    }
    
    public List<string> GetKeys()
    {
        return [key];
    }
}