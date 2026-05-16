# CampusEats — Cafeteria Ordering System

A modular cafeteria ordering platform built with .NET 8 Minimal API,
designed for university campus environments. The system handles menu
management, order processing, kitchen operations, Stripe payments and
a loyalty program.

## Tech Stack

- **Backend:** C# / .NET 8 Minimal API
- **Architecture:** Vertical Slice Architecture + CQRS (MediatR)
- **Database:** PostgreSQL + Entity Framework Core
- **Testing:** XUnit, NSubstitute, FluentValidation
- **Payments:** Stripe API

## Features

- Menu management with category and item configuration
- Order placement and real-time kitchen tracking
- Stripe payment integration
- Loyalty program with points accumulation
- Role-based access for customers, kitchen staff and admins
- Full unit test coverage across all features
