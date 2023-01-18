// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    // Player state
    public enum SessionStatus
    {
        Authed = 0,                                      // Player authenticated (_player == NULL, m_playerRecentlyLogout = false or will be reset before handler call, m_GUID have garbage)
        Loggedin,                                        // Player in game (_player != NULL, m_GUID == _player.GetGUID(), inWorld())
        Transfer,                                        // Player transferring to another map (_player != NULL, m_GUID == _player.GetGUID(), !inWorld())
        LoggedinOrRecentlyLogout,                    // _player != NULL or _player == NULL && m_playerRecentlyLogout && m_playerLogout, m_GUID store last _player guid)
    }

    public enum PacketProcessing
    {
        Inplace = 0,             //process packet whenever we receive it - mostly for non-handled or non-implemented packets
        ThreadUnsafe,            //packet is not thread-safe - process it in World.UpdateSessions()
        ThreadSafe               //packet is thread-safe - process it in Map.Update()
    }
}
