using Godot;
using System;

public class UnicornShield : Spatial
{
    private const float SHIELD_COST = 30f;
    public bool shieldOn = false;
    public float shieldCooldown = 0f;
    private Player_Unicorn player;
    private MeshInstance firstShield;
    private MeshInstance thirdShield;

    private SpatialMaterial material;
    private bool playOnetime;

    private LightsCheck lights;

    private AudioStreamPlayer audi;
    

    public override void _Ready()
    {
        player = GetParent<Player_Unicorn>();
        firstShield = GetNode<MeshInstance>("first");
        thirdShield = GetNode<MeshInstance>("third");
        lights = GetNode<LightsCheck>("../lightsCheck");

        material = (SpatialMaterial)firstShield.GetSurfaceMaterial(0);

        audi = GetNode<AudioStreamPlayer>("../sound/audi_shield");
    }

    public override void _Process(float delta)
    {
        if (shieldCooldown > 0)
        {
            shieldCooldown -= delta;
        }
        
        float tempCost = SHIELD_COST * delta * player.ManaDelta;
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
            color.a = player.Mana / 150f;
            material.AlbedoColor = color;
            firstShield.SetSurfaceMaterial(0, material);
            thirdShield.SetSurfaceMaterial(0, material);
            lights.OnLight = true;
            if (!playOnetime)
            {
                audi.Play();
                playOnetime = true;
            }
       
        } else {
            if (firstShield.Visible) {
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
