using Godot;

public class StealthBuckEffect : Effect
{
    private Player player;
    public StealthBuckEffect()
    {
        maxTime = 60;
        badEffect = false;
    }

    // проходит по первым дочерним элементам и имет там меш
    private static MeshInstance FindMeshesInParent(Spatial parent)
    {
        while (true)
        {
            if (parent == null || parent.GetChildCount() <= 0)
            {
                return null;
            }
            
            parent = parent.GetChild(0) as Spatial;

            if (parent is MeshInstance instance)
            {
                return instance;
            }
        }
    }

    private static bool CanChangeTransparency(Resource material)
    {
        return !material.ResourcePath.EndsWith("wings.material");
    }

    private static void ChangeMeshVisibility(MeshInstance meshInstance, bool visible)
    {
        Mesh mesh = meshInstance.Mesh;
        int materialsCount = mesh.GetSurfaceCount();
        for (int i = 0; i < materialsCount; i++)
        {
            SpatialMaterial tempMaterial = mesh.SurfaceGetMaterial(i) as SpatialMaterial;
            if (tempMaterial == null) continue;
            if (CanChangeTransparency(tempMaterial))
            {
                tempMaterial.FlagsTransparent = !visible;
            }
            
            Color old = tempMaterial.AlbedoColor;
            tempMaterial.AlbedoColor = new Color(old.r, old.g, old.b, visible ? 1 : 0.02f);
        }
    }

    private void ChangePlayerVisibility(bool visible)
    {
        var bodyMesh = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/Body");
        var bodyThirdMesh = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/Body_third");

        ChangeMeshVisibility(bodyMesh, visible);
        ChangeMeshVisibility(bodyThirdMesh, visible);
        
        //смена прозрачности для оружия
        if (player.Weapons.GunOn)
        {
            var weaponParent = player.GetWeaponParent(player.Weapons.isPistol);
            var gunArmature = Global.FindNodeInScene(weaponParent, "Gun-armature") as Spatial;
            var weaponMesh = FindMeshesInParent(gunArmature);
            if (weaponMesh != null)
            {
                ChangeMeshVisibility(weaponMesh, visible);
            }

            if (!player.Weapons.isPistol)
            {
                var weaponBag = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
                ChangeMeshVisibility(weaponBag, visible);
            }
        }
        
        var artifact = player.GetNode<MeshInstance>("player_body/Armature/Skeleton/artifact");
        if (artifact.Visible)
        {
            ChangeMeshVisibility(artifact, visible);
        }

        //смена прозрачности у "тени крыльев" для пегаса
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