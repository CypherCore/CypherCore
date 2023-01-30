// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;

namespace Game.DataStorage
{
    public interface IDB2Storage
    {
        bool HasRecord(uint id);

        void WriteRecord(uint id, Locale locale, ByteBuffer buffer);

        void EraseRecord(uint id);

        string GetName();
    }
}