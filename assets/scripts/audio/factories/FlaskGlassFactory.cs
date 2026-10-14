using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public class FlaskGlassFactory : IAudioEffectsFactory
{
    private const string key = "flaskGlass";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var notchFilter = new AudioEffectNotchFilter();
        notchFilter.Db = AudioEffectFilter.FilterDB.Filter12db;
        notchFilter.Resonance = 0.8f;
        notchFilter.CutoffHz = 3000;

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