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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Framework.Rest
{
    [DataContract]
    public class LogonData
    {
        public string this[string inputId] => Inputs.SingleOrDefault(i => i.Id == inputId)?.Value;

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "program_id")]
        public string Program { get; set; }

        [DataMember(Name = "platform_id")]
        public string Platform { get; set; }

        [DataMember(Name = "inputs")]
        public List<FormInputValue> Inputs { get; set; } = new List<FormInputValue>();
    }
}
