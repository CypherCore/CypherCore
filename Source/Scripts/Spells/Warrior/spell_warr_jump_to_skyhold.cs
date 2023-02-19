// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Jump to Skyhold Jump - 192085
	[SpellScript(192085)]
	public class spell_warr_jump_to_skyhold : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();


		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return Global.SpellMgr.GetSpellInfo(WarriorSpells.JUMP_TO_SKYHOLD_TELEPORT, Difficulty.None) != null;
		}

		private void HandleJump(int effIndex)
		{
			PreventHitDefaultEffect(effIndex);

			var caster = GetCaster();

			if (caster != null)
			{
				var pos_x = caster.GetPositionX();
				var pos_y = caster.GetPositionY();
				var pos_z = caster.GetPositionZ() + 30.0f;

				var arrivalCast = new JumpArrivalCastArgs();
				arrivalCast.SpellId = WarriorSpells.JUMP_TO_SKYHOLD_TELEPORT;
				arrivalCast.Target  = caster.GetGUID();
				caster.GetMotionMaster().MoveJump(pos_x, pos_y, pos_z, caster.GetOrientation(), 20.0f, 20.0f, EventId.Jump, false, arrivalCast);

				caster.RemoveAura(WarriorSpells.JUMP_TO_SKYHOLD_AURA);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleJump, 0, SpellEffectName.JumpDest, SpellScriptHookType.Launch));
		}
	}
}