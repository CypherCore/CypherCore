using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
	[SpellScript(new uint[]
	             {
		             111859, 111895, 111896, 111897, 111898
	             })]
	public class spell_warl_grimoire_of_service : SpellScript, IHasSpellEffects, ISpellOnSummon
	{
		public List<ISpellEffect> SpellEffects => new();

		private struct eServiceSpells
		{
			public const uint SPELL_IMP_SINGE_MAGIC = 89808;
			public const uint SPELL_VOIDWALKER_SUFFERING = 17735;
			public const uint SPELL_SUCCUBUS_SEDUCTION = 6358;
			public const uint SPELL_FELHUNTER_SPELL_LOCK = 19647;
			public const uint SPELL_FELGUARD_AXE_TOSS = 89766;
		}

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(eServiceSpells.SPELL_FELGUARD_AXE_TOSS, Difficulty.None) != null ||
			    Global.SpellMgr.GetSpellInfo(eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, Difficulty.None) != null ||
			    Global.SpellMgr.GetSpellInfo(eServiceSpells.SPELL_IMP_SINGE_MAGIC, Difficulty.None) != null ||
			    Global.SpellMgr.GetSpellInfo(eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, Difficulty.None) != null ||
			    Global.SpellMgr.GetSpellInfo(eServiceSpells.SPELL_VOIDWALKER_SUFFERING, Difficulty.None) != null)
				return false;

			return true;
		}

		public void OnSummon(Creature creature)
		{
			var caster = GetCaster();
			var target = GetExplTargetUnit();

			if (caster == null || creature == null || target == null)
				return;

			switch (GetSpellInfo().Id)
			{
				case WarlockSpells.GRIMOIRE_IMP: // Imp
					creature.CastSpell(caster, eServiceSpells.SPELL_IMP_SINGE_MAGIC, true);

					break;
				case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
					creature.CastSpell(target, eServiceSpells.SPELL_VOIDWALKER_SUFFERING, true);

					break;
				case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
					creature.CastSpell(target, eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, true);

					break;
				case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
					creature.CastSpell(target, eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, true);

					break;
				case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
					creature.CastSpell(target, eServiceSpells.SPELL_FELGUARD_AXE_TOSS, true);

					break;
			}
		}
	}
}