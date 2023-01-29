using Framework.Constants;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities;

public class SpellNonMeleeDamage
{
    public uint Absorb;

    public ObjectGuid CastId;
    public uint Damage;
    public SpellCastVisual SpellVisual;

    public SpellNonMeleeDamage(Unit _attacker, Unit _target, SpellInfo _spellInfo, SpellCastVisual spellVisual, SpellSchoolMask _schoolMask, ObjectGuid _castId = default)
    {
        Target = _target;
        Attacker = _attacker;
        Spell = _spellInfo;
        SpellVisual = spellVisual;
        SchoolMask = _schoolMask;
        CastId = _castId;
        PreHitHealth = (uint)_target.GetHealth();
    }

    public Unit Attacker { get; set; }
    public uint Blocked { get; set; }

    // Used for help
    public uint CleanDamage { get; set; }
    public bool FullBlock { get; set; }
    public HitInfo HitInfo { get; set; }
    public uint OriginalDamage { get; set; }
    public bool PeriodicLog { get; set; }
    public uint PreHitHealth { get; set; }
    public uint Resist { get; set; }
    public SpellSchoolMask SchoolMask { get; set; }
    public SpellInfo Spell { get; set; }

    public Unit Target { get; set; }
}