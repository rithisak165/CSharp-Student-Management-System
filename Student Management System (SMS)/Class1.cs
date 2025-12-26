using System;
using Npgsql;
using Student_Management_System__SMS_.Models;
using Student_Management_System__SMS_.DataAccess; // <--- IMPORTANT: Links your new files

namespace StudentManagementSystem
{
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
            // 1. Create the Worker (Instance)
            TeacherSide teacher = new TeacherSide();

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
                // 2. Use the Worker to do the job
                case "1": teacher.AddStudent(); break;
                case "2": teacher.ViewAllStudentProfiles(); break;
                case "3": teacher.ManageScores(); break;
                case "4": teacher.RecordAttendance(); break;
                case "5": teacher.ViewAttendanceReport(); break; 
                case "6": teacher.ViewAcademicReport(); break;
                case "7": teacher.UpdateStudentInfo(); break;    
                case "8": teacher.DeleteStudent(); break;        

                case "9": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }

        // --- Student Menu ---
        static void ShowStudentMenu()
        {
            // 1. Create the Worker and give them the Current User
            StudentSide student = new StudentSide(currentUser);

            Console.Clear();
            Console.WriteLine("=== STUDENT DASHBOARD ===");
            Console.WriteLine("1. View My Scores & Grades");
            Console.WriteLine("2. View My Attendance");
             Console.WriteLine("3. View Academic Report");
            Console.WriteLine("4. Logout");
            Console.Write("Select: ");

            switch (Console.ReadLine())
            {
                case "1": student.ViewMyScores(); break;
                case "2": student.ViewMyAttendance(); break;
                case "3": student.ViewAcademicReport(); break;
                case "4": currentUser = null; break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

}