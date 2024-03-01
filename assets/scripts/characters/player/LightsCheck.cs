using Godot;
using System.Collections.Generic;

public partial class LightsCheck : Area3D
{
    private List<Node3D> myLights = new();
    private int lightI;

    private List<Node3D> myLamps = new();
    private int lampI;

    public bool OnLight;

    public void _on_lightsCheck_body_entered(Node3D body)
    {
        if (body.Name.ToString().Contains("Light3D"))
        {
            var lightSource = body.GetNodeOrNull<BreakableObject>("lightSource");
            if (lightSource != null && !lightSource.Broken) 
            {
                OnLight = true;
                myLights.Add(body);
            }
        }
        
        if (body.Name.ToString().Contains("lamp"))
        {
            if (body is BreakableObject { Broken: false })
            {
                OnLight = true;
                myLamps.Add(body);
            }
        }
    }

    public void _on_lightsCheck_body_exited(Node3D body)
    {
        if (body.Name.ToString().Contains("Light3D") && myLights.Contains(body))
        {
            myLamps.Remove(body);
            CheckOff();
        }
        if (body.Name.ToString().Contains("lamp") && myLamps.Contains(body))
        {
            myLamps.Remove(body);
            CheckOff();
        }
    }

    private void CheckOff()
    {
        if (myLamps.Count == 0 && myLights.Count == 0)
        {
            OnLight = false;
        }
    }

    int IncreaseI(int value, int arraySize)
    {
        if (value < arraySize - 1) {
            value++;
        } else {
            value = 0;
        }
        return value;
    }

    public override void _Process(double delta)
    {
        if (myLights.Count > 0)
        {
            if (myLights[lightI].GetNode("lightSource") == null)
            {
                myLights.RemoveAt(lightI);
                CheckOff();
            }

            IncreaseI(lightI, myLights.Count);
        }
        else
        {
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

            if (lampNullOrBroken)
            {
                myLamps.RemoveAt(lampI);
                CheckOff();
            }

            IncreaseI(lampI, myLamps.Count);
        }
        else
        {
            lampI = 0;
        }
    }
}
