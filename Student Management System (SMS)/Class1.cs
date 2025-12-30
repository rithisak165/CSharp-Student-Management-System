using System;
using System.Threading.Tasks;
using Npgsql;
using Student_Management_System__SMS_.Models;     // Matches your DataModels namespace
using Student_Management_System__SMS_.DataAccess; // Matches your Folder Structure

namespace Student_Management_System__SMS_
{
    class Program
    {
        // This holds the logged-in user (Teacher or Student)
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

        // ==========================================
        // 1. LOGIN LOGIC (FIXED)
        // ==========================================
        static void ShowLoginMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=====================================================");
            Console.WriteLine("=           STUDENT MANAGEMENT SYSTEM LOGIN         =");
            Console.WriteLine("=====================================================");
            Console.ResetColor();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // CRITICAL FIX: specific query to get the 'subject' column
                    string query = "SELECT UserId, Role, StudentId, Subject FROM Users WHERE Email = @e AND Password = @p";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("e", email);
                        cmd.Parameters.AddWithValue("p", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Create the User object with data from database
                                currentUser = new User
                                {
                                    UserId = reader.GetInt32(0),
                                    Email = email,
                                    Role = reader.GetString(1),
                                    // Handle Nulls safely
                                    StudentId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                    Subject = reader.IsDBNull(3) ? null : reader.GetString(3)
                                };

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"\nLogin Successful!");
                                Console.WriteLine($"Welcome, {currentUser.Role}.");

                                // If it's a teacher, show them which subject they loaded
                                if (currentUser.Role == "Teacher" && !string.IsNullOrEmpty(currentUser.Subject))
                                {
                                    Console.WriteLine($"You are managing: {currentUser.Subject}");
                                }

                                Console.ResetColor();
                                System.Threading.Thread.Sleep(1500);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("\nInvalid Email or Password.");
                                Console.ResetColor();
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

        // ==========================================
        // 2. TEACHER MENU
        // ==========================================
        static void ShowTeacherMenu()
        {
            // PASS 'currentUser' so TeacherSide knows the subject
            TeacherSide teacher = new TeacherSide(currentUser);

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            string subjectDisplay = currentUser.Subject ?? "No Subject";
            Console.WriteLine("=====================================================");
            Console.WriteLine($"=        TEACHER DASHBOARD ({subjectDisplay})         =");
            Console.WriteLine("=====================================================");
            Console.ResetColor();

            Console.WriteLine("1. Add New Student");
            Console.WriteLine("2. View All Student Profiles");
            Console.WriteLine("3. Enter/Update Scores");
            Console.WriteLine("4. Record Attendance");
            Console.WriteLine("5. View Attendance Report");
            Console.WriteLine("6. View Academic Report");
            Console.WriteLine("7. Update Student Info");
            Console.WriteLine("8. Delete Student");
            Console.WriteLine("9. Review Excuses");
            Console.WriteLine("0. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": teacher.AddStudent(); break;
                case "2": teacher.ViewAllStudentProfiles(); break;
                case "3": teacher.ManageScores(); break;       // Uses Subject Logic
                case "4": teacher.RecordAttendance(); break;
                case "5": teacher.ViewAttendanceReport(); break;
                case "6": teacher.ViewAcademicReport(); break;
                case "7": teacher.UpdateStudentInfo(); break;
                case "8": teacher.DeleteStudent(); break;
                case "9": teacher.ReviewExcuses(); break;      // Uses Subject Logic
                case "0": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }

        // ==========================================
        // 3. STUDENT MENU
        // ==========================================
        static void ShowStudentMenu()
        {
            StudentSide student = new StudentSide(currentUser);

            Console.Clear();
            Console.WriteLine("=== STUDENT DASHBOARD ===");
            Console.WriteLine("1. View My Scores");
            Console.WriteLine("2. View My Attendance");
            Console.WriteLine("3. View Academic Report");
            Console.WriteLine("4. Send Absence Excuse");
            Console.WriteLine("5. View My Profile");
            Console.WriteLine("6. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": student.ViewMyScores(); break;
                case "2": student.ViewMyAttendance(); break;
                case "3": student.ViewAcademicReport(); break;
                case "4": student.RequestPermission(); break;
                case "5": student.ViewProfile(); break;
                case "6": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }
}