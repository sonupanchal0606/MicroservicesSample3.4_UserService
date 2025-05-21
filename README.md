# 🧩 Microservices E-Commerce App (.NET 8 + Ocelot)

This project is a microservices-based e-commerce application built using **.NET 8**, **Ocelot API Gateway**, **Entity Framework Core**, and **ASP.NET Core Identity**. It features role-based authentication, inter-service communication, and token-based secure access.

---

## 🏗️ Microservices Overview

| Service        | Port   | Description                                        |
|----------------|--------|----------------------------------------------------|
| **UserService**    | 5000   | Handles user registration, login, product viewing, and placing orders. |
| **ProductService** | 5001   | Exposes APIs to fetch product data.               |
| **OrderService**   | 5002   | Manages order creation, modification, and user-specific retrieval. |
| **API Gateway**    | 9000   | Routes all requests through Ocelot Gateway with secure API key and JWT support. |

---

## 📦 Features
- 🔐 JWT Authentication with role-based access (`Admin`, `Customer`)
- 🛍️ View and purchase products
- 📦 Place and manage orders
- 📡 Secure inter-service communication using `HttpClientFactory`
- 🛡️ API Gateway using **Ocelot**
- 🔑 API Key support for service authorization
- 🎯 Clean architecture with DTOs and Dependency Injection

---

## 📁 Project Structure
```
/MicroservicesApp
│
├── /UserService # Handles user logic and delegates orders/products
├── /ProductService # Manages products (mocked for demo)
├── /OrderService # Processes and returns order info
├── /ApiGateway # Ocelot routes all APIs here
├── /Shared # DTOs and common logic
```
--- 

## ⚙️ 2. Configure `appsettings.json`

Each service requires a valid `appsettings.json` file configured with:

- **ConnectionStrings**
- **JWT Secret keys**
- **API Key** (shared between services)

---

## ▶️ 3. Run Services

### Individual Projects (via Visual Studio / CLI)

```bash
dotnet run --project UserService
dotnet run --project ProductService
dotnet run --project OrderService
dotnet run --project ApiGateway
```
---
## 🌐 API Gateway Access and Overview

This project sets up a secure and scalable API Gateway using **Ocelot** for microservice communication. It includes JWT-based security, role-based authorization, and a shared API key mechanism. Designed with ASP.NET Core 8, it serves both customer and admin roles.

### 🚀 Access the Gateway
Navigate to the API Gateway via:`https://localhost:9000`

---
## 🔄 Sample API Endpoints via Gateway

| HTTP Verb | Endpoint                                 | Description                      |
|-----------|-------------------------------------------|----------------------------------|
| GET       | `/gateway/products`                       | Get all products                 |
| POST      | `/gateway/user/buyproduct`                | Place an order                   |
| GET       | `/gateway/user/orders/{userId}`           | Get all orders of a user         |
| PUT       | `/gateway/user/orders/{orderId}`          | Update an order                  |
| GET       | `/gateway/user/getallusers`               | **(Admin)** Get all users        |

---

## 🧪 Testing Roles

- **Admin**
  - Can view all registered users

- **Customer**
  - Can view products and make purchases

👉 You may seed users or register via an exposed `/register` endpoint (if implemented).

---

## 🔐 Security

- **JWT-based authentication**
- **Shared `x-api-key`** between services
- API Gateway (Ocelot) validates:
  - `access_token`
  - Route authorization

---

## 📚 Technologies Used

- **ASP.NET Core 8**
- **Entity Framework Core**
- **Identity Framework**
- **Ocelot (API Gateway)**
- **JWT + Role-Based Authorization**
- **HttpClientFactory**
- **Docker** *(optional)*

---

## 🛠️ Future Enhancements

- Add **RabbitMQ** for asynchronous communication
- Integrate **Swagger UI** for each microservice
- Move **product data to a database**
- Add a frontend using **Blazor** or **React**
