using Godot;

public class StealthBuckEffect : Effect
{
    private Player player;
    public StealthBuckEffect()
    {
        maxTime = 60;
        badEffect = false;
    }

    private void ChangeMeshVisibility(MeshInstance meshInstance, bool visible)
    {
        Mesh mesh = meshInstance.Mesh;
        int materialsCount = mesh.GetSurfaceCount();
        for (int i = 0; i < materialsCount; i++)
        {
            SpatialMaterial tempMaterial = mesh.SurfaceGetMaterial(i) as SpatialMaterial;
            if (tempMaterial != null)
            {
                tempMaterial.FlagsTransparent = !visible;
                Color old = tempMaterial.AlbedoColor;
                tempMaterial.AlbedoColor = new Color(old.r, old.g, old.b, visible ? 1 : 0.02f);
            }
        }
    }

    private void ChangePlayerVisibility(bool visible)
    {
        var bodyMesh = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/Body");
        var bodyThirdMesh = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/Body_third");

        ChangeMeshVisibility(bodyMesh, visible);
        ChangeMeshVisibility(bodyThirdMesh, visible);

        if (Global.Get().playerRace != Race.Pegasus) return;
        
        var wingsShadow = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/Wings");
        wingsShadow.Visible = visible;
    }
    
    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "stealthbuck";
        
        player.IsStealthBuck = true;
        ChangePlayerVisibility(false);
        
        base.SetOn(handler);
    }

    public override void SetOff(bool startPostEffect = true)
    {
        player.IsStealthBuck = false;
        ChangePlayerVisibility(true);
        base.SetOff(false);
    }
}