using Godot;
using System;

public partial class UnicornShield : Node3D
{
    private const float SHIELD_COST = 30f;
    public bool shieldOn = false;
    public float shieldCooldown = 0f;
    private Player_Unicorn player;
    private MeshInstance3D firstShield;
    private MeshInstance3D thirdShield;

    private StandardMaterial3D material;
    private bool playOnetime;

    private LightsCheck lights;

    private AudioStreamPlayer audi;
    

    public override void _Ready()
    {
        player = GetParent<Player_Unicorn>();
        firstShield = GetNode<MeshInstance3D>("first");
        thirdShield = GetNode<MeshInstance3D>("third");
        lights = GetNode<LightsCheck>("../lightsCheck");

        material = (StandardMaterial3D)firstShield.GetSurfaceOverrideMaterial(0);

        audi = GetNode<AudioStreamPlayer>("../sound/audi_shield");
    }

    public override void _Process(double delta)
    {
        if (shieldCooldown > 0)
        {
            shieldCooldown -= (float)delta;
        }
        
        float tempCost = SHIELD_COST * (float)delta * player.ManaDelta;
        if (player.MayMove 
            && Input.IsActionPressed("ui_shift") 
            && player.ManaIsEnough(tempCost) 
            && shieldCooldown <= 0)
        {  
            shieldOn = true;
            firstShield.Visible = true;
            thirdShield.Visible = true;
            player.SetMagicEmit(true);
            player.DecreaseMana(tempCost);

            var color = material.AlbedoColor;
            color.A = player.Mana / 150f;
            material.AlbedoColor = color;
            firstShield.SetSurfaceOverrideMaterial(0, material);
            thirdShield.SetSurfaceOverrideMaterial(0, material);
            lights.OnLight = true;
            if (!playOnetime)
            {
                audi.Play();
                playOnetime = true;
            }
        } 
        else 
        {
            if (firstShield.Visible) 
            {
                shieldOn = false;
                firstShield.Visible = false;
                thirdShield.Visible = false;

                audi.Stop();
                player.SetMagicEmit(false);
                playOnetime = false;
            }
        }
    }
}
