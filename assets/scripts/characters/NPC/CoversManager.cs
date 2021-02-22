using Godot;
using System.Collections.Generic;

//Класс укрытий для неписей
public class CoversManager : Node
{
    public List<Cover> covers = new List<Cover>();

    public override void _Ready()
    {
        foreach(Node child in GetChildren()) {
            covers.Add(new Cover(child as Spatial));
        }
    }

    //Берет самое дальнее от противника укрытие
    public Cover GetCover(Spatial enemy)
    {
        if (covers.Count == 0) {
            return null;
        }

        Cover closestCover = covers[0];
        //если первое укрытие занято, не проверяем
        if (!closestCover.free) {
            closestCover = null;
        }

        if (covers.Count > 1) {
            Vector3 enemyPos = enemy.GlobalTransform.origin;
            float oldDistance = (closestCover != null) ? closestCover.center.DistanceTo(enemyPos) : 0;

            for(int i = 1; i < covers.Count; i++) {
                if (!covers[i].free) continue;
                float tempDistance = covers[i].center.DistanceTo(enemyPos);
                if (tempDistance > oldDistance) {
                    closestCover = covers[i];
                    oldDistance = tempDistance;
                }
            }
        }

        if (closestCover != null) {
            closestCover.free = false;
            return closestCover;
        } else {
            return null;
        }
    }
}

//Класс укрытия
public class Cover
{
    public bool free = true;
    public Vector3 center;
    public List<Vector3> places = new List<Vector3>();

    public Cover(Spatial place)
    {
        center = place.GlobalTransform.origin;
        foreach(Node tempPlace in place.GetChildren()) {
            places.Add((tempPlace as Spatial).GlobalTransform.origin);
        }
    }

    //Берет самое дальнее от противника подукрытие
    public Vector3 GetFarPlace(Vector3 enemyPos)
    {
        if (places.Count == 0) {
            GD.PrintErr("there are no covers!");
            return Vector3.Zero;
        }

        Vector3 farPlace = places[0];

        if (places.Count > 1) {
            float oldDistance = farPlace.DistanceTo(enemyPos);

            for(int i = 1; i < places.Count; i++) {
                Vector3 tempPos = places[i];
                float tempDistance = tempPos.DistanceTo(enemyPos);

                if (tempDistance > oldDistance) {
                    farPlace = places[i];
                    oldDistance = tempDistance;
                }
            }
        }

        return farPlace;
    }
}
