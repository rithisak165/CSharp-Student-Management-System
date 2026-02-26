# 🎓 Student Management System (SMS)

A comprehensive, console-based Student Management System built with **C#** and **PostgreSQL**. This application was developed as a major assignment for the **OOP & M** (Object-Oriented Programming & Management) course at Norton University, demonstrating core OOP principles, role-based access control, and persistent database integration.

## ✨ Features

The system features two distinct, role-based dashboards:

### 🧑‍🏫 Teacher Dashboard
* **Student Management:** Add new students to the system (auto-generates default scores).
* **Academic Tracking:** Input and update scores for specific assigned subjects.
* **Attendance System:** Record daily attendance (Present/Absent) and process student excuse requests in real-time.
* **Reporting:** Generate formatted Academic Reports, Attendance Reports, and a dedicated Behavior Report (flags critical absences or permission spam).
* **Communication:** Announce class cancellations or updates.

### 🧑‍🎓 Student Dashboard
* **Academic Progress:** View finalized grades, totals, and averages across all subjects.
* **Attendance Tracking:** Check personal attendance records and percentages per subject.
* **Permission Requests:** Submit digital absence excuses to teachers (limited to 10 requests per subject).
* **Announcements:** View real-time class updates and announcements from teachers.

## 🛠️ Tech Stack

* **Language:** C# (.NET Console Application)
* **Database:** PostgreSQL
* **Data Provider:** Npgsql
* **Architecture:** Object-Oriented Programming (OOP), MVC-inspired separation of concerns (Models/Controllers).

## 🚀 Getting Started

### Prerequisites
1. [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.
2. [PostgreSQL](https://www.postgresql.org/download/) installed and running locally.
3. [pgAdmin 4](https://www.pgadmin.org/) (optional, but recommended for database management).

## 👥 Developers

This project was built by Year 2 Software Development students at **Norton University** for the OOP & M Assignment:

| Developer Name | Student ID | Role |
| :--- | :--- | :--- |
| **Meng Rithisak** | B20244047 | Lead System Developer |
| **Rim Sovannara** | B20244288 | Technical Writer |
| **Meng Rithichey** | B20244046 | Console Interface Designer |

**Lecturer:** Nguon Viravud  
**Academic Year:** Year 2 (2025-2026)

## 📄 License
This project was created for educational purposes.
