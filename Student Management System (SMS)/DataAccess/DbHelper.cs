using Npgsql;

namespace Student_Management_System__SMS_.DataAccess
{
    // ==========================================
    // 2. DATABASE HELPER
    // ==========================================
    public static class DbHelper
    {
        private static string connString = "User Id=postgres.unyhqtmcnfipflbhrfwd;Password=J6kF65cghYQLH0no;Server=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connString);
        }
    }
}