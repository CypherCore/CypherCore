using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 6544 Heroic leap
	internal class spell_warr_heroic_leap : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.HEROIC_LEAP_JUMP);
		}

		public SpellCastResult CheckCast()
		{
			var dest = GetExplTargetDest();

			if (dest != null)
			{
				if (GetCaster().HasUnitMovementFlag(MovementFlag.Root))
					return SpellCastResult.Rooted;

				if (GetCaster().GetMap().Instanceable())
				{
					var range = GetSpellInfo().GetMaxRange(true, GetCaster()) * 1.5f;

					PathGenerator generatedPath = new(GetCaster());
					generatedPath.SetPathLengthLimit(range);

					var result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);

					if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
						return SpellCastResult.OutOfRange;
					else if (!result ||
					         generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
						return SpellCastResult.NoPath;
				}
				else if (dest.GetPositionZ() > GetCaster().GetPositionZ() + 4.0f)
				{
					return SpellCastResult.NoPath;
				}

				return SpellCastResult.SpellCastOk;
			}

			return SpellCastResult.NoValidTargets;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
		}

		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleDummy(uint effIndex)
		{
			var dest = GetHitDest();

			if (dest != null)
				GetCaster().CastSpell(dest.GetPosition(), WarriorSpells.HEROIC_LEAP_JUMP, new CastSpellExtraArgs(true));
		}
	}
}