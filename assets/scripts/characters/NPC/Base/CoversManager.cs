using Godot;
using System.Collections.Generic;

//Класс укрытий для неписей
public class CoversManager : Node
{
    public List<Cover> covers = new List<Cover>();

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            covers.Add(new Cover(child as Spatial));
        }
    }

    //Берет ближайшее к себе укрытие
    public Cover GetCover(Spatial npc)
    {
        if (covers.Count == 0)
        {
            return null;
        }

        Cover closestCover = covers[0];
        //если первое укрытие занято, не проверяем

        if (covers.Count > 1)
        {
            Vector3 npcPos = npc.GlobalTransform.origin;
            float oldDistance = closestCover.center.DistanceTo(npcPos);

            for (int i = 1; i < covers.Count; i++)
            {
                float tempDistance = covers[i].center.DistanceTo(npcPos);
                if (tempDistance < oldDistance)
                {
                    closestCover = covers[i];
                    oldDistance = tempDistance;
                }
            }
        }

        if (closestCover != null)
        {
            covers.Remove(closestCover);
            return closestCover;
        }
        else
        {
            return null;
        }
    }

    public void ReturnCover(Cover cover)
    {
        covers.Add(cover);
    }
}

//Класс укрытия
public class Cover
{
    public Vector3 center;
    public List<Vector3> places = new List<Vector3>();

    public Cover(Spatial place)
    {
        center = place.GlobalTransform.origin;
        foreach (Node tempPlace in place.GetChildren())
        {
            places.Add((tempPlace as Spatial).GlobalTransform.origin);
        }
    }

    //Берет самое дальнее от противника подукрытие
    public Vector3 GetFarPlace(Vector3 enemyPos)
    {
        if (places.Count == 0)
        {
            GD.PrintErr("there are no covers!");
            return Vector3.Zero;
        }

        Vector3 farPlace = places[0];

        if (places.Count > 1)
        {
            float oldDistance = farPlace.DistanceTo(enemyPos);

            for (int i = 1; i < places.Count; i++)
            {
                Vector3 tempPos = places[i];
                float tempDistance = tempPos.DistanceTo(enemyPos);

                if (tempDistance > oldDistance)
                {
                    farPlace = places[i];
                    oldDistance = tempDistance;
                }
            }
        }

        return farPlace;
    }
}