using Godot;

public class MirrorArea: Area {
    Global global;
    Player player;
    Mirror mirror;
    bool playerIn = false;
    float timerOn = 0;
    bool viewChanged = true;

    bool thirdView = false;
    bool mirrorOn = false;


    public override async void _Ready()
    {
        global = Global.Get();
        mirror = GetNode<Mirror>("../Mirror/Mirror");
        mirror.mirrorArea = this;
        mirror.pixelsPerUnit = global.Settings.reflections;
        mirror.MirrorOn();
        SignalAwaiter toTimer = ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        await toTimer;
        mirror.MirrorOff();
    }

    public void _on_Area_body_entered(Node body) {
        if (mirror == null || WeakRef(mirror).GetRef() == null) {
            return;
        }

        if (body.Name.Contains("Player") && !mirrorOn) {
            player = body as Player;

            if (global.Settings.reflections != 0) {
                mirror.MirrorOn();
                mirrorOn = true;
            }
            playerIn = true;
            thirdView = player.ThirdView;
        }
    }

    public void _on_mirrortrigger_body_exited(Node body) {
        if (mirror == null || WeakRef(mirror).GetRef() == null) {
            return;
        }

        if (body.Name.Contains("Player") && !mirror.cameraSee) {
            mirror.MirrorOff();
            mirrorOn = false;
            playerIn = false;
        }
    }

    public void DestroyMirror() {
        mirror = null;
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        if (playerIn) {
            if (WeakRef(mirror).GetRef() == null) {
                SetProcess(false);
                return;
            }

            if (global.Settings.reflections == 0) {
                if (mirrorOn) {
                    mirror.MirrorOff();
                    mirrorOn = false;
                    mirror.pixelsPerUnit = 0;
                }
            } else {
                if (!mirrorOn) {
                    viewChanged = false;
                    timerOn = 0.1f;

                    mirrorOn = true;
                }
            }

            if (mirror.pixelsPerUnit != global.Settings.reflections) {
                mirror.pixelsPerUnit = global.Settings.reflections;
                viewChanged = false;
                timerOn = 0.1f;
            }

            if (thirdView != player.ThirdView) {
                thirdView = player.ThirdView;
                mirror.MirrorOff();
                viewChanged = false;
                timerOn = 0.1f;
            }

            if (timerOn > 0) {
                timerOn -= delta;
            } else if(!viewChanged) {
                if (global.Settings.reflections != 0) {
                    mirror.MirrorOn();
                }
                viewChanged = true;
            }
        }
    }
}