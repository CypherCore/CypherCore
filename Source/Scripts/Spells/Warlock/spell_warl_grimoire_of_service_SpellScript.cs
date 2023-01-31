// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
    public class spell_warl_grimoire_of_service_SpellScript : SpellScript, IOnSummon
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

        public void HandleSummon(Creature creature)
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

            if (caster == null ||
                creature == null ||
                target == null)
                return;

            switch (GetSpellInfo().Id)
            {
                case SpellIds.GRIMOIRE_IMP: // Imp
                    creature.CastSpell(caster, (uint)eServiceSpells.SPELL_IMP_SINGE_MAGIC, true);

                    break;
                case SpellIds.GRIMOIRE_VOIDWALKER: // Voidwalker
                    creature.CastSpell(target, (uint)eServiceSpells.SPELL_VOIDWALKER_SUFFERING, true);

                    break;
                case SpellIds.GRIMOIRE_SUCCUBUS: // Succubus
                    creature.CastSpell(target, (uint)eServiceSpells.SPELL_SUCCUBUS_SEDUCTION, true);

                    break;
                case SpellIds.GRIMOIRE_FELHUNTER: // Felhunter
                    creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELHUNTER_SPELL_LOCK, true);

                    break;
                case SpellIds.GRIMOIRE_FELGUARD: // Felguard
                    creature.CastSpell(target, (uint)eServiceSpells.SPELL_FELGUARD_AXE_TOSS, true);

                    break;
            }
        }
    }
}
