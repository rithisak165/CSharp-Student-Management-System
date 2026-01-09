using System;
using System.Collections.Generic;
using Npgsql;
using Student_Management_System__SMS_.Models;

namespace Student_Management_System__SMS_.DataAccess
{
    public class StudentSide
    {
        private User _currentUser;

        // UI Colors
        private static readonly ConsoleColor HeaderColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
        private static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;

        public StudentSide(User user)
        {
            _currentUser = user;
        }
        // ==========================================
        // 1. VIEW CLASS INFORMATIOM
        // ==========================================
        public void ViewClassInfo()
        {
            Console.Clear();
            PrintHeader("CLASS ANNOUNCEMENTS");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // CHANGED SQL: Select Subject column as well, removed WHERE clause
                    // We order by Date DESC so the newest announcements appear at the top
                    string sql = @"
                SELECT ClassDate, Subject, Message 
                FROM ClassInfo 
                ORDER BY ClassDate DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("\nNo announcements found.");
                            Console.WriteLine("\nPress any key to go back...");
                            Console.ReadKey();
                            return;
                        }

                        // TABLE HEADER
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("{0,-12} {1,-18} {2,-50}", "Date", "Subject", "Message");
                        Console.WriteLine(new string('─', 85));
                        Console.ResetColor();

                        while (reader.Read())
                        {
                            DateTime date = reader.GetDateTime(0);
                            string rawSubject = reader.GetString(1);
                            string message = reader.GetString(2);

                            // Clean up Subject Name (Optional: Make it look nicer)
                            string displaySubject = (rawSubject == "OOP_OOM") ? "OOP" : rawSubject;

                            // Highlight TODAY'S announcements in Yellow
                            if (date.Date == DateTime.Today)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                            }

                            Console.WriteLine("{0,-12} {1,-18} {2,-50}",
                                date.ToString("yyyy-MM-dd"),
                                displaySubject,
                                message);

