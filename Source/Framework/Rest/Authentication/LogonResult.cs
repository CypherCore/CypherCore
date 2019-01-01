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

using System.Runtime.Serialization;

namespace Framework.Rest
{
    [DataContract]
    public class LogonResult
    {
        [DataMember(Name = "authentication_state")]
        public string AuthenticationState { get; set; }

        [DataMember(Name = "login_ticket")]
        public string LoginTicket { get; set; }

        [DataMember(Name = "error_code")]
        public string ErrorCode { get; set; }

        [DataMember(Name = "error_message")]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "support_error_code")]
        public string SupportErrorCode { get; set; }

        [DataMember(Name = "authenticator_form")]
        public FormInputs AuthenticatorForm { get; set; } = new FormInputs();
    }

    public enum AuthenticationState
    {
        NONE = 0,
        LOGIN = 1,
        LEGAL = 2,
        AUTHENTICATOR = 3,
        DONE = 4,
    }
}
