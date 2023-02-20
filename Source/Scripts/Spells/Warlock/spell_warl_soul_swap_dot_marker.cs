// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[SpellScript(92795)] //! Soul Swap Copy Spells - 92795 - Simply copies spell IDs.
	internal class spell_warl_soul_swap_dot_marker : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleHit(int effIndex)
		{
			var swapVictim = GetCaster();
			var warlock = GetHitUnit();

			if (!warlock ||
				!swapVictim)
				return;

			spell_warl_soul_swap_override swapSpellScript = null;
			var swapOverrideAura = warlock.GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

			if (swapOverrideAura != null)
				swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>();

			if (swapSpellScript == null)
				return;

			var classMask = GetEffectInfo().SpellClassMask;

            var appliedAuras = swapVictim.GetAppliedAurasQuery();
            foreach (var itr in appliedAuras.HasCasterGuid(warlock.GetGUID()).HasSpellFamily(SpellFamilyNames.Warlock).GetResults())
			{
				var spellProto = itr.GetBase().GetSpellInfo();

				if (spellProto.SpellFamilyFlags & classMask)
					swapSpellScript.AddDot(itr.GetBase().GetId());
			}

			swapSpellScript.SetOriginalSwapSource(swapVictim);
		}
	}
}