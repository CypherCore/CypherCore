using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(85804)] // 85804 - Selfless Healer
    internal class spell_pal_selfless_healer : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }
    }
}
