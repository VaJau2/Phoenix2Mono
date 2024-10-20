using Godot;
using System.Collections.Generic;

//Класс укрытий для неписей
public class CoversManager : Node
{
    public List<Cover> Covers = [];
    
    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            Covers.Add(new Cover(child as Spatial));
        }
    }

    //Берет ближайшее к себе укрытие
    public Cover GetCover(Spatial npc)
    {
        if (Covers.Count == 0)
        {
            return null;
        }

        Cover closestCover = Covers[0];
        
        //если первое укрытие занято, не проверяем
        if (Covers.Count > 1)
        {
            Vector3 npcPos = npc.GlobalTransform.origin;
            float oldDistance = closestCover.center.DistanceTo(npcPos);

            for (int i = 1; i < Covers.Count; i++)
            {
                float tempDistance = Covers[i].center.DistanceTo(npcPos);
                if (tempDistance < oldDistance)
                {
                    closestCover = Covers[i];
                    oldDistance = tempDistance;
                }
            }
        }

        if (closestCover == null) return null;
        
        Covers.Remove(closestCover);
        return closestCover;
    }

    public void ReturnCover(Cover cover)
    {
        Covers.Add(cover);
    }
}