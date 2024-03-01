namespace Phoenix2Mono.assets.scripts.audio.factories;

public enum FactoryType
{
    antiradio,
    powerArmor,
    
    bunker,
    
    flaskWater,
    flaskGlass
}

public partial class FactoriesManager
{
    public static IAudioEffectsFactory GetFactory(FactoryType type)
    {
        return type switch
        {
            FactoryType.antiradio => new AntiradioFactory(),
            FactoryType.powerArmor => new PowerArmorFactory(),
            FactoryType.bunker => new BunkerFactory(),
            FactoryType.flaskWater => new FlaskWaterFactory(),
            FactoryType.flaskGlass => new FlaskGlassFactory(),
            _ => null
        };
    }
}