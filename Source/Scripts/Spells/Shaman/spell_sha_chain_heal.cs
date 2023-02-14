// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 1064 - Chain Heal
	[SpellScript(1064)]
	public class spell_sha_chain_heal : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_HIGH_TIDE, Difficulty.None) != null)
				return false;

			return true;
		}

		private void CatchInitialTarget(ref WorldObject target)
		{
			_primaryTarget = target;
		}

		private void SelectAdditionalTargets(List<WorldObject> targets)
		{
			var caster   = GetCaster();
			var highTide = caster.GetAuraEffect(ShamanSpells.SPELL_SHAMAN_HIGH_TIDE, 1);

			if (highTide == null)
				return;

			var range      = 25.0f;
			var targetInfo = new SpellImplicitTargetInfo(Targets.UnitChainhealAlly);
			var conditions = GetSpellInfo().GetEffect(0).ImplicitTargetConditions;

			var containerTypeMask = GetSpell().GetSearcherTypeMask(targetInfo.GetObjectType(), conditions);

			if (containerTypeMask == 0)
				return;

			var chainTargets = new List<WorldObject>();
			var check        = new WorldObjectSpellAreaTargetCheck(range, _primaryTarget, caster, caster, GetSpellInfo(), targetInfo.GetCheckType(), conditions, SpellTargetObjectTypes.Unit);
			var searcher     = new WorldObjectListSearcher(caster, chainTargets, check, containerTypeMask);
			Cell.VisitGrid(_primaryTarget, searcher, range);

			chainTargets.RemoveIf(new UnitAuraCheck<WorldObject>(false, ShamanSpells.SPELL_SHAMAN_RIPTIDE, caster.GetGUID()));

			if (chainTargets.Count == 0)
				return;

			chainTargets.Sort();
			targets.Sort();

			var extraTargets = new List<WorldObject>();
			extraTargets = chainTargets.Except(targets).ToList();
			extraTargets.RandomResize((uint)highTide.GetAmount());
			targets.AddRange(extraTargets);
		}

		private WorldObject _primaryTarget = null;

		public override void Register()
		{
			SpellEffects.Add(new ObjectTargetSelectHandler(this.CatchInitialTarget, 0, Targets.UnitChainhealAlly));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectAdditionalTargets, 0, Targets.UnitChainhealAlly));
		}
	}
}