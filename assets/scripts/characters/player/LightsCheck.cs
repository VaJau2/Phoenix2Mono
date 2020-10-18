using Godot;
using System.Collections.Generic;

public class LightsCheck : Area
{
    private List<Spatial> myLights = new List<Spatial>();
    private int lightI;

    private List<Spatial> myLamps = new List<Spatial>();
    private int lampI;

    public bool OnLight;

    public void _on_lightsCheck_body_entered(Spatial body)
    {
        if (body.Name.Contains("Light"))
        {
            var lightSource = body.GetNode<BreakableObject>("lightSource");
            if (lightSource != null && !lightSource.Broken) 
            {
                OnLight = true;
                myLights.Add(body);
            }
        }
        if (body.Name.Contains("lamp"))
        {
            var lamp = body as BreakableObject;
            if (lamp != null && !lamp.Broken)
            {
                OnLight = true;
                myLamps.Add(body);
            }
        }
    }

    public void _on_lightsCheck_body_exited(Spatial body)
    {
        if (body.Name.Contains("Light") && myLights.Contains(body))
        {
            myLamps.Remove(body);
            checkOff();
        }
        if (body.Name.Contains("lamp") && myLamps.Contains(body))
        {
            myLamps.Remove(body);
            checkOff();
        }
    }

    private void checkOff()
    {
        if (myLamps.Count == 0 && myLights.Count == 0)
        {
            OnLight = false;
        }
    }

    int increaseI(int value, int arraySize)
    {
        if (value < arraySize - 1) {
            value++;
        } else {
            value = 0;
        }
        return value;
    }

    public override void _Process(float delta)
    {
        if (myLights.Count > 0)
        {
            if(myLights[lightI].GetNode("lightSource") == null)
            {
                myLights.RemoveAt(lightI);
                checkOff();
            }
            increaseI(lightI, myLights.Count);
        } else {
            lightI = 0;
        }

        if (myLamps.Count > 0)
        {
            bool lampNullOrBroken = (myLamps[lampI] == null);
            if (myLamps[lampI] != null)
            {
                var tempLamp = myLamps[lampI] as BreakableObject;
                lampNullOrBroken = tempLamp.Broken;
            }
                
            if(lampNullOrBroken)
            {
                myLamps.RemoveAt(lampI);
                checkOff();
            }
            increaseI(lampI, myLamps.Count);

        } else {
            lampI = 0;
        }
    }
}
