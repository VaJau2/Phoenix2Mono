using Godot;

/// <summary>
/// скрипт взаимодействия с предметами
/// внезапно управляет перемещением на локацию базы из локации обучения через предмет карты
/// </summary>
public class PlayerCamera: Camera {
    //TODO
    //дописать взаимодействие для DialogueMenu
    //ну и все остальное тоже дописать, да

    const float RAY_LENGH = 6;

    Messages messages;

    Player player;
    float tempLength;
    int rayLayer = 3;

    Control labelBack;
    Label label;
    float closedTimer = 0;
    string closedText = "Закрыто";

    Spatial tempObject;
    bool onetime = false;
}