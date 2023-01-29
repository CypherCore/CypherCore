// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public class SceneTemplate
    {
        public bool Encrypted { get; set; }
        public SceneFlags PlaybackFlags { get; set; }
        public uint SceneId { get; set; }
        public uint ScenePackageId { get; set; }
        public uint ScriptId { get; set; }
    }
}