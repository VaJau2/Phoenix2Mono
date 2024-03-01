public interface IActivated
{
    bool IsActive { get; }

    void SetActive(bool newActive);
}