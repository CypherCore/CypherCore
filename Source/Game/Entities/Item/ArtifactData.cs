// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    internal class ArtifactData
    {
        public uint ArtifactAppearanceId { get; set; }
        public List<ArtifactPowerData> ArtifactPowers { get; set; } = new();
        public uint ArtifactTierId { get; set; }
        public ulong Xp { get; set; }
    }
}