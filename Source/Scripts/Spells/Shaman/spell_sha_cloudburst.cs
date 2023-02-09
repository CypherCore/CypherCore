using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    //Cloudburst - 157503
    [SpellScript(157503)]
    public class spell_sha_cloudburst : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            l_TargetCount = 0;
            return true;
        }

        private void HandleHeal(uint UnnamedParameter)
        {
            if (l_TargetCount != 0)
            {
                SetHitHeal(GetHitHeal() / l_TargetCount);
            }
        }

        private void CountTargets(List<WorldObject> p_Targets)
        {
            l_TargetCount = (byte)p_Targets.Count;
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitDestAreaAlly));
            SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }

        private byte l_TargetCount;
    }
}
