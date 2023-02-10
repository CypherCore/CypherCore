using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 114911 - Ancestral Guidance Heal
internal class spell_sha_ancestral_guidance_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.AncestralGuidance);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(ResizeTargets, 0, Targets.UnitDestAreaAlly));
	}

	private void ResizeTargets(List<WorldObject> targets)
	{
		SelectRandomInjuredTargets(targets, 3, true);
	}
}