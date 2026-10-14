using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public class FlaskWaterFactory : IAudioEffectsFactory
{
    private const string key = "flaskWater";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var notchFilter = new AudioEffectNotchFilter();
        notchFilter.Db = AudioEffectFilter.FilterDB.Filter12db;
        notchFilter.Resonance = 0.3f;
        notchFilter.CutoffHz = 2000;

        return new Dictionary<string, AudioEffect>
        {
            {key, notchFilter}
        };
    }
    
    public List<string> GetKeys()
    {
        List<string> keys = new();
        keys.Add(key);
    
        return keys;
    }
}