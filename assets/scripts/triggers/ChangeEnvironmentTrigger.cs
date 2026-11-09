using Godot;
using Godot.Collections;

public partial class ChangeEnvironmentTrigger : TriggerBase
{
    //свойства, которыми различаются окружения времени суток — их плавно ведём к новым значениям
    private static readonly string[] EnvironmentProperties =
    [
        "ambient_light_color", "ambient_light_energy", "ambient_light_sky_contribution"
    ];
    private static readonly string[] SkyProperties = ["sky_top_color", "sky_horizon_color"];

    [Export] private NodePath envPath;
    [Export] private Environment envResource;
    [Export] private float transitionTime; //длительность плавной смены окружения, с; 0 — сразу
    private WorldEnvironment env;
    private bool changed; //окружение уже сменено — в сохранении, чтобы после загрузки не вернулось старое (B-33)

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive || changed) return;

        changed = true;
        env = GetNode<WorldEnvironment>(envPath);
        //в Godot 3 условие было перевёрнуто (!IsInstanceValid) — окружение никогда не менялось (B-20)
        if (IsInstanceValid(env))
        {
            if (transitionTime > 0)
            {
                StartTransition();
                return;
            }

            env.Environment = envResource;
        }
        base._on_activate_trigger();
    }

    //плавный переход на копии текущего окружения: сами ресурсы окружений не меняются (B-33).
    //Триггер удаляется только после перехода — вместе с ним остановился бы и твин
    private void StartTransition()
    {
        var fading = (Environment)env.Environment.Duplicate(true);
        env.Environment = fading;

        var tween = CreateTween().SetParallel();
        foreach (var property in EnvironmentProperties)
        {
            tween.TweenProperty(fading, property, envResource.Get(property), transitionTime);
        }

        if (fading.Sky?.SkyMaterial is ProceduralSkyMaterial sky
            && envResource.Sky?.SkyMaterial is ProceduralSkyMaterial targetSky)
        {
            foreach (var property in SkyProperties)
            {
                tween.TweenProperty(sky, property, targetSky.Get(property), transitionTime);
            }
        }

        tween.Chain().TweenCallback(Callable.From(FinishTransition));
    }

    private void FinishTransition()
    {
        env.Environment = envResource;
        base._on_activate_trigger();
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["changed"] = changed;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        changed = data["changed"].AsBool();

        if (changed)
        {
            GetNode<WorldEnvironment>(envPath).Environment = envResource;
        }
    }
}
