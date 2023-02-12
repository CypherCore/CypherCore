using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(191840)]
public class spell_monk_essence_font_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void FilterTargets(List<WorldObject> p_Targets)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			p_Targets.RemoveIf((WorldObject @object) =>
			                   {
				                   if (@object == null || @object.ToUnit() == null)
					                   return true;

				                   var unit = @object.ToUnit();

				                   if (unit == caster)
					                   return true;

				                   if (unit.HasAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_HEAL) && unit.GetAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_HEAL).GetDuration() > 5 * Time.InMilliseconds)
					                   return true;

				                   return false;
			                   });

			if (p_Targets.Count > 1)
			{
				p_Targets.Sort(new HealthPctOrderPred());
				p_Targets.Resize(1);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
	}
}