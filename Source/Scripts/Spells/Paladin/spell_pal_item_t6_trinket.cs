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
    [SpellScript(40470)] // 40470 - Paladin Tier 6 Trinket
    internal class spell_pal_item_t6_trinket : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.EnduringLight, PaladinSpells.EnduringJudgement);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Holy Light & Flash of Light
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0xC0000000))
            {
                spellId = PaladinSpells.EnduringLight;
                chance = 15;
            }
            // Judgements
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00800000u))
            {
                spellId = PaladinSpells.EnduringJudgement;
                chance = 50;
            }
            else
            {
                return;
            }

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), spellId, new CastSpellExtraArgs(aurEff));
        }
    }
}
