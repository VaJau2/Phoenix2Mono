using System.Collections.Generic;
using Godot;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public partial class BunkerFactory : IAudioEffectsFactory
{
    private const string key = "bunker";
    
    public Dictionary<string, AudioEffect> CreateEffects()
    {
        var reverb = new AudioEffectReverb();
        reverb.RoomSize = 0.9f;
        reverb.Hipass = 0.9f;

        return new Dictionary<string, AudioEffect>
        {
            {key, reverb}
        };
    }

    public List<string> GetKeys()
    {
        return [key];
    }
}