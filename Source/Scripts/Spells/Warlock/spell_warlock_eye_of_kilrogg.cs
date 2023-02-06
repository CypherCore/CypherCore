using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 126 - Eye of Kilrogg
    [SpellScript(126)]
    public class spell_warlock_eye_of_kilrogg : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.ToPlayer())
            {
                return;
            }

            if (caster.ToPlayer().GetPet())
            {
                caster.m_Events.AddEventAtOffset(() =>
                {
                    caster.ToPlayer().PetSpellInitialize();
                }, TimeSpan.FromMilliseconds(250));
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.ModInvisibilityDetect, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
