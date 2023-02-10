using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	//AT id : 3691
	//Spell ID : 61882
	[Script]
	public class at_sha_earthquake_totem : AreaTriggerAI
	{
		public int timeInterval;

		public struct UsedSpells
		{
			public const uint SPELL_SHAMAN_EARTHQUAKE_DAMAGE = 77478;
			public const uint SPELL_SHAMAN_EARTHQUAKE_STUN = 77505;
		}

		public at_sha_earthquake_totem(AreaTrigger areatrigger) : base(areatrigger)
		{
			timeInterval = 200;
		}

		public override void OnUpdate(uint p_Time)
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			if (!caster.ToPlayer())
				return;

			// Check if we can handle actions
			timeInterval += (int)p_Time;

			if (timeInterval < 1000)
				return;

			var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(200));

			if (tempSumm != null)
			{
				tempSumm.SetFaction(caster.GetFaction());
				tempSumm.SetSummonerGUID(caster.GetGUID());
				PhasingHandler.InheritPhaseShift(tempSumm, caster);

				tempSumm.CastSpell(caster,
				                   UsedSpells.SPELL_SHAMAN_EARTHQUAKE_DAMAGE,
				                   new CastSpellExtraArgs(TriggerCastFlags.FullMask)
					                   .AddSpellMod(SpellValueMod.BasePoint0, (int)(caster.GetTotalSpellPowerValue(SpellSchoolMask.Normal, false) * 0.3)));
			}

			timeInterval -= 1000;
		}
	}
}