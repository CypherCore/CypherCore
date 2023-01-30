// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public enum GameEventState
    {
        Normal = 0,          // standard game events
        WorldInactive = 1,   // not yet started
        WorldConditions = 2, // condition matching phase
        WorldNextPhase = 3,  // conditions are met, now 'length' timer to start next event
        WorldFinished = 4,   // next events are started, unapply this one
        Internal = 5         // never handled in update
    }
}