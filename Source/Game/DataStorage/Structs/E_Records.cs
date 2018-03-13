/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

namespace Game.DataStorage
{
    public sealed class EmotesRecord
    {
        public uint Id;
        public long RaceMask;
        public uint EmoteSlashCommand;
        public uint EmoteFlags;
        public uint SpellVisualKitID;
        public ushort AnimID;
        public byte EmoteSpecProc;
        public int ClassMask;
        public byte EmoteSpecProcParam;
        public ushort EmoteSoundID;
    }

    public sealed class EmotesTextRecord
    {
        public uint Id;
        public string Name;
        public ushort EmoteID;
    }

    public sealed class EmotesTextSoundRecord
    {
        public uint Id;
        public byte RaceId;
        public byte SexId;
        public byte ClassId;
        public uint SoundId;
        public uint EmotesTextId;
    }
}
