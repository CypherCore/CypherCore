using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 210621 - Path of Flames Spread
internal class spell_sha_path_of_flames_spread : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.FlameShock);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.Remove(GetExplTargetUnit());
		targets.RandomResize(target => target.IsTypeId(TypeId.Unit) && !target.ToUnit().HasAura(ShamanSpells.FlameShock, GetCaster().GetGUID()), 1);
	}

	private void HandleScript(uint effIndex)
	{
		var mainTarget = GetExplTargetUnit();

		if (mainTarget)
		{
			var flameShock = mainTarget.GetAura(ShamanSpells.FlameShock, GetCaster().GetGUID());

			if (flameShock != null)
			{
				var newAura = GetCaster().AddAura(ShamanSpells.FlameShock, GetHitUnit());

				if (newAura != null)
				{
					newAura.SetDuration(flameShock.GetDuration());
					newAura.SetMaxDuration(flameShock.GetDuration());
				}
			}
		}
	}
}