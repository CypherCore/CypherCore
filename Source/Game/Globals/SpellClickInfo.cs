// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game
{
    public class SpellClickInfo
    {
        public byte CastFlags { get; set; }
        public uint SpellId { get; set; }
        public SpellClickUserTypes UserType { get; set; }

        // helpers
        public bool IsFitToRequirements(Unit clicker, Unit clickee)
        {
            Player playerClicker = clicker.ToPlayer();

            if (playerClicker == null)
                return true;

            Unit summoner = null;

            // Check summoners for party
            if (clickee.IsSummon())
                summoner = clickee.ToTempSummon().GetSummonerUnit();

            if (summoner == null)
                summoner = clickee;

            // This only applies to players
            switch (UserType)
            {
                case SpellClickUserTypes.Friend:
                    if (!playerClicker.IsFriendlyTo(summoner))
                        return false;

                    break;
                case SpellClickUserTypes.Raid:
                    if (!playerClicker.IsInRaidWith(summoner))
                        return false;

                    break;
                case SpellClickUserTypes.Party:
                    if (!playerClicker.IsInPartyWith(summoner))
                        return false;

                    break;
                default:
                    break;
            }

            return true;
        }
    }
}