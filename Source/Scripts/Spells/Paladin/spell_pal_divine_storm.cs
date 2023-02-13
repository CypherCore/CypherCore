using Game.DataStorage;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.AI.SmartAction;

namespace Scripts.Spells.Paladin
{
    [SpellScript(224239)] // 224239 - Divine Storm
    internal class spell_pal_divine_storm : SpellScript, ISpellOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualKitStorage.HasRecord(PaladinSpellVisualKit.DivineStorm);
        }

        public void OnCast()
        {
            GetCaster().SendPlaySpellVisualKit(PaladinSpellVisualKit.DivineStorm, 0, 0);
        }
    }
}
