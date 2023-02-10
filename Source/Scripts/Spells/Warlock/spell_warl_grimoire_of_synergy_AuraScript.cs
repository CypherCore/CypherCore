using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// Grimoire of Synergy - 171975
	[SpellScript(171975, "spell_warl_grimoire_of_synergy")]
	public class spell_warl_grimoire_of_synergy_AuraScript : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var actor = eventInfo.GetActor();

			if (actor == null)
				return false;

			if (actor.IsPet() ||
			    actor.IsGuardian())
			{
				var owner = actor.GetOwner();

				if (owner == null)
					return false;

				if (RandomHelper.randChance(10))
					owner.CastSpell(owner, WarlockSpells.GRIMOIRE_OF_SYNERGY_BUFF, true);

				return true;
			}

			var player = actor.ToPlayer();

			if (actor.ToPlayer())
			{
				var guardian = player.GetGuardianPet();

				if (guardian == null)
					return false;

				if (RandomHelper.randChance(10))
					player.CastSpell(guardian, WarlockSpells.GRIMOIRE_OF_SYNERGY_BUFF, true);

				return true;
			}

			return false;
		}
	}
}