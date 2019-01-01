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

using Framework.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Database
{
    public class DatabaseUpdater<T>
    {
        public DatabaseUpdater(MySqlBase<T> database)
        {
            _database = database;
        }

        public bool Populate()
        {
            SQLResult result = _database.Query("SHOW TABLES");
            if (!result.IsEmpty() && !result.IsEmpty())
                return true;

            Log.outInfo(LogFilter.SqlUpdates, $"Database {_database.GetDatabaseName()} is empty, auto populating it...");

            string path = GetSourceDirectory();
            string fileName = "Unknown";
            switch (_database.GetType().Name)
            {
                case "LoginDatabase":
                    fileName = @"/sql/base/auth_database.sql";
                    break;
                case "CharacterDatabase":
                    fileName = @"/sql/base/characters_database.sql";
                    break;
                case "WorldDatabase":
                    fileName = @"/sql/TDB_world_735.00_2018_02_19.sql";
                    break;
                case "HotfixDatabase":
                    fileName = @"/sql/TDB_hotfixes_735.00_2018_02_19.sql";
                    break;
            }

            if (!File.Exists(path + fileName))
            {
                Log.outError(LogFilter.SqlUpdates, $"File \"{path + fileName}\" is missing, download it from \"http://www.trinitycore.org/f/files/category/1-database/\"" +
                    " and place it in your sql directory.");
                return false;
            }

            // Update database
            Log.outInfo(LogFilter.SqlUpdates, $"Applying \'{fileName}\'...");
            _database.ApplyFile(path + fileName);

            Log.outInfo(LogFilter.SqlUpdates, $"Done Applying \'{fileName}\'");
            return true;
        }

        public bool Update()
        {
            Log.outInfo(LogFilter.SqlUpdates, $"Updating {_database.GetDatabaseName()} database...");

            string sourceDirectory = GetSourceDirectory();

            if (!Directory.Exists(sourceDirectory))
            {
                Log.outError(LogFilter.SqlUpdates, $"DBUpdater: Given source directory {sourceDirectory} does not exist, skipped!");
                return false;
            }

            var availableFiles = GetFileList();
            var appliedFiles = ReceiveAppliedFiles();

            bool redundancyChecks = ConfigMgr.GetDefaultValue("Updates.Redundancy", true);
            bool archivedRedundancy = ConfigMgr.GetDefaultValue("Updates.Redundancy", true);

            UpdateResult result = new UpdateResult();

            // Count updates
            foreach (var entry in appliedFiles)
            {
                if (entry.Value.State == State.RELEASED)
                    ++result.recent;
                else
                    ++result.archived;
            }

            foreach (var availableQuery in availableFiles)
            {
                Log.outDebug(LogFilter.SqlUpdates, $"Checking update \"{availableQuery.GetFileName()}\"...");

                var applied = appliedFiles.LookupByKey(availableQuery.GetFileName());
                if (applied != null)
                {
                    // If redundancy is disabled skip it since the update is already applied.
                    if (!redundancyChecks)
                    {
                        Log.outDebug(LogFilter.SqlUpdates, "Update is already applied, skipping redundancy checks.");
                        appliedFiles.Remove(availableQuery.GetFileName());
                        continue;
                    }

                    // If the update is in an archived directory and is marked as archived in our database skip redundancy checks (archived updates never change).
                    if (!archivedRedundancy && (applied.State == State.ARCHIVED) && (availableQuery.state == State.ARCHIVED))
                    {
                        Log.outDebug(LogFilter.SqlUpdates, "Update is archived and marked as archived in database, skipping redundancy checks.");
                        appliedFiles.Remove(availableQuery.GetFileName());
                        continue;
                    }
                }

                // Calculate hash
                string hash = CalculateHash(availableQuery.path);

                UpdateMode mode = UpdateMode.Apply;

                // Update is not in our applied list
                if (applied == null)
                {
                    // Catch renames (different filename but same hash)
                    var hashIter = appliedFiles.Values.FirstOrDefault(p => p.Hash == hash);
                    if (hashIter != null)
                    {
                        // Check if the original file was removed if not we've got a problem.
                        var renameFile = availableFiles.Find(p => p.GetFileName() == hashIter.Name);
                        if (renameFile != null)
                        {
                            Log.outWarn(LogFilter.SqlUpdates, $"Seems like update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\' was renamed, but the old file is still there! " +
                                $"Trade it as a new file! (Probably its an unmodified copy of file \"{renameFile.GetFileName()}\")");
                        }
                        // Its save to trade the file as renamed here
                        else
                        {
                            Log.outInfo(LogFilter.SqlUpdates, $"Renaming update \"{hashIter.Name}\" to \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'.");

                            RenameEntry(hashIter.Name, availableQuery.GetFileName());
                            appliedFiles.Remove(hashIter.Name);
                            continue;
                        }
                    }
                    // Apply the update if it was never seen before.
                    else
                    {
                        Log.outInfo(LogFilter.SqlUpdates, $"Applying update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'...");
                    }
                }
                // Rehash the update entry if it is contained in our database but with an empty hash.
                else if (ConfigMgr.GetDefaultValue("Updates.AllowRehash", true) && string.IsNullOrEmpty(applied.Hash))
                {
                    mode = UpdateMode.Rehash;

                    Log.outInfo(LogFilter.SqlUpdates, $"Re-hashing update \"{availableQuery.GetFileName()}\" \'{hash.Substring(0, 7)}\'...");
                }
                else
                {
                    // If the hash of the files differs from the one stored in our database reapply the update (because it was changed).
                    if (applied.Hash != hash && applied.State != State.ARCHIVED)
                    {
                        Log.outInfo(LogFilter.SqlUpdates, $"Reapplying update \"{availableQuery.GetFileName()}\" \'{applied.Hash.Substring(0, 7)}\' . \'{hash.Substring(0, 7)}\' (it changed)...");
                    }
                    else
                    {
                        // If the file wasn't changed and just moved update its state if necessary.
                        if (applied.State != availableQuery.state)
                        {
                            Log.outDebug(LogFilter.SqlUpdates, $"Updating state of \"{availableQuery.GetFileName()}\" to \'{availableQuery.state}\'...");

                            UpdateState(availableQuery.GetFileName(), availableQuery.state);
                        }

                        Log.outDebug(LogFilter.SqlUpdates, $"Update is already applied and is matching hash \'{hash.Substring(0, 7)}\'.");

                        appliedFiles.Remove(applied.Name);
                        continue;
                    }
                }

                uint speed = 0;
                AppliedFileEntry file = new AppliedFileEntry(availableQuery.GetFileName(), hash, availableQuery.state, 0);

                switch (mode)
                {
                    case UpdateMode.Apply:
                        speed = ApplyTimedFile(availableQuery.path);
                        goto case UpdateMode.Rehash;
                    case UpdateMode.Rehash:
                        UpdateEntry(file, speed);
                        break;
                }

                if (applied != null)
                    appliedFiles.Remove(applied.Name);

                if (mode == UpdateMode.Apply)
                    ++result.updated;
            }

            // Cleanup up orphaned entries if enabled
            if (!appliedFiles.Empty())
            {
                int cleanDeadReferencesMaxCount = ConfigMgr.GetDefaultValue("Updates.CleanDeadRefMaxCount", 3);
                bool doCleanup = (cleanDeadReferencesMaxCount < 0) || (appliedFiles.Count <= cleanDeadReferencesMaxCount);

                foreach (var entry in appliedFiles)
                {
                    Log.outWarn(LogFilter.SqlUpdates, $"File \'{entry.Key}\' was applied to the database but is missing in your update directory now!");

                    if (doCleanup)
                        Log.outInfo(LogFilter.SqlUpdates, $"Deleting orphaned entry \'{entry.Key}\'...");
                }

                if (doCleanup)
                    CleanUp(appliedFiles);
                else
                {
                    Log.outError(LogFilter.SqlUpdates, $"Cleanup is disabled! There are {appliedFiles.Count} dirty files that were applied to your database but are now missing in your source directory!");
                }
            }

            string info = $"Containing {result.recent} new and {result.archived} archived updates.";

            if (result.updated == 0)
                Log.outInfo(LogFilter.SqlUpdates, $"{_database.GetDatabaseName()} database is up-to-date! {info}");
            else
                Log.outInfo(LogFilter.SqlUpdates, $"Applied {result.updated} query(s). {info}");

            return true;
        }

        string GetSourceDirectory()
        {
            return ConfigMgr.GetDefaultValue("Updates.SourcePath", "../../../");
        }

        uint ApplyTimedFile(string path)
        {
            // Benchmark query speed
            uint oldMSTime = Time.GetMSTime();

            // Update database
            if (!_database.ApplyFile(path))
                Log.outError(LogFilter.Sql, $"Update: {path} Failed. You need to apply it manually");

            // Return time the query took to apply
            return Time.GetMSTimeDiffToNow(oldMSTime);
        }

        void UpdateEntry(AppliedFileEntry entry, uint speed)
        {
            string update = $"REPLACE INTO `updates` (`name`, `hash`, `state`, `speed`) VALUES (\"{entry.Name}\", \"{entry.Hash}\", \'{entry.State}\', {speed})";

            // Update database
            _database.Execute(update);
        }

        void RenameEntry(string from, string to)
        {
            // Delete target if it exists
            {
                string update = $"DELETE FROM `updates` WHERE `name`=\"{to}\"";

                // Update database
                _database.Execute(update);
            }

            // Rename
            {
                string update = $"UPDATE `updates` SET `name`=\"{to}\" WHERE `name`=\"{from}\"";

                // Update database
                _database.Execute(update);
            }
        }

        void CleanUp(Dictionary<string, AppliedFileEntry> storage)
        {
            if (storage.Empty())
                return;

            int remaining = storage.Count;
            string update = "DELETE FROM `updates` WHERE `name` IN(";

            foreach (var entry in storage)
            {
                update += $"\"{entry.Key}\"";
                if ((--remaining) > 0)
                    update += ", ";
            }

            update += ")";

            // Update database
            _database.Execute(update);
        }

        void UpdateState(string name, State state)
        {
            string update = $"UPDATE `updates` SET `state`=\'{state}\' WHERE `name`=\"{name}\"";

            // Update database
            _database.Execute(update);
        }

        List<FileEntry> GetFileList()
        {
            List<FileEntry> fileList = new List<FileEntry>();

            SQLResult result = _database.Query("SELECT `path`, `state` FROM `updates_include`");
            if (result.IsEmpty())
                return fileList;

            do
            {
                string path = result.Read<string>(0);
                if (path[0] == '$')
                    path = GetSourceDirectory() + path.Substring(1);

                if (!Directory.Exists(path))
                {
                    Log.outWarn(LogFilter.SqlUpdates, $"DBUpdater: Given update include directory \"{path}\" isn't existing, skipped!");
                    continue;
                }

                State state = result.Read<string>(1).ToEnum<State>();
                fileList.AddRange(GetFilesFromDirectory(path, state));

                Log.outDebug(LogFilter.SqlUpdates, $"Added applied file \"{path}\" from remote.");

            } while (result.NextRow());

            return fileList;
        }

        Dictionary<string, AppliedFileEntry> ReceiveAppliedFiles()
        {
            Dictionary<string, AppliedFileEntry> map = new Dictionary<string, AppliedFileEntry>();

            SQLResult result = _database.Query("SELECT `name`, `hash`, `state`, UNIX_TIMESTAMP(`timestamp`) FROM `updates` ORDER BY `name` ASC");
            if (result.IsEmpty())
                return map;

            do
            {
                AppliedFileEntry entry = new AppliedFileEntry(result.Read<string>(0), result.Read<string>(1), result.Read<string>(2).ToEnum<State>(), result.Read<ulong>(3));
                map.Add(entry.Name, entry);
            }
            while (result.NextRow());

            return map;
        }

        IEnumerable<FileEntry> GetFilesFromDirectory(string directory, State state)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(directory);
            while (queue.Count > 0)
            {
                directory = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(directory))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                string[] files = Directory.GetFiles(directory, "*.sql");
                for (int i = 0; i < files.Length; i++)
                {
                    yield return new FileEntry(files[i], state);
                }
            }
        }

        string CalculateHash(string fileName)
        {
            using (SHA1 sha1 = new SHA1Managed())
            {
                string text = File.ReadAllText(fileName).Replace("\r", "");
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(text)).ToHexString();
            }
        }

        MySqlBase<T> _database;
    }

    public class AppliedFileEntry
    {
        public AppliedFileEntry(string name, string hash, State state, ulong timestamp)
        {
            Name = name;
            Hash = hash;
            State = state;
            Timestamp = timestamp;
        }

        public string Name;
        public string Hash;
        public State State;
        public ulong Timestamp;
    }

    public class FileEntry
    {
        public FileEntry(string _path, State _state)
        {
            path = _path;
            state = _state;
        }

        public string GetFileName()
        {
            return Path.GetFileName(path);
        }

        public string path;
        public State state;
    }

    struct UpdateResult
    {
        public int updated;
        public int recent;
        public int archived;
    }

    public enum State
    {
        RELEASED,
        ARCHIVED
    }

    enum UpdateMode
    {
        Apply,
        Rehash
    }
}
