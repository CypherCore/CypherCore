using System;
using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class HealInfo
{
    private readonly Unit _healer;
    private readonly uint _originalHeal;
    private readonly SpellSchoolMask _schoolMask;
    private readonly SpellInfo _spellInfo;
    private readonly Unit _target;
    private uint _absorb;
    private uint _effectiveHeal;
    private uint _heal;
    private ProcFlagsHit _hitMask;

    public HealInfo(Unit healer, Unit target, uint heal, SpellInfo spellInfo, SpellSchoolMask schoolMask)
    {
        _healer = healer;
        _target = target;
        _heal = heal;
        _originalHeal = heal;
        _spellInfo = spellInfo;
        _schoolMask = schoolMask;
    }

    public void AbsorbHeal(uint amount)
    {
        amount = Math.Min(amount, GetHeal());
        _absorb += amount;
        _heal -= amount;
        amount = Math.Min(amount, GetEffectiveHeal());
        _effectiveHeal -= amount;
        _hitMask |= ProcFlagsHit.Absorb;
    }

    public void SetEffectiveHeal(uint amount)
    {
        _effectiveHeal = amount;
    }

    public Unit GetHealer()
    {
        return _healer;
    }

    public Unit GetTarget()
    {
        return _target;
    }

    public uint GetHeal()
    {
        return _heal;
    }

    public uint GetOriginalHeal()
    {
        return _originalHeal;
    }

    public uint GetEffectiveHeal()
    {
        return _effectiveHeal;
    }

    public uint GetAbsorb()
    {
        return _absorb;
    }

    public SpellInfo GetSpellInfo()
    {
        return _spellInfo;
    }

    public SpellSchoolMask GetSchoolMask()
    {
        return _schoolMask;
    }

    private ProcFlagsHit GetHitMask()
    {
        return _hitMask;
    }
}