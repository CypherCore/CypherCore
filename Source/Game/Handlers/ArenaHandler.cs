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
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        void SendNotInArenaTeamPacket(ArenaTypes type)
        {            
            ArenaError arenaError = new ArenaError();
            arenaError.ErrorType = ArenaErrorType.NoTeam;
            arenaError.TeamSize = (byte)type;                                // team type (2=2v2, 3=3v3, 5=5v5), can be used for custom types...
            SendPacket(arenaError);
        }
    }
}
