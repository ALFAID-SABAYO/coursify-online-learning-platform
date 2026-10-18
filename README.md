# Coursify

Coursify is a course learning platform inspired by Udemy, built as an academic project in C#. It supports three user roles (Admin, Teacher and Student), module-based course delivery, certificate generation, payment integration and automated purchase confirmation emails. It is a practical prototype of a real-world learning platform.

## Features

### Admin
- Manage users (teachers and students)
- Approve or remove courses
- Monitor platform activity
- Manage categories and system data
- Oversee teacher and student accounts

### Teacher
- Create and manage courses
- Upload lessons and course materials
- Update course details
- View enrolled students
- Track student progress

### Student
- Browse available courses
- Enroll in courses
- Access course modules and lessons
- Track learning progress
- Download certificates after completion
- Receive purchase confirmation emails
- View purchased content anytime

## Tech Stack
- Language: C#
- Database: PostgreSQL (hosted on Neon)
- ORM: Entity Framework Migrations
- Core concepts: OOP, Role-Based Access, CRUD Operations
- Version control: Git and GitHub

## Future Improvements
- Replace the current hard-coded payment logic with a dynamic, production-ready integration
- Add live video lectures and streaming
- Add quizzes, assignments and progress-based assessments
- Add AI-powered learning assistance for students
- Improve the UI and UX for smoother course navigation
- Add course reviews, ratings and teacher feedback
- Improve analytics dashboards for admins and teachers

## How to Run
  Add your database connection string and email settings (SMTP username and app password) in `appsettings.Development.json`. Never commit real credentials.
