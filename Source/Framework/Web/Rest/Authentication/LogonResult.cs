// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Framework.Web
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
