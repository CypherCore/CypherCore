// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Maps.Notifiers;

namespace Game.Spells
{
    public class DynObjAura : Aura
    {
        public DynObjAura(AuraCreateInfo createInfo) : base(createInfo)
        {
            LoadScripts();
            Cypher.Assert(GetDynobjOwner() != null);
            Cypher.Assert(GetDynobjOwner().IsInWorld);
            Cypher.Assert(GetDynobjOwner().GetMap() == createInfo.Caster.GetMap());
            _InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
            GetDynobjOwner().SetAura(this);
        }

        public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsRemoved())
                return;

            _Remove(removeMode);
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            Unit dynObjOwnerCaster = GetDynobjOwner().GetCaster();
            float radius = GetDynobjOwner().GetRadius();

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (!HasEffect(spellEffectInfo.EffectIndex))
                    continue;

                // we can't use effect Type like area Auras to determine check Type, check targets
                SpellTargetCheckTypes selectionType = spellEffectInfo.TargetA.GetCheckType();

                if (spellEffectInfo.TargetB.GetReferenceType() == SpellTargetReferenceTypes.Dest)
                    selectionType = spellEffectInfo.TargetB.GetCheckType();

                List<Unit> targetList = new();
                var condList = spellEffectInfo.ImplicitTargetConditions;

                WorldObjectSpellAreaTargetCheck check = new(radius, GetDynobjOwner(), dynObjOwnerCaster, dynObjOwnerCaster, GetSpellInfo(), selectionType, condList, SpellTargetObjectTypes.Unit);
                UnitListSearcher searcher = new(GetDynobjOwner(), targetList, check);
                Cell.VisitAllObjects(GetDynobjOwner(), searcher, radius);

                // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for Auras it is not acceptable
                targetList.RemoveAll(unit => !unit.IsSelfOrInSameMap(GetDynobjOwner()));

                foreach (var unit in targetList)
                {
                    if (!targets.ContainsKey(unit))
                        targets[unit] = 0;

                    targets[unit] |= 1u << (int)spellEffectInfo.EffectIndex;
                }
            }
        }
    }
}