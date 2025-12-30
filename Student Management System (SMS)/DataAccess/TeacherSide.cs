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
        private User currentUser;

        public TeacherSide(User currentUser)
        {
            this.currentUser = currentUser;
        }

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
        // 1. ADD NEW STUDENT
        // ==========================================
        public void AddStudent()
        {
            Console.Clear();
            PrintHeader("ADD NEW STUDENT");
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
                string input = Console.ReadLine()?.Trim().ToUpper();

                if (input == "0") return;

                if (input == "MS1" || input == "MS2")
                {
                    className = input;
                    break;
                }
                else
                {
                    PrintError("Group must be exactly 'MS1' or 'MS2'.");
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

                    // === FIX IS HERE === 
                    // Changed "ClassName" to "Class" to match your database
                    string insertStudent = "INSERT INTO Students (StudentCode, FullName, Class) VALUES (@c, @n, @cl) RETURNING StudentId";

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

                    // Init Scores (Set all to 0)
                    string initScore = @"INSERT INTO Scores 
                               (StudentId, Python, OOP_OOM, WritingII, DatabaseSystem, Microprocessor, Network, CoreEnglish) 
                               VALUES (@sid, 0, 0, 0, 0, 0, 0, 0)";
                    using (var cmd = new NpgsqlCommand(initScore, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", newStudentId);
                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess("Student Added Successfully!");
                    Console.WriteLine($"Login Credentials -> Email: {email} | Password: {pass}");
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
        // 2. VIEW ALL PROFILES
        // ==========================================
        public void ViewAllStudentProfiles()
        {
            PrintHeader("ALL STUDENT PROFILES");

            Console.ForegroundColor = TableHeaderColor;
            Console.WriteLine("{0,-10} {1,-25} {2,-10} {3,-30} {4,-15}", "Code", "Full Name", "Group", "Email", "Password");
            Console.WriteLine(new string('─', 95));
            Console.ResetColor();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // FIXED: Changed 's.ClassName' to 's.Class'
                    string sql = @"
                SELECT s.StudentCode, s.FullName, s.Class, u.Email, u.Password 
                FROM Students s 
                JOIN Users u ON s.StudentId = u.StudentId 
                ORDER BY s.StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            PrintWarning("No students found.");
                        }

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);

                            // FIXED: Handle potential nulls safely
                            string cls = reader.IsDBNull(2) ? "-" : reader.GetString(2);

                            string email = reader.GetString(3);
                            string password = reader.GetString(4);

                            // Truncate long names for display
                            if (name.Length > 23) name = name.Substring(0, 20) + "...";

                            Console.WriteLine("{0,-10} {1,-25} {2,-10} {3,-30} {4,-15}", code, name, cls, email, password);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                // Debugging hint:
                if (ex.Message.Contains("does not exist"))
                {
                    Console.WriteLine("\n[Hint]: Check your database columns in pgAdmin. Expected column 'Class' in table 'Students'.");
                }
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }

        // ==========================================
        // 3. HELPER FUNCTION
        // ==========================================
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

                PrintError("Input cannot be empty! (Type '0' to Cancel)");
            }
        }
        // ==========================================
        // 3. MANAGE SCORES
        // ==========================================
        public void ManageScores()
        {
            // Debug check to prove code updated
            Console.WriteLine("\n*** NEW CODE LOADED: ManageScores ***\n");

            if (currentUser == null || string.IsNullOrEmpty(currentUser.Subject))
            {
                Console.WriteLine("Error: You are not assigned a subject.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine($"=== MANAGE SCORES: {currentUser.Subject} ===");
            Console.WriteLine("(Type '0' to Cancel)\n");

            Console.Write("Enter Student Code (e.g., B20244047): ");
            string code = Console.ReadLine();
            if (string.IsNullOrEmpty(code) || code == "0") return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Get Student ID
                    int studentId = 0;
                    string findSql = "SELECT StudentId FROM Students WHERE StudentCode = @c";
                    using (var cmd = new NpgsqlCommand(findSql, conn))
                    {
                        cmd.Parameters.AddWithValue("c", code);
                        var res = cmd.ExecuteScalar();
                        if (res != null) studentId = (int)res;
                    }

                    if (studentId == 0)
                    {
                        Console.WriteLine("Student not found.");
                        Console.ReadKey();
                        return;
                    }

                    // 2. Input Score for YOUR SUBJECT ONLY
                    Console.WriteLine($"\nEditing {currentUser.Subject} score for student: {code}");

                    float score = -1;
                    while (true)
                    {
                        Console.Write($"Enter {currentUser.Subject} Score (0-100): ");
                        string input = Console.ReadLine();

                        if (input == "exit") return;

                        if (float.TryParse(input, out score) && score >= 0 && score <= 100)
                        {
                            break;
                        }
                        Console.WriteLine("Invalid score.");
                    }

                    // 3. Update Database (Dynamic Column Name)
                    // Note: We use double quotes around the column name to handle case sensitivity if needed
                    string sql = $"UPDATE Scores SET \"{currentUser.Subject.ToLower()}\" = @s WHERE StudentId = @sid";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("s", score);
                        cmd.Parameters.AddWithValue("sid", studentId);
                        cmd.ExecuteNonQuery();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nSuccess! Updated {currentUser.Subject} score to {score}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
            }
            Console.ReadKey();
        }
        // ==========================================
        // 4. RECORD ATTENDANCE
        // ==========================================
        public void RecordAttendance()
        {
            // Debug check
            Console.WriteLine("\n*** NEW CODE LOADED: RecordAttendance ***\n");

            if (currentUser == null || string.IsNullOrEmpty(currentUser.Subject))
            {
                Console.WriteLine("Error: No Subject assigned.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine($"=== ATTENDANCE: {currentUser.Subject} ===");
            DateTime date = DateTime.Today;
            Console.WriteLine($"Date: {date:yyyy-MM-dd}\n");
            Console.WriteLine("Press 'Y' for Present, 'N' for Absent.\n");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Get Students
                    var students = new List<Student>();
                    string sql = "SELECT StudentId, FullName, StudentCode FROM Students ORDER BY StudentCode ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new Student
                            {
                                StudentId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                StudentCode = reader.GetString(2)
                            });
                        }
                    }

                    // 2. Loop
                    int presentCount = 0;
                    foreach (var s in students)
                    {
                        Console.Write($"{s.StudentCode} - {s.FullName}: ");
                        string status = "Absent";

                        while (true)
                        {
                            var key = Console.ReadKey(intercept: true).Key;
                            if (key == ConsoleKey.Y)
                            {
                                Console.Write(" Present\n");
                                status = "Present";
                                presentCount++;
                                break;
                            }
                            if (key == ConsoleKey.N)
                            {
                                Console.Write(" Absent\n");
                                status = "Absent";
                                break;
                            }
                        }

                        // 3. Insert/Update Attendance
                        // Matches your DB table structure exactly
                        string insert = @"INSERT INTO Attendance (StudentId, Subject, ClassDate, Status) 
                                          VALUES (@sid, @sub, @date, @stat)
                                          ON CONFLICT (StudentId, Subject, ClassDate) 
                                          DO UPDATE SET Status = @stat";

                        using (var cmd = new NpgsqlCommand(insert, conn))
                        {
                            cmd.Parameters.AddWithValue("sid", s.StudentId);
                            cmd.Parameters.AddWithValue("sub", currentUser.Subject);
                            cmd.Parameters.AddWithValue("date", date);
                            cmd.Parameters.AddWithValue("stat", status);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine($"\nDone! {presentCount} Present.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.ReadKey();
        }

        // ==========================================
        // 5. VIEW ATTENDANCE REPORT
        // ==========================================
        public void ViewAttendanceReport()
        {
            PrintHeader("ATTENDANCE REPORT");

            Console.ForegroundColor = HeaderColor;
            Console.WriteLine("{0,-25} {1,-10} {2,-10} {3,-10} {4,-8}",
                "Student Name", "Total", "Present", "Absent", "Percent");
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
                        if (!reader.HasRows) Console.WriteLine("No attendance records yet.");

                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            long total = reader.GetInt64(1);
                            long present = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                            long absent = reader.IsDBNull(3) ? 0 : reader.GetInt64(3);
                            double pct = total == 0 ? 0 : Math.Round((double)present / total * 100, 1);

                            ConsoleColor pctColor = pct >= 90 ? SuccessColor : pct >= 70 ? WarningColor : ErrorColor;

                            Console.Write("{0,-25} {1,-10} ", name, total);
                            Console.ForegroundColor = SuccessColor;
                            Console.Write("{0,-10} ", present);
                            Console.ForegroundColor = WarningColor; // Orange/Yellow for Absent count
                            Console.Write("{0,-10} ", absent);
                            Console.ForegroundColor = pctColor;
                            Console.WriteLine("{0,-8}", pct + "%");
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // ===============================
        // 6. VIEW ACADEMIC REPORT
        // ===============================
        public void ViewAcademicReport()
        {
            PrintHeader("ACADEMIC PERFORMANCE REPORT");

            Console.ForegroundColor = HeaderColor;
            Console.WriteLine("{0,-10} {1,-20} {2,-8} {3,-6} {4,-6} {5,-8} {6,-8} {7,-10}",
                "Code", "Name", "Total", "Avg", "Grade", "Present", "Absent", "Status");
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
                            // Loop through columns 2 to 8 (The 7 subjects)
                            for (int i = 2; i <= 8; i++)
                            {
                                // Changed to Convert.ToSingle for safety
                                if (!reader.IsDBNull(i)) total += Convert.ToSingle(reader.GetValue(i));
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

                            Console.Write("{0,-10} {1,-20} {2,-8} {3,-6} {4,-6} {5,-8} {6,-8} ",
                                code, name, total, avgDisplay, gradeDisplay, present, absent);

                            Console.ForegroundColor = statusColor;
                            Console.WriteLine(statusDisplay);
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }

        private string CalculateGrade(float avg)
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
        // ==========================================
        // 9. REVIEW PERMISSION REQUESTS (EXCUSES)
        // ==========================================
        public void ReviewExcuses()
        {
            PrintHeader("REVIEW STUDENT EXCUSES");

            // 1. LIST TO STORE DATA (We fetch first, process later)
            var requests = new List<dynamic>();

            try
            {
                // STEP A: FETCH DATA ONLY
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                SELECT e.ExcuseId, s.StudentCode, s.FullName, e.ClassDate, e.Reason, s.StudentId 
                FROM Excuses e
                JOIN Students s ON e.StudentId = s.StudentId
                WHERE e.Status = 'Pending'
                ORDER BY e.ClassDate DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Code = reader.GetString(1),
                                Name = reader.GetString(2),
                                Date = reader.GetDateTime(3),
                                Reason = reader.GetString(4),
                                StudentId = reader.GetInt32(5)
                            });
                        }
                    }
                } // Connection 1 closes here. Safe!

                if (requests.Count == 0)
                {
                    PrintSuccess("No pending requests.");
                    Console.WriteLine("\nPress any key to return...");
                    Console.ReadKey();
                    return;
                }

                // STEP B: PROCESS REQUESTS (Ask User & Save)
                foreach (var req in requests)
                {
                    Console.WriteLine($"\nStudent: {req.Code} - {req.Name}");
                    Console.WriteLine($"Date:    {req.Date:yyyy-MM-dd}");
                    Console.WriteLine($"Reason:  {req.Reason}");
                    Console.WriteLine(new string('-', 40));

                    string choice = "";
                    while (true)
                    {
                        Console.Write("Accept this excuse? (Y/N/Skip): ");
                        choice = Console.ReadLine()?.Trim().ToUpper();
                        if (choice == "Y" || choice == "N" || choice == "SKIP") break;
                    }

                    if (choice == "SKIP") continue;

                    string status = (choice == "Y") ? "Accepted" : "Rejected";
                    string attendanceStatus = (choice == "Y") ? "Present" : "Absent";

                    // STEP C: SAVE CHANGES (Open a NEW FRESH connection for every save)
                    try
                    {
                        using (var conn = DbHelper.GetConnection())
                        {
                            conn.Open();
                            using (var trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // 1. Update Excuse Status
                                    using (var cmd = new NpgsqlCommand("UPDATE Excuses SET Status = @st WHERE ExcuseId = @id", conn))
                                    {
                                        cmd.Parameters.AddWithValue("st", status);
                                        cmd.Parameters.AddWithValue("id", req.Id);
                                        cmd.ExecuteNonQuery();
                                    }

                                    // 2. Update Attendance
                                    string attendSql = @"
                                INSERT INTO Attendance (StudentId, Subject, ClassDate, Status)
                                VALUES (@sid, @sub, @date, @stat)
                                ON CONFLICT (StudentId, Subject, ClassDate)
                                DO UPDATE SET Status = @stat";

                                    using (var cmd = new NpgsqlCommand(attendSql, conn))
                                    {
                                        cmd.Parameters.AddWithValue("sid", req.StudentId);
                                        cmd.Parameters.AddWithValue("sub", currentUser.Subject);
                                        cmd.Parameters.AddWithValue("date", req.Date);
                                        cmd.Parameters.AddWithValue("stat", attendanceStatus);
                                        cmd.ExecuteNonQuery();
                                    }

                                    trans.Commit();

                                    if (choice == "Y") PrintSuccess("Accepted & Marked Present.");
                                    else PrintError("Rejected & Marked Absent.");
                                }
                                catch (Exception ex)
                                {
                                    trans.Rollback();
                                    PrintError("Save failed: " + ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError("Connection error: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError("Error loading requests: " + ex.Message);
            }

            Console.WriteLine("\nAll pending requests reviewed.");
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }
    }
}