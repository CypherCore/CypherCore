namespace Game.Entities;

public class DispelInfo
{
    private readonly WorldObject _dispeller;
    private readonly uint _dispellerSpell;
    private byte _chargesRemoved;

    public DispelInfo(WorldObject dispeller, uint dispellerSpellId, byte chargesRemoved)
    {
        _dispeller = dispeller;
        _dispellerSpell = dispellerSpellId;
        _chargesRemoved = chargesRemoved;
    }

    public WorldObject GetDispeller()
    {
        return _dispeller;
    }

    public byte GetRemovedCharges()
    {
        return _chargesRemoved;
    }

    public void SetRemovedCharges(byte amount)
    {
        _chargesRemoved = amount;
    }

    private uint GetDispellerSpellId()
    {
        return _dispellerSpell;
    }
}