                            // Reset color for next row
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError("Error loading announcements: " + ex.Message);
            }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }
        // ==========================================
        // 2. VIEW MY SCORES
        // ==========================================
        public void ViewMyScores()
        {
            Console.Clear();
            PrintHeader("MY ACADEMIC RESULT");

            if (_currentUser == null || _currentUser.StudentId == null) return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT s.FullName, 
                               sc.Python, sc.OOP_OOM, sc.WritingII, 
                               sc.DatabaseSystem, sc.Microprocessor, sc.Network, sc.CoreEnglish 
                        FROM Students s 
                        LEFT JOIN Scores sc ON s.StudentId = sc.StudentId 
                        WHERE s.StudentId = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", _currentUser.StudentId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString(0);
                                string[] subjects = { "Python", "OOP_OOM", "Writing II", "DB System", "Microprocessor", "Network", "English" };
                                float[] scores = new float[7];
                                float total = 0;
                                bool hasAnyScore = false;

                                // Read scores
                                for (int i = 0; i < 7; i++)
                                {
                                    if (!reader.IsDBNull(i + 1))
                                    {
                                        // Use Convert.ToSingle to be safe against double/decimal types in DB
                                        float val = Convert.ToSingle(reader.GetValue(i + 1));
                                        scores[i] = val;
                                        total += val;
                                        if (val > 0) hasAnyScore = true;
                                    }
                                }

                                if (!hasAnyScore)
                                {
                                    Console.WriteLine($"Student: {name}");
                                    Console.WriteLine(new string('-', 40));
                                    Console.WriteLine("  Scores have not been finalized yet.");
                                    Console.WriteLine(new string('-', 40));
                                }
                                else
                                {
                                    Console.WriteLine($"Student: {name}\n");
                                    Console.WriteLine("{0,-20} {1}", "SUBJECT", "SCORE");
                                    Console.WriteLine(new string('-', 30));

                                    for (int i = 0; i < 7; i++)
                                    {
                                        Console.WriteLine($"{subjects[i],-20} {scores[i]}");
                                    }

                                    float avg = total / 7;
                                    string grade = CalculateGrade(avg);
                                    string status = avg >= 60 ? "PASS" : "FAIL";

                                    Console.WriteLine(new string('-', 30));
                                    Console.WriteLine($"Total:   {total}");
                                    Console.WriteLine($"Average: {avg:F1}");

                                    Console.Write("Grade:   ");
                                    Console.ForegroundColor = avg >= 60 ? SuccessColor : ErrorColor;
                                    Console.WriteLine($"{grade} ({status})");
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        // ==========================================
        // 3. VIEW MY ATTENDANCE
        // ==========================================
        public void ViewMyAttendance()
        {
            Console.Clear();
            PrintHeader("MY ATTENDANCE SUMMARY");

            if (_currentUser == null || _currentUser.StudentId == null) return;

            // 1. Check for Pending Requests (Optional Alert)
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string pendingSql = "SELECT COUNT(*) FROM Excuses WHERE StudentId = @sid AND Status = 'Pending'";
                    using (var cmd = new NpgsqlCommand(pendingSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        long pending = (long)cmd.ExecuteScalar();
                        if (pending > 0)
                        {
                            PrintWarning($"* NOTE: You have {pending} pending excuse request(s).\n");
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError("Error checking alerts: " + ex.Message); }

            // 2. Select Subject Menu
            // Make sure these names match exactly what is in your DB
            string[] subjects = { "Python", "OOP_OOM", "Network", "Database System", "Microprocessor", "Writing II", "Core English" };

            Console.WriteLine("Select a Subject to view attendance:");
            for (int i = 0; i < subjects.Length; i++)
            {
                Console.WriteLine($" {i + 1}. {subjects[i]}");
            }
            Console.WriteLine(" 0. Go Back");

            Console.Write("\nEnter choice: ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > subjects.Length)
            {
                PrintError("Invalid selection.");
                Console.ReadKey();
                return;
            }

            if (choice == 0) return;

            string selectedSubject = subjects[choice - 1]; // Get subject string from array

            Console.Clear();
            PrintHeader($"ATTENDANCE: {selectedSubject.ToUpper()}");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 3. Query filtered by Subject AND Student
                    string sql = @"
                SELECT ClassDate, Status 
                FROM Attendance 
                WHERE StudentId = @sid AND Subject = @sub 
                ORDER BY ClassDate DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        cmd.Parameters.AddWithValue("sub", selectedSubject);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("{0,-15} | {1}", "Date", "Status");
                                Console.WriteLine(new string('-', 30));

                                int presentCount = 0;
                                int absentCount = 0;

                                while (reader.Read())
                                {
                                    DateTime date = reader.GetDateTime(0);
                                    string status = reader.GetString(1);

                                    Console.Write("{0,-15} | ", date.ToString("yyyy-MM-dd"));

                                    if (status == "Present" || status == "Late")
                                    {
                                        Console.ForegroundColor = SuccessColor;
                                        presentCount++;
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ErrorColor;
                                        absentCount++;
                                    }

                                    Console.WriteLine(status);
                                    Console.ResetColor();
                                }

                                // 4. Mini Summary for this specific subject
                                Console.WriteLine(new string('-', 30));
                                Console.WriteLine($"Total Present: {presentCount}");
                                Console.WriteLine($"Total Absent:  {absentCount}");
                            }
                            else
                            {
                                Console.WriteLine($"No attendance records found for {selectedSubject}.");
                                Console.WriteLine("Class has not started yet or no data recorded.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        // ==========================================
        // 4. VIEW ACADEMIC REPORT (FOR ALL STUDENTS)
        // ==========================================
        public void ViewAcademicReport()
        {
            Console.Clear();
            PrintHeader("ACADEMIC REPORT");

            if (_currentUser == null || _currentUser.StudentId == null) return;

            // 1. SELECT SUBJECT
            string[] subjectNames = { "Python", "OOP_OOM", "Network", "DatabaseSystem", "Microprocessor", "WritingII", "CoreEnglish" };
            string[] displayNames = { "Python", "OOP", "Network", "Database System", "Microprocessor", "Writing II", "Core English" };

            Console.WriteLine("Select Subject:");
            for (int i = 0; i < displayNames.Length; i++)
            {
                Console.WriteLine($" {i + 1}. {displayNames[i]}");
            }
            Console.WriteLine(" 0. Go Back");

            Console.Write("\nEnter choice: ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > subjectNames.Length)
            {
                PrintError("Invalid selection.");
                Console.ReadKey();
                return;
            }

            if (choice == 0) return;

            string dbColumnName = subjectNames[choice - 1];
            string displaySub = displayNames[choice - 1];

            Console.Clear();
            PrintHeader("ACADEMIC RESULT");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // ====================================================
                    // STEP 2: GET ATTENDANCE
                    // ====================================================
                    long present = 0;
                    long absent = 0;

                    string attSql = @"
                SELECT 
                    COUNT(CASE WHEN Status = 'Present' OR Status = 'Late' THEN 1 END),
                    COUNT(CASE WHEN Status = 'Absent' THEN 1 END)
                FROM Attendance 
                WHERE StudentId = @sid AND Subject = @sub";

                    using (var cmd = new NpgsqlCommand(attSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);

                        string attSubjectName = (dbColumnName == "OOP_OOM") ? "OOP" :
                                                (dbColumnName == "DatabaseSystem") ? "Database System" :
                                                (dbColumnName == "WritingII") ? "Writing II" :
                                                (dbColumnName == "CoreEnglish") ? "Core English" : dbColumnName;

                        cmd.Parameters.AddWithValue("sub", attSubjectName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                present = reader.GetInt64(0);
                                absent = reader.GetInt64(1);
                            }
                        }
                    }

                    // ====================================================
                    // STEP 3: GET SCORE & INFO
                    // ====================================================
                    string scoreSql = $@"
                SELECT s.StudentCode, s.FullName, sc.{dbColumnName} 
                FROM Students s 
                LEFT JOIN Scores sc ON s.StudentId = sc.StudentId 
                WHERE s.StudentId = @sid";

                    string code = "N/A";
                    string name = "Unknown";
                    float score = 0;
                    bool hasScore = false;

                    using (var cmd = new NpgsqlCommand(scoreSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) code = reader.GetString(0);
                                if (!reader.IsDBNull(1)) name = reader.GetString(1);

                                if (name.Length > 18) name = name.Substring(0, 17) + ".";

                                if (!reader.IsDBNull(2))
                                {
                                    score = Convert.ToSingle(reader.GetValue(2));
                                    if (score > 0) hasScore = true;
                                }
                            }
                        }
                    }

                    // ====================================================
                    // STEP 4: CALCULATE PERCENTAGE FOR STATUS
                    // ====================================================
                    long totalClasses = present + absent;
                    string statusStr;
                    ConsoleColor statusColor;

                    if (totalClasses == 0)
                    {
                        // Class hasn't started yet
                        statusStr = "-";
                        statusColor = ConsoleColor.White;
                    }
                    else
                    {
                        double percentage = (double)present / totalClasses * 100;
                        statusStr = $"{percentage:F0}%";

                        if (percentage >= 80) statusColor = SuccessColor;
                        else if (percentage >= 50) statusColor = WarningColor;
                        else statusColor = ErrorColor;
                    }

                    string scoreStr = hasScore ? score.ToString() : "-";
                    string gradeStr = hasScore ? CalculateGrade(score) : "-";

                    // ====================================================
                    // STEP 5: DISPLAY TABLE
                    // ====================================================
                    Console.ForegroundColor = HeaderColor;
                    Console.WriteLine("{0,-12} {1,-20} {2,-15} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}",
                        "Code", "Name", "Subject", "Score", "Grade", "Present", "Absent", "Status");
                    Console.WriteLine(new string('─', 100));
                    Console.ResetColor();

                    Console.Write("{0,-12} {1,-20} {2,-15} {3,-8} {4,-8} {5,-8} {6,-8} ",
                        code, name, displaySub, scoreStr, gradeStr, present, absent);

                    Console.ForegroundColor = statusColor;
                    Console.WriteLine(statusStr);
                    Console.ResetColor();
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }
        // ==========================================
        // 5. REQUEST PERMISSION (The Missing Feature)
        // ==========================================
        public void RequestPermission()
        {
            Console.Clear();
            PrintHeader("REQUEST PERMISSION / EXCUSE");
            Console.WriteLine("(Type '0' to Cancel)\n");

            if (_currentUser == null || _currentUser.StudentId == null)
            {
                PrintError("Error: Student ID not found. Please log in again.");
                Console.ReadKey();
                return;
            }

            // ==========================================
            // 1. Select Subject (NEW STEP)
            // ==========================================
            // We use the same list as your other functions for consistency
            string[] subjects = { "Python", "OOP", "Network", "Database System", "Microprocessor", "Writing II", "Core English" };

            Console.WriteLine("Select Subject for the Request:");
            for (int i = 0; i < subjects.Length; i++)
            {
                Console.WriteLine($" {i + 1}. {subjects[i]}");
            }

            Console.Write("\nEnter choice: ");
            string choiceInput = Console.ReadLine();
            if (choiceInput == "0") return;

            if (!int.TryParse(choiceInput, out int choice) || choice < 1 || choice > subjects.Length)
            {
                PrintError("Invalid selection.");
                Console.ReadKey();
                return;
            }

            string selectedSubject = subjects[choice - 1];

            // ==========================================
            // 2. Get Date
            // ==========================================
            DateTime date = DateTime.Today;
            Console.Write($"\nEnter Date (YYYY-MM-DD) [Default: {date:yyyy-MM-dd}]: ");
            string dateInput = Console.ReadLine()?.Trim();

            if (dateInput == "0") return;
            if (!string.IsNullOrEmpty(dateInput))
            {
                if (!DateTime.TryParse(dateInput, out date))
                {
                    PrintWarning("Invalid date format. Using today's date instead.");
                    date = DateTime.Today;
                }
            }

            // ==========================================
            // 3. Get Reason
            // ==========================================
            string reason = "";
            while (string.IsNullOrWhiteSpace(reason))
            {
                Console.Write("Enter Reason: ");
                reason = Console.ReadLine()?.Trim();
                if (reason == "0") return;
                if (string.IsNullOrWhiteSpace(reason)) PrintWarning("Reason cannot be empty.");
            }

            // ==========================================
            // 4. Insert into Database
            // ==========================================
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Check for duplicates (Same Student + Same Date + Same Subject)
                    string checkSql = "SELECT COUNT(*) FROM Excuses WHERE StudentId = @sid AND ClassDate = @date AND Subject = @sub";
                    using (var cmd = new NpgsqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        cmd.Parameters.AddWithValue("date", date);
                        cmd.Parameters.AddWithValue("sub", selectedSubject);

                        long count = (long)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            PrintError($"You already have a pending/approved request for {selectedSubject} on this date.");
                            Console.ReadKey();
                            return;
                        }
                    }

                    // Insert Record (Make sure your table has a 'Subject' column!)
                    string sql = "INSERT INTO Excuses (StudentId, ClassDate, Reason, Status, Subject) VALUES (@sid, @date, @r, 'Pending', @sub)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        cmd.Parameters.AddWithValue("date", date);
                        cmd.Parameters.AddWithValue("r", reason);
                        cmd.Parameters.AddWithValue("sub", selectedSubject);

                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess($"Request for {selectedSubject} submitted successfully!");
                    Console.WriteLine("Your teacher will review it.");
                }
            }
            catch (Exception ex)
            {
                PrintError("Database Error: " + ex.Message);
                Console.WriteLine("\nNote: Does your 'Excuses' table have a 'Subject' column?");
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        // ==========================================
        // 6. VIEW PROFILE (Replaces Academic Report)
        // ==========================================
        public void ViewProfile()
        {
            Console.Clear();
            PrintHeader("MY PROFILE");

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT StudentCode, FullName, class FROM Students WHERE StudentId = @sid";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine($"Name:     {reader.GetString(1)}");
                                Console.WriteLine($"Code:     {reader.GetString(0)}");
                                Console.WriteLine($"Group:    {reader.GetString(2)}");
                                Console.WriteLine($"Email:    {_currentUser.Email}");
                                Console.WriteLine($"Role:     {_currentUser.Role}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { PrintError(ex.Message); }

            Console.WriteLine("\nPress any key to go back...");
            Console.ReadKey();
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private string CalculateGrade(float score)
        {
            if (score >= 90) return "A";
            if (score >= 80) return "B";
            if (score >= 70) return "C";
            if (score >= 60) return "D";
            return "F";
        }

        private void PrintHeader(string title)
        {
            Console.ForegroundColor = HeaderColor;
            Console.WriteLine($"=== {title} ===");
            Console.ResetColor();
        }

        private void PrintSuccess(string msg)
        {
            Console.ForegroundColor = SuccessColor;
            Console.WriteLine($"✓ {msg}");
            Console.ResetColor();
        }

        private void PrintError(string msg)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine($"✗ Error: {msg}");
            Console.ResetColor();
        }

        private void PrintWarning(string msg)
        {
            Console.ForegroundColor = WarningColor;
            Console.WriteLine($"! {msg}");
            Console.ResetColor();
        }
    }
}