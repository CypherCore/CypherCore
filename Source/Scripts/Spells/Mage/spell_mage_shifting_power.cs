using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage
{
    internal class spell_mage_shifting_power : SpellScript, IHasSpellEffects
    {
        // The amount of cooldown reduction in seconds
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MageSpells.ShiftingPower);
        }
        public static void HandleSpellEffect(Player caster)
        {

            // Get a list of all spells with a cooldown - Need method to grab all spells with a cooldown, not sure how to implement currently
            List<Spell> spellsOnCooldown = new List<Spell>();

            // Reduce the cooldown of each spell by the specified amount, once the list is made this loop will reduce all of their Recovery Times by 3 seconds due to the -3000 base points in the spell ID 382440
            foreach (Spell spell in spellsOnCooldown)
            {
                //spellInfo.RecoveryTime += ShiftingPower.GetSpellInfo().GetEffect(1).BasePoints() * Time.InMilliseconds;
            }
        }
    }
}
