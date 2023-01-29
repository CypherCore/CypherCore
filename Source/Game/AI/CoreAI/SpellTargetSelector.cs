using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI;

public class SpellTargetSelector : ICheck<Unit>
{
    private readonly Unit _caster;
    private readonly SpellInfo _spellInfo;

    public SpellTargetSelector(Unit caster, uint spellId)
    {
        _caster = caster;
        _spellInfo = Global.SpellMgr.GetSpellInfo(spellId, caster.GetMap().GetDifficultyID());

        Cypher.Assert(_spellInfo != null);
    }

    public bool Invoke(Unit target)
    {
        if (target == null)
            return false;

        if (_spellInfo.CheckTarget(_caster, target) != SpellCastResult.SpellCastOk)
            return false;

        // copypasta from Spell.CheckRange
        float minRange = 0.0f;
        float maxRange = 0.0f;
        float rangeMod = 0.0f;

        if (_spellInfo.RangeEntry != null)
        {
            if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
            {
                rangeMod = _caster.GetCombatReach() + 4.0f / 3.0f;
                rangeMod += target.GetCombatReach();

                rangeMod = Math.Max(rangeMod, SharedConst.NominalMeleeRange);
            }
            else
            {
                float meleeRange = 0.0f;

                if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                {
                    meleeRange = _caster.GetCombatReach() + 4.0f / 3.0f;
                    meleeRange += target.GetCombatReach();

                    meleeRange = Math.Max(meleeRange, SharedConst.NominalMeleeRange);
                }

                minRange = _caster.GetSpellMinRangeForTarget(target, _spellInfo) + meleeRange;
                maxRange = _caster.GetSpellMaxRangeForTarget(target, _spellInfo);

                rangeMod = _caster.GetCombatReach();
                rangeMod += target.GetCombatReach();

                if (minRange > 0.0f &&
                    !_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                    minRange += rangeMod;
            }

            if (_caster.IsMoving() &&
                target.IsMoving() &&
                !_caster.IsWalking() &&
                !target.IsWalking() &&
                (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee) || target.IsTypeId(TypeId.Player)))
                rangeMod += 8.0f / 3.0f;
        }

        maxRange += rangeMod;

        minRange *= minRange;
        maxRange *= maxRange;

        if (target != _caster)
        {
            if (_caster.GetExactDistSq(target) > maxRange)
                return false;

            if (minRange > 0.0f &&
                _caster.GetExactDistSq(target) < minRange)
                return false;
        }

        return true;
    }
}