# Tactiq Product Roadmap

## Overview

**Tactiq** is an AI-powered football team builder and player performance analytics platform designed for amateur football teams.

The application enables users to manage players, record match statistics, generate balanced squads, and analyze player performance over time. The long-term vision is to combine statistical analysis with AI-generated insights to help teams make better tactical and squad decisions.

---

## Architecture

```text
Flutter Mobile App
        │
        ▼
ASP.NET Core REST API
        │
 ┌──────┴──────┐
 ▼             ▼
PostgreSQL   Redis
        │
     Docker
        │
     AWS EC2
```

---

# Phase 1 — MVP

### Goal

Allow users to register, manage players, record matches, and automatically generate balanced teams through the backend.

**Status:** ✅ Completed

### Implemented Features

* User authentication (Register / Login / JWT Authentication)
* Player management (CRUD)
* Match management
* Match statistics

  * Goals
  * Assists
  * Shots on target
  * Successful passes
  * Tackles
  * Saves
* Team Builder

  * 6v6–11v11 support
  * Optional formations
  * Position balancing
  * Team strength balancing
  * Balance score calculation
* Swagger/OpenAPI with JWT Authorization

### Intentionally Deferred

The following features were intentionally postponed to keep the MVP focused:

* On-field player positioning
* Detailed FIFA-style player attributes
* AI-generated player analysis
* Flutter mobile client

---

# Phase 1.5 — Team Builder v2

### Goal

**Status:** ✅ Completed

Allow users to select both match size and formation before generating teams.

### Features

### Team Size

* 6v6
* 7v7
* 8v8
* 9v9
* 10v10
* 11v11

### Supported Formations

**7v7**

* 2-3-1
* 3-2-1
* 3-3-0

**8v8**

* 3-3-1
* 2-4-1

**11v11**

* 4-3-3
* 4-4-2
* 3-5-2

### Team Balancing Rules

* Equal distribution of player positions
* Equal distribution of overall team strength
* Maximum position difference of one player
* Formation-aware player placement

### Player Profile

Each player will include:

* Name
* Primary Position
* Preferred Foot
* Playstyle
* Overall Rating
* Current Form

### Technical Tasks

* Add `Overall`
* Add `Form`
* Add `PrimaryPlaystyle`
* Add `Rating` to PlayerStats
* Automatically assign players to formation slots

---

# Phase 2 — Playstyle & Overall Rating 

### Goal

Build a rule-based player identity system before introducing AI.

**Status:** ✅ Completed

### Example Playstyles

* Finesse Shot
* Intercept
* Trickster
* Quick Step
* Far Throw

### Overall Rating Formula

```text
Base Rating
+ Recent Form (Last 5 Matches)
+ Productivity (Last 10 Matches)
+ Position-Specific Statistics
```

Additional match input:

* Player Rating (1–10)

---

# Phase 3 — AI Performance Analysis

### Goal

Use AI as an intelligent assistant rather than a decision maker.

Instead of making tactical decisions, AI will explain player performance using historical statistics.

**Status:** 🚧 Still in progress

### Example Output

```text
Your performance has improved by 18% over the last five matches.

Your attacking contribution has increased significantly, while your passing contribution has slightly decreased.

Maintaining stronger team play could further improve your overall impact.
```

### AI Input Data

* Last 5 match summary
* Last 10 match statistics
* Playstyle
* Form trend
* Overall rating history

---

# Current REST API

## Authentication 

* POST /api/auth/register
* POST /api/auth/login

## Users

* GET /api/users/me
* PUT /api/users/me
* DELETE /api/users/me

## Players

* GET /api/players
* GET /api/players/{id}
* POST /api/players
* PUT /api/players/{id}
* DELETE /api/players/{id}

## Matches

* GET /api/matches
* GET /api/matches/{id}
* POST /api/matches
* PUT /api/matches/{id}
* DELETE /api/matches/{id}

## Team Builder

* POST /api/team-builder/balance

```json
{
  "teamSize": 7,
  "formation": "2-3-1",
  "playerIds": [1,2,3,4,5,6,7,8,9,10,11,12,13,14]
}
```

## Playstyles

* GET /api/playstyles/players
* GET /api/playstyles/players/{playerId}

---


## Sprint 2.1 — Mobile MVP

* Mobile authentication flow
* Team Builder
* Player selection
* Formation selection
* Interactive football pitch
* Match statistics input
* Player management
* Health & Debug tools
* Bulk JSON import
* AI fallback endpoints

```text
POST /api/ai/player-analysis/{playerId}

POST /api/ai/team-analysis
```

---

## Sprint 2.2

* Mobile UI polish
* Shareable squad visualization
* Player profile page
* AI-generated player explanations using OpenAI
* Performance charts and trend visualization

---

# Future Vision

Tactiq aims to become more than a team builder.

The long-term goal is to provide amateur football teams with an intelligent platform for player management, tactical decision support, performance analytics, and AI-assisted insights while maintaining a simple and intuitive user experience.
