using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using static System.Net.Mime.MediaTypeNames;

namespace Scripts.Spells.Items
{
    [SpellScript(42545)]
    internal class runic_mana_injector : SpellScript, ISpellEnergizedBySpell
    {
        public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
        {
            Player player = target.ToPlayer();
            if (player != null)
                if (player.HasSkill(SkillType.Engineering))
                    MathFunctions.AddPct(ref amount, 25);
        }
    }
}
