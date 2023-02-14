// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//105427 Skyfury Totem
	[CreatureScript(105427)]
	public class npc_skyfury_totem : ScriptedAI
	{
		public npc_skyfury_totem(Creature creature) : base(creature)
		{
		}

		public uint m_uiBuffTimer;
		public int m_buffDuration = 15000;

		public struct TotemData
		{
			public const uint SPELL_TO_CAST = TotemSpells.SPELL_TOTEM_SKYFURY_EFFECT;
			public const uint RANGE = 40;
			public const uint DELAY = 500;
		}

		public override void Reset()
		{
			m_uiBuffTimer = (uint)TotemData.DELAY;
			ApplyBuff();
		}

		public override void UpdateAI(uint uiDiff)
		{
			m_buffDuration -= (int)uiDiff;

			if (m_uiBuffTimer <= uiDiff)
				ApplyBuff();
			else
				m_uiBuffTimer -= uiDiff;
		}

		public void ApplyBuff()
		{
			m_uiBuffTimer = (uint)TotemData.DELAY;

			if (!me)
				return;

			var targets  = new List<Unit>();
			var check    = new AnyFriendlyUnitInObjectRangeCheck(me, me, TotemData.RANGE);
			var searcher = new UnitListSearcher(me, targets, check, Framework.Constants.GridType.All);
			Cell.VisitGrid(me, searcher, TotemData.RANGE);

			foreach (var itr in targets)
			{
				if (!itr)
					continue;

				if (!itr.HasAura(TotemSpells.SPELL_TOTEM_SKYFURY_EFFECT))
				{
					me.CastSpell(itr, TotemSpells.SPELL_TOTEM_SKYFURY_EFFECT, true);
					var aura = itr.GetAura(TotemSpells.SPELL_TOTEM_SKYFURY_EFFECT);

					if (aura != null)
						aura.SetDuration(m_buffDuration);
				}
			}
		}
	}
}