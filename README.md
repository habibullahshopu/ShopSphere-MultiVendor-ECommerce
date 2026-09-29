# 🛒 ShopSphere
## AI-Powered Multi-Vendor E-Commerce Management System

ShopSphere is an AI-powered multi-vendor e-commerce management system built with ASP.NET Core MVC. The platform provides separate functionality for customers, vendors, and administrators, with features for product management, shopping, orders, payments, reviews, coupons, returns, support, and AI-assisted insights.

## ✨ Key Features

### 👤 Customer
- User registration and authentication
- Product browsing and search
- Product details and reviews
- Shopping cart
- Wishlist
- Coupon support
- Checkout and order placement
- Online payment integration
- Order tracking
- Return requests
- Customer support

### 🏪 Vendor
- Vendor dashboard
- Product management
- Inventory management
- Order management
- Sales monitoring
- Product and category management
- Vendor-specific operations

### 🛡️ Admin
- Admin dashboard
- User management
- Vendor management
- Product management
- Category management
- Order management
- Coupon management
- Return management
- Support management
- System-level administration

## AI Features

ShopSphere includes AI-assisted functionality for e-commerce management, including:

- AI-powered product search
- AI-based sales prediction
- AI-based inventory prediction

These features are designed to help users find products and help the system provide data-driven insights for e-commerce operations.

## 💳 Payment

The system integrates **SSLCommerz** for online payment processing.

> Payment credentials and API secrets are stored outside the source code and are not included in this repository.

## 🔐 Security

- ASP.NET Core Identity authentication
- Role-based authorization
- Customer, Vendor, and Admin access control
- Secure configuration using User Secrets
- Sensitive credentials excluded from source control

## 🛠️ Technology Stack

### Backend
- C#
- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity

### Frontend
- HTML
- CSS
- JavaScript
- Razor Views
- Bootstrap

### Database
- Microsoft SQL Server

### AI
- OpenAI API
- AI-assisted search and prediction services

### Payment
- SSLCommerz

### Development Tools
- Visual Studio 2022
- Git
- GitHub

## 🏗️ Architecture

The project follows the **Model-View-Controller (MVC)** architectural pattern.

```text
ShopSphere
│
├── Areas
│   ├── Admin
│   └── Customer
│
├── Controllers
├── Data
├── Helpers
├── Migrations
├── Models
├── Services
├── ViewModels
├── Views
├── wwwroot
│
├── Program.cs
└── ShopSphere.csproj
