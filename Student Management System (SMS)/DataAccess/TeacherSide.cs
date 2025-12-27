using System;
using System.Collections.Generic;
using Npgsql;
using Student_Management_System__SMS_.Models;

namespace Student_Management_System__SMS_.DataAccess
{
    public class TeacherSide
    {
        private static readonly ConsoleColor HeaderColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        private static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
        private static readonly ConsoleColor InfoColor = ConsoleColor.Magenta;
        private static readonly ConsoleColor TableHeaderColor = ConsoleColor.White;
        private static readonly ConsoleColor HighlightColor = ConsoleColor.DarkCyan;

        private static void PrintHeader(string title)
        {
            Console.Clear();

            int width = Console.WindowWidth;

            Console.ForegroundColor = HeaderColor;

            // Top full-width border
            Console.WriteLine(new string('═', width));

            // Centered title with ║ on both sides
            int titlePadding = (width - title.Length - 4) / 2; // -4 for the two ║ and spaces
            string leftPadding = new string(' ', titlePadding + 1);
            string rightPadding = new string(' ', width - title.Length - titlePadding - 3);

            Console.WriteLine($"║{leftPadding}{title}{rightPadding}║");

            // Bottom full-width border
            Console.WriteLine(new string('═', width));

            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintSuccess(string msg)
        {
            Console.ForegroundColor = SuccessColor;
            Console.WriteLine($"✓ {msg}");
            Console.ResetColor();
        }

        private static void PrintError(string msg)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine($"✗ Error: {msg}");
            Console.ResetColor();
        }

        private static void PrintWarning(string msg)
        {
            Console.ForegroundColor = WarningColor;
            Console.WriteLine($" {msg}");
            Console.ResetColor();
        }

