using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 203964 - Galactic Guardian
	internal class spell_dru_galactic_guardian : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.GalacticGuardianAura);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo != null)
			{
				var target = GetTarget();

				// free automatic moonfire on Target
				target.CastSpell(damageInfo.GetVictim(), DruidSpellIds.MoonfireDamage, true);

				// Cast aura
				target.CastSpell(damageInfo.GetVictim(), DruidSpellIds.GalacticGuardianAura, true);
			}
		}
	}
}