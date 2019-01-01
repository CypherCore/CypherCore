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

using Framework.Configuration;
using Framework.Rest;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace BNetServer
{
    public class SessionManager : Singleton<SessionManager>
    {
        SessionManager()
        {
            _formInputs = new FormInputs();
        }

        public bool Initialize()
        {
            int _port = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);
            if (_port < 0 || _port > 0xFFFF)
            {
                Log.outError(LogFilter.Network, "Specified login service port ({0}) out of allowed range (1-65535), defaulting to 8081", _port);
                _port = 8081;
            }

            string configuredAddress = ConfigMgr.GetDefaultValue("LoginREST.ExternalAddress", "127.0.0.1");
            IPAddress address;
            if (!IPAddress.TryParse(configuredAddress, out address))
            {
                Log.outError(LogFilter.Network, "Could not resolve LoginREST.ExternalAddress {0}", configuredAddress);
                return false;
            }
            _externalAddress = new IPEndPoint(address, _port);

            configuredAddress = ConfigMgr.GetDefaultValue("LoginREST.LocalAddress", "127.0.0.1");
            if (!IPAddress.TryParse(configuredAddress, out address))
            {
                Log.outError(LogFilter.Network, "Could not resolve LoginREST.ExternalAddress {0}", configuredAddress);
                return false;
            }

            _localAddress = new IPEndPoint(address, _port);

            // set up form inputs 
            _formInputs.Type = "LOGIN_FORM";

            var input = new FormInput();
            input.Id = "account_name";
            input.Type = "text";
            input.Label = "E-mail";
            input.MaxLength = 320;
            _formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "password";
            input.Type = "password";
            input.Label = "Password";
            input.MaxLength = 16;
            _formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "log_in_submit";
            input.Type = "submit";
            input.Label = "Log In";
            _formInputs.Inputs.Add(input);

            _certificate = new X509Certificate2("BNetServer.pfx");

            return true;
        }

        public IPEndPoint GetAddressForClient(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return _localAddress;

            return _externalAddress;
        }

        public FormInputs GetFormInput()
        {
            return _formInputs;
        }

        public X509Certificate2 GetCertificate()
        {
            return _certificate;
        }

        FormInputs _formInputs;
        IPEndPoint _externalAddress;
        IPEndPoint _localAddress;
        X509Certificate2 _certificate;
    }

    public enum BanMode
    {
        Ip = 0,
        Account = 1
    }
}
