using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[Script] // 52042 - Healing Stream Totem
internal class spell_sha_healing_stream_totem_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitDestAreaAlly));
	}

	private void SelectTargets(List<WorldObject> targets)
	{
		SelectRandomInjuredTargets(targets, 1, true);
	}
}