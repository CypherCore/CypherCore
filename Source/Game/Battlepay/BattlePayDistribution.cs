using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    public enum BattlePayDistribution
    {
        // character boost
        CHARACTER_BOOST = 2,
        CHARACTER_BOOST_ALLOW = 1,
        CHARACTER_BOOST_CHOOSED = 2,
        CHARACTER_BOOST_ITEMS = 3,
        CHARACTER_BOOST_APPLIED = 4,
        CHARACTER_BOOST_TEXT_ID = 88,
        CHARACTER_BOOST_SPEC_MASK = 0xFFF,
        CHARACTER_BOOST_FACTION_ALLIANCE = 0x1000000

    }
}
