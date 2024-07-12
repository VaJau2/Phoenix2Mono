using Godot;
using Godot.Collections;

public class GunParticles : Spatial
{
    private Dictionary materials = new() 
    {
        {"black", GD.Load<Material>("res://assets/materials/weapon/result/black.tres")},
        {"grass", GD.Load<Material>("res://assets/materials/weapon/result/grass.tres")},
        {"dirt", GD.Load<Material>("res://assets/materials/weapon/result/dirt.tres")},
        {"stone", GD.Load<Material>("res://assets/materials/weapon/result/stone.tres")},
        {"blood", GD.Load<Material>("res://assets/materials/weapon/result/blood.tres")},
        {"glass", GD.Load<Material>("res://assets/materials/weapon/result/glass.tres")},
        {"water", GD.Load<Material>("res://assets/materials/weapon/result/water.tres")},
    };

    private Particles parts;
    private bool deleteParts;
    private float timerParts = 1.0f;

    private bool deleteSelf;
    private float timerSelf = 100.0f;

    public override void _Process(float delta)
    {
        if (deleteParts)
        {
            if (timerParts > 0)
            {
                timerParts -= delta;
            }
            else
            {
                parts.QueueFree();
                deleteParts = false;
            }
        }
	
        if (deleteSelf)
        {
            if (timerSelf > 0)
            {
                timerSelf -= delta;
            }
            else
            {
                QueueFree();
            }
        }
    }

    public void StartEmitting(Vector3 direction, string materialName, string objName = "")
    {
        parts = GetNode<Particles>("Particles");
        parts.ProcessMaterial.Set("direction", direction);
        
        if (!string.IsNullOrEmpty(materialName) && materials.Contains(materialName))
        {
            parts.DrawPass1.SurfaceSetMaterial(0, (Material)materials[materialName]);
            parts.Emitting = true;
        }

        var hole = GetNode<Spatial>("hole");

        if (!string.IsNullOrEmpty(materialName)
            && materialName != "blood"
            && materialName != "glass"
            && materialName != "black"
            && !string.IsNullOrEmpty(objName)
            && !objName.Contains("Physical Bone")
            && !objName.Contains("door")
            && !objName.Contains("box"))
        {
            GD.Print("On. Name: " + objName + ". Material: " + materialName + '\n');
            
            if (direction.z < 0.9 && direction.z > -0.9)
            {
                hole.LookAt(hole.GlobalTransform.origin + direction, Vector3.Back);
            }

            hole.Translation += (direction / 15);
        }
        else
        {
            hole.Visible = false;
            GD.Print("Off. Name: " + objName + ". Material: " + materialName + '\n');
        }

        deleteParts = true;
        deleteSelf = true;
    }
}