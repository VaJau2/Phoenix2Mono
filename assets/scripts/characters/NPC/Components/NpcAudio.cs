using Godot;
using Godot.Collections;

public class NpcAudio : AudioStreamPlayer3D
{
    public void PlayRandomSound(Array<AudioStreamSample> array)
    {
        if (array is not { Count: > 0 }) return;
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        var randomNum = rand.RandiRange(0, array.Count - 1);
        Stream = array[randomNum];
        Play();
    }

    public void PlayStream(AudioStreamSample stream)
    {
        if (stream == null) return;
        
        Stream = stream;
        Play();
    }
}
