using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Undulation
    // 8004 Healing Surge
    // 77472 Healing Wave
    [SpellScript(new uint[] { 8004, 77472 })]
    public class spell_sha_undulation : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            var variableStore = GetCaster().VariableStorage;
            int count = variableStore.GetValue("spell_sha_undulation", 0);

            if (count >= 3)
            {
                variableStore.Remove("spell_sha_undulation");
                GetCaster().CastSpell(null, ShamanSpells.SPELL_SHAMAN_UNDULATION_PROC, true);
            }
            else
            {
                variableStore.Set("spell_sha_undulation", count + 1);
                GetCaster().RemoveAurasDueToSpell(ShamanSpells.SPELL_SHAMAN_UNDULATION_PROC);
            }
        }
    }
}
