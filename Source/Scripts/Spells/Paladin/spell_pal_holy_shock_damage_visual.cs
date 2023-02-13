using Game.DataStorage;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(25912)] // 25912 - Holy Shock
    internal class spell_pal_holy_shock_damage_visual : SpellScript, ISpellAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockDamage) && CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockDamageCrit);
        }

        public void AfterHit()
        {
            GetCaster().SendPlaySpellVisual(GetHitUnit(), IsHitCrit() ? SpellVisual.HolyShockDamageCrit : SpellVisual.HolyShockDamage, 0, 0, 0.0f, false);
        }
    }
}
