using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public partial class FlaskWaterFactory : IAudioEffectsFactory
{
    private const string key = "flaskWater";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var notchFilter = new AudioEffectNotchFilter();
        notchFilter.Db = AudioEffectFilter.FilterDB.Filter12Db;
        notchFilter.Resonance = 0.3f;
        notchFilter.CutoffHz = 2000;

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