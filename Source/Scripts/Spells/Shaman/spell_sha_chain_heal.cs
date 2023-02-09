using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 1064 - Chain Heal
    [SpellScript(1064)]
    public class spell_sha_chain_heal : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_HIGH_TIDE, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        private void CatchInitialTarget(ref WorldObject target)
        {
            _primaryTarget = target;
        }

        private void SelectAdditionalTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            AuraEffect highTide = caster.GetAuraEffect(ShamanSpells.SPELL_SHAMAN_HIGH_TIDE, 1);
            if (highTide == null)
            {
                return;
            }

            float range = 25.0f;
            SpellImplicitTargetInfo targetInfo = new SpellImplicitTargetInfo(Targets.UnitChainhealAlly);
            var conditions = GetSpellInfo().GetEffect(0).ImplicitTargetConditions;

            var containerTypeMask = GetSpell().GetSearcherTypeMask(targetInfo.GetObjectType(), conditions);
            if (containerTypeMask == 0)
            {
                return;
            }

            List<WorldObject> chainTargets = new List<WorldObject>();
            WorldObjectSpellAreaTargetCheck check = new WorldObjectSpellAreaTargetCheck(range, _primaryTarget, caster, caster, GetSpellInfo(), targetInfo.GetCheckType(), conditions, SpellTargetObjectTypes.Unit);
            WorldObjectListSearcher searcher = new WorldObjectListSearcher(caster, chainTargets, check, containerTypeMask);
            Cell.VisitAllObjects(_primaryTarget, searcher, range);

            chainTargets.RemoveIf(new UnitAuraCheck<WorldObject>(false, ShamanSpells.SPELL_SHAMAN_RIPTIDE, caster.GetGUID()));
            if (chainTargets.Count == 0)
            {
                return;
            }

            chainTargets.Sort();
            targets.Sort();

            List<WorldObject> extraTargets = new List<WorldObject>();
            extraTargets = chainTargets.Except(targets).ToList();
            extraTargets.RandomResize((uint)highTide.GetAmount());
            targets.AddRange(extraTargets);
        }

        private WorldObject _primaryTarget = null;

        public override void Register()
        {
            SpellEffects.Add(new ObjectTargetSelectHandler(this.CatchInitialTarget, 0, Targets.UnitChainhealAlly));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectAdditionalTargets, 0, Targets.UnitChainhealAlly));
        }
    }
}
