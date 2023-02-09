using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 195255 - Stormlash
    [SpellScript(195255)]
    public class aura_sha_stormlash : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_STORMLASH_BUFF, true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
