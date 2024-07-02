﻿using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.CMMySqlDatabase.Data;

namespace WDE.CMMySqlDatabase.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class WorldDatabaseSettingsProvider : IWorldDatabaseSettingsProvider, IMySqlWorldConnectionStringProvider
    {
        private readonly IUserSettings userSettings;

        public WorldDatabaseSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
        }

        public string ConnectionString => $"Server={Settings.Host};Port={Settings.Port ?? 3306};Database={Settings.Database};Uid={Settings.User};Pwd={Settings.Password};AllowUserVariables=True;TreatTinyAsBoolean=False";
        public string DatabaseName => Settings.Database ?? "";
        public bool IsEmpty => string.IsNullOrEmpty(Settings.Host) || string.IsNullOrEmpty(Settings.Database) || string.IsNullOrEmpty(Settings.User);

        public IDbAccess Settings
        {
            get => userSettings.Get<DbAccess>(DbAccess.Default);
            set => userSettings.Update(new DbAccess()
            {
                Host = value.Host,
                Port = value.Port,
                User = value.User,
                Password = value.Password,
                Database = value.Database,
            });
        }
    }
}