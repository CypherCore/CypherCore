// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Framework.Web
{
    public class RealmListUpdate
    {
        [JsonPropertyName("update")]
        public RealmEntry Update { get; set; } = new RealmEntry();

        [JsonPropertyName("deleting")]
        public bool Deleting { get; set; }
    }
}
