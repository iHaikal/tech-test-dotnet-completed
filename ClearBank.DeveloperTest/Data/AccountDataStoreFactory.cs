using System;
using System.Configuration;

namespace ClearBank.DeveloperTest.Data
{
    public class AccountDataStoreFactory : IAccountDataStoreFactory
    {
        public IAccountDataStore CreateAccountDataStore()
        {
            var dataStoreType = ConfigurationManager.AppSettings["DataStoreType"];
            if (string.Equals(dataStoreType, "Backup", StringComparison.OrdinalIgnoreCase))
            {
                return new BackupAccountDataStore();
            }

            return new AccountDataStore();
        }
    }
}
