//Используется в PlayerCamera как интерфейс для всех объектов,
//с которыми можно взаимодействовать
public interface IInteractable
{
    bool MayInteract { get; }
    string InteractionHintCode { get; }
    void Interact(PlayerCamera interactor);
}
