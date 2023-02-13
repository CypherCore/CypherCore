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
    // 248033 - Awakening
    [SpellScript(248033)]
    internal class spell_pal_awakening : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.AvengingWrath) && spellInfo.GetEffects().Count >= 1;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            TimeSpan extraDuration = TimeSpan.Zero;
            AuraEffect durationEffect = GetEffect(1);

            if (durationEffect != null)
                extraDuration = TimeSpan.FromSeconds(durationEffect.GetAmount());

            Aura avengingWrath = GetTarget().GetAura(PaladinSpells.AvengingWrath);

            if (avengingWrath != null)
            {
                avengingWrath.SetDuration((int)(avengingWrath.GetDuration() + extraDuration.TotalMilliseconds));
                avengingWrath.SetMaxDuration((int)(avengingWrath.GetMaxDuration() + extraDuration.TotalMilliseconds));
            }
            else
            {
                GetTarget()
                    .CastSpell(GetTarget(),
                               PaladinSpells.AvengingWrath,
                               new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                                   .SetTriggeringSpell(eventInfo.GetProcSpell())
                                   .AddSpellMod(SpellValueMod.Duration, (int)extraDuration.TotalMilliseconds));
            }
        }
    }
}
