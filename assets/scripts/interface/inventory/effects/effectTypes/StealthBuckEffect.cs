using Godot;

public partial class StealthBuckEffect : Effect
{
    private const float TransparentValue = 0.02f;
    
    private Player player;

    public StealthBuckEffect()
    {
        maxTime = 60;
        badEffect = false;
    }

    // проходит по первым дочерним элементам и имет там меш
    private static MeshInstance3D FindMeshesInParent(Node3D parent)
    {
        while (true)
        {
            if (parent == null || parent.GetChildCount() <= 0)
            {
                return null;
            }

            parent = parent.GetChild(0) as Node3D;

            if (parent is MeshInstance3D instance)
            {
                return instance;
            }
        }
    }

    private static bool CanChangeTransparency(Resource material)
    {
        return !material.ResourcePath.EndsWith("wings.material");
    }

    private static void ChangeMeshVisibility(MeshInstance3D meshInstance, bool visible)
    {
        Mesh mesh = meshInstance.Mesh;
        int materialsCount = mesh.GetSurfaceCount();
        for (int i = 0; i < materialsCount; i++)
        {
            StandardMaterial3D tempMaterial = mesh.SurfaceGetMaterial(i) as StandardMaterial3D;
            if (tempMaterial == null) continue;
            if (CanChangeTransparency(tempMaterial))
            {
                tempMaterial.Transparency = visible
                    ? BaseMaterial3D.TransparencyEnum.Disabled
                    : BaseMaterial3D.TransparencyEnum.Alpha;
            }

            Color old = tempMaterial.AlbedoColor;
            tempMaterial.AlbedoColor = new Color(old.R, old.G, old.B, visible ? 1 : TransparentValue);
        }
    }

    private void ChangePlayerVisibility(bool visible)
    {
        var bodyMesh = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/Body");
        var bodyThirdMesh = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/Body_third");

        ChangeMeshVisibility(bodyMesh, visible);
        ChangeMeshVisibility(bodyThirdMesh, visible);

        //смена прозрачности для оружия
        if (player.Weapons.GunOn)
        {
            var weaponParent = player.GetWeaponParent(player.Weapons.isPistol);
            var gunArmature = Global.FindNodeInScene(weaponParent, "Gun-armature") as Node3D;
            var weaponMesh = FindMeshesInParent(gunArmature);
            if (weaponMesh != null)
            {
                ChangeMeshVisibility(weaponMesh, visible);
            }

            if (!player.Weapons.isPistol)
            {
                var weaponBag =
                    player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/BoneAttachment3D 2/shotgunBag");
                ChangeMeshVisibility(weaponBag, visible);
            }
        }

        var artifact = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/artifact");
        if (artifact.Visible)
        {
            ChangeMeshVisibility(artifact, visible);
        }

        //смена прозрачности у "тени крыльев" для пегаса
        if (Global.Get().playerRace != Race.Pegasus) return;

        var wingsShadow = player.GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/Wings");
        wingsShadow.Visible = visible;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "stealthBuck";

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