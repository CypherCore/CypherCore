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
    [SpellScript(28789)] // 28789 - Holy Power
    internal class spell_pal_t3_6p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.HolyPowerArmor, PaladinSpells.HolyPowerAttackPower, PaladinSpells.HolyPowerSpellPower, PaladinSpells.HolyPowerMp5);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            switch (target.GetClass())
            {
                case Class.Paladin:
                case Class.Priest:
                case Class.Shaman:
                case Class.Druid:
                    spellId = PaladinSpells.HolyPowerMp5;

                    break;
                case Class.Mage:
                case Class.Warlock:
                    spellId = PaladinSpells.HolyPowerSpellPower;

                    break;
                case Class.Hunter:
                case Class.Rogue:
                    spellId = PaladinSpells.HolyPowerAttackPower;

                    break;
                case Class.Warrior:
                    spellId = PaladinSpells.HolyPowerArmor;

                    break;
                default:
                    return;
            }

            caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
        }
    }
}
