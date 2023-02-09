using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_two_forms : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public SpellCastResult CheckCast()
	{
		if (GetCaster().IsInCombat())
		{
			SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

			return SpellCastResult.CustomError;
		}

		// Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
		if (GetCaster().GetAuraEffectsByType(AuraType.WorgenAlteredForm).Count > 1)
		{
			SetCustomCastResultMessage(SpellCustomErrors.CantTransform);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleTransform, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleTransform(uint effIndex)
	{
		Unit target = GetHitUnit();
		PreventHitDefaultEffect(effIndex);

		if (target.HasAuraType(AuraType.WorgenAlteredForm))
			target.RemoveAurasByType(AuraType.WorgenAlteredForm);
		else // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
			target.CastSpell(target, GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
	}
}