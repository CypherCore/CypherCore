using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman
{
    //207498 ancestral protection
    [SpellScript(207498)]
    public class spell_sha_ancestral_protection_totem_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            amount = -1;
        }

        private void HandleAfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
            {
                return;
            }

            Unit totem = GetCaster();
            if (totem == null)
            {
                return;
            }

            totem.CastSpell(GetTargetApplication().GetTarget(), TotemSpells.SPELL_TOTEM_TOTEMIC_REVIVAL, true);
            totem.KillSelf();
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
            AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
