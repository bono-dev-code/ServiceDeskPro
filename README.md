## ServiceDesk Pro Professional IT Service Desk & Ticket Management System

## Overview

ServiceDesk Pro is a modern IT Service Management (ITSM) and Help Desk application built using C# WPF (.NET 8) and SQLite. The system allows organizations to manage support tickets, technicians, departments, categories, SLA tracking, activity logs, reporting, and company administration through a professional desktop interface.

## This project was designed to simulate a real-world enterprise service desk environment and demonstrates skills in:

Object-Oriented Programming (OOP)
Desktop Application Development
Database Design
User Authentication
CRUD Operations
Reporting & Analytics
UI/UX Design
Software Architecture
IT Service Management Processes

## Project Purpose

## Organizations receive hundreds of IT support requests every day.

Without a proper ticketing system:

Requests get lost
Technicians become overloaded
Users receive slow support
Managers cannot track performance

ServiceDesk Pro solves these challenges by providing a centralized platform where support requests can be logged, assigned, monitored, and resolved efficiently.

## Real-World Problem Solved

Many small and medium businesses still rely on:

Emails
WhatsApp messages
Phone calls
Excel spreadsheets

to manage IT incidents.

## This often leads to:

 Lost tickets

 Delayed responses

 Poor technician accountability

 No reporting

 No performance tracking

## ServiceDesk Pro provides a professional solution by introducing:

 Ticket Tracking

 Technician Assignment
 
 Department Management

 SLA Monitoring

 Reporting Dashboard

 Activity Auditing

## Technology Stack
Technology	Purpose
C#	Application Logic
WPF	Desktop User Interface
.NET 8	Framework
SQLite	Local Database
XAML	UI Design
Visual Studio 2022	Development Environment

## System Architecture
Presentation Layer (WPF UI)
            ↓
Business Logic Layer (Services)
            ↓
Data Access Layer (SQLite Database)
            ↓
Database Storage

## Project Structure

```text
ServiceDeskPro/
│
├── Assets/
│
├── Models/
│   ├── Company.cs
│   ├── DashboardStats.cs
│   ├── Department.cs
│   ├── Notification.cs
│   ├── ReportRow.cs
│   ├── SlaRule.cs
│   ├── TechnicianSummary.cs
│   ├── Ticket.cs
│   ├── TicketComment.cs
│   └── User.cs
│
├── Services/
│   ├── AuthService.cs
│   ├── DatabaseService.cs
│   ├── ReportService.cs
│   ├── SessionService.cs
│   ├── TicketService.cs
│   └── ValidationService.cs
│
├── Views/
│   ├── LoginWindow.xaml
│   ├── RegisterCompanyWindow.xaml
│   ├── DashboardWindow.xaml
│   ├── TechnicianEditWindow.xaml
│   ├── TicketDetailsWindow.xaml
│   ├── ActivityLogWindow.xaml
│   └── ForgotPasswordWindow.xaml
│
├── Data/
│   └── servicedeskpro.db
│
├── App.xaml
├── App.xaml.cs
└── ServiceDeskPro.csproj
```

## Authentication Module

The system includes a secure authentication process.

Features
User Login
User Logout
Session Management
Company Registration
Password Validation
Role-Based Access
Supported Roles
Administrator

Can:

Create tickets
Assign technicians
Manage users
Manage departments
Manage categories
Generate reports
View activity logs
Technician

Can:

View assigned tickets
Update ticket status
Resolve tickets
Track workload

## Company Registration Module

Unlike demo systems, ServiceDesk Pro allows organizations to create their own environment.

Information Captured
Company Name
Administrator Name
Email Address
Contact Number
Password

Each organization starts with a clean database environment.

## Ticket Management Module

## This is the heart of the system.

Users can create support requests and track them through their lifecycle.

Ticket Information
Ticket Number
Title
Description
Category
Department
Priority
Status
Assigned Technician
Created Date
Due Date
Resolution Date
Ticket Lifecycle
Open
 ↓
Assigned
 ↓
In Progress
 ↓
Resolved
 ↓
Closed

