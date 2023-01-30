// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells
{
    public class WorldObjectSpellTargetCheck : ICheck<WorldObject>
    {
        internal WorldObject Caster;
        internal SpellInfo SpellInfo;
        private readonly List<Condition> _condList;
        private readonly ConditionSourceInfo _condSrcInfo;
        private readonly SpellTargetObjectTypes _objectType;
        private readonly WorldObject _referer;
        private readonly SpellTargetCheckTypes _targetSelectionType;

        public WorldObjectSpellTargetCheck(WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        {
            Caster = caster;
            _referer = referer;
            SpellInfo = spellInfo;
            _targetSelectionType = selectionType;
            _condList = condList;
            _objectType = objectType;

            if (condList != null)
                _condSrcInfo = new ConditionSourceInfo(null, caster);
        }

        public virtual bool Invoke(WorldObject target)
        {
            if (SpellInfo.CheckTarget(Caster, target, true) != SpellCastResult.SpellCastOk)
                return false;

            Unit unitTarget = target.ToUnit();
            Corpse corpseTarget = target.ToCorpse();

            if (corpseTarget != null)
            {
                // use owner for party/assistance checks
                Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());

                if (owner != null)
                    unitTarget = owner;
                else
                    return false;
            }

            Unit refUnit = _referer.ToUnit();

            if (unitTarget != null)
            {
                // do only faction checks here
                switch (_targetSelectionType)
                {
                    case SpellTargetCheckTypes.Enemy:
                        if (unitTarget.IsTotem())
                            return false;

                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() &&
                            !Caster.IsValidAttackTarget(unitTarget, SpellInfo))
                            return false;

                        break;
                    case SpellTargetCheckTypes.Ally:
                        if (unitTarget.IsTotem())
                            return false;

                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() &&
                            !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
                            return false;

                        break;
                    case SpellTargetCheckTypes.Party:
                        if (refUnit == null)
                            return false;

                        if (unitTarget.IsTotem())
                            return false;

                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() &&
                            !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
                            return false;

                        if (!refUnit.IsInPartyWith(unitTarget))
                            return false;

                        break;
                    case SpellTargetCheckTypes.RaidClass:
                        if (!refUnit)
                            return false;

                        if (refUnit.GetClass() != unitTarget.GetClass())
                            return false;

                        goto case SpellTargetCheckTypes.Raid;
                    case SpellTargetCheckTypes.Raid:
                        if (refUnit == null)
                            return false;

                        if (unitTarget.IsTotem())
                            return false;

                        // TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
                        if (!target.IsCorpse() &&
                            !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
                            return false;

                        if (!refUnit.IsInRaidWith(unitTarget))
                            return false;

                        break;
                    case SpellTargetCheckTypes.Summoned:
                        if (!unitTarget.IsSummon())
                            return false;

                        if (unitTarget.ToTempSummon().GetSummonerGUID() != Caster.GetGUID())
                            return false;

                        break;
                    case SpellTargetCheckTypes.Threat:
                        if (!_referer.IsUnit() ||
                            _referer.ToUnit().GetThreatManager().GetThreat(unitTarget, true) <= 0.0f)
                            return false;

                        break;
                    case SpellTargetCheckTypes.Tap:
                        if (_referer.GetTypeId() != TypeId.Unit ||
                            unitTarget.GetTypeId() != TypeId.Player)
                            return false;

                        if (!_referer.ToCreature().IsTappedBy(unitTarget.ToPlayer()))
                            return false;

                        break;
                    default:
                        break;
                }

                switch (_objectType)
                {
                    case SpellTargetObjectTypes.Corpse:
                    case SpellTargetObjectTypes.CorpseAlly:
                    case SpellTargetObjectTypes.CorpseEnemy:
                        if (unitTarget.IsAlive())
                            return false;

                        break;
                    default:
                        break;
                }
            }

            if (_condSrcInfo == null)
                return true;

            _condSrcInfo.ConditionTargets[0] = target;

            return Global.ConditionMgr.IsObjectMeetToConditions(_condSrcInfo, _condList);
        }
    }
}