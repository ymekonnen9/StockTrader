# StockTrader Exchange

**StockTrader Exchange** is a simulated web-based stock trading platform built with .NET 8. This project serves as a comprehensive learning exercise to explore and implement advanced concepts in backend development, API design, database management, and integration with third-party services using modern .NET technologies.

## Project Description

The goal of StockTrader Exchange is to create a feature-rich simulated environment where users can:
* Register and manage their accounts.
* Add virtual funds to their trading accounts (simulated via Stripe's test environment).
* View (simulated) stock information and prices.
* Place buy and sell orders for stocks (upcoming feature).
* Track their portfolio performance, including cash balance and stock holdings.

This application is built with a focus on Clean Architecture principles to ensure a maintainable, scalable, and testable codebase.

## Features Implemented (So Far - as of May 18, 2025)

* **User Management:**
    * User Registration with password validation and hashing.
    * User Login with JWT (JSON Web Token) based authentication.
* **API Security:**
    * JWT authentication for protecting API endpoints.
    * Role-based authorization (Admin, User roles seeded).
* **Portfolio Management (Foundation):**
    * API endpoint (`GET /api/portfolio`) to view the authenticated user's basic portfolio information, including:
        * User ID, Username, Email.
        * Current Cash Balance.
        * *The structure for displaying stock holdings is in place in the API response, though holdings will be empty until buy/sell functionality is implemented.*
* **Stock Information:**
    * API endpoint (`GET /api/stocks`) to list all available (seeded) stocks.
    * API endpoint (`GET /api/stocks/{symbol}`) to get details for a specific stock by its symbol.
* **Database & Core Entities:**
    * Entity Framework Core for ORM.
    * MySQL database integration.
    * Database migrations to manage schema evolution. Tables created for:
        * Users & Roles (ASP.NET Core Identity)
        * Stocks (`Stocks`)
        * User Stock Holdings (`UserStockHoldings`) - *links users to stocks they own with quantity and average price.*
        * Orders (`Orders`) - *to record buy/sell transactions.*
        * Payment Transactions (`PaymentTransactions`) - *for tracking fund deposits.*
    * Initial data seeding for roles, a default admin user, and sample stocks.
* **Payment Integration (Sandbox/Test Mode for Adding Funds):**
    * Integration with **Stripe Checkout** (in test mode) to simulate adding funds to a user's cash balance.
    * Backend API endpoint (`POST /api/payments/create-checkout-session`) to create Stripe Checkout Sessions.
    * Backend webhook endpoint (`POST /api/payments/webhook` or similar) to listen for Stripe events (e.g., `checkout.session.completed`) and update user balances accordingly.
    * Secure handling of webhook events using signature verification.
* **Order Infrastructure (Foundation Laid):**
    * `Order` domain entity and related DTOs (e.g., `BuyOrderRequestDto`, `OrderPlacementResultDto`) have been defined.
    * Database table for `Orders` is created via migrations.
    * Service interfaces (e.g., `IOrderService`) for order operations have been defined.
    * *The actual implementation of the buy/sell order processing logic and API endpoints is the next step.*

## Core Technologies Used

* **.NET 8**
* **ASP.NET Core Web API** (for RESTful services)
* **Entity Framework Core 8** (for data access)
* **MySQL Server** (database)
* **ASP.NET Core Identity** (for user authentication and authorization)
* **JWT (JSON Web Tokens)** (for securing API endpoints)
* **Stripe.net SDK** (for payment gateway integration - Test Mode)
* **Serilog/ILogger** (for structured logging)
* **Swagger/OpenAPI** (for API documentation and testing)

## Architectural Approach

This project follows the principles of **Clean Architecture**, separating concerns into distinct layers:
* **Domain:** Contains core business logic, entities, enums, and domain events.
* **Application:** Contains application-specific business logic, use cases, DTOs, and interfaces for infrastructure services.
* **Infrastructure:** Implements services defined in the Application layer, such as database access (EF Core DbContext, repositories), payment gateway integration, and other external concerns.
* **API (Presentation):** Exposes the application's functionality via RESTful API endpoints.

## Setup and Installation

To run this project locally, you'll need the following prerequisites:

1.  **.NET 8 SDK** ([download here](https://dotnet.microsoft.com/download/dotnet/8.0))
2.  **MySQL Server** installed and running. You can use MySQL Workbench to manage it.
3.  **(Required for Payment Testing) Stripe Account:** Sign up at [stripe.com](https://stripe.com) and access your **Test API Keys** (Publishable Key, Secret Key) and generate a **Webhook Signing Secret** for the webhook endpoint.
4.  **(Recommended for Webhook Testing) ngrok:** ([download here](https://ngrok.com/download)) or Stripe CLI for forwarding webhook events to your local machine.

**Steps:**

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/ymekonnen9/StockTraderExchange.git
    cd StockTraderExchange
    ```

2.  **Configure Application Settings:**
    * Navigate to the `StockTraderExchange.API` project folder.
    * Create an `appsettings.Development.json` file (you can copy `appsettings.json` if it exists and rename, or create it from scratch).
    * Update `appsettings.Development.json` with your specific configurations:
        * **`ConnectionStrings:DefaultConnection`**: Set your MySQL connection string.
        * **`JwtSettings:Key`**: Provide a long, strong, unique secret key for JWT signing.
        * **`JwtSettings:Issuer` & `JwtSettings:Audience`**: Update with your local development URL (e.g., `https://localhost:7001` - check `launchSettings.json`).
        * **`StripeSettings:PublishableKey`**: Your Stripe Test Publishable Key.
        * **`StripeSettings:SecretKey`**: Your Stripe Test Secret Key.
        * **`StripeSettings:WebhookSecret`**: Your Stripe Webhook Signing Secret for the `/api/payments/webhook` endpoint.

3.  **Apply Database Migrations:**
    * Open a terminal or Package Manager Console in Visual Studio.
    * Ensure `StockTraderExchange.API` is set as the Startup Project.
    * If using Package Manager Console (ensure `StockTraderExchange.API` is default/startup):
        ```powershell
        Update-Database
        ```
    * If using .NET CLI (navigate to the `StockTraderExchange.API` project directory):
        ```bash
        dotnet ef database update
        ```

4.  **Run the Application:**
    * From Visual Studio (F5 or Ctrl+F5) or using the .NET CLI:
        ```bash
        cd StockTraderExchange.API
        dotnet run
        ```

## API Endpoints & Testing

* **Swagger UI:** Navigate to `/swagger` (e.g., `https://localhost:XXXX/swagger`) for interactive API documentation.
* **Testing:**
    1. Register a user via `/api/accounts/register`.
    2. Login via `/api/accounts/login` to get a JWT.
    3. Authorize in Swagger using the JWT (`Bearer <token>`).
    4. Test stock listing (`/api/stocks`), portfolio (`/api/portfolio`), and Stripe checkout session creation (`/api/payments/create-checkout-session`).
    5. For Stripe webhook testing, use ngrok to expose your local `/api/payments/webhook` endpoint and configure it in your Stripe dashboard.

## Learning Objectives

This project is primarily a learning endeavor focused on:
* Implementing Clean Architecture in a .NET application.
* Building secure and robust RESTful APIs.
* User authentication and authorization using ASP.NET Core Identity and JWTs.
* Data persistence with Entity Framework Core and MySQL.
* Database migrations and seeding.
* Integrating third-party services like Stripe for payments.
* Preparing for more advanced topics like real-time communication and complex business logic.

## Future Enhancements / Roadmap

* **Implement Core Trading Functionality:**
    * **Buy Order Placement:** Finalize the service logic and API endpoint for users to buy stocks (market orders, immediate fill simulation). This includes:
        * Validating sufficient cash balance.
        * Updating user's cash balance and creating/updating `UserStockHolding` records.
        * Recording the `Order`.
    * **Sell Order Placement:** Implement the service logic and API endpoint for users to sell stocks they own.
* **Real-Time Price Updates:** Integrate SignalR for live stock price updates on the client-side.
* **Real-Time Order Status Notifications (SignalR).**
* **Limit Orders & Stop Orders:** Implement more advanced order types.
* **Order Matching Engine:** Develop a more sophisticated (simulated) order matching engine (especially for limit orders).
* **Advanced Portfolio Analytics:** More detailed performance tracking, profit/loss calculations, charts.
* **Transaction History:** API endpoints for users to view their order and payment history.
* **Two-Factor Authentication (2FA).**
* **Comprehensive Unit and Integration Testing.**
* **Frontend Application:** Develop a client-side application (e.g., using Blazor, React, Angular, or Vue) to consume the API.
* **Containerization with Docker.**
* **CI/CD Pipeline Setup.**

---
