/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.LogoutRequest)]
        void HandleLogoutRequest(LogoutRequest packet)
        {
            Player pl = GetPlayer();
            if (!GetPlayer().GetLootGUID().IsEmpty())
                GetPlayer().SendLootReleaseAll();

            bool instantLogout = (pl.HasFlag(PlayerFields.Flags, PlayerFlags.Resting) && !pl.IsInCombat() ||
                pl.IsInFlight() || HasPermission(RBACPermissions.InstantLogout));

            bool canLogoutInCombat = pl.HasFlag(PlayerFields.Flags, PlayerFlags.Resting);

            int reason = 0;
            if (pl.IsInCombat() && !canLogoutInCombat)
                reason = 1;
            else if (pl.IsFalling())
                reason = 3;                                         // is jumping or falling
            else if (pl.duel != null || pl.HasAura(9454)) // is dueling or frozen by GM via freeze command
                reason = 2;                                         // FIXME - Need the correct value

            LogoutResponse logoutResponse = new LogoutResponse();
            logoutResponse.LogoutResult = reason;
            logoutResponse.Instant = instantLogout;
            SendPacket(logoutResponse);

            if (reason != 0)
            {
                SetLogoutStartTime(0);
                return;
            }

            // instant logout in taverns/cities or on taxi or for admins, gm's, mod's if its enabled in worldserver.conf
            if (instantLogout)
            {
                LogoutPlayer(true);
                return;
            }

            // not set flags if player can't free move to prevent lost state at logout cancel
            if (pl.CanFreeMove())
            {
                if (pl.GetStandState() == UnitStandStateType.Stand)
                    pl.SetStandState(UnitStandStateType.Sit);
                pl.SetRooted(true);
                pl.SetFlag(UnitFields.Flags, UnitFlags.Stunned);
            }

            SetLogoutStartTime(Time.UnixTime);
        }

        [WorldPacketHandler(ClientOpcodes.LogoutCancel)]
        void HandleLogoutCancel(LogoutCancel packet)
        {
            // Player have already logged out serverside, too late to cancel
            if (!GetPlayer())
                return;

            SetLogoutStartTime(0);

            SendPacket(new LogoutCancelAck());

            // not remove flags if can't free move - its not set in Logout request code.
            if (GetPlayer().CanFreeMove())
            {
                //!we can move again
                GetPlayer().SetRooted(false);

                //! Stand Up
                GetPlayer().SetStandState(UnitStandStateType.Stand);

                //! DISABLE_ROTATE
                GetPlayer().RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);
            }
        }
    }
}
