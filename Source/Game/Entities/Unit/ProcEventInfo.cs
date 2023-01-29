using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class ProcEventInfo
{
    private readonly Unit _actionTarget;

    private readonly Unit _actor;
    private readonly DamageInfo _damageInfo;
    private readonly HealInfo _healInfo;
    private readonly ProcFlagsHit _hitMask;
    private readonly Unit _procTarget;
    private readonly Spell _spell;
    private readonly ProcFlagsSpellPhase _spellPhaseMask;
    private readonly ProcFlagsSpellType _spellTypeMask;
    private readonly ProcFlagsInit _typeMask;

    public ProcEventInfo(Unit actor, Unit actionTarget, Unit procTarget, ProcFlagsInit typeMask, ProcFlagsSpellType spellTypeMask,
        ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
    {
        _actor = actor;
        _actionTarget = actionTarget;
        _procTarget = procTarget;
        _typeMask = typeMask;
        _spellTypeMask = spellTypeMask;
        _spellPhaseMask = spellPhaseMask;
        _hitMask = hitMask;
        _spell = spell;
        _damageInfo = damageInfo;
        _healInfo = healInfo;
    }

    public Unit GetActor()
    {
        return _actor;
    }

    public Unit GetActionTarget()
    {
        return _actionTarget;
    }

    public Unit GetProcTarget()
    {
        return _procTarget;
    }

    public ProcFlagsInit GetTypeMask()
    {
        return _typeMask;
    }

    public ProcFlagsSpellType GetSpellTypeMask()
    {
        return _spellTypeMask;
    }

    public ProcFlagsSpellPhase GetSpellPhaseMask()
    {
        return _spellPhaseMask;
    }

    public ProcFlagsHit GetHitMask()
    {
        return _hitMask;
    }

    public SpellInfo GetSpellInfo()
    {
        if (_spell)
            return _spell.GetSpellInfo();

        if (_damageInfo != null)
            return _damageInfo.GetSpellInfo();

        if (_healInfo != null)
            return _healInfo.GetSpellInfo();

        return null;
    }

    public SpellSchoolMask GetSchoolMask()
    {
        if (_spell)
            return _spell.GetSpellInfo().GetSchoolMask();

        if (_damageInfo != null)
            return _damageInfo.GetSchoolMask();

        if (_healInfo != null)
            return _healInfo.GetSchoolMask();

        return SpellSchoolMask.None;
    }

    public DamageInfo GetDamageInfo()
    {
        return _damageInfo;
    }

    public HealInfo GetHealInfo()
    {
        return _healInfo;
    }

    public Spell GetProcSpell()
    {
        return _spell;
    }
}