using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public class PowerArmorFactory : IAudioEffectsFactory
{
    private const string keyNotchFilter = "powerArmor_NotchFilter";
    private const string keyCompressor = "powerArmor_Compressor";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var notchFilter = new AudioEffectNotchFilter();
        notchFilter.Db = AudioEffectFilter.FilterDB.Filter12db;
        notchFilter.Resonance = 0.1f;
        notchFilter.CutoffHz = 100;

        var compressor = new AudioEffectCompressor();

        return new Dictionary<string, AudioEffect>
        {
            {keyNotchFilter, notchFilter},
            {keyCompressor, compressor}
        };
    }
    
    public List<string> GetKeys()
    {
        List<string> keys = new();
        keys.Add(keyNotchFilter);
        keys.Add(keyCompressor);

        return keys;
    }
}