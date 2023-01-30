using Game.Spells;

namespace Game.Entities;

public class SpellPeriodicAuraLogInfo
{
    public SpellPeriodicAuraLogInfo(AuraEffect _auraEff, uint _damage, uint _originalDamage, uint _overDamage, uint _absorb, uint _resist, float _multiplier, bool _critical)
    {
        AuraEff = _auraEff;
        Damage = _damage;
        OriginalDamage = _originalDamage;
        OverDamage = _overDamage;
        Absorb = _absorb;
        Resist = _resist;
        Multiplier = _multiplier;
        Critical = _critical;
    }

    public uint Absorb { get; set; }

    public AuraEffect AuraEff { get; set; }
    public bool Critical { get; set; }
    public uint Damage { get; set; }
    public float Multiplier { get; set; }
    public uint OriginalDamage { get; set; }
    public uint OverDamage { get; set; } // overkill/overheal
    public uint Resist { get; set; }
}