        // ==========================================
        // 1. ADD STUDENT
        // ==========================================
        public void AddStudent()
        {
            Console.Clear();
            Console.WriteLine("==========================================");
            Console.WriteLine("           ADD NEW STUDENT");
            Console.WriteLine("==========================================");
            Console.WriteLine("(Type '0' at any step to Cancel and Go Back)\n");

            // 1. Standard Inputs
            string name = GetValidInput("Full Name: ");
            if (name == null) return;

            string code = GetValidInput("Student Code: ");
            if (code == null) return;

            // 2. SPECIAL INPUT: Group (MS1 or MS2 Only)
            string className = "";
            while (true)
            {
                Console.Write("Group (MS1/MS2): ");
                string input = Console.ReadLine()?.Trim().ToUpper(); // Auto-convert to UPPERCASE

                if (input == "0") return; // Allow Cancel

                if (input == "MS1" || input == "MS2")
                {
                    className = input;
                    break; // Valid input, exit loop
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Group must be exactly 'MS1' or 'MS2'.");
                    Console.ResetColor();
                }
            }

            // 3. Continue Standard Inputs
            string email = GetValidInput("Email: ");
            if (email == null) return;

            string pass = GetValidInput("Set Password: ");
            if (pass == null) return;

            // 4. Save to Database
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Insert Student
                    string insertStudent = "INSERT INTO Students (StudentCode, FullName, class) VALUES (@c, @n, @cl) RETURNING StudentId";
                    int newStudentId;
                    using (var cmd = new NpgsqlCommand(insertStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        cmd.Parameters.AddWithValue("n", name);
                        cmd.Parameters.AddWithValue("cl", className);
                        newStudentId = (int)cmd.ExecuteScalar();
                    }

                    // Insert User
                    string insertUser = "INSERT INTO Users (Email, Password, Role, StudentId) VALUES (@e, @p, 'Student', @sid)";
                    using (var cmd = new NpgsqlCommand(insertUser, conn))
                    {
                        cmd.Parameters.AddWithValue("e", email);
                        cmd.Parameters.AddWithValue("p", pass);
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    // Init Scores
                    string initScore = @"INSERT INTO Scores 
                (StudentId, Python, OOP_OOM, WritingII, DatabaseSystem, Microprocessor, Network, CoreEnglish) 
                VALUES (@sid, 0, 0, 0, 0, 0, 0, 0)";
                    using (var cmd = new NpgsqlCommand(initScore, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nStudent Added Successfully!");
                    Console.ResetColor();
                    Console.WriteLine($"Login Credentials -> Email: {email} | Password: {pass}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // ==========================================
        // HELPER FUNCTION: Get Valid Input
        // ==========================================
        // This function forces the user to type something.
        // If they type "0", it returns null (signal to cancel).
        private string GetValidInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine()?.Trim();

                if (input == "0")
                {
                    Console.WriteLine(">> Operation Cancelled.");
                    return null;
                }

                if (!string.IsNullOrEmpty(input))
                {
                    return input;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Input cannot be empty! (Type '0' to Cancel)");
                Console.ResetColor();
            }
        }
        // ==========================================
        // 2. VIEW PROFILES
        // ==========================================
        public void ViewAllStudentProfiles()
        {
            PrintHeader("           ALL STUDENT PROFILES");

            Console.ForegroundColor = TableHeaderColor;
            Console.WriteLine(String.Format("{0,-10} {1,-25} {2,-10} {3,-30} {4,-15}",
                "Code", "Full Name", "Group", "Email", "Password"));
            Console.WriteLine(new string('─', 95));
            Console.ResetColor();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT s.StudentCode, s.FullName, s.class, u.Email, u.Password 
                        FROM Students s 
                        JOIN Users u ON s.StudentId = u.StudentId 
                        ORDER BY s.StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.ForegroundColor = WarningColor;
                            Console.WriteLine("No students found.");
                            Console.ResetColor();
                        }

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);
                            string className = reader.IsDBNull(2) ? "-" : reader.GetString(2);
                            string email = reader.GetString(3);
                            string password = reader.GetString(4);

                            if (name.Length > 23) name = name.Substring(0, 20) + "...";

                            Console.WriteLine(String.Format("{0,-10} {1,-25} {2,-10} {3,-30} {4,-15}",
                                code, name, className, email, password));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }

        // ========================
        // 3. MANAGE SCORES
        // ========================
        public void ManageScores()
        {
            Console.Clear();
            Console.WriteLine("=== MANAGE STUDENT SCORES ===");
            Console.WriteLine("(Type '0' to Cancel Selection)\n");

            // 1. Get Student Code
            // We reuse the helper we made earlier
            string code = GetValidInput("Enter Student Code: ");
            if (code == null) return; // User typed 0 to go back

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 2. Find Student ID
                    // We use a new helper function here
                    int studentId = GetStudentIdByCode(conn, code);

                    if (studentId == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nStudent with code '{code}' not found.");
                        Console.ResetColor();
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                        return;
                    }

                    // 3. Enter Scores
                    Console.WriteLine($"\nEditing Scores for: {code}");
                    Console.WriteLine("Enter scores (0-100). Type 'exit' to cancel.\n");

                    // We use 'GetValidScore' to handle numbers and validation
                    float py = GetValidScore("Python");
                    if (py == -1) return; // -1 means user typed 'exit'

                    float oop = GetValidScore("OOP & OOM");
                    if (oop == -1) return;

                    float writ = GetValidScore("Writing II");
                    if (writ == -1) return;

                    float db = GetValidScore("Database System");
                    if (db == -1) return;

                    float micro = GetValidScore("Microprocessor");
                    if (micro == -1) return;

                    float net = GetValidScore("Network");
                    if (net == -1) return;

                    float eng = GetValidScore("Core English");
                    if (eng == -1) return;

                    // 4. Update Database
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

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nScores updated successfully!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        public float GetValidScore(string subjectName)
        {
            float score;
            while (true)
            {
                Console.ForegroundColor = InfoColor;
                Console.Write($"{subjectName}: ");
                Console.ResetColor();
                string input = Console.ReadLine();

                if (float.TryParse(input, out score) && score >= 0 && score <= 100)
                {
                    return score;
                }

                PrintError($"{subjectName} score must be a number between 0 and 100.");
            }
        }

        public int GetStudentIdByCode(NpgsqlConnection conn, string code)
        {
            string sql = "SELECT StudentId FROM Students WHERE StudentCode = @c";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("c", code);
                var res = cmd.ExecuteScalar();
                if (res != null) return (int)res;
            }
            PrintWarning("Student not found with that code.");
            return 0;
        }

        // ==========================================
        // 4. RECORD ATTENDANCE
        // ==========================================
        public void RecordAttendance()
        {
            PrintHeader("            RECORD ATTENDANCE");

            string[] subjects = { "Python", "OOP_OOM", "WritingII", "DatabaseSystem", "Microprocessor", "Network", "CoreEnglish" };
            Console.WriteLine("Select Subject:\n");
            for (int i = 0; i < subjects.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {subjects[i]}");
            }

            Console.Write("\nChoice (1-7): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > 7)
            {
                PrintError("Invalid selection.");
                Console.ReadKey();
                return;
            }

            string selectedSubject = subjects[choice - 1];
            DateTime date = DateTime.Today;

            Console.ForegroundColor = HighlightColor;
            Console.WriteLine($"\nRecording attendance for: {selectedSubject}");
            Console.WriteLine($"Date: {date:yyyy-MM-dd}\n");
            Console.ResetColor();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
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

                    foreach (var s in students)
                    {
                        Console.Write($"Is {s.FullName} Present? (Y/n): ");
                        string input = Console.ReadLine().Trim().ToLower();
                        string status = (input == "n") ? "Absent" : "Present";

                        Console.ForegroundColor = status == "Present" ? SuccessColor : WarningColor;
                        Console.WriteLine($"   → {status}\n");
                        Console.ResetColor();

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

                    PrintSuccess("Attendance recorded successfully for all students!");
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // ==========================================
        // 5. VIEW ATTENDANCE REPORT
        // ==========================================
        public void ViewAttendanceReport()
        {
            PrintHeader("            ATTENDANCE REPORT");

            Console.ForegroundColor = TableHeaderColor;
            Console.WriteLine(String.Format("{0,-25} {1,-10} {2,-10} {3,-10} {4,-8}",
                "Student Name", "Total", "Present", "Absent", "Percent"));
            Console.WriteLine(new string('─', 70));
            Console.ResetColor();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
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
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("No attendance records yet.");
                        }

                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            long total = reader.GetInt64(1);
                            long present = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                            long absent = reader.IsDBNull(3) ? 0 : reader.GetInt64(3);
                            double pct = total == 0 ? 0 : Math.Round((double)present / total * 100, 1);

                            ConsoleColor pctColor = pct >= 90 ? SuccessColor : pct >= 70 ? ConsoleColor.Yellow : ErrorColor;

                            Console.Write(String.Format("{0,-25} {1,-10} ", name, total));
                            Console.ForegroundColor = SuccessColor;
                            Console.Write(String.Format("{0,-10} ", present));
                            Console.ForegroundColor = WarningColor;
                            Console.Write(String.Format("{0,-10} ", absent));
                            Console.ForegroundColor = pctColor;
                            Console.WriteLine(String.Format("{0,-8}", pct + "%"));
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // ===============================
        // 6. VIEW ACADEMIC REPORT
        // ===============================
        public void ViewAcademicReport()
        {
            PrintHeader("            ACADEMIC PERFORMANCE REPORT");

            Console.ForegroundColor = TableHeaderColor;
            Console.WriteLine(String.Format("{0,-10} {1,-20} {2,-8} {3,-6} {4,-6} {5,-8} {6,-8} {7,-10}",
                "Code", "Name", "Total", "Avg", "Grade", "Present", "Absent", "Status"));
            Console.WriteLine(new string('─', 85));
            Console.ResetColor();

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
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("No records found.");
                            Console.ReadKey();
                            return;
                        }

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);
                            if (name.Length > 18) name = name.Substring(0, 17) + "...";

                            float total = 0;
                            for (int i = 2; i <= 8; i++)
                            {
                                if (!reader.IsDBNull(i)) total += (float)reader.GetDecimal(i);
                            }

                            long present = reader.GetInt64(9);
                            long absent = reader.GetInt64(10);

                            string avgDisplay, gradeDisplay, statusDisplay;
                            ConsoleColor statusColor;

                            if (total == 0)
                            {
                                avgDisplay = "-";
                                gradeDisplay = "-";
                                statusDisplay = "Pending";
                                statusColor = WarningColor;
                            }
                            else
                            {
                                float avg = total / 7;
                                avgDisplay = avg.ToString("F1");
                                gradeDisplay = CalculateGrade(avg);
                                statusDisplay = avg >= 60 ? "PASS" : "FAIL";
                                statusColor = avg >= 60 ? SuccessColor : ErrorColor;
                            }

                            Console.Write(String.Format("{0,-10} {1,-20} {2,-8} {3,-6} {4,-6} {5,-8} {6,-8} ",
                                code, name, total, avgDisplay, gradeDisplay, present, absent));

                            Console.ForegroundColor = statusColor;
                            Console.WriteLine(statusDisplay);
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }

        public string CalculateGrade(float avg)
        {
            if (avg >= 90) return "A";
            if (avg >= 80) return "B";
            if (avg >= 70) return "C";
            if (avg >= 60) return "D";
            return "F";
        }

        // ==========================================
        // 7. UPDATE STUDENT INFO
        // ==========================================
        public void UpdateStudentInfo()
        {
            PrintHeader("            UPDATE STUDENT INFORMATION");
            Console.WriteLine("(Type '0' at any step to Cancel and Go Back)\n");
            Console.Write("Enter Student Code: ");
            string code =Console.ReadLine();
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

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
                            if (!reader.Read())
                            {
                                PrintWarning("Student not found.");
                                Console.ReadKey();
                                return;
                            }

                            studentId = reader.GetInt32(0);
                            currentName = reader.GetString(1);
                            currentClass = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            currentEmail = reader.GetString(3);
                        }
                    }

                    Console.ForegroundColor = HighlightColor;
                    Console.WriteLine($"\nEditing: {currentName} ({code})\n");
                    Console.ResetColor();
                    Console.WriteLine("(Press Enter to keep current value)\n");

                    Console.Write($"Name [{currentName}]: ");
                    string newName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newName)) newName = currentName;

                    Console.Write($"Group [{currentClass}]: ");
                    string newClass = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newClass)) newClass = currentClass;

