using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_book_of_glyph_mastery : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public SpellCastResult CheckCast()
	{
		if (SkillDiscovery.HasDiscoveredAllSpells(GetSpellInfo().Id, GetCaster().ToPlayer()))
		{
			SetCustomCastResultMessage(SpellCustomErrors.LearnedEverything);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleScript(uint effIndex)
	{
		Player caster  = GetCaster().ToPlayer();
		uint   spellId = GetSpellInfo().Id;

		// learn random explicit discovery recipe (if any)
		uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

		if (discoveredSpellId != 0)
			caster.LearnSpell(discoveredSpellId, false);
	}
}