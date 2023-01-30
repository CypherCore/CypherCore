// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class SmoothPhasingInfo
    {
        // Fields visible on client
        public ObjectGuid? ReplaceObject;

        public SmoothPhasingInfo(ObjectGuid replaceObject, bool replaceActive, bool stopAnimKits)
        {
            ReplaceObject = replaceObject;
            ReplaceActive = replaceActive;
            StopAnimKits = stopAnimKits;
        }

        // Serverside fields
        public bool Disabled { get; set; } = false;

        public bool ReplaceActive { get; set; } = true;
        public bool StopAnimKits { get; set; } = true;
    }
}