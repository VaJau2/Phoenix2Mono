
//класс отвечает за управление открытием/закрытием менюшек, чтобы они не перекрывали друг друга
public static class MenuManager {

    static IMenu openedMenu;
    static IMenu backgroundMenu = null;

    public static void TryToOpenMenu(IMenu menu, bool closeOther = false)
    {
        if (openedMenu != null) {
            if (closeOther) {
                if (openedMenu.mustBeClosed) {
                    openedMenu.CloseMenu();
                } else {
                    backgroundMenu = openedMenu;
                }
            } else {
                return;
            }
        }
        openedMenu = menu;
        menu.OpenMenu();
    }

    public static void CloseMenu(IMenu menu)
    {
        if (menu == openedMenu) {
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
}

public interface IMenu {
    bool mustBeClosed {get;}
    void OpenMenu();
    void CloseMenu();
}