Priority Levels
Priority	Description
Low	Minor issue
Medium	Standard support
High	Significant impact
Critical	Business-critical incident
SLA Tracking

## The system automatically monitors Service Level Agreements.

SLA Statuses
On Track

Ticket is within target time.

Breached

Ticket has exceeded resolution deadline.

Completed

Ticket has been resolved.

## Technician Management

## Administrators can manage technicians from the dashboard.

Technician Details
Full Name
Department
Email
Contact Number
Availability Status
Availability Statuses
Available

Ready to receive tickets.

On Break

Temporarily unavailable.

## Offline

Not available for assignment.

Department Management

## Organizations can create unlimited departments.

## Examples:

IT Support
Cyber Security
Infrastructure
Networking
Software Development
Customer Support
Features
Add Department
Delete Department
View Departments
Category Management

Organizations can create custom categories.

Examples:

Password Reset
Hardware Issue
Software Installation
Network Failure
Printer Issue
Email Support
Security Incident
Features
Add Category
Delete Category
View Categories

## Dashboard Module

The dashboard provides a real-time overview of the service desk.

Metrics Displayed
Open Tickets

Active unresolved tickets.

In Progress Tickets

Tickets currently being worked on.

Critical Tickets

High-priority incidents.

Resolved Tickets

Completed tickets.

Analytics Module

The analytics section provides management insights.

Examples
Ticket volume
Resolution trends
Technician performance
Department workload
Critical incident tracking

## Reporting Module

Managers can generate reports for operational analysis.

Report Information
Ticket statistics
Technician performance
Resolution rates
SLA compliance
Department performance

## Activity Log Module

Every major action performed in the system is recorded.

Examples:

Ticket created
Ticket assigned
Ticket resolved
Department added
Category added
Technician updated
User logged in

## Benefits:

Accountability
Auditing
Compliance
Performance Review

## Database Design

## SQLite is used as the backend database.

Main Tables
Companies

Stores organization details.

Users

Stores user accounts.

Tickets

Stores support requests.

Departments

Stores company departments.

Categories

Stores ticket categories.

ActivityLogs

Stores system actions.

TicketComments

Stores ticket communication history.

Notifications

Stores alerts and updates.

Object-Oriented Programming Concepts Used
Encapsulation

## Data stored inside model classes.

## Services hide database complexity.

## Application separated into:

Models
Services
Views
Reusability

## Shared services are reused throughout the application.

Validation Features

## The system validates:

Empty fields
Duplicate data
Invalid inputs
Missing required information

## This helps maintain data integrity.

## User Interface Design

## The application follows a professional enterprise design philosophy.

Design Goals
Clean Layout
Modern Appearance
Easy Navigation
Minimal Learning Curve
Professional Color Palette

## Skills Demonstrated

Software Development
C#
.NET 8
WPF
Database Development
SQLite
Data Persistence
CRUD Operations
Software Engineering
OOP Principles
Separation of Concerns
Layered Architecture
Business Analysis
Service Desk Processes
Incident Management
Ticket Lifecycle Design
UI/UX Design
Desktop Application Design
User-Friendly Interfaces
Professional Layouts
Future Enhancements

## Potential future versions may include:

Email Notifications
SMS Notifications
Multi-Company Support
Cloud Database Integration
Microsoft Active Directory Integration
Asset Management
Knowledge Base
Live Chat Support
Mobile Application
AI Ticket Classification
AI Technician Recommendations
Power BI Integration
Why Recruiters Should Notice This Project

## This project demonstrates far more than a basic CRUD application.

## It showcases the ability to:

Design a real-world business solution
Build a complete desktop application
Work with databases
Implement authentication systems
Create scalable software architecture
Develop enterprise-style reporting
Apply software engineering principles

## ServiceDesk Pro mirrors the type of software used in real IT departments and help desk environments, making it an excellent portfolio project for software development, desktop development, IT support systems, and enterprise application roles.

## Developer

## NENGUDA BONO

Software Developmer

Passionate about building real-world software solutions that solve business problems through clean design, structured architecture, and practical functionality.