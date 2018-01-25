using System;
using static System.Console;

namespace testSMO
{
    class Program
    {
        const string SQLserver = @"PC-9\SQLEXPRESS";
        const string Database = "Titchy";
        const string Database2 = "Titchy2";
        const string BackupFile = "Titchy.bak";

        static void Main(string[] args)
        {
            var smo = new SMO(SQLserver, Database);
            
            WriteLine("Doing Backup");
            if (smo.BackUpSQlDatabase(Database, BackupFile, Update))
            {
                WriteLine($"Backup of {Database} to {BackupFile} succeeded");
            }
            else
            {
                WriteLine($"Backup of {Database} to {BackupFile} failed");
            }

            WriteLine("Doing Restore");
            if (smo.RestoreDatabase(Database2, BackupFile, Update))
            {
                WriteLine($"Restore of {Database2} from {BackupFile} succeeded");
            }
            else
            {
                WriteLine($"Restore of {Database2} from {BackupFile} failed");
            }

        }

        public static void Update(object sender, EventArgs e)
        {
            WriteLine(sender as string);
        }
    }
}
