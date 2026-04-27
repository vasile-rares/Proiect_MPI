# Keyless - Typing Speed Test Application

A full-stack typing speed test application with real-time performance tracking, user authentication, and comprehensive statistics aggregation.

## Overview

Keyless is a competitive typing speed test platform designed to help users measure and improve their typing skills. The application tracks key metrics including Words Per Minute (WPM), accuracy, consistency, and raw typing speed. Users can participate in typing tests, view detailed statistics, and compete on the leaderboard.

## Languages

This repository is primarily written in:
- C# — Backend and core business logic (ASP.NET Core / .NET 10)
- JavaScript — Frontend (React / Vite)
- CSS — Styling and UI
- Other — Configs, CI, Dockerfiles, documentation, etc.

## Architecture

The application follows a layered client-server architecture with a clear separation of concerns:

- Frontend: Single Page Application (React) that handles user interaction, UI state and presentation.
- Backend: ASP.NET Core Web API implementing business logic, authentication, and data access.
- Database: PostgreSQL (production) and SQLite (for E2E/test scenarios).

High-level diagram:

Client (React SPA) <-- HTTP/REST --> ASP.NET Core API

Within the backend, responsibilities are split across layers:

- API / Controllers: HTTP endpoints, request/response mapping, authentication middleware.
- Application Layer: Services implementing business rules and coordination (e.g., statistics calculation).
- Domain Layer: Core entities and domain models.
- Infrastructure Layer: Repositories, EF Core DbContext, migrations, integrations.

Design patterns in use:
- Clean / Layered Architecture
- Repository pattern for data access
- Service layer for business logic
- DTOs for API contracts

## Project Structure

Proiect_MPI/

- backend/
  - Keyless.API/               # API entry point, controllers, middleware
  - Keyless.Application/       # Business logic and services (e.g., StatisticsGameService)
  - Keyless.Domain/            # Domain entities and value objects
  - Keyless.Infrastructure/    # Repositories, EF Core mapping
  - Keyless.Shared/            # Shared utilities and DTOs
  - Keyless.Tests/             # Unit and integration tests
  - Dockerfile                 # Container build for backend
  - Keyless.sln                # .NET solution file

- frontend/
  - src/
    - pages/                   # Page components (Game, History, Leaderboard, Profile, ...)
    - components/              # Reusable UI components
    - App.jsx / index.js       # App entry and routing
    - App.css / index.css      # Styling and theme variables
  - package.json               # Frontend scripts & dependencies
  - README.md                  # Frontend-specific notes
  - eslint.config.mjs          # Linting configuration
  - .dockerignore              # Docker ignore for frontend builds

- render.yaml                  # Render.com deployment configuration (infrastructure-as-code)
- LICENSE                      # MIT License
- README.md                    # This consolidated README (root)

Note: repository contains other helper files such as Dockerfile (backend), Render config, e2e and load-test scripts, and frontend build/config files.

## Key Features

1. Typing Test Engine
   - Real-time measurement of typing speed and accuracy
   - Visual feedback per letter (correct, incorrect, extra)
   - Caret positioning and responsive typing area
   - Configurable test durations and modes

2. Authentication & Profiles
   - User registration and login
   - JWT-based authentication for API access
   - Editable user profile and persisted preferences

3. Performance Metrics
   - Words Per Minute (WPM) and Raw WPM calculations
   - Accuracy and Consistency metrics
   - Per-game breakdown: correct, incorrect, extra, missed characters
   - Aggregate statistics: best/average values and games count

4. Leaderboard & History
   - Global leaderboard with pagination
   - Per-user history with detailed result modal
   - Load testing harness for leaderboard scalability

5. Testing & Quality
   - Unit tests for services and core logic (backend)
   - Playwright end-to-end suite for frontend + API
   - Load-test scripts and reporting (Markdown & JSON)

6. Deployment
   - Dockerfile for backend image
   - Frontend build ready for static hosting
   - Render.yaml for automated deployment on Render

## How Metrics Are Calculated (brief)

- Words per minute (WPM) is computed from correct characters: correct_chars / 5 / duration_minutes.
- Raw WPM uses total typed characters (correct + incorrect + extra) divided by 5 and duration.
- Accuracy = correct_chars * 100 / (correct + incorrect + extra + missed).
- Consistency = (WPM / RawWPM) * 100 when RawWPM > 0.

(Implementation reference: Keyless.Application.Services.StatisticsGameService)

## Getting Started (Development)

Prerequisites:
- Node.js (v20.x recommended)
- .NET 10 SDK
- Docker (for the local PostgreSQL database)

### 1. Configure environment

Copy `.env.example` to `.env` and generate a JWT key:

```bash
cp .env.example .env
```

Then open `.env` and replace `JWT_KEY` with a random secret of at least 32 characters, for example:

```bash
# Linux / macOS
openssl rand -base64 48

# Windows PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### 2. Start the database

```bash
docker compose up -d postgres
```

> **Windows + WSL note:** `appsettings.Development.json` uses `Host=127.0.0.1` instead of `localhost` to avoid WSL's IPv6 relay intercepting the connection on port 55432.

### 3. Run the backend

```bash
cd backend
dotnet restore
dotnet run --project Keyless.API
```

Backend default: http://localhost:5232

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend default: http://localhost:5173

## Scripts (quick)

Frontend:
- npm run dev         # start dev server
- npm run build       # build production bundle
- npm run e2e         # run Playwright e2e tests (starts API + frontend)
- npm run load:leaderboard  # run leaderboard load test

Backend:
- dotnet run          # run API
- dotnet test         # run tests
- dotnet publish      # publish for production

## Deployment

The repository includes a render.yaml that provisions:
- A PostgreSQL managed database
- A static web service for the frontend
- A Docker-based web service for the backend

Environment variables and secrets (JWT key, DB connection) are set via Render.

## Contributing

- Fork the repository and create feature branches.
- Open PRs with clear descriptions and tests.
- Run unit and e2e tests before submitting.

## Our team

- Stancu Nicol
- Stoica Denisa
- Tatu Denis
- Vasile Rareș
