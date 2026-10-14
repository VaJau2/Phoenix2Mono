namespace Phoenix2Mono.assets.scripts.audio.factories;

public enum FactoryType
{
    antiradio,
    powerArmor,
    
    bunker,
    
    flaskWater,
    flaskGlass
}

public class FactoriesManager
{
    public static IAudioEffectsFactory GetFactory(FactoryType type)
    {
        switch (type)
        {
            case FactoryType.antiradio:
                return new AntiradioFactory();
            
            case FactoryType.powerArmor:
                return new PowerArmorFactory();
            
            case FactoryType.bunker:
                return new BunkerFactory();
            
            case FactoryType.flaskWater:
                return new FlaskWaterFactory();
            
            case FactoryType.flaskGlass:
                return new FlaskGlassFactory();
        }

        return null;
    }
}