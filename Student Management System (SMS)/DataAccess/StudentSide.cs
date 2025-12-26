using System;
using System.Collections.Generic;
using Npgsql;
using Student_Management_System__SMS_.Models;

namespace Student_Management_System__SMS_.DataAccess
{
    public class StudentSide
    {
        // We store the logged-in user here so all functions can use it
        private User _currentUser;

        // CONSTRUCTOR: We force the main program to send us the user
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

            if (_currentUser == null || _currentUser.StudentId == null)
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
                        cmd.Parameters.AddWithValue("id", _currentUser.StudentId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString(0);

                                // 1. PREPARE DATA
                                string[] subjects = { "Python", "OOP_OOM", "Writing II", "DB System", "Microprocessor", "Network", "English" };
                                float[] scores = new float[7];
                                float total = 0;
                                bool hasAnyScore = false;

                                // 2. READ AND SUM DATA
                                for (int i = 0; i < 7; i++)
                                {
                                    // Column 0 is Name, so scores start at Column 1
                                    if (!reader.IsDBNull(i + 1))
                                    {
                                        float val = Convert.ToSingle(reader.GetValue(i + 1));
                                        scores[i] = val;
                                        total += val;
                                        if (val > 0) hasAnyScore = true;
                                    }
                                }

                                // 3. SHOW RESULT
                                if (!hasAnyScore)
                                {
                                    Console.WriteLine($"=== ACADEMIC RESULT FOR: {name} ===");
                                    Console.WriteLine("\n==========================================");
                                    Console.WriteLine("|        📢 RESULTS COMING SOON         |");
                                    Console.WriteLine("==========================================");
                                    Console.WriteLine("\nYour instructors have not finalized the scores.");
                                }
                                else
                                {
                                    Console.WriteLine($"=== ACADEMIC RESULT FOR: {name} ===");
                                    Console.WriteLine(new string('-', 35));

                                    for (int i = 0; i < 7; i++)
                                    {
                                        Console.WriteLine($"{subjects[i],-18}: {scores[i]}");
                                    }

                                    float avg = total / 7;
                                    string grade = CalculateGrade(avg);
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

        // ==========================================
        // 2. VIEW MY ATTENDANCE
        // ==========================================
        public void ViewMyAttendance()
        {
            Console.Clear();
            Console.WriteLine("=== MY ATTENDANCE SUMMARY ===");

            if (_currentUser == null || _currentUser.StudentId == null)
            {
                Console.WriteLine("Error: User data missing.");
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT Subject, ClassDate, Status FROM Attendance WHERE StudentId = @sid ORDER BY ClassDate DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("sid", _currentUser.StudentId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("\n{0,-20} | {1,-15} | {2}", "Subject", "Date", "Status");
                                Console.WriteLine(new string('-', 50));

                                while (reader.Read())
                                {
                                    string subject = reader.GetString(0);
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
        // 3. HELPER (For Grades)
        // ==========================================
        // 
        private string CalculateGrade(float score)
        {
            if (score >= 85) return "A";
            if (score >= 75) return "B";
            if (score >= 65) return "C";
            if (score >= 50) return "D";
            return "F";
        }
        // ===========================================
        // 4. VIEW FULL ACADEMIC REPORT (FOR STUDENTS)
        // ===========================================
        public void ViewAcademicReport()
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
    }
}