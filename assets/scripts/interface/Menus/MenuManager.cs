
//класс отвечает за управление открытием/закрытием менюшек, чтобы они не перекрывали друг друга

using Godot;

public static class MenuManager 
{
    public static IMenu openedMenu { get; private set; }
    static IMenu backgroundMenu;

    public static bool SomeMenuOpen => openedMenu != null;

    public static bool TryToOpenMenu(IMenu menu, bool closeOther = false)
    {
        if (IsMenuExists(openedMenu))
        {
            if (closeOther) 
            {
                if (openedMenu.mustBeClosed) 
                {
                    openedMenu.CloseMenu();
                } 
                else 
                {
                    backgroundMenu = openedMenu;
                }
            } 
            else 
            {
                return false;
            }
        }
        openedMenu = menu;
        menu.OpenMenu();
        return true;
    }

    public static void CloseMenu(IMenu menu)
    {
        if (menu == openedMenu) 
        {
            openedMenu.CloseMenu();
            openedMenu = backgroundMenu;
            backgroundMenu = null;
        } 
    }

    public static void ClearMenus()
    {
        openedMenu = null;
        backgroundMenu = null;
    }

    private static bool IsMenuExists(IMenu menu)
    {
        var menuNode = menu as Node;
        return menuNode != null && Godot.Object.IsInstanceValid(menuNode);
    }
}

public interface IMenu 
{
    bool mustBeClosed {get;}
    void OpenMenu();
    void CloseMenu();
}