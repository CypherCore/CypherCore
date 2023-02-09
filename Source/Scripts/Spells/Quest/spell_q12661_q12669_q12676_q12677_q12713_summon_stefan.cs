using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 55368 - Summon Stefan
internal class spell_q12661_q12669_q12676_q12677_q12713_summon_stefan : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
	}

	private void SetDest(ref SpellDestination dest)
	{
		// Adjust effect summon position
		Position offset = new(0.0f, 0.0f, 20.0f, 0.0f);
		dest.RelocateOffset(offset);
	}
}