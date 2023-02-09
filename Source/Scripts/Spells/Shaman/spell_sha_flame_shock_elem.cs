using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	//188389
	[SpellScript(188389)]
	public class spell_sha_flame_shock_elem : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private int m_ExtraSpellCost;

		public override bool Load()
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			m_ExtraSpellCost = Math.Min(caster.GetPower(PowerType.Maelstrom), 20);

			return true;
		}

		private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var m_newDuration = GetDuration() + (GetDuration() * (m_ExtraSpellCost / 20));
			SetDuration(m_newDuration);

			var caster = GetCaster();

			if (caster != null)
			{
				var m_newMael = caster.GetPower(PowerType.Maelstrom) - m_ExtraSpellCost;

				if (m_newMael < 0)
					m_newMael = 0;

				var mael = caster.GetPower(PowerType.Maelstrom);

				if (mael > 0)
					caster.SetPower(PowerType.Maelstrom, m_newMael);
			}
		}

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.HasAura(ShamanSpells.SPELL_SHAMAN_LAVA_SURGE) && RandomHelper.randChance(15))
			{
				caster.CastSpell(null, ShamanSpells.SPELL_SHAMAN_LAVA_SURGE_CAST_TIME);
				caster.GetSpellHistory().ResetCooldown(ShamanSpells.SPELL_SHAMAN_LAVA_BURST, true);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDamage));
		}
	}
}