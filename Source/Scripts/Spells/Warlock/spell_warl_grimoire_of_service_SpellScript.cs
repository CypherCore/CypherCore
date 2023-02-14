// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
	[SpellScript(new uint[]
	             {
		             111859, 111895, 111896, 111897, 111898
	             })]
	public class spell_warl_grimoire_of_service_SpellScript : SpellScript, ISpellOnSummon
	{
		public enum eServiceSpells
		{
			SPELL_IMP_SINGE_MAGIC = 89808,
			SPELL_VOIDWALKER_SUFFERING = 17735,
			SPELL_SUCCUBUS_SEDUCTION = 6358,
			SPELL_FELHUNTER_SPELL_LOCK = 19647,
			SPELL_FELGUARD_AXE_TOSS = 89766
		}

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_FELGUARD_AXE_TOSS, Difficulty.None) != null ||
			       SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, Difficulty.None) != null ||
			       SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_IMP_SINGE_MAGIC, Difficulty.None) != null ||
			       SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, Difficulty.None) != null ||
			       SpellManager.Instance.GetSpellInfo((uint)eServiceSpells.SPELL_VOIDWALKER_SUFFERING, Difficulty.None) != null;
		}

		public void OnSummon(Creature creature)
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();

			if (caster == null ||
			    creature == null ||
			    target == null)
				return;

			switch (GetSpellInfo().Id)
			{
				case WarlockSpells.GRIMOIRE_IMP: // Imp
					creature.CastSpell(caster, (uint)eServiceSpells.SPELL_IMP_SINGE_MAGIC, true);

					break;
				case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
					creature.CastSpell(target, (uint)eServiceSpells.SPELL_VOIDWALKER_SUFFERING, true);

					break;
				case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
					creature.CastSpell(target, (uint)eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, true);

					break;
				case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
					creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, true);

					break;
				case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
					creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELGUARD_AXE_TOSS, true);

					break;
			}
		}
	}
}