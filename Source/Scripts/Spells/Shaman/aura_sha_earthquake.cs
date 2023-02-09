using Framework.Constants;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Scripts.Spells.Shaman
{
    // 61882
    [SpellScript(61882)]
    public class aura_sha_earthquake : AuraScript
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(ShamanSpells.SPELL_SHAMAN_EARTHQUAKE);
        }

        private void HandlePeriodic(AuraEffect UnnamedParameter)
        {
            AreaTrigger at = GetTarget().GetAreaTrigger(ShamanSpells.SPELL_SHAMAN_EARTHQUAKE);
            if (at != null)
                GetTarget().CastSpell(at.GetPosition(), ShamanSpells.SPELL_SHAMAN_EARTHQUAKE_TICK, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }
    }
}
