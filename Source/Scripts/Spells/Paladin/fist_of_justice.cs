using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.AI.SmartAction;

namespace Scripts.Spells.Paladin
{
    //234299
    [Script]
    public class fist_of_justice : ScriptObjectAutoAdd, IPlayerOnModifyPower
    {
        public fist_of_justice() : base("fist_of_justice")
        {
        }

        public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
        {
            if (player.GetClass() != Class.Paladin)
            {
                return;
            }

            if (!player.HasAura(PaladinSpells.SPELL_PALADIN_FIST_OF_JUSTICE))
            {
                return;
            }

            if (player.GetPowerType() == PowerType.HolyPower)
            {
                if (newValue < oldValue)
                {
                    if (player.HasAura(PaladinSpells.SPELL_PALADIN_FIST_OF_JUSTICE))
                    {
                        player.GetSpellHistory().ModifyCooldown(PaladinSpells.SPELL_PALADIN_HAMMER_OF_JUSTICE, TimeSpan.FromSeconds(-2));
                    }
                }
            }
        }
    }
}
