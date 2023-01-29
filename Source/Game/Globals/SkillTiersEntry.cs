// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public class SkillTiersEntry
    {
        public uint Id { get; set; }
        public uint[] Value { get; set; } = new uint[SkillConst.MaxSkillStep];
    }
}