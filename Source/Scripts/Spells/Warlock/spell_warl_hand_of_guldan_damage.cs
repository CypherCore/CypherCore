// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Hand of Guldan damage - 86040
	[SpellScript(86040)]
	internal class spell_warl_hand_of_guldan_damage : SpellScript, IHasSpellEffects
	{
		private int _soulshards = 1;

		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Load()
		{
			_soulshards += GetCaster().GetPower(PowerType.SoulShards);

			if (_soulshards > 4)
			{
				GetCaster().SetPower(PowerType.SoulShards, 1);
				_soulshards = 4;
			}
			else
			{
				GetCaster().SetPower(PowerType.SoulShards, 0);
			}

			return true;
		}

		private void HandleOnHit(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var target = GetHitUnit();

				if (target != null)
				{
					var dmg = GetHitDamage();
					SetHitDamage(dmg * _soulshards);

					if (caster.HasAura(WarlockSpells.HAND_OF_DOOM))
						caster.CastSpell(target, WarlockSpells.DOOM, true);
				}
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}