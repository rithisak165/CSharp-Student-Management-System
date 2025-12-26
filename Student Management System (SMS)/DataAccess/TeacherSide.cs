using System;
using System.Collections.Generic;
using Npgsql;
using Student_Management_System__SMS_.Models;

namespace Student_Management_System__SMS_.DataAccess
{
    public class TeacherSide
    {
        // ==========================================
        // 1. ADD STUDENT
        // ==========================================
        public void AddStudent()
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

        // ==========================================
        // 2. VIEW PROFILES
        // ==========================================
        public void ViewAllStudentProfiles()
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
        // ========================
        // 3. MANAGE SCORES
        // ========================
        public void ManageScores()
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
        // =====================================================================================
        // This helper function gets a valid score input from the user(For ManageScore Function)
        // =====================================================================================
        public float GetValidScore(string subjectName)
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
        // =========================================================================
        // Helper function to get StudentId by StudentCode(For ManageScore Function)
        // =========================================================================
        public int GetStudentIdByCode(NpgsqlConnection conn, string code)
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
        // ==========================================
        // 4. RECORD ATTENDANCE (COMBINED VERSION)
        // ==========================================
        public void RecordAttendance()
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
        // ==========================================
        // 5. VIEW ATTENDANCE REPORT
        // ==========================================
        public void ViewAttendanceReport()
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
        // ===============================
        // 6. View ACADEMIC REPORT
        // ===============================
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
        // ===========================================================================
        // Helper function to calculate grade based on average(For ViewAcademicReport)
        // ===========================================================================
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
        // ==========================================
        // 8. DELETE STUDENT
        // ==========================================
        public void DeleteStudent()
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
    } // Class Ends Here
} // Namespace Ends Here