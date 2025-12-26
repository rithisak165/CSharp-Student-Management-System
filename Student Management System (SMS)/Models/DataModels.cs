using System;

namespace Student_Management_System__SMS_.Models
{
    // ==========================================
    // 1. DATA MODELS
    // ==========================================

    // Defines who is logging in
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // "Teacher" or "Student"
        public int? StudentId { get; set; } // Nullable (Teachers don't have this)
    }

    // Defines student info
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
    }

    // Defines the grades
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
}