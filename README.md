# 🧾 Invoice App Backend (ASP.NET Core Web API)

This is the backend service for the **Invoice Management App**, built with **.NET 8 Web API** and **Entity Framework Core**.  
It provides secure APIs for user management, customer handling, invoice creation, and payment information management.

---

## 🚀 Features

- 🔐 **User Authentication**
  - Custom authentication (not using Identity)
  - Role-based access (User, Admin, SuperAdmin)
- 👥 **Customer Management**
  - Users can create and manage their own clients
  - A customer is required before creating invoices
- 🧾 **Invoice Management**
  - Create, view, and manage invoices tied to specific customers
  - Automatically fetches the **last invoice number** for new invoice generation (handled on the frontend)
  - Optional payment information snapshot for invoices
- 💳 **Payment Information**
  - Each user can maintain one active bank/payment info
  - Updatable at any time
- 📦 **DTO-Based Architecture**
  - Clean separation between database models and API responses
- ⚙️ **Scalable Architecture**
  - Follows a layered pattern (Models, DTOs, Services, Controllers)
  - Easy to extend with new features like subscription tiers or reports

---

## 🏗️ Project Structure

InvoiceApp/
│
├── Controllers/
│ ├── UserController.cs
│ ├── CustomerController.cs
│ ├── InvoiceController.cs
│ └── PaymentInfoController.cs
│
├── Dtos/
│ ├── User/
│ ├── Customer/
│ ├── Invoice/
│ └── PaymentInfo/
│
├── Models/
│
├── Services/
│ ├── Interfaces/
│ └── Implementations/
│
├── Data/
│ ├── ApplicationDbContext.cs
│
├── Helpers/
│
├── Program.cs
└── appsettings.json


---

## ⚙️ Technologies Used

| Stack | Description |
|-------|--------------|
| **.NET 9 Web API** | Core framework for the REST API |
| **Entity Framework Core** | ORM for data access |
| **POSTGRESQL / SQL Server** | Supported database engines |
| **JWT Authentication** | For user login & token-based authorization |
| **Swagger** | For API documentation and testing |

---

## 🧰 Getting Started

### 🔹 1. Clone the Repository

```bash
git clone https://github.com/<your-username>/invoice-app-backend.git
cd invoice-app-backend
