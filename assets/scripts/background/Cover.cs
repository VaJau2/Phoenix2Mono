//Класс укрытия

using System.Collections.Generic;
using Godot;

public class Cover
{
    public Vector3 center;
    public List<Vector3> places = new();

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
