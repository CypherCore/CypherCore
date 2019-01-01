/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Framework.Database;
using System.Collections.Generic;

namespace Game.DataStorage
{
    public class CharacterTemplateDataStorage : Singleton<CharacterTemplateDataStorage>
    {
        CharacterTemplateDataStorage() { }

        public void LoadCharacterTemplates()
        {
            uint oldMSTime = Time.GetMSTime();
            _characterTemplateStore.Clear();

            MultiMap<uint, CharacterTemplateClass> characterTemplateClasses = new MultiMap<uint, CharacterTemplateClass>();
            SQLResult classesResult = DB.World.Query("SELECT TemplateId, FactionGroup, Class FROM character_template_class");
            if (!classesResult.IsEmpty())
            {
                do
                {
                    uint templateId = classesResult.Read<uint>(0);
                    FactionMasks factionGroup = (FactionMasks)classesResult.Read<byte>(1);
                    byte classID = classesResult.Read<byte>(2);

                    if (!((factionGroup & (FactionMasks.Player | FactionMasks.Alliance)) == (FactionMasks.Player | FactionMasks.Alliance)) &&
                        !((factionGroup & (FactionMasks.Player | FactionMasks.Horde)) == (FactionMasks.Player | FactionMasks.Horde)))
                    {
                        Log.outError(LogFilter.Sql, "Faction group {0} defined for character template {1} in `character_template_class` is invalid. Skipped.", factionGroup, templateId);
                        continue;
                    }

                    if (!CliDB.ChrClassesStorage.ContainsKey(classID))
                    {
                        Log.outError(LogFilter.Sql, "Class {0} defined for character template {1} in `character_template_class` does not exists, skipped.", classID, templateId);
                        continue;
                    }

                    characterTemplateClasses.Add(templateId, new CharacterTemplateClass(factionGroup, classID));
                }
                while (classesResult.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 character template classes. DB table `character_template_class` is empty.");
            }

            SQLResult templates = DB.World.Query("SELECT Id, Name, Description, Level FROM character_template");
            if (templates.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 character templates. DB table `character_template` is empty.");
                return;
            }

            do
            {
                CharacterTemplate templ = new CharacterTemplate();
                templ.TemplateSetId = templates.Read<uint>(0);
                templ.Name = templates.Read<string>(1);
                templ.Description = templates.Read<string>(2);
                templ.Level = templates.Read<byte>(3);
                templ.Classes = characterTemplateClasses[templ.TemplateSetId];

                if (templ.Classes.Empty())
                {
                    Log.outError(LogFilter.Sql, "Character template {0} does not have any classes defined in `character_template_class`. Skipped.", templ.TemplateSetId);
                    continue;
                }

                _characterTemplateStore[templ.TemplateSetId] = templ;
            }
            while (templates.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} character templates in {1} ms.", _characterTemplateStore.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public Dictionary<uint, CharacterTemplate> GetCharacterTemplates()
        {
            return _characterTemplateStore;
        }

        public CharacterTemplate GetCharacterTemplate(uint templateId)
        {
            return _characterTemplateStore.LookupByKey(templateId);
        }

        Dictionary<uint, CharacterTemplate> _characterTemplateStore = new Dictionary<uint, CharacterTemplate>();
    }

    public struct CharacterTemplateClass
    {
        public CharacterTemplateClass(FactionMasks factionGroup, byte classID)
        {
            FactionGroup = factionGroup;
            ClassID = classID;
        }

        public FactionMasks FactionGroup;
        public byte ClassID;
    }

    public class CharacterTemplate
    {
        public uint TemplateSetId;
        public List<CharacterTemplateClass> Classes;
        public string Name;
        public string Description;
        public byte Level;
    }
}
