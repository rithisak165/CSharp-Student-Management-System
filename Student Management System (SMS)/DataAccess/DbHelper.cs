using Npgsql;

namespace Student_Management_System__SMS_.DataAccess
{
    public static class DbHelper
    {
        // CHANGE 1: Port=5432 (Stable Session Mode)
        // CHANGE 2: You can keep Pooling=false, or remove it. It works better with 5432.
        // DbHelper.cs
        private static string connString =
            "User Id=postgres.unyhqtmcnfipflbhrfwd;" +
            "Password=J6kF65cghYQLH0no;" +
            "Server=aws-1-ap-south-1.pooler.supabase.com;" +
            "Port=5432;" +
            "Database=postgres;" +
            "Ssl Mode=Require;Trust Server Certificate=true;" +
            "Pooling=false;";
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connString);
        }
    }
}