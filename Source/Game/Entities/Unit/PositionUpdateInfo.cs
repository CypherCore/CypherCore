namespace Game.Entities;

internal struct PositionUpdateInfo
{
    public bool Relocated;
    public bool Turned;

    public void Reset()
    {
        Relocated = false;
        Turned = false;
    }
}