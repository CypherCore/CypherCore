using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class CalcDamageInfo
{
    public uint Absorb;
    public uint Damage;
    public Unit Attacker { get; set; } // Attacker
    public Unit Target { get; set; }   // Target for Damage
    public uint DamageSchoolMask { get; set; }
    public uint OriginalDamage { get; set; }
    public uint Resist { get; set; }
    public uint Blocked { get; set; }
    public HitInfo HitInfo { get; set; }
    public VictimState TargetState { get; set; }

    // Helper
    public WeaponAttackType AttackType { get; set; }
    public ProcFlagsInit ProcAttacker { get; set; }
    public ProcFlagsInit ProcVictim { get; set; }
    public uint CleanDamage { get; set; } // Used only for rage calculation
    public MeleeHitOutcome HitOutCome { get; set; } // TODO: remove this field (need use TargetState)
}