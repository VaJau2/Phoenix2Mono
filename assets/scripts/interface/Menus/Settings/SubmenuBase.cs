using Godot;

//Общий класс для всех сабменю
public abstract class SubmenuBase: Control
{
    protected SettingsMenu parentMenu;
    
    //Тут все надписи и кнопочки загружаются в переменные/свойства
    public virtual void LoadSubmenu(SettingsMenu parent)
    {
        parentMenu = parent;
    }

    //Тут текст загружается в надписи и кнопочки при смене языка
    public abstract void LoadInterfaceLanguage();
}
