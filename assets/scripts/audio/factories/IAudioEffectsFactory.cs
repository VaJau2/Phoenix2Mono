using Godot;
using System.Collections.Generic;

namespace Phoenix2Mono.assets.scripts.audio.factories;

public interface IAudioEffectsFactory
{
    public Dictionary<string, AudioEffect>  CreateEffects();

    public List<string> GetKeys();
}