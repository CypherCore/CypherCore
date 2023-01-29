// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public struct ScriptAction
    {
        public ObjectGuid OwnerGUID;

        // owner of source if source is Item
        public ScriptInfo Script;

        public ObjectGuid SourceGUID;
        public ObjectGuid TargetGUID;
    }
}