using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 1856 - Vanish - SPELL_ROGUE_VANISH
internal class spell_rog_vanish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.VanishAura, RogueSpells.StealthShapeshiftAura);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnLaunchTarget, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
	}

	private void OnLaunchTarget(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		var target = GetHitUnit();

		target.RemoveAurasByType(AuraType.ModStalked);

		if (!target.IsPlayer())
			return;

		if (target.HasAura(RogueSpells.VanishAura))
			return;

		target.CastSpell(target, RogueSpells.VanishAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		target.CastSpell(target, RogueSpells.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}