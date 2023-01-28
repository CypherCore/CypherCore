using Framework.Constants;

namespace Game.Entities;

public class CleanDamage
{
    public CleanDamage(uint mitigated, uint absorbed, WeaponAttackType _attackType, MeleeHitOutcome _hitOutCome)
    {
        AbsorbedDamage  = absorbed;
        MitigatedDamage = mitigated;
        AttackType       = _attackType;
        HitOutCome       = _hitOutCome;
    }

    public uint AbsorbedDamage { get; }
    public uint MitigatedDamage { get; set; }

    public WeaponAttackType AttackType { get; }
    public MeleeHitOutcome HitOutCome { get; }
}