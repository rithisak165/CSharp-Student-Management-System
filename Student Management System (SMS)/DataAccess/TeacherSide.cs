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
            // Use Today's Date automatically (Old Logic)
            DateTime date = DateTime.Today;

            if (currentUser == null || string.IsNullOrEmpty(currentUser.Subject))
            {
                PrintError("Error: No Subject assigned.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine($"=== ATTENDANCE: {currentUser.Subject} ===");
            Console.WriteLine($"Date: {date:yyyy-MM-dd}\n");
            Console.WriteLine("Instruction: Press 'Y' for Present, 'N' for Absent.");
            Console.WriteLine("(If a student has an excuse, you will be asked to Allow it first)\n");

            // Clean subject name for searching excuses (Space insensitive)
            string cleanSubject = currentUser.Subject.Split('_')[0].Replace(" ", "");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. GET STUDENTS
                    var students = new List<dynamic>();
                    string sqlStudents = "SELECT StudentId, FullName, StudentCode FROM Students ORDER BY StudentCode ASC";
                    using (var cmd = new NpgsqlCommand(sqlStudents, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new
                            {
                                StudentId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                StudentCode = reader.GetString(2)
                            });
                        }
                    }

                    // 2. GET EXCUSES (Only for TODAY and THIS SUBJECT)
                    var excuses = new Dictionary<int, dynamic>();
                    string sqlExcuses = @"
                SELECT StudentId, Reason, ExcuseId 
                FROM Excuses 
                WHERE ClassDate = @date 
                  AND Status = 'Pending'
                  AND (REPLACE(Subject, ' ', '') ILIKE @sub OR @sub ILIKE '%' || REPLACE(Subject, ' ', '') || '%')";

                    using (var cmd = new NpgsqlCommand(sqlExcuses, conn))
                    {
                        cmd.Parameters.AddWithValue("date", date);
                        cmd.Parameters.AddWithValue("sub", cleanSubject);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                excuses[reader.GetInt32(0)] = new
                                {
                                    Reason = reader.GetString(1),
                                    ExcuseId = reader.GetInt32(2)
                                };
                            }
                        }
                    }

                    // 3. LOOP THROUGH STUDENTS
                    int presentCount = 0;
                    foreach (var s in students)
                    {
                        Console.Write($"{s.StudentCode} - {s.FullName}: ");
                        string status = "Absent";

                        // --- CHECK: Does this student have an excuse for TODAY? ---
                        if (excuses.ContainsKey(s.StudentId))
                        {
                            var excuse = excuses[s.StudentId];

                            // Show the Excuse Prompt
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"\n   [?] Excuse Request: \"{excuse.Reason}\"\n   Allow? (Y=Present / N=Absent): ");
                            Console.ResetColor();

                            string decision = Console.ReadLine()?.Trim().ToUpper();

                            if (decision == "Y")
                            {
                                status = "Present";
                                presentCount++;
                                // Update Excuse DB -> Accepted
                                using (var updateCmd = new NpgsqlCommand("UPDATE Excuses SET Status = 'Accepted' WHERE ExcuseId = @eid", conn))
                                {
                                    updateCmd.Parameters.AddWithValue("eid", excuse.ExcuseId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                Console.WriteLine("   -> Allowed. Marked Present.");
                            }
                            else
                            {
                                status = "Absent";
                                // Update Excuse DB -> Rejected
                                using (var updateCmd = new NpgsqlCommand("UPDATE Excuses SET Status = 'Rejected' WHERE ExcuseId = @eid", conn))
                                {
                                    updateCmd.Parameters.AddWithValue("eid", excuse.ExcuseId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                Console.WriteLine("   -> Denied. Marked Absent.");
                            }
                        }
                        else
                        {
                            // --- NO EXCUSE: Use your OLD Logic (Key Press) ---
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
                        }

                        // 4. INSERT/UPDATE ATTENDANCE
                        string insert = @"
                    INSERT INTO Attendance (StudentId, Subject, ClassDate, Status) 
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
            PrintHeader($"ATTENDANCE REPORT: {currentUser.Subject}");

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();

                // ========================================================
                // 1. SAFETY CHECK: Did the class even start?
                // ========================================================
                // We count how many attendance rows exist for this Subject.
                string checkSql = "SELECT COUNT(*) FROM Attendance WHERE Subject = @sub";
                using (var cmd = new NpgsqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("sub", currentUser.Subject);
                    long totalRecords = (long)cmd.ExecuteScalar();

                    if (totalRecords == 0)
                    {
                        // If the count is 0, it means NO ONE has been marked yet.
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\nNotice: No attendance records found for {currentUser.Subject}.");
                        Console.WriteLine("The class has not started yet (or no records entered).");
                        Console.ResetColor();

                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey();
                        return; // Stop here. Don't show the table.
                    }
                }

                // ========================================================
                // 2. SHOW REPORT (Only if records exist)
                // ========================================================
                string sql = @"
            SELECT s.StudentCode, s.FullName, 
                   COUNT(CASE WHEN a.Status = 'Present' THEN 1 END) as PresentCount,
                   COUNT(CASE WHEN a.Status = 'Absent' THEN 1 END) as AbsentCount
            FROM Students s
            LEFT JOIN Attendance a ON s.StudentId = a.StudentId 
                 AND a.Subject = @sub  -- Ensure we only count THIS subject
            GROUP BY s.StudentId, s.StudentCode, s.FullName
            ORDER BY s.StudentCode";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("sub", currentUser.Subject);

                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("{0,-10} {1,-20} {2,-10} {3,-10}", "Code", "Name", "Present", "Absent");
                        Console.WriteLine(new string('-', 55));

                        while (reader.Read())
                        {
                            string code = reader.GetString(0);
                            string name = reader.GetString(1);
                            long present = reader.GetInt64(2);
                            long absent = reader.GetInt64(3);

                            // If a specific student is 0|0, they just haven't attended THIS subject yet.
                            Console.WriteLine("{0,-10} {1,-20} {2,-10} {3,-10}", code, name, present, absent);
                        }
                    }
                }
            }
            Console.WriteLine("\nPress any key to return...");
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
            PrintHeader("            DELETE STUDENT");

            PrintWarning("This action will permanently delete the student and ALL related data (scores, attendance, excuses, login).");
            Console.Write("\nEnter Student Code: ");
            string code = Console.ReadLine();
            Console.Write($"\nType 'DELETE {code}' to confirm: ");
            string confirm = Console.ReadLine();

            if (confirm != $"DELETE {code}")
            {
                Console.WriteLine("Deletion cancelled.");
                Console.ReadKey();
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Find the Student ID
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

                    // 2. Delete EVERYTHING related to this ID
                    // I added "DELETE FROM Excuses" to the top of this list.
                    string masterDeleteSql = @"
                DELETE FROM Excuses WHERE StudentId = @id;     -- <--- NEW LINE ADDED
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
        public void ViewBehaviorReport()
        {
            PrintHeader("DETAILED BEHAVIOR REPORT");

            if (string.IsNullOrEmpty(currentUser.Subject))
            {
                PrintError("Error: Your account has no subject.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Subject: {currentUser.Subject}");
            Console.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n");

            var goodStudents = new List<string>();
            var badStudents = new List<string>();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // SQL: Get Counts AND The List of Dates for Absences/Requests
                    // STRING_AGG joins the dates into a single text string like "2023-01-01, 2023-01-05"
                    string sql = @"
                SELECT 
                    s.StudentCode, 
                    s.FullName,
                    -- 2. Count Presents
                    (SELECT COUNT(*) FROM Attendance a WHERE a.StudentId = s.StudentId AND a.Subject = @sub AND a.Status = 'Present') as P_Count,
                    
                    -- 3. Count Absents & List Dates
                    (SELECT COUNT(*) FROM Attendance a WHERE a.StudentId = s.StudentId AND a.Subject = @sub AND a.Status = 'Absent') as A_Count,
                    (SELECT STRING_AGG(TO_CHAR(ClassDate, 'MM-dd'), ', ') FROM Attendance a WHERE a.StudentId = s.StudentId AND a.Subject = @sub AND a.Status = 'Absent') as A_Dates,

                    -- 4. Count Requests & List Dates
                    (SELECT COUNT(*) FROM Excuses e WHERE e.StudentId = s.StudentId AND e.Subject = @sub) as R_Count,
                    (SELECT STRING_AGG(TO_CHAR(ClassDate, 'MM-dd'), ', ') FROM Excuses e WHERE e.StudentId = s.StudentId AND e.Subject = @sub) as R_Dates

                FROM Students s
                ORDER BY P_Count DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sub", currentUser.Subject);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string code = reader.GetString(0);
                                string name = reader.GetString(1);
                                long pCount = reader.GetInt64(2);

                                long aCount = reader.GetInt64(3);
                                string aDates = reader.IsDBNull(4) ? "" : reader.GetString(4); // Handle nulls

                                long rCount = reader.GetInt64(5);
                                string rDates = reader.IsDBNull(6) ? "" : reader.GetString(6); // Handle nulls

                                // --- LOGIC ---
                                // "Bad" if Absents > 3 OR Requests >= 10
                                if (aCount > 3 || rCount >= 10)
                                {
                                    string info = $" {code} - {name}";
                                    info += $"\n\t   [!] Absences ({aCount}): {aDates}";
                                    info += $"\n\t   [!] Requests ({rCount}): {rDates}";

                                    // Add a special warning tag
                                    string reason = (aCount > 3) ? "CRITICAL ABSENCE" : "PERMISSION SPAM";
                                    badStudents.Add($"{info}\n\t   >>> WARNING: {reason}");
                                }
                                else
                                {
                                    // Good Student Row
                                    string row = $" {code,-10} | {name,-18} | Present: {pCount,-3} | Absent: {aCount,-3} (Dates: {aDates})";
                                    goodStudents.Add(row);
                                }
                            }
                        }
                    }
                }

                // ============================================
                // 1. PRINT GOOD STUDENTS (GREEN)
                // ============================================
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("================================================================================");
                Console.WriteLine("   ✅ TOP ATTENDANCE (SAFE ZONE)");
                Console.WriteLine("================================================================================");

                if (goodStudents.Count > 0)
                {
                    foreach (var s in goodStudents)
                    {
                        Console.WriteLine(s);
                    }
                }
                else
                {
                    Console.WriteLine("   (No students found)");
                }

                // ============================================
                // 2. PRINT WARNING STUDENTS (RED)
                // ============================================
                Console.WriteLine("\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("================================================================================");
                Console.WriteLine("   ⚠️  WARNING ZONE (List of Days Missed/Requested)");
                Console.WriteLine("================================================================================");

                if (badStudents.Count > 0)
                {
                    foreach (var s in badStudents)
                    {
                        Console.WriteLine(s);
                        Console.WriteLine(new string('-', 74));
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("   (No students at risk)");
                }

                Console.ResetColor();
            }
            catch (Exception ex)
            {
                PrintError("Error generating report: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        // ==========================================
        // 10. ANNOUNCE CLASS UPDATE / CANCELLATION
        // ==========================================
        public void AnnounceNoClass()
        {
            Console.Clear();
            PrintHeader("ANNOUNCE CLASS UPDATE / CANCELLATION");

            // Ensure Teacher has a subject
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Subject) || currentUser.Subject == "General")
            {
                PrintError("Error: Your account does not have a specific Subject assigned.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Subject: {currentUser.Subject}\n");

            // 1. Get Date
            DateTime date = DateTime.Today;
            Console.Write($"Enter Date (YYYY-MM-DD) [Default: {date:yyyy-MM-dd}]: ");
            string dateInput = Console.ReadLine()?.Trim();

            if (dateInput == "0") return;
            if (!string.IsNullOrEmpty(dateInput))
            {
                if (!DateTime.TryParse(dateInput, out date))
                {
                    PrintWarning("Invalid date. Using today's date.");
                    date = DateTime.Today;
                }
            }

            // 2. Get Message (Reason)
            string message = "";
            while (string.IsNullOrWhiteSpace(message))
            {
                Console.Write("Enter Message (e.g., 'No Class', 'Sick', 'Holiday'): ");
                message = Console.ReadLine()?.Trim();
                if (message == "0") return;
                if (string.IsNullOrWhiteSpace(message)) PrintWarning("Message cannot be empty.");
            }

            // 3. Save to Database
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Upsert: If date exists for this subject, update the message. If not, insert new.
                    string sql = @"
                INSERT INTO ClassInfo (Subject, ClassDate, Message)
                VALUES (@sub, @date, @msg)
                ON CONFLICT (Subject, ClassDate)
                DO UPDATE SET Message = @msg";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sub", currentUser.Subject);
                        cmd.Parameters.AddWithValue("date", date);
                        cmd.Parameters.AddWithValue("msg", message);
                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess($"Announcement for {currentUser.Subject} on {date:yyyy-MM-dd} saved!");
                }
            }
            catch (Exception ex)
            {
                PrintError("Database Error: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        public void ShowSystemGuide()
        {
            Console.Clear();
            PrintHeader("SYSTEM USER GUIDE");

            Console.WriteLine("Welcome to the Student Management System. Here is how to use the key features:\n");

            // 1. ATTENDANCE
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1. RECORDING ATTENDANCE (Option 4)");
            Console.ResetColor();
            Console.WriteLine("   - The system automatically selects TODAY'S date.");
            Console.WriteLine("   - Logic: It loops through every student.");
            Console.WriteLine("   - If a student sent an Excuse Request, the system PAUSES and asks you:");
            Console.WriteLine("     \"Allow this excuse? (Y/N)\"");
            Console.WriteLine("     -> YES: Marks Present (Excuse Accepted).");
            Console.WriteLine("     -> NO:  Marks Absent (Excuse Rejected).");
            Console.WriteLine("   - If no excuse, simply press 'Y' (Present) or 'N' (Absent) quickly.\n");

            // 2. BEHAVIOR REPORT
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("2. BEHAVIOR REPORT (Option 9)");
            Console.ResetColor();
            Console.WriteLine("   - Shows a split list of students.");
            Console.WriteLine("   - GREEN ZONE: Students with good attendance.");
            Console.WriteLine("   - RED ZONE: Students with >3 Absences or >10 Permission Requests.");
            Console.WriteLine("   - It lists specific dates of absence so you can warn them effectively.\n");

            // 3. SCORES
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("3. MANAGING SCORES (Option 3)");
            Console.ResetColor();
            Console.WriteLine("   - Scores are tied to YOUR subject only.");
            Console.WriteLine("   - You can update Midterm, Final, and Assignment scores.");
            Console.WriteLine("   - The system automatically calculates the Total and Grade (A-F).\n");

            // 4. STUDENT REQUESTS
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("4. STUDENT PERMISSIONS");
            Console.ResetColor();
            Console.WriteLine("   - Students can request permission via their menu.");
            Console.WriteLine("   - LIMIT: Students are limited to 10 requests per subject.");
            Console.WriteLine("   - You do not need to check a mailbox; requests appear automatically");
            Console.WriteLine("     when you run 'Record Attendance'.\n");

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
        }
    }
}