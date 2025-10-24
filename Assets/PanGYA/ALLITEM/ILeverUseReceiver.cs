// Any script that should react to the lever being used implements this.
public interface ILeverUseReceiver
{
    void OnLeverUsed(DoorPuzzleLeverSocket source);
}
