// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Maps.Notifiers;

namespace Game.Spells
{
    public class UnitAura : Aura
    {
        private readonly Dictionary<ObjectGuid, uint> _staticApplications = new(); // non-area Auras
        private DiminishingGroup _auraDRGroup;                                     // Diminishing

        public UnitAura(AuraCreateInfo createInfo) : base(createInfo)
        {
            _auraDRGroup = DiminishingGroup.None;
            LoadScripts();
            _InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
            GetUnitOwner()._AddAura(this, createInfo.Caster);
        }

        public override void _ApplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
        {
            base._ApplyForTarget(target, caster, aurApp);

            // register aura diminishing on apply
            if (_auraDRGroup != DiminishingGroup.None)
                target.ApplyDiminishingAura(_auraDRGroup, true);
        }

        public override void _UnapplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
        {
            base._UnapplyForTarget(target, caster, aurApp);

            // unregister aura diminishing (and store last Time)
            if (_auraDRGroup != DiminishingGroup.None)
                target.ApplyDiminishingAura(_auraDRGroup, false);
        }

        public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsRemoved())
                return;

            GetUnitOwner().RemoveOwnedAura(this, removeMode);
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            Unit refe = caster;

            if (refe == null)
                refe = GetUnitOwner();

            // add non area aura targets
            // static applications go through spell system first, so we assume they meet conditions
            foreach (var targetPair in _staticApplications)
            {
                Unit target = Global.ObjAccessor.GetUnit(GetUnitOwner(), targetPair.Key);

                if (target == null &&
                    targetPair.Key == GetUnitOwner().GetGUID())
                    target = GetUnitOwner();

                if (target)
                    targets.Add(target, targetPair.Value);
            }

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (!HasEffect(spellEffectInfo.EffectIndex))
                    continue;

                // area Auras only
                if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                    continue;

                // skip area update if owner is not in world!
                if (!GetUnitOwner().IsInWorld)
                    continue;

                if (GetUnitOwner().HasUnitState(UnitState.Isolated))
                    continue;

                List<Unit> units = new();
                var condList = spellEffectInfo.ImplicitTargetConditions;

                float radius = spellEffectInfo.CalcRadius(refe);
                float extraSearchRadius = 0.0f;

                SpellTargetCheckTypes selectionType = SpellTargetCheckTypes.Default;

                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.ApplyAreaAuraParty:
                    case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                        selectionType = SpellTargetCheckTypes.Party;

                        break;
                    case SpellEffectName.ApplyAreaAuraRaid:
                        selectionType = SpellTargetCheckTypes.Raid;

                        break;
                    case SpellEffectName.ApplyAreaAuraFriend:
                        selectionType = SpellTargetCheckTypes.Ally;

                        break;
                    case SpellEffectName.ApplyAreaAuraEnemy:
                        selectionType = SpellTargetCheckTypes.Enemy;
                        extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;

                        break;
                    case SpellEffectName.ApplyAreaAuraPet:
                        if (condList == null ||
                            Global.ConditionMgr.IsObjectMeetToConditions(GetUnitOwner(), refe, condList))
                            units.Add(GetUnitOwner());

                        goto case SpellEffectName.ApplyAreaAuraOwner;
                    /* fallthrough */
                    case SpellEffectName.ApplyAreaAuraOwner:
                        {
                            Unit owner = GetUnitOwner().GetCharmerOrOwner();

                            if (owner != null)
                                if (GetUnitOwner().IsWithinDistInMap(owner, radius))
                                    if (condList == null ||
                                        Global.ConditionMgr.IsObjectMeetToConditions(owner, refe, condList))
                                        units.Add(owner);

                            break;
                        }
                    case SpellEffectName.ApplyAuraOnPet:
                        {
                            Unit pet = Global.ObjAccessor.GetUnit(GetUnitOwner(), GetUnitOwner().GetPetGUID());

                            if (pet != null)
                                if (condList == null ||
                                    Global.ConditionMgr.IsObjectMeetToConditions(pet, refe, condList))
                                    units.Add(pet);

                            break;
                        }
                    case SpellEffectName.ApplyAreaAuraSummons:
                        {
                            if (condList == null ||
                                Global.ConditionMgr.IsObjectMeetToConditions(GetUnitOwner(), refe, condList))
                                units.Add(GetUnitOwner());

                            selectionType = SpellTargetCheckTypes.Summoned;

                            break;
                        }
                }

                if (selectionType != SpellTargetCheckTypes.Default)
                {
                    WorldObjectSpellAreaTargetCheck check = new(radius, GetUnitOwner(), refe, GetUnitOwner(), GetSpellInfo(), selectionType, condList, SpellTargetObjectTypes.Unit);
                    UnitListSearcher searcher = new(GetUnitOwner(), units, check);
                    Cell.VisitAllObjects(GetUnitOwner(), searcher, radius + extraSearchRadius);

                    // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for Auras it is not acceptable
                    units.RemoveAll(unit => !unit.IsSelfOrInSameMap(GetUnitOwner()));
                }

                foreach (Unit unit in units)
                {
                    if (!targets.ContainsKey(unit))
                        targets[unit] = 0;

                    targets[unit] |= 1u << (int)spellEffectInfo.EffectIndex;
                }
            }
        }

        public void AddStaticApplication(Unit target, uint effMask)
        {
            // only valid for non-area Auras
            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
                if ((effMask & (1u << (int)spellEffectInfo.EffectIndex)) != 0 &&
                    !spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                    effMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            if (effMask == 0)
                return;

            if (!_staticApplications.ContainsKey(target.GetGUID()))
                _staticApplications[target.GetGUID()] = 0;

            _staticApplications[target.GetGUID()] |= effMask;
        }

        // Allow Apply Aura Handler to modify and access _AuraDRGroup
        public void SetDiminishGroup(DiminishingGroup group)
        {
            _auraDRGroup = group;
        }

        public DiminishingGroup GetDiminishGroup()
        {
            return _auraDRGroup;
        }
    }
}