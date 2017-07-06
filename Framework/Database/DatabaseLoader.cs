﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Framework.Database
{
    public class DatabaseLoader
    {
        public DatabaseLoader(DatabaseTypeFlags defaultUpdateMask)
        {
            _autoSetup = ConfigMgr.GetDefaultValue("Updates.AutoSetup", true);
            _updateFlags = ConfigMgr.GetDefaultValue("Updates.EnableDatabases", defaultUpdateMask);
        }

        public void AddDatabase<T>(MySqlBase<T> database, string name)
        {
            bool updatesEnabled = database.IsAutoUpdateEnabled(_updateFlags);
            _open.Add(() =>
            {
                ConnectionObject co = new ConnectionObject
                                      {
                                          Database = ConfigMgr.GetDefaultValue(name + "DatabaseInfo.Database", ""),
                                          Host = ConfigMgr.GetDefaultValue(name + "DatabaseInfo.Host", ""),
                                          Password = ConfigMgr.GetDefaultValue(name + "DatabaseInfo.Password", ""),
                                          Port = ConfigMgr.GetDefaultValue(name + "DatabaseInfo.Port", ""),
                                          Username = ConfigMgr.GetDefaultValue(name + "DatabaseInfo.Username", "")
                                      };

                var error = database.Initialize(co);
                if (error != MySqlErrorCode.None)
                {
                    // Database does not exist
                    if (error == MySqlErrorCode.UnknownDatabase && updatesEnabled && _autoSetup)
                    {
                        Log.outInfo(LogFilter.ServerLoading, "Database \"{0}\" does not exist, do you want to create it? [yes (default) / no]: ", name);

                        string answer = Console.ReadLine();
                        if (string.IsNullOrEmpty(answer) || answer[0] != 'y')
                            return false;

                        Log.outInfo(LogFilter.ServerLoading, "Creating database \"{0}\"...", name);
                        string sqlString = string.Format("CREATE DATABASE `{0}` DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci", name);
                        // Try to create the database and connect again if auto setup is enabled
                        if (database.Apply(sqlString) && database.Initialize(co) == MySqlErrorCode.None)
                            error = MySqlErrorCode.None;
                    }

                    // If the error wasn't handled quit
                    if (error != MySqlErrorCode.None)
                    {
                        Log.outError(LogFilter.ServerLoading, "\nDatabase {0} NOT opened. There were errors opening the MySQL connections. Check your SQLErrors for specific errors.", name);
                        return false;
                    }

                    Log.outInfo(LogFilter.ServerLoading, "Done.");
                }
                return true;
            });

            if (updatesEnabled)
            {
                // Populate and update only if updates are enabled for this pool
                _populate.Add(() =>
                {
                    //Hack used to allow big querys
                    database.Apply("SET GLOBAL max_allowed_packet=1073741824;");
                    if (!database.GetUpdater().Populate())
                    {
                        Log.outError(LogFilter.ServerLoading, "Could not populate the {0} database, see log for details.", name);
                        return false;
                    }
                    return true;
                });

                _update.Add(() =>
                {
                    //Hack used to allow big querys
                    database.Apply("SET GLOBAL max_allowed_packet=1073741824;");
                    if (!database.GetUpdater().Update())
                    {
                        Log.outError(LogFilter.ServerLoading, "Could not update the {0} database, see log for details.", name);
                        return false;
                    }
                    return true;
                });
            }

            _prepare.Add(() =>
            {
                database.LoadPreparedStatements();
                return true;
            });
        }

        public bool Load()
        {
            if (_updateFlags == 0)
                Log.outInfo(LogFilter.SqlUpdates, "Automatic database updates are disabled for all databases!");

            if (!OpenDatabases())
                return false;

            if (!PopulateDatabases())
                return false;

            if (!UpdateDatabases())
                return false;

            if (!PrepareStatements())
                return false;

            return true;
        }

        bool OpenDatabases()
        {
            return Process(_open);
        }

        // Processes the elements of the given stack until a predicate returned false.
        bool Process(List<Func<bool>> list)
        {
            while (!list.Empty())
            {
                if (!list[0].Invoke())
                    return false;

                list.RemoveAt(0);
            }
            return true;
        }

        bool PopulateDatabases()
        {
            return Process(_populate);
        }

        bool UpdateDatabases()
        {
            return Process(_update);
        }

        bool PrepareStatements()
        {
            return Process(_prepare);
        }

        bool _autoSetup;
        DatabaseTypeFlags _updateFlags;
        List<Func<bool>> _open = new List<Func<bool>>();
        List<Func<bool>> _populate = new List<Func<bool>>();
        List<Func<bool>> _update = new List<Func<bool>>();
        List<Func<bool>> _prepare = new List<Func<bool>>();
    }

    public enum DatabaseTypeFlags
    {
        None = 0,

        Login = 1,
        Character = 2,
        World = 4,
        Hotfix = 8,

        All = Login | Character | World | Hotfix
    }
}
