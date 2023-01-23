// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;

namespace Framework.Database
{
    public class DatabaseLoader
    {
        public DatabaseLoader(DatabaseTypeFlags defaultUpdateMask)
        {
            _autoSetup = ConfigMgr.GetDefaultValue("Updates.AutoSetup", true);
            _updateFlags = ConfigMgr.GetDefaultValue("Updates.EnableDatabases", defaultUpdateMask);
        }

        public void AddDatabase<T>(MySqlBase<T> database, string baseDBName)
        {
            bool updatesEnabled = database.IsAutoUpdateEnabled(_updateFlags);
            _open.Add(() =>
            {
                MySqlConnectionInfo connectionObject = new()
                {
                    Host = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.Host", ""),
                    PortOrSocket = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.Port", ""),
                    Username = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.Username", ""),
                    Password = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.Password", ""),
                    Database = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.Database", ""),
                    UseSSL = ConfigMgr.GetDefaultValue(baseDBName + "DatabaseInfo.SSL", false)
                };

                var error = database.Initialize(connectionObject);
                if (error != MySqlErrorCode.None)
                {
                    // Database does not exist
                    if (error == MySqlErrorCode.UnknownDatabase && updatesEnabled && _autoSetup)
                    {
                        // Try to create the database and connect again if auto setup is enabled
                        if (CreateDatabase(connectionObject, database))
                            error = database.Initialize(connectionObject);
                    }

                    // If the error wasn't handled quit
                    if (error != MySqlErrorCode.None)
                    {
                        Log.outError(LogFilter.ServerLoading, $"\nDatabase {connectionObject.Database} NOT opened. There were errors opening the MySQL connections. Check your SQLErrors for specific errors.");
                        return false;
                    }
                }

                return true;
            });

            if (updatesEnabled)
            {
                // Populate and update only if updates are enabled for this pool
                _populate.Add(() =>
                {
                    if (!database.GetUpdater().Populate())
                    {
                        Log.outError(LogFilter.ServerLoading, $"Could not populate the {database.GetDatabaseName()} database, see log for details.");
                        return false;
                    }
                    return true;
                });

                _update.Add(() =>
                {
                    if (!database.GetUpdater().Update())
                    {
                        Log.outError(LogFilter.ServerLoading, $"Could not update the {database.GetDatabaseName()} database, see log for details.");
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

        public bool CreateDatabase<T>(MySqlConnectionInfo connectionObject, MySqlBase<T> database)
        {
            Log.outInfo(LogFilter.ServerLoading, $"Database \"{connectionObject.Database}\" does not exist, do you want to create it? [yes (default) / no]: ");

            string answer = Console.ReadLine();
            if (!answer.IsEmpty() && answer[0] != 'y')
                return false;

            Log.outInfo(LogFilter.ServerLoading, $"Creating database \"{connectionObject.Database}\"...");

            // Path of temp file
            string temp = "create_table.sql";

            // Create temporary query to use external MySQL CLi
            try
            {
                using StreamWriter streamWriter = new(File.Open(temp, FileMode.Create, FileAccess.Write));
                streamWriter.Write($"CREATE DATABASE `{connectionObject.Database}` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            }
            catch (Exception)
            {
                Log.outFatal(LogFilter.SqlUpdates, $"Failed to create temporary query file \"{temp}\"!");
                return false;
            }

            try
            {
                database.ApplyFile(temp, false);
            }
            catch (Exception)
            {
                Log.outFatal(LogFilter.SqlUpdates, $"Failed to create database {database.GetDatabaseName()}! Does the user (named in *.conf) have `CREATE`, `ALTER`, `DROP`, `INSERT` and `DELETE` privileges on the MySQL server?");
                File.Delete(temp);
                return false;
            }

            Log.outInfo(LogFilter.SqlUpdates, "Done.");
            File.Delete(temp);
            return true;
        }

        public bool Load()
        {
            if (_updateFlags == 0)
                Log.outInfo(LogFilter.SqlUpdates, "Automatic database updates are disabled for all databases!");

            if (_updateFlags != 0 && !DBExecutableUtil.CheckExecutable())
                return false;

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
        List<Func<bool>> _open = new();
        List<Func<bool>> _populate = new();
        List<Func<bool>> _update = new();
        List<Func<bool>> _prepare = new();
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
