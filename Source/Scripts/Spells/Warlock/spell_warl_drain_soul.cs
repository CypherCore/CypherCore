using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[Script] // 198590 - Drain Soul
	internal class spell_warl_drain_soul : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.DRAIN_SOUL_ENERGIZE);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
				return;

			var caster = GetCaster();

			caster?.CastSpell(caster, WarlockSpells.DRAIN_SOUL_ENERGIZE, true);
		}
	}
}