                    Console.Write($"Email [{currentEmail}]: ");
                    string newEmail = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(newEmail)) newEmail = currentEmail;

                    Console.Write("New Password (leave blank to keep): ");
                    string newPass = Console.ReadLine();

                    string updateStudent = "UPDATE Students SET FullName=@n, class=@cl WHERE StudentId=@id";
                    using (var cmd = new NpgsqlCommand(updateStudent, conn))
                    {
                        cmd.Parameters.AddWithValue("n", newName);
                        cmd.Parameters.AddWithValue("cl", newClass);
                        cmd.Parameters.AddWithValue("id", studentId);
                        cmd.ExecuteNonQuery();
                    }

                    string updateUser = "UPDATE Users SET Email=@e" +
                                       (!string.IsNullOrWhiteSpace(newPass) ? ", Password=@p" : "") +
                                       " WHERE StudentId=@id";

                    using (var cmd = new NpgsqlCommand(updateUser, conn))
                    {
                        cmd.Parameters.AddWithValue("e", newEmail);
                        cmd.Parameters.AddWithValue("id", studentId);
                        if (!string.IsNullOrWhiteSpace(newPass))
                            cmd.Parameters.AddWithValue("p", newPass);
                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess("Student information updated successfully!");
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // ==========================================
        // 8. DELETE STUDENT
        // ==========================================
        public void DeleteStudent()
        {
            PrintHeader("           DELETE STUDENT");

            PrintWarning("This action will permanently delete the student and ALL related data (scores, attendance, login).");
            Console.Write("\nEnter Student Code: ");
            string code = Console.ReadLine();
            Console.Write($"\nType 'DELETE {code}' to confirm: ");
            string confirm = Console.ReadLine();
            if (confirm != $"DELETE {code}"|| confirm == "no" || confirm == "No")
            {
                Console.WriteLine("Deletion cancelled or Wrong student ID");
                Console.ReadKey();
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    string findSql = "SELECT StudentId FROM Students WHERE StudentCode = @c";
                    int studentId = 0;

                    using (var cmd = new NpgsqlCommand(findSql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        object result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            PrintWarning("Student not found.");
                            Console.ReadKey();
                            return;
                        }
                        studentId = (int)result;
                    }

                    string masterDeleteSql = @"
                        DELETE FROM Attendance WHERE StudentId = @id;
                        DELETE FROM Scores WHERE StudentId = @id;
                        DELETE FROM Users WHERE StudentId = @id;
                        DELETE FROM Students WHERE StudentId = @id;";

                    using (var cmd = new NpgsqlCommand(masterDeleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", studentId);
                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess($"Student {code} and all related data deleted permanently.");
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}