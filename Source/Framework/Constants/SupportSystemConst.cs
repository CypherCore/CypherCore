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
    public enum GMTicketSystemStatus
    {
        Disabled = 0,
        Enabled = 1
    }

    public enum GMSupportComplaintType
    {
        None = 0,
        Language = 2,
        PlayerName = 4,
        Cheat = 15,
        GuildName = 23,
        Spamming = 24
    }

    public enum SupportSpamType
    {
        Mail = 0,
        Chat = 1,
        Calendar = 2
    }
}
