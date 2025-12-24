using System;
using System.Collections.Generic;
using Npgsql;

namespace StudentManagementSystem
{
    // ==========================================
    // 1. DATA MODELS
    // ==========================================
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int? StudentId { get; set; } // Nullable for Teachers
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
    }

    public class Score
    {
        public float Python { get; set; }
        public float OOP_OOM { get; set; }
        public float WritingII { get; set; }
        public float DatabaseSystem { get; set; }
        public float Microprocessor { get; set; }
        public float Network { get; set; }
        public float CoreEnglish { get; set; }
    }

    // ==========================================
    // 2. DATABASE HELPER
    // ==========================================
    public static class DbHelper
    {
        // CHANGE THIS CONNECTION STRING to match your Postgres setup
        private static string connString = "Host=localhost;Username=postgres;Password=Msksak1651;Database=c_sharp_sms";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connString);
        }
    }

    // ==========================================
    // 3. MAIN PROGRAM & MENUS
    // ==========================================
    class Program
    {
        static User currentUser = null;

        static void Main(string[] args)
        {
            while (true)
            {
                if (currentUser == null)
                {
                    ShowLoginMenu();
                }
                else if (currentUser.Role == "Teacher")
                {
                    ShowTeacherMenu();
                }
                else if (currentUser.Role == "Student")
                {
                    ShowStudentMenu();
                }
            }
        }

        // --- Authentication ---
        static void ShowLoginMenu()
        {
            Console.Clear();
            Console.WriteLine("=== STUDENT MANAGEMENT SYSTEM LOGIN ===");
            Console.Write("Email: ");
            string email = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.ReadLine();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT UserId, Role, StudentId FROM Users WHERE Email = @e AND Password = @p";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("e", email);
                        cmd.Parameters.AddWithValue("p", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUser = new User
                                {
                                    UserId = reader.GetInt32(0),
                                    Email = email,
                                    Role = reader.GetString(1),
                                    StudentId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2)
                                };
                                Console.WriteLine($"Login Successful! Welcome {currentUser.Role}.");
                            }
                            else
                            {
                                Console.WriteLine("Invalid Credentials.");
                                Console.ReadKey();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadKey();
            }
        }

        // --- Teacher Menu ---
        static void ShowTeacherMenu()
        {
            Console.Clear();
            Console.WriteLine("=== TEACHER DASHBOARD ===");
            Console.WriteLine("1. Add New Student");
            Console.WriteLine("2. View All Student Profiles (Info)");
            Console.WriteLine("3. Enter/Update Scores");
            Console.WriteLine("4. Record Attendance");
            Console.WriteLine("5. View Attendance Report");
            Console.WriteLine("6. View Student Academic Report");
            Console.WriteLine("7. Update Student Info (Name, Email, Pass)");
            Console.WriteLine("8. Delete Student");
            Console.WriteLine("9. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": AddStudent(); break;
                case "2": ViewAllStudentProfiles(); break;
                case "3": ManageScores(); break;
                case "4": RecordAttendance(); break;
                case "5": ViewAttendanceReport(); break;
                case "6": ViewAcademicReport(); break;
                case "7": UpdateStudentInfo(); break;
                case "8": DeleteStudent(); break;
                case "9": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }

        // --- Student Menu ---
        static void ShowStudentMenu()
        {
            Console.Clear();
            Console.WriteLine("=== STUDENT DASHBOARD ===");
            Console.WriteLine("1. View My Scores & Grades");
            Console.WriteLine("2. View My Attendance");
            Console.WriteLine("3. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": ViewMyScores(); break;
                case "2": ViewMyAttendance(); break;
                case "3": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }

        // ==========================================
        // 4. TEACHER FEATURES IMPLEMENTATION
        // ==========================================

        static void AddStudent()
        {
            Console.WriteLine("\n-- Add New Student --");
            Console.Write("Full Name: "); string name = Console.ReadLine();
            Console.Write("Student Code: "); string code = Console.ReadLine();
            Console.Write("Group(MS1,MS2): "); string className = Console.ReadLine();
            Console.Write("Email: "); string email = Console.ReadLine();
            Console.Write("Set Password: "); string pass = Console.ReadLine();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // 1. Insert into Students table and get ID
                    string insertStudent = "INSERT INTO Students (StudentCode, FullName, ClassName) VALUES (@c, @n, @cl) RETURNING StudentId";
                    int newStudentId;

                    using (var cmd = new NpgsqlCommand(insertStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        cmd.Parameters.AddWithValue("n", name);
                        cmd.Parameters.AddWithValue("cl", className);
                        newStudentId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Create Login in Users table (Using the email you typed)
                    string insertUser = "INSERT INTO Users (Email, Password, Role, StudentId) VALUES (@e, @p, 'Student', @sid)";

                    using (var cmd = new NpgsqlCommand(insertUser, conn))
                    {
                        cmd.Parameters.AddWithValue("e", email); // Uses the manual email
                        cmd.Parameters.AddWithValue("p", pass);
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Initialize empty scores
                    string initScore = "INSERT INTO Scores (StudentId) VALUES (@sid)";
                    using (var cmd = new NpgsqlCommand(initScore, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine($"Student Added Successfully! Login Email: {email}");
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            Console.ReadKey();
        }
        static void ViewAllStudentProfiles()
        {
            Console.Clear();
            Console.WriteLine("=== ALL STUDENT PROFILES ===");

            // Header
            Console.WriteLine(String.Format("{0,-8} | {1,-20} | {2,-8} | {3,-25} | {4,-15}",
                "Code", "Name", "Class", "Email", "Password"));
            Console.WriteLine(new string('-', 85));

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Join Students and Users to get personal info AND login info
                    string sql = @"
                SELECT s.StudentCode, s.FullName, s.ClassName, u.Email, u.Password 
                FROM Students s 
                JOIN Users u ON s.StudentId = u.StudentId 
                ORDER BY s.StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows) Console.WriteLine("No students found.");

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);
                            // Handle potential nulls for ClassName
                            string className = reader.IsDBNull(2) ? "-" : reader.GetString(2);
                            string email = reader.GetString(3);
                            string password = reader.GetString(4);

                            // Truncate long names to keep table clean
                            if (name.Length > 18) name = name.Substring(0, 15) + "...";

                            Console.WriteLine(String.Format("{0,-8} | {1,-20} | {2,-8} | {3,-25} | {4,-15}",
                                code, name, className, email, password));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }
        static void ManageScores()
        {
            Console.Write("\nEnter Student Code to Update: ");
            string code = Console.ReadLine();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Find Student ID
                    int studentId = GetStudentIdByCode(conn, code);
                    if (studentId == 0) return;

                    Console.WriteLine("Enter Scores (0-100):");
                    Console.Write("Python: "); float py = float.Parse(Console.ReadLine());
                    Console.Write("OOP: "); float oop = float.Parse(Console.ReadLine());
                    Console.Write("Writing II: "); float writ = float.Parse(Console.ReadLine());
                    Console.Write("DB System: "); float db = float.Parse(Console.ReadLine());
                    Console.Write("Microprocessor: "); float micro = float.Parse(Console.ReadLine());
                    Console.Write("Network: "); float net = float.Parse(Console.ReadLine());
                    Console.Write("English: "); float eng = float.Parse(Console.ReadLine());

                    string sql = @"UPDATE Scores SET 
                                   Python=@py, OOP_OOM=@oop, WritingII=@writ, 
                                   DatabaseSystem=@db, Microprocessor=@micro, Network=@net, CoreEnglish=@eng 
                                   WHERE StudentId=@sid";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("py", py);
                        cmd.Parameters.AddWithValue("oop", oop);
                        cmd.Parameters.AddWithValue("writ", writ);
                        cmd.Parameters.AddWithValue("db", db);
                        cmd.Parameters.AddWithValue("micro", micro);
                        cmd.Parameters.AddWithValue("net", net);
                        cmd.Parameters.AddWithValue("eng", eng);
                        cmd.Parameters.AddWithValue("sid", studentId);
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("Scores Updated Successfully.");
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            Console.ReadKey();
        }

        static void RecordAttendance()
        {
            Console.WriteLine("\n-- Record Attendance --");

            // 1. Select Subject (No changes here, keeping your menu)
            string[] subjects = { "Python", "OOP_OOM", "WritingII", "DatabaseSystem", "Microprocessor", "Network", "CoreEnglish" };

            Console.WriteLine("Select Subject:");
            for (int i = 0; i < subjects.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {subjects[i]}");
            }

            Console.Write("Enter Subject Number (1-7): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > 7)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            string selectedSubject = subjects[choice - 1];

            // 2. AUTOMATIC DATE (No User Input)
            DateTime date = DateTime.Now;
            Console.WriteLine($"\nRecording Attendance for: {selectedSubject}");
            Console.WriteLine($"Date: {date:yyyy-MM-dd}"); // Show user the auto-date

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Get all students
                    var students = new List<Student>();
                    string sql = "SELECT StudentId, FullName FROM Students";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new Student { StudentId = reader.GetInt32(0), FullName = reader.GetString(1) });
                        }
                    }

                    // Loop through students
                    foreach (var s in students)
                    {
                        Console.Write($"Is {s.FullName} Present? (Y/n): ");
                        string input = Console.ReadLine();
                        // Default to Present if user just hits Enter
                        string status = (input.Trim().ToLower() == "n") ? "Absent" : "Present";

                        string insertAtt = "INSERT INTO Attendance (StudentId, Subject, ClassDate, Status) VALUES (@sid, @sub, @date, @stat)";
                        using (var cmd = new NpgsqlCommand(insertAtt, conn))
                        {
                            cmd.Parameters.AddWithValue("sid", s.StudentId);
                            cmd.Parameters.AddWithValue("sub", selectedSubject);
                            cmd.Parameters.AddWithValue("date", date);
                            cmd.Parameters.AddWithValue("stat", status);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("Attendance Recorded Successfully.");
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            Console.ReadKey();
        }
        static void ViewAttendanceReport()
        {
            Console.Clear();
            Console.WriteLine("\n-- Attendance Report --");

            // Updated Header: Added "Absent" column
            Console.WriteLine(String.Format("{0,-20} | {1,-8} | {2,-8} | {3,-8} | {4,-8}",
                "Name", "Total", "Present", "Absent", "%"));
            Console.WriteLine(new string('-', 65));

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Updated SQL: Added SUM case for 'Absent'
                    string sql = @"
                SELECT s.FullName, 
                       COUNT(a.AttendanceId) as Total,
                       SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) as PresentCount,
                       SUM(CASE WHEN a.Status = 'Absent' THEN 1 ELSE 0 END) as AbsentCount
                FROM Students s
                LEFT JOIN Attendance a ON s.StudentId = a.StudentId
                GROUP BY s.StudentId, s.FullName
                ORDER BY s.FullName";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows) Console.WriteLine("No attendance records found.");

                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            long total = reader.GetInt64(1);
                            // Handle NULLs if no attendance exists yet
                            long present = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                            long absent = reader.IsDBNull(3) ? 0 : reader.GetInt64(3); // Read Absent count

                            double pct = total == 0 ? 0 : (double)present / total * 100;

                            // Print Row with Absent column
                            Console.WriteLine(String.Format("{0,-20} | {1,-8} | {2,-8} | {3,-8} | {4,-8:F1}%",
                                name, total, present, absent, pct));
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
        static void ViewAcademicReport()
        {
            Console.Clear();
            Console.WriteLine("=== FULL STUDENT REPORT ===");

            // Adjusted columns to fit "Coming Soon" text
            Console.WriteLine(String.Format("{0,-8} | {1,-18} | {2,-6} | {3,-6} | {4,-5} | {5,-4} | {6,-4} | {7}",
                "Code", "Name", "Total", "Avg", "Grd", "Pres", "Abs", "Status"));
            Console.WriteLine(new string('-', 85));

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                SELECT s.StudentCode, s.FullName, 
                       sc.Python, sc.OOP_OOM, sc.WritingII, sc.DatabaseSystem, 
                       sc.Microprocessor, sc.Network, sc.CoreEnglish,
                       (SELECT COUNT(*) FROM Attendance a WHERE a.StudentId = s.StudentId AND a.Status = 'Present') as PresentCount,
                       (SELECT COUNT(*) FROM Attendance a WHERE a.StudentId = s.StudentId AND a.Status = 'Absent') as AbsentCount
                FROM Students s 
                JOIN Scores sc ON s.StudentId = sc.StudentId
                ORDER BY s.StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows) Console.WriteLine("No records found.");

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);
                            if (name.Length > 18) name = name.Substring(0, 15) + "...";

                            // Calculate Total Score
                            float total = 0;
                            for (int i = 2; i <= 8; i++)
                            {
                                if (!reader.IsDBNull(i)) total += (float)reader.GetDecimal(i);
                            }

                            // Attendance Counts
                            long present = reader.GetInt64(9);
                            long absent = reader.GetInt64(10);

                            // --- NEW LOGIC: Check if scores exist ---
                            string avgDisplay, gradeDisplay, statusDisplay;

                            if (total == 0)
                            {
                                // Teacher hasn't entered scores yet
                                avgDisplay = "-";
                                gradeDisplay = "-";
                                statusDisplay = "Coming Soon";
                            }
                            else
                            {
                                // Scores exist
                                float avg = total / 7;
                                avgDisplay = avg.ToString("F1");
                                gradeDisplay = CalculateGrade(avg);
                                statusDisplay = avg >= 60 ? "PASS" : "FAIL";
                            }

                            // Print Row
                            Console.WriteLine(String.Format("{0,-8} | {1,-18} | {2,-6} | {3,-6} | {4,-5} | {5,-4} | {6,-4} | {7}",
                                code, name, total, avgDisplay, gradeDisplay, present, absent, statusDisplay));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }
        static void UpdateStudentInfo()
        {
            Console.WriteLine("\n-- Update Student Information --");
            Console.Write("Enter Student Code to Edit: ");
            string code = Console.ReadLine();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Get Current Info (Join Students and Users table)
                    string sql = @"
                SELECT s.StudentId, s.FullName, s.ClassName, u.Email 
                FROM Students s 
                JOIN Users u ON s.StudentId = u.StudentId 
                WHERE s.StudentCode = @c";

                    int studentId = 0;
                    string currentName = "", currentClass = "", currentEmail = "";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentId = reader.GetInt32(0);
                                currentName = reader.GetString(1);
                                currentClass = reader.GetString(2);
                                currentEmail = reader.GetString(3);
                            }
                            else
                            {
                                Console.WriteLine("Student not found.");
                                Console.ReadKey();
                                return;
                            }
                        }
                    }

                    // 2. Ask for New Info (Leave blank to keep old)
                    Console.WriteLine($"\nEditing Student: {currentName}");
                    Console.WriteLine("(Press Enter to keep current value)");

                    Console.Write($"Name [{currentName}]: ");
                    string newName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newName)) newName = currentName;

                    Console.Write($"Class [{currentClass}]: ");
                    string newClass = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newClass)) newClass = currentClass;

                    Console.Write($"Email [{currentEmail}]: ");
                    string newEmail = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newEmail)) newEmail = currentEmail;

                    Console.Write("New Password (leave blank to keep unchanged): ");
                    string newPass = Console.ReadLine();

                    // 3. Execute Updates
                    // Update Student Table
                    string updateStudent = "UPDATE Students SET FullName=@n, ClassName=@cl WHERE StudentId=@id";
                    using (var cmd = new NpgsqlCommand(updateStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("n", newName);
                        cmd.Parameters.AddWithValue("cl", newClass);
                        cmd.Parameters.AddWithValue("id", studentId);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Users Table (Email)
                    string updateUser = "UPDATE Users SET Email=@e WHERE StudentId=@id";
                    // If password was changed, update it too
                    if (!string.IsNullOrWhiteSpace(newPass))
                    {
                        updateUser = "UPDATE Users SET Email=@e, Password=@p WHERE StudentId=@id";
                    }

                    using (var cmd = new NpgsqlCommand(updateUser, conn))
                    {
                        cmd.Parameters.AddWithValue("e", newEmail);
                        cmd.Parameters.AddWithValue("id", studentId);
                        if (!string.IsNullOrWhiteSpace(newPass))
                        {
                            cmd.Parameters.AddWithValue("p", newPass);
                        }
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("Student Information Updated Successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.ReadKey();
        }
        static void DeleteStudent()
        {
            Console.WriteLine("\n-- Delete Student --");
            Console.WriteLine("WARNING: This will delete the student, their login, scores, and attendance history.");
            Console.Write("Enter Student Code to Delete: ");
            string code = Console.ReadLine();

            Console.Write($"Are you sure you want to delete {code}? (Type 'yes' to confirm): ");
            string confirm = Console.ReadLine();

            if (confirm.ToLower() != "yes")
            {
                Console.WriteLine("Deletion Cancelled.");
                Console.ReadKey();
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Because we used "ON DELETE CASCADE" in the database creation script,
                    // we only need to delete from the parent 'Students' table.
                    // PostgreSQL will automatically delete the linked User, Scores, and Attendance rows.

                    string sql = "DELETE FROM Students WHERE StudentCode = @c";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Student deleted successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Student not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.ReadKey();
        }
        // ==========================================
        // 5. STUDENT FEATURES IMPLEMENTATION
        // ==========================================

        static void ViewMyScores()
        {
            // Get Code from current user ID
            string code = GetCodeByStudentId(currentUser.StudentId.Value);
            PrintStudentResult(code);
            Console.ReadKey();
        }

        static void ViewMyAttendance()
        {
            Console.WriteLine("\n-- My Attendance Summary --");
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT Subject, ClassDate, Status FROM Attendance WHERE StudentId = @sid ORDER BY ClassDate DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", currentUser.StudentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine(String.Format("{0,-20} | {1,-15} | {2}", "Subject", "Date", "Status"));
                            while (reader.Read())
                            {
                                Console.WriteLine(String.Format("{0,-20} | {1,-15:d} | {2}",
                                    reader.GetString(0), reader.GetDateTime(1), reader.GetString(2)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Console.ReadKey();
        }

        // ==========================================
        // 6. SHARED LOGIC & CALCULATIONS
        // ==========================================

        static void PrintStudentResult(string studentCode)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT s.FullName, sc.* FROM Students s 
                        JOIN Scores sc ON s.StudentId = sc.StudentId 
                        WHERE s.StudentCode = @c";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", studentCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.Clear();
                                Console.WriteLine($"=== RESULT FOR {reader.GetString(0)} ===");
                                float[] scores = new float[7];
                                // Reader index starts at 2 because 0=Name, 1=ScoreId, 2=StudentId (in 'sc.*' usually)
                                // Actually sc.* returns ScoreId, StudentId, then subjects. 
                                // Let's map safely based on column order in CREATE TABLE:
                                // 0:Name, 1:ScoreId, 2:StudentId, 3:Py, 4:OOP, 5:Writ, 6:DB, 7:Micro, 8:Net, 9:Eng

                                string[] subjects = { "Python", "OOP", "Writing", "DB Sys", "Micro", "Network", "English" };
                                float total = 0;

                                for (int i = 0; i < 7; i++)
                                {
                                    scores[i] = (float)reader.GetDecimal(i + 3); // Adjust index based on SELECT
                                    total += scores[i];
                                    Console.WriteLine($"{subjects[i],-15}: {scores[i]}");
                                }

                                float avg = total / 7;
                                string grade = CalculateGrade(avg);
                                string status = avg >= 60 ? "PASS" : "FAIL";

                                Console.WriteLine("-----------------------------");
                                Console.WriteLine($"Total Score    : {total}");
                                Console.WriteLine($"Average        : {avg:F2}");
                                Console.WriteLine($"Grade          : {grade}");
                                Console.WriteLine($"Status         : {status}");
                            }
                            else
                            {
                                Console.WriteLine("Student not found or no scores initialized.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
        }

        static string CalculateGrade(float avg)
        {
            if (avg >= 90) return "A";
            if (avg >= 80) return "B";
            if (avg >= 70) return "C";
            if (avg >= 60) return "D";
            return "F";
        }

        static int GetStudentIdByCode(NpgsqlConnection conn, string code)
        {
            string sql = "SELECT StudentId FROM Students WHERE StudentCode = @c";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("c", code);
                var res = cmd.ExecuteScalar();
                if (res != null) return (int)res;
            }
            Console.WriteLine("Student not found.");
            return 0;
        }

        static string GetCodeByStudentId(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT StudentCode FROM Students WHERE StudentId = @id";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    return cmd.ExecuteScalar().ToString();
                }
            }
        }
    }
}