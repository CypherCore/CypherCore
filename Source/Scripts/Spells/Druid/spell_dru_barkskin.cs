using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 22812 - Barkskin
	internal class spell_dru_barkskin : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.BramblesPassive);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
		}

		private void HandlePeriodic(AuraEffect aurEff)
		{
			var target = GetTarget();

			if (target.HasAura(DruidSpellIds.BramblesPassive))
				target.CastSpell(target, DruidSpellIds.BramblesDamageAura, true);
		}
	}
}