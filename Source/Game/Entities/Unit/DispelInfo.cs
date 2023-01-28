namespace Game.Entities;

public class DispelInfo
{
    private byte _chargesRemoved;

    private readonly WorldObject _dispeller;
    private readonly uint _dispellerSpell;

    public DispelInfo(WorldObject dispeller, uint dispellerSpellId, byte chargesRemoved)
    {
        _dispeller      = dispeller;
        _dispellerSpell = dispellerSpellId;
        _chargesRemoved = chargesRemoved;
    }

    public WorldObject GetDispeller()
    {
        return _dispeller;
    }

    private uint GetDispellerSpellId()
    {
        return _dispellerSpell;
    }

    public byte GetRemovedCharges()
    {
        return _chargesRemoved;
    }

    public void SetRemovedCharges(byte amount)
    {
        _chargesRemoved = amount;
    }
}