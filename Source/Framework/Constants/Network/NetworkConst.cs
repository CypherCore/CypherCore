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

namespace Framework.Constants
{
    // Player state
    public enum SessionStatus
    {
        Authed = 0,                                      // Player authenticated (_player == NULL, m_playerRecentlyLogout = false or will be reset before handler call, m_GUID have garbage)
        Loggedin,                                        // Player in game (_player != NULL, m_GUID == _player->GetGUID(), inWorld())
        Transfer,                                        // Player transferring to another map (_player != NULL, m_GUID == _player->GetGUID(), !inWorld())
        LoggedinOrRecentlyLogout,                    // _player != NULL or _player == NULL && m_playerRecentlyLogout && m_playerLogout, m_GUID store last _player guid)
    }

    public enum PacketProcessing
    {
        Inplace = 0,             //process packet whenever we receive it - mostly for non-handled or non-implemented packets
        ThreadUnsafe,            //packet is not thread-safe - process it in World.UpdateSessions()
        ThreadSafe               //packet is thread-safe - process it in Map.Update()
    }
}
