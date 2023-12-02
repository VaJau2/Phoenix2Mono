using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public class BunkerFactory : IAudioEffectsFactory
{
    private const string key = "bunker";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var reverb = new AudioEffectReverb();
        reverb.RoomSize = 0.8f;
        reverb.Spread = 0.25f;
        
        return new Dictionary<string, AudioEffect>
        {
            {key, reverb}
        };
    }

    public List<string> GetKeys()
    {
        List<string> keys = new();
        keys.Add(key);

        return keys;
    }
}