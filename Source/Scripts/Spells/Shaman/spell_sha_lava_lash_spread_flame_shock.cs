using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 105792 - Lava Lash
	[SpellScript(105792)]
	public class spell_sha_lava_lash_spread_flame_shock : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Load()
		{
			return GetCaster().GetTypeId() == TypeId.Player;
		}

		private void FilterTargets(List<WorldObject> targets)
		{
			targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, ShamanSpells.SPELL_SHAMAN_FLAME_SHOCK, GetCaster().GetGUID()));
		}

		private void HandleScript(int UnnamedParameter)
		{
			var mainTarget = GetExplTargetUnit();

			if (mainTarget != null)
			{
				var flameShock = mainTarget.GetAura(ShamanSpells.SPELL_SHAMAN_FLAME_SHOCK, GetCaster().GetGUID());

				if (flameShock != null)
				{
					var newAura = GetCaster().AddAura(ShamanSpells.SPELL_SHAMAN_FLAME_SHOCK, GetHitUnit());

					if (newAura != null)
					{
						newAura.SetDuration(flameShock.GetDuration());
						newAura.SetMaxDuration(flameShock.GetDuration());
					}
				}
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
			SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}