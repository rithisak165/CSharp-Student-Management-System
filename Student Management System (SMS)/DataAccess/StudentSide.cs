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
        // 1. VIEW MY SCORES
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
        // 2. VIEW MY ATTENDANCE
        // ==========================================
        public void ViewMyAttendance()
        {
            Console.Clear();
            PrintHeader("MY ATTENDANCE SUMMARY");

            if (_currentUser == null || _currentUser.StudentId == null) return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Get Pending Requests Warning
                    string pendingSql = "SELECT COUNT(*) FROM Excuses WHERE StudentId = @sid AND Status = 'Pending'";
                    using (var cmd = new NpgsqlCommand(pendingSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        long pending = (long)cmd.ExecuteScalar();
                        if (pending > 0)
                        {
                            Console.ForegroundColor = WarningColor;
                            Console.WriteLine($"* You have {pending} pending excuse request(s).\n");
                            Console.ResetColor();
                        }
                    }

                    // 2. Get Records
                    string sql = "SELECT Subject, ClassDate, Status FROM Attendance WHERE StudentId = @sid ORDER BY ClassDate DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("{0,-15} | {1,-12} | {2}", "Subject", "Date", "Status");
                                Console.WriteLine(new string('-', 45));

                                while (reader.Read())
                                {
                                    string subject = reader.GetString(0);
                                    string date = reader.GetDateTime(1).ToString("yyyy-MM-dd");
                                    string status = reader.GetString(2);

                                    Console.Write("{0,-15} | {1,-12} | ", subject, date);

                                    if (status == "Present") Console.ForegroundColor = SuccessColor;
                                    else Console.ForegroundColor = ErrorColor;

                                    Console.WriteLine(status);
                                    Console.ResetColor();
                                }
                            }
                            else
                            {
                                Console.WriteLine("No attendance records found.");
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
        // 3. VIEW ACADEMIC REPORT (FOR ALL STUDENTS)
        // ==========================================
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
        // ==========================================
        // 4. REQUEST PERMISSION (The Missing Feature)
        // ==========================================
        public void RequestPermission()
        {
            Console.Clear();
            PrintHeader("REQUEST PERMISSION / EXCUSE");
            Console.WriteLine("(Type '0' to Cancel)\n");

            if (_currentUser == null || _currentUser.StudentId == null)
            {
                PrintError("User error. Cannot identify student ID.");
                Console.ReadKey();
                return;
            }

            // 1. Get Date
            DateTime date = DateTime.Today;
            Console.Write($"Enter Date (YYYY-MM-DD) [Default: {date:yyyy-MM-dd}]: ");
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

            // 2. Get Reason
            string reason = "";
            while (string.IsNullOrWhiteSpace(reason))
            {
                Console.Write("Enter Reason for absence: ");
                reason = Console.ReadLine()?.Trim();
                if (reason == "0") return;
                if (string.IsNullOrWhiteSpace(reason)) PrintWarning("Reason cannot be empty.");
            }

            // 3. Insert into Database
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Check if request already exists for this day
                    string checkSql = "SELECT COUNT(*) FROM Excuses WHERE StudentId = @sid AND ClassDate = @date";
                    using (var cmd = new NpgsqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        cmd.Parameters.AddWithValue("date", date);
                        long count = (long)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            PrintError("You have already submitted a request for this date.");
                            Console.ReadKey();
                            return;
                        }
                    }

                    // Insert the excuse
                    string sql = @"INSERT INTO Excuses (StudentId, ClassDate, Reason, Status) 
                                   VALUES (@sid, @date, @r, 'Pending')";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);
                        cmd.Parameters.AddWithValue("date", date);
                        cmd.Parameters.AddWithValue("r", reason);
                        cmd.ExecuteNonQuery();
                    }

                    PrintSuccess("Request submitted successfully!");
                    Console.WriteLine("Your teacher will review it. Check status later.");
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
        // ==========================================
        // 5. VIEW PROFILE (Replaces Academic Report)
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