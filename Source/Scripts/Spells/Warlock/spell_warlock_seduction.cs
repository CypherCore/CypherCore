// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 6358 - Seduction, 115268 - Mesmerize
	[SpellScript(new uint[]
	             {
		             6358, 115268
	             })]
	public class spell_warlock_seduction : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			// Glyph of Demon Training
			var target = GetTarget();
			var caster = GetCaster();

			if (caster == null)
				return;

			var owner = caster.GetOwner();

			if (owner != null)
				if (owner.HasAura(WarlockSpells.GLYPH_OF_DEMON_TRAINING))
				{
					target.RemoveAurasByType(AuraType.PeriodicDamage);
					target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
				}

			// remove invisibility from Succubus on successful cast
			caster.RemoveAura(WarlockSpells.PET_LESSER_INVISIBILITY);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
		}
	}
}