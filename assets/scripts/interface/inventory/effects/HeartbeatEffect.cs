using Godot;
using Godot.Collections;

public class HeartbeatEffect 
{
    const int COUNT_0 = 3;
    const int COUNT_1 = 4;
    const int COUNT_2 = 5;
    const int COUNT_3 = 6;
    private Player player;
    private int count;

    private Dictionary<int, AudioStreamSample> sounds = new Dictionary<int, AudioStreamSample>();
    private AudioStreamPlayer audi;

    public HeartbeatEffect()
    {
        sounds.Add(COUNT_0, GetAudio("0"));
        sounds.Add(COUNT_1, GetAudio("1"));
        sounds.Add(COUNT_2, GetAudio("2"));
        sounds.Add(COUNT_3, GetAudio("3"));
    }

    private AudioStreamSample GetAudio(string name)
    {
        return GD.Load<AudioStreamSample>("res://assets/audio/heartbeat/" + name + ".wav");
    }

    private void CheckPlayerEmpty()
    {
        if (player == null) {
            player = Global.Get().player;
            audi = player.GetNode<AudioStreamPlayer>("sound/audi_heartbeat");
        }
    }

    private void UpdateSound()
    {
        CheckPlayerEmpty();

        if (count < COUNT_0) {
            audi.Stop();
        } else {
            if(count > COUNT_3) {
                audi.Stream = sounds[COUNT_3];
            } else {
                audi.Stream = sounds[count];
            }
            audi.Play();
        }
        
    }

    public void CheckAddEffect(Effect effect)
    {
        if (effect.badEffect) {
            count++;
            UpdateSound();
        }
    }

    public void CheckRemoveEffect(Effect effect)
    {
        if (effect.badEffect) {
            count--;
            UpdateSound();
        }
    }

    public void ClearEffects()
    {
        count = 0;
        UpdateSound();
    }
}