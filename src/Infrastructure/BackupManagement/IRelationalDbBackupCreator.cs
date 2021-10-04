using System;
using System.Threading.Tasks;

namespace Infrastructure.BackupManagement
{
    public interface IRelationalDbBackupCreator : IBackupCreator
    {
        public string VirtualDbName { get; }
        public string RealDbName { get; }
        public string Prefix { get; }
        RelationalDbType RelationalDbType { get; }
    }
}
