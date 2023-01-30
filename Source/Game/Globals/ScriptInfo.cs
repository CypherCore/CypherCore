// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ScriptInfo
    {
        [FieldOffset(0)] public ScriptsType type;

        [FieldOffset(4)] public uint id;

        [FieldOffset(8)] public uint delay;

        [FieldOffset(12)] public ScriptCommands command;

        [FieldOffset(16)] public raw Raw;

        [FieldOffset(16)] public talk Talk;

        [FieldOffset(16)] public emote Emote;

        [FieldOffset(16)] public fieldset FieldSet;

        [FieldOffset(16)] public moveto MoveTo;

        [FieldOffset(16)] public flagtoggle FlagToggle;

        [FieldOffset(16)] public teleportto TeleportTo;

        [FieldOffset(16)] public questexplored QuestExplored;

        [FieldOffset(16)] public killcredit KillCredit;

        [FieldOffset(16)] public respawngameobject RespawnGameObject;

        [FieldOffset(16)] public tempsummoncreature TempSummonCreature;

        [FieldOffset(16)] public toggledoor ToggleDoor;

        [FieldOffset(16)] public removeaura RemoveAura;

        [FieldOffset(16)] public castspell CastSpell;

        [FieldOffset(16)] public playsound PlaySound;

        [FieldOffset(16)] public createitem CreateItem;

        [FieldOffset(16)] public despawnself DespawnSelf;

        [FieldOffset(16)] public loadpath LoadPath;

        [FieldOffset(16)] public callscript CallScript;

        [FieldOffset(16)] public kill Kill;

        [FieldOffset(16)] public orientation Orientation;

        [FieldOffset(16)] public equip Equip;

        [FieldOffset(16)] public model Model;

        [FieldOffset(16)] public playmovie PlayMovie;

        [FieldOffset(16)] public movement Movement;

        [FieldOffset(16)] public playanimkit PlayAnimKit;

        public string GetDebugInfo()
        {
            return $"{command} ('{Global.ObjectMgr.GetScriptsTableNameByType(type)}' script Id: {id})";
        }

        #region Structs

        public unsafe struct raw
        {
            public fixed uint nData[3];
            public fixed float fData[4];
        }

        public struct talk // TALK (0)
        {
            public ChatMsg ChatType;   // datalong
            public eScriptFlags Flags; // datalong2
            public int TextID;         // dataint
        }

        public struct emote // EMOTE (1)
        {
            public uint EmoteID;       // datalong
            public eScriptFlags Flags; // datalong2
        }

        public struct fieldset // FIELDSET (2)
        {
            public uint FieldID;    // datalong
            public uint FieldValue; // datalong2
        }

        public struct moveto // MOVETO (3)
        {
            public uint Unused1;    // datalong
            public uint TravelTime; // datalong2
            public int Unused2;     // dataint

            public float DestX;
            public float DestY;
            public float DestZ;
        }

        public struct flagtoggle // FLAGSET (4)
                                 // FLAGREMOVE (5)
        {
            public uint FieldID;    // datalong
            public uint FieldValue; // datalong2
        }

        public struct teleportto // TELEPORTTO (6)
        {
            public uint MapID;         // datalong
            public eScriptFlags Flags; // datalong2
            public int Unused1;        // dataint

            public float DestX;
            public float DestY;
            public float DestZ;
            public float Orientation;
        }

        public struct questexplored // QUESTEXPLORED (7)
        {
            public uint QuestID;  // datalong
            public uint Distance; // datalong2
        }

        public struct killcredit // KILLCREDIT (8)
        {
            public uint CreatureEntry; // datalong
            public eScriptFlags Flags; // datalong2
        }

        public struct respawngameobject // RESPAWNGAMEOBJECT (9)
        {
            public uint GOGuid;       // datalong
            public uint DespawnDelay; // datalong2
        }

        public struct tempsummoncreature // TEMPSUMMONCREATURE (10)
        {
            public uint CreatureEntry; // datalong
            public uint DespawnDelay;  // datalong2
            public int Unused1;        // dataint

            public float PosX;
            public float PosY;
            public float PosZ;
            public float Orientation;
        }

        public struct toggledoor // CLOSEDOOR (12)
                                 // OPENDOOR (11)
        {
            public uint GOGuid;     // datalong
            public uint ResetDelay; // datalong2
        }

        // ACTIVATEOBJECT (13)

        public struct removeaura // REMOVEAURA (14)
        {
            public uint SpellID;       // datalong
            public eScriptFlags Flags; // datalong2
        }

        public struct castspell // CASTSPELL (15)
        {
            public uint SpellID;       // datalong
            public eScriptFlags Flags; // datalong2
            public int CreatureEntry;  // dataint

            public float SearchRadius;
        }

        public struct playsound // PLAYSOUND (16)
        {
            public uint SoundID;       // datalong
            public eScriptFlags Flags; // datalong2
        }

        public struct createitem // CREATEITEM (17)
        {
            public uint ItemEntry; // datalong
            public uint Amount;    // datalong2
        }

        public struct despawnself // DESPAWNSELF (18)
        {
            public uint DespawnDelay; // datalong
        }

        public struct loadpath // LOADPATH (20)
        {
            public uint PathID;       // datalong
            public uint IsRepeatable; // datalong2
        }

        public struct callscript // CALLSCRIPTTOUNIT (21)
        {
            public uint CreatureEntry; // datalong
            public uint ScriptID;      // datalong2
            public uint ScriptType;    // dataint
        }

        public struct kill // KILL (22)
        {
            public uint Unused1;     // datalong
            public uint Unused2;     // datalong2
            public int RemoveCorpse; // dataint
        }

        public struct orientation // ORIENTATION (30)
        {
            public eScriptFlags Flags; // datalong
            public uint Unused1;       // datalong2
            public int Unused2;        // dataint

            public float Unused3;
            public float Unused4;
            public float Unused5;
            public float _Orientation;
        }

        public struct equip // EQUIP (31)
        {
            public uint EquipmentID; // datalong
        }

        public struct model // MODEL (32)
        {
            public uint ModelID; // datalong
        }

        // CLOSEGOSSIP (33)

        public struct playmovie // PLAYMOVIE (34)
        {
            public uint MovieID; // datalong
        }

        public struct movement // SCRIPT_COMMAND_MOVEMENT (35)
        {
            public uint MovementType;     // datalong
            public uint MovementDistance; // datalong2
            public int Path;              // dataint
        }

        public struct playanimkit // SCRIPT_COMMAND_PLAY_ANIMKIT (36)
        {
            public uint AnimKitID; // datalong
        }

        #endregion
    }
}