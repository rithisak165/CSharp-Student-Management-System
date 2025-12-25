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
        public int? StudentId { get; set; } 
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
        // FIX: Added the missing 'f' in the User Id (look at "...fipfl...")
        private static string connString = "User Id=postgres.unyhqtmcnfipflbhrfwd;Password=J6kF65cghYQLH0no;Server=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;";

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
            Console.WriteLine("3. View Academic Report");
            Console.WriteLine("4. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": ViewMyScores(); break;
                case "2": ViewMyAttendance(); break;
                case "3": ViewAcademicReport();break;
                case "4": currentUser = null; break;
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

                    // 1. Insert into Students table
                    string insertStudent = "INSERT INTO Students (StudentCode, FullName, class) VALUES (@c, @n, @cl) RETURNING StudentId";
                    int newStudentId;

                    using (var cmd = new NpgsqlCommand(insertStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        cmd.Parameters.AddWithValue("n", name);
                        cmd.Parameters.AddWithValue("cl", className);
                        newStudentId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Create Login in Users table
                    string insertUser = "INSERT INTO Users (Email, Password, Role, StudentId) VALUES (@e, @p, 'Student', @sid)";

                    using (var cmd = new NpgsqlCommand(insertUser, conn))
                    {
                        cmd.Parameters.AddWithValue("e", email);
                        cmd.Parameters.AddWithValue("p", pass);
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Initialize scores with 0 (FIX FOR ERROR 23502)
                    string initScore = @"INSERT INTO Scores 
                (StudentId, Python, OOP_OOM, WritingII, DatabaseSystem, Microprocessor, Network, CoreEnglish) 
                VALUES 
                (@sid, 0, 0, 0, 0, 0, 0, 0)";

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
                "Code", "Name", "Group", "Email", "Password"));
            Console.WriteLine(new string('-', 85));

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Join Students and Users to get personal info AND login info
                    string sql = @"
                SELECT s.StudentCode, s.FullName, s.class, u.Email, u.Password 
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
                    if (studentId == 0) return; // Student not found, exit

                    Console.WriteLine("Enter Scores (0-100):");

                    // --- CHANGED SECTION START ---
                    // We use the helper function GetValidScore for each subject
                    // This ensures if Python is wrong, it asks for Python again immediately.
                    float py = GetValidScore("Python");
                    float oop = GetValidScore("OOP");
                    float writ = GetValidScore("Writing II");
                    float db = GetValidScore("DB System");
                    float micro = GetValidScore("Microprocessor");
                    float net = GetValidScore("Network");
                    float eng = GetValidScore("English");
                    // --- CHANGED SECTION END ---

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
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.ReadKey();
        }

        // --- NEW HELPER FUNCTION ---
        // This function handles the "Ask again immediately" logic
        static float GetValidScore(string subjectName)
        {
            float score;
            while (true) // Infinite loop until valid input
            {
                Console.Write($"{subjectName}: ");
                string input = Console.ReadLine();

                // 1. Check if input is a valid number (float.TryParse)
                // 2. Check if number is between 0 and 100
                if (float.TryParse(input, out score) && score >= 0 && score <= 100)
                {
                    return score; // Input is good, return it and exit the loop
                }

                // If we get here, the input was wrong
                Console.WriteLine($"Error: {subjectName} score must be between 0 and 100. Please try again.");
            }
        }
        static void RecordAttendance()
        {
            Console.WriteLine("\n-- Record Attendance --");

            // 1. Select Subject
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

            // 2. AUTOMATIC DATE (Fixed)
            // We use DateTime.Today instead of Now to get '2025-12-25 00:00:00'
            // This ensures re-running the tool on the same day updates the existing record instead of creating a duplicate.
            DateTime date = DateTime.Today;

            Console.WriteLine($"\nRecording Attendance for: {selectedSubject}");
            Console.WriteLine($"Date: {date:yyyy-MM-dd}");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Get all students
                    var students = new List<Student>();
                    string sql = "SELECT StudentId, FullName FROM Students ORDER BY StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new Student
                            {
                                StudentId = reader.GetInt32(0),
                                FullName = reader.GetString(1)
                            });
                        }
                    }

                    // Loop through students
                    foreach (var s in students)
                    {
                        Console.Write($"Is {s.FullName} Present? (Y/n): ");
                        string input = Console.ReadLine();
                        string status = (input.Trim().ToLower() == "n") ? "Absent" : "Present";

                        // FIX: Use 'ON CONFLICT' to handle duplicates safely
                        string insertAtt = @"
                    INSERT INTO Attendance (StudentId, Subject, ClassDate, Status) 
                    VALUES (@sid, @sub, @date, @stat)
                    ON CONFLICT (StudentId, Subject, ClassDate) 
                    DO UPDATE SET Status = @stat";

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

                    // 👇 FIX: Changed 'ClassName' to 'class'
                    string sql = @"
        SELECT s.StudentId, s.FullName, s.class, u.Email 
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
                                // Handle potential nulls for class
                                currentClass = reader.IsDBNull(2) ? "" : reader.GetString(2);
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

                    // 2. Ask for New Info
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

                    // 👇 FIX: Changed 'ClassName' to 'class'
                    string updateStudent = "UPDATE Students SET FullName=@n, class=@cl WHERE StudentId=@id";
                    using (var cmd = new NpgsqlCommand(updateStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("n", newName);
                        cmd.Parameters.AddWithValue("cl", newClass);
                        cmd.Parameters.AddWithValue("id", studentId);
                        cmd.ExecuteNonQuery();
                    }

                    // Update Users Table (Email)
                    string updateUser = "UPDATE Users SET Email=@e WHERE StudentId=@id";
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
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Get Student ID first (to ensure they exist)
                    string findSql = "SELECT StudentId FROM Students WHERE StudentCode = @c";
                    int studentId = 0;

                    using (var cmd = new NpgsqlCommand(findSql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            studentId = (int)result;
                        }
                        else
                        {
                            Console.WriteLine("Student not found.");
                            Console.ReadKey();
                            return;
                        }
                    }

                    // 2. ONE-SHOT DELETE
                    // Instead of opening a transaction transaction object, we send all 4 commands
                    // in a single string. This prevents the "Object Disposed" crash.
                    string masterDeleteSql = @"
                DELETE FROM Attendance WHERE StudentId = @id;
                DELETE FROM Scores WHERE StudentId = @id;
                DELETE FROM Users WHERE StudentId = @id;
                DELETE FROM Students WHERE StudentId = @id;
            ";

                    using (var cmd = new NpgsqlCommand(masterDeleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", studentId);

                        // This executes all 4 lines above in order.
                        cmd.ExecuteNonQuery();

                        Console.WriteLine("Success! Student and all related data have been deleted.");
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
            Console.Clear();

            if (currentUser == null || currentUser.StudentId == null)
            {
                Console.WriteLine("Error: User not found.");
                Console.ReadKey();
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // SQL: Use LEFT JOIN so we get the Student Name even if Scores are missing
                    string sql = @"
                SELECT s.FullName, 
                       sc.Python, sc.OOP_OOM, sc.WritingII, 
                       sc.DatabaseSystem, sc.Microprocessor, sc.Network, sc.CoreEnglish 
                FROM Students s 
                LEFT JOIN Scores sc ON s.StudentId = sc.StudentId 
                WHERE s.StudentId = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", currentUser.StudentId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString(0);

                                // 1. PREPARE DATA
                                string[] subjects = { "Python", "OOP_OOM", "Writing II", "DB System", "Microprocessor", "Network", "English" };
                                float[] scores = new float[7];
                                float total = 0;
                                bool hasAnyScore = false; // Flag to check if there is real data

                                // 2. READ AND SUM DATA FIRST (Don't print yet)
                                for (int i = 0; i < 7; i++)
                                {
                                    // Column 0 is Name, so scores start at Column 1 (i + 1)
                                    if (!reader.IsDBNull(i + 1))
                                    {
                                        float val = Convert.ToSingle(reader.GetValue(i + 1));
                                        scores[i] = val;
                                        total += val;

                                        // If we find a score greater than 0, we know results are out
                                        if (val > 0) hasAnyScore = true;
                                    }
                                }

                                // 3. DECISION: SHOW MESSAGE OR SHOW TABLE
                                if (!hasAnyScore)
                                {
                                    // === SCENARIO A: RESULTS NOT OUT (Total is 0) ===
                                    Console.WriteLine($"=== ACADEMIC RESULT FOR: {name} ===");
                                    Console.WriteLine("\n\n");
                                    Console.WriteLine("==========================================");
                                    Console.WriteLine("|                                        |");
                                    Console.WriteLine("|        📢 RESULTS COMING SOON         |");
                                    Console.WriteLine("|                                        |");
                                    Console.WriteLine("==========================================");
                                    Console.WriteLine("\nYour instructors have not finalized the scores.");
                                    Console.WriteLine("Please check back later.");
                                    Console.WriteLine("\n\n");
                                }
                                else
                                {
                                    // === SCENARIO B: RESULTS ARE OUT (Show the Table) ===
                                    Console.WriteLine($"=== ACADEMIC RESULT FOR: {name} ===");
                                    Console.WriteLine(new string('-', 35));

                                    for (int i = 0; i < 7; i++)
                                    {
                                        Console.WriteLine($"{subjects[i],-18}: {scores[i]}");
                                    }

                                    float avg = total / 7;
                                    string grade = CalculateGrade(avg); // Make sure you have this helper function
                                    string status = avg >= 50 ? "PASS" : "FAIL";

                                    Console.WriteLine(new string('-', 35));
                                    Console.WriteLine($"Total Score       : {total}");
                                    Console.WriteLine($"Average           : {avg:F2}");
                                    Console.WriteLine($"Grade             : {grade}");
                                    Console.WriteLine($"Final Status      : {status}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No student record found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        static void ViewMyAttendance()
        {
            Console.Clear();
            Console.WriteLine("=== MY ATTENDANCE SUMMARY ===");

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
                            if (reader.HasRows)
                            {
                                Console.WriteLine("\n{0,-20} | {1,-15} | {2}", "Subject", "Date", "Status");
                                Console.WriteLine(new string('-', 50));

                                while (reader.Read())
                                {
                                    string subject = reader.GetString(0);
                                    // :d formats datetime to Short Date (e.g., 12/25/2025)
                                    string date = reader.GetDateTime(1).ToString("d");
                                    string status = reader.GetString(2);

                                    Console.WriteLine("{0,-20} | {1,-15} | {2}", subject, date, status);
                                }
                            }
                            else
                            {
                                Console.WriteLine("\nNo attendance records found for your account.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading attendance: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
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
    }
}