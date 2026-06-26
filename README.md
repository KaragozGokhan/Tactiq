![Flutter](https://img.shields.io/badge/Flutter-02569B?style=for-the-badge\&logo=flutter\&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?style=for-the-badge\&logo=dotnet\&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge\&logo=postgresql\&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-D82C20?style=for-the-badge\&logo=redis\&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge\&logo=docker\&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge\&logo=jsonwebtokens)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-2088FF?style=for-the-badge\&logo=github-actions\&logoColor=white)

# Tactiq

**AI-Powered Football Team Builder & Player Performance Analytics Platform**

Tactiq is a mobile-first football analytics platform that helps amateur football teams manage players, track match statistics, generate balanced squads, and analyze player performance using statistical analysis and AI-assisted insights.

---

## ✨ Key Features

* 🤖 AI-assisted player performance analysis
* ⚽ Automatic balanced squad generation
* 📊 Player performance analytics
* 📱 Mobile-first architecture
* 🔐 JWT Authentication & Role-Based Authorization
* 🐳 Dockerized backend
* 📄 Swagger / OpenAPI documentation
* 🚀 CI/CD with GitHub Actions

---

## 📱 Screenshots

<p align="center">
  <img width="190" alt="tactiq_login" src="https://github.com/user-attachments/assets/2d22eb80-ae8a-4bbf-aa31-5f0a0fdddea9" />
  <img width="190" alt="tactiq_main_page" src="https://github.com/user-attachments/assets/041c95d1-f798-4a4b-8a16-353c7c9df40a" />
  <img width="190" alt="tactiq-ai-balance" src="https://github.com/user-attachments/assets/41f19fb0-3741-4cc7-985e-882b4ecaeb72" />
</p>

<p align="center">
  <img width="190" alt="tactiq-match-screen" src="https://github.com/user-attachments/assets/a77dc147-2fa1-439a-bc64-5b12ae0040ec" />
  <img width="190" alt="tactiq-player-screen" src="https://github.com/user-attachments/assets/ee9f5a3e-c71d-414c-85bc-c54afce19374" />
</p>

---

## 🏗️ Architecture

```mermaid
flowchart TD
    A[Flutter Mobile App] --> B[ASP.NET Core REST API]

    B --> C[(PostgreSQL)]
    B --> D[(Redis Cache)]

    B --> E[JWT Authentication]
    B --> F[Team Builder Engine]
    B --> G[AI Analysis Endpoints]

    G --> H[OpenAI API]

    B --> I[Docker]
    I --> J[AWS EC2]
```

---

## ⚽ Team Builder Flow

```mermaid
flowchart LR
    A[Select Players] --> B[Choose Team Size]
    B --> C[Choose Formation]
    C --> D[Calculate Position Balance]
    D --> E[Calculate Overall Balance]
    E --> F[Generate Balanced Squads]
    F --> G[Return Team Results]
```

---

## 🤖 AI Analysis Pipeline

```mermaid
flowchart TD
    A[Last 5 Matches] --> F[AI Analysis Engine]
    B[Last 10 Match Totals] --> F
    C[Playstyle] --> F
    D[Current Form] --> F
    E[Overall Rating Trend] --> F

    F --> G[Player Performance Summary]
    F --> H[Improvement Suggestions]
    F --> I[Team Contribution Insights]
```

---

## 🔐 Authentication Flow

```mermaid
sequenceDiagram
    participant App as Flutter App
    participant API as ASP.NET Core API
    participant DB as PostgreSQL
    participant JWT as JWT Service

    App->>API: Login request
    API->>DB: Validate user credentials
    DB-->>API: User found
    API->>JWT: Generate access token
    JWT-->>API: JWT token
    API-->>App: Return access token

    App->>API: Authorized API request
    API->>JWT: Validate token
    JWT-->>API: Token valid
    API-->>App: Return protected resource
```

---

## 🚀 CI/CD Pipeline

```mermaid
flowchart LR
    A[GitHub Push] --> B[GitHub Actions]
    B --> C[Restore Dependencies]
    C --> D[Build Backend]
    D --> E[Run Checks]
    E --> F[Docker Build]
    F --> G[Deployment Ready]
```

---

## 🗄️ Simplified Data Model

```mermaid
erDiagram
    USER ||--o{ PLAYER : owns
    USER ||--o{ MATCH : creates

    PLAYER ||--o{ PLAYER_STATS : has
    MATCH ||--o{ PLAYER_STATS : contains

    PLAYER }o--|| PLAYSTYLE : assigned
```

---

## 🛠️ Tech Stack

### Mobile

* Flutter

### Backend

* ASP.NET Core (.NET 9)
* RESTful API
* Entity Framework Core

### Database & Cache

* PostgreSQL
* Redis

### Authentication

* JWT Authentication
* Role-Based Authorization

### DevOps

* Docker
* GitHub Actions
* CI/CD

### Documentation

* Swagger / OpenAPI

### Deployment

* AWS EC2

---

## 📂 Project Structure

### 📱 Mobile App (`Tactiq/`)

Flutter application responsible for:

* Authentication
* Player management
* Match tracking
* Squad generation
* Player performance analytics

---

### 🌐 Backend API (`TactiqAPI/`)

ASP.NET Core REST API responsible for:

* JWT Authentication
* User management
* Player management
* Match management
* Team Builder algorithm
* Playstyle analysis
* AI integration endpoints
* Business logic

---

### 📄 Documentation

* Product roadmap
* Architecture planning
* Sprint planning
* API documentation

---

## 📚 Documentation

| Document                                 | Description                                            |
| ---------------------------------------- | ------------------------------------------------------ |
| 📄 [Product Roadmap](PRODUCT_ROADMAP.md) | Product vision, roadmap, architecture and future plans |

---

## 🌐 REST API

The backend exposes RESTful endpoints for:

* Authentication
* User management
* Player management
* Match management
* Team Builder
* Playstyle analysis
* AI integration

Interactive API documentation is available through Swagger after running the backend.

---

## 🚀 Roadmap

### ✅ Completed

* Backend API
* JWT Authentication
* Role-Based Authorization
* Player CRUD operations
* Match CRUD operations
* Team Builder algorithm
* Swagger documentation
* Docker support
* GitHub Actions workflow

### 🚧 In Progress

* Flutter mobile application
* AI player analysis
* Overall rating system
* Player form tracking
* Mobile UI polish

### 🔮 Planned

* Team performance dashboard
* Shareable squad images
* AI tactical suggestions
* Play Store release
* Advanced player analytics

---

## 🔮 Future Vision

Tactiq aims to become more than a football team builder.

The long-term vision is to provide amateur football teams with an intelligent platform for player management, tactical decision support, performance analytics, and AI-assisted insights while maintaining a simple and intuitive user experience.
