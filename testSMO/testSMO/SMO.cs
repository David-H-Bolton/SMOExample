using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;
using System.IO;

namespace testSMO
{
    class SMO
    {

        public string Error { get; set; }
        public string ServerName { get; set; }
        public EventHandler UpdateCallback { get; set; }

        private Server server;
        private Database Db;

        public SMO(string servername, string databasename)
        {
            ServerName = servername;
            InitServer(databasename);
        }

        public Database InitServer(string databasename)
        {
            if (server == null)
            {
                try
                {
                    server = new Server(ServerName);
                    server.ConnectionContext.DatabaseName = databasename; // needed or insert sql fails            


                }
                catch (Exception ex)
                {
                    Error = $"Unable to access SQlServer {ServerName} : {ex.Message}";
                    return null;
                }
            }
            try
            {
                Db = server.Databases[databasename];
            }
            catch (Exception ex)
            {
                Error = $"Unable to read database table list from server - {ex.Message}";
                return null;
            }
            return Db;
        }

        private void CompletionStatusInPercent(object sender, PercentCompleteEventArgs args)
        {
            var s = $"{args.Percent}%";
            UpdateCallback?.Invoke(s, new EventArgs());
        }

        public Boolean BackUpSQlDatabase(string databaseName, string BackupFilename, EventHandler handler)
        {
            Error = "";
            try
            {
                Db = InitServer(databaseName);
                UpdateCallback = handler;
                // Define a Backup object variable.   
                var bk = new Backup();
                bk.PercentComplete += CompletionStatusInPercent;
                bk.Initialize = true; // overwrites existing files
                bk.CopyOnly = true; // special type of backup ideal for our use                 
                // Set the Incremental property to False to specify that this is a full database backup.  
                bk.Incremental = false;
                // Specify the type of backup, the description, the name, and the database to be backed up.   
                bk.Action = BackupActionType.Database;
                bk.BackupSetDescription = $"Full backup of {databaseName}";
                bk.BackupSetName = $"Backup {DateTime.Now.ToShortDateString()}";
                bk.Database = databaseName;
 
                // Declare a BackupDeviceItem by supplying the backup device file name in the constructor, and the type of device is a file.   
                var bdi = new BackupDeviceItem(BackupFilename, DeviceType.File);

                // Add the device to the Backup object.   
                bk.Devices.Add(bdi);

                // Set the expiration date. 15 seconds from now 
                bk.ExpirationDate = DateTime.Now.AddSeconds(15); // Can't overwrite if not expired

                // Specify that the log must be truncated after the backup is complete.   
                bk.LogTruncation = BackupTruncateLogType.Truncate;
                // Run SqlBackup Just before to perform the full database backup on the instance of SQL Server.   
                bk.SqlBackup(server);
                // Remove the backup device from the Backup object.   
                bk.Devices.Remove(bdi);
                return true;
            }
            catch (Exception ex)
            {
                Error = $"An error ocurred backup up database {databaseName} -  {ex.Message}";
                return false;
            }
        }

        internal bool RestoreDatabase(string databaseRestoreName, string BackupFilename, EventHandler handler)
        {
            UpdateCallback = handler;
            Db = InitServer(databaseRestoreName);
            Error = "";
            try
            {
                var rs = new Restore
                {
                    NoRecovery = false,
                };
                rs.PercentComplete += CompletionStatusInPercent;

                var bdi = new BackupDeviceItem(BackupFilename, DeviceType.File);
 
                rs.Devices.Add(bdi);
                rs.ReplaceDatabase = true;
                rs.Database = databaseRestoreName;
                rs.Action = RestoreActionType.Database;

                var fileList = new DataTable();
                try
                {
                    fileList = rs.ReadFileList(server);
                }
                catch (Exception tex)
                {
                    Error=$"{ tex.Message}";
                }
                string dataLogicalName = fileList.Rows[0][0].ToString();
                // string dataPhysicalName = fileList.Rows[0][1].ToString();
                string logLogicalName = fileList.Rows[1][0].ToString();
                // string logPhysicalName = fileList.Rows[1][1].ToString();

                var path = server.MasterDBPath; 
                var DataFile = new RelocateFile
                {
                    LogicalFileName = dataLogicalName,
                    PhysicalFileName = Path.Combine(path, databaseRestoreName) + ".mdf"
                };

                var LogFile = new RelocateFile()
                {
                    LogicalFileName = logLogicalName,
                    PhysicalFileName = Path.Combine(path, databaseRestoreName) + "_log.ldf"
                };

                rs.RelocateFiles.Clear();
                rs.RelocateFiles.Add(DataFile);
                rs.RelocateFiles.Add(LogFile);

                rs.SqlRestore(server);
           }
            catch (Exception ex)
            {
                Error = $"An error ocurred backup up database {databaseRestoreName} -  {ex.Message}";
                return false;
            }

            return true;
        }

        // Not returning anything
        public Boolean ExecSQL(string sql)
        {
            try
            {
                server.ConnectionContext.ExecuteNonQuery(sql);
                return true;
            }
            catch (Exception ex)
            {
                Error = $"Sql Query {sql} failed - {ex.Message}";
                return false;
            }
        }

        // Run sql with DataSet results back
        public DataSet Query(string sql)
        {
            try
            {
                return Db.ExecuteWithResults(sql);
            }
            catch (Exception ex)
            {
                Error = $"SQL query {sql} failed- {ex.Message}";
                return null;
            }
        }
    }
}
