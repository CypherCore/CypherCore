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

		private void HandleHit(uint effIndex)
		{
			var swapVictim = GetCaster();
			var warlock    = GetHitUnit();

			if (!warlock ||
			    !swapVictim)
				return;

			var                           appliedAuras     = swapVictim.GetAppliedAuras();
			spell_warl_soul_swap_override swapSpellScript  = null;
			var                           swapOverrideAura = warlock.GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

			if (swapOverrideAura != null)
				swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>();

			if (swapSpellScript == null)
				return;

			var classMask = GetEffectInfo().SpellClassMask;

			foreach (var itr in appliedAuras.KeyValueList)
			{
				var spellProto = itr.Value.GetBase().GetSpellInfo();

				if (itr.Value.GetBase().GetCaster() == warlock)
					if (spellProto.SpellFamilyName == SpellFamilyNames.Warlock &&
					    (spellProto.SpellFamilyFlags & classMask))
						swapSpellScript.AddDot(itr.Key);
			}

			swapSpellScript.SetOriginalSwapSource(swapVictim);
		}
	}
}