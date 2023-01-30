// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class GameObjectTemplateAddon : GameObjectOverride
    {
        public uint AIAnimKitID { get; set; }
        public uint[] ArtKits { get; set; } = new uint[5];
        public uint Maxgold { get; set; }
        public uint Mingold { get; set; }
        public uint WorldEffectID { get; set; }
    }
}