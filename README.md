# RaceDay — Event Management System

[![Validate Repository Structure](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/validate-structure.yml/badge.svg)](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/validate-structure.yml)
[![Build and Test](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/build-and-test.yml)
![Part 3 CI](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/part3-ci.yml/badge.svg)

**PROG6212 Portfolio of Evidence** — Part 1: System Planning and Database · Part 2: RESTful API Development · Part 3: MVC Web Application, Azure Blob Storage and Docker

## About the System

RaceDay is a web-based event management system built for the South African road running, walking, and cycling community. Many community races, walks, and cycling events are still run through paper-based registration, spreadsheets, and disconnected communication channels — RaceDay replaces that with a single platform.

Event **Organisers** create and manage events, categories, and participant results. **Participants** browse upcoming events, enter events by choosing a category, and track their personal results.

- **Part 1** (planning): ERD, API endpoint plan, SQL script, data dictionary.
- **Part 2** (Part 2): an ASP.NET Core API with Entity Framework Core (Code-First), role-based session authentication, Swagger, 47 unit tests, and a GitHub Actions workflow that builds the project and runs the tests on every push.

## Roles

The system has two roles, enforced on every protected endpoint:

- **Organiser** — can create, edit and delete their own events; manage the categories of their events; record finish times and positions; and view all enrolments for their events.
- **Participant** — can register, browse events and categories, enrol in an event by selecting a category, view and cancel their own enrolments, and view their own results.

A user picks a role when registering. Only `Organiser` and `Participant` are accepted. Anyone can view events and categories without logging in. A user from the wrong role gets **403 Forbidden**, and a user who is not logged in gets **401 Unauthorized**.

## Repository Structure

```
/docs                      Part 1 planning documents
  raceday_erd.drawio       Entity Relationship Diagram (draw.io source)
  raceday_erd.png          Exported ERD image
  api_endpoint_plan.md     API endpoint plan
  raceday_schema.sql       SQL script from Part 1
  data_dictionary.md       Column-level reference for every table
  assumptions.md           Design assumptions
  ci_cd_success.png        Part 1 CI screenshot
  part2_ci_success.png     Part 2 CI screenshot (build and tests)
/Part2/RaceDay             Part 2 solution
  RaceDay/                 ASP.NET Core project (API, EF Core, Identity, Swagger)
  RaceDay.Tests/           xUnit test project (47 tests)
/.github/workflows         CI workflows (structure check, build and test)
README.md                  This file
```

## Part 2: Setup and Run Instructions

### Requirements

- Visual Studio 2022 with the **ASP.NET and web development** workload (or the .NET 8 SDK)
- SQL Server Express (the project was developed against `localhost\SQLEXPRESS01`)
- SQL Server Management Studio (optional, to inspect the database)

### 1. Clone the repository

```
git clone https://github.com/Mthethwa11/PROG6212-RaceDay-POE.git
```

Open `Part2/RaceDay/RaceDay.sln` in Visual Studio.

### 2. Check the connection string

In `Part2/RaceDay/RaceDay/appsettings.json`:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=RaceDayApiDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

If your SQL Server instance has a different name (for example `localhost\\SQLEXPRESS` or `(localdb)\\MSSQLLocalDB`), change the `Server=` part. The database is called `RaceDayApiDB` and is created automatically in the next step.

### 3. Create the database (EF Core Code-First migrations)

In Visual Studio, open **Tools → NuGet Package Manager → Package Manager Console**, set the default project to `RaceDay`, and run:

```
Update-Database
```

Or from a terminal in `Part2/RaceDay/RaceDay`:

```
dotnet ef database update
```

This creates all tables: the ASP.NET Identity tables, plus `Events`, `Categories`, `Routes`, `Enrolments` and `Results`.

### 4. Run the application

Press **F5**. The `Organiser` and `Participant` roles are created automatically on first start. Then open Swagger at:

```
https://localhost:7297/swagger
```

(Use the port shown in your browser's address bar if it differs.)

### 5. Try it in Swagger

1. `POST /api/auth/register` — create an Organiser and a Participant account (the password needs an upper-case letter, a lower-case letter, a digit and a symbol, for example `Test@1234`).
2. `POST /api/auth/login` — log in. A login cookie is issued and the user's ID and role are stored in the server-side session.
3. Call the endpoints. Log in as a different user to see the role rules in action.

### Run the unit tests

In Visual Studio: **Test → Run All Tests**. Or from a terminal in `Part2/RaceDay`:

```
dotnet test
```

The tests start the real application against an in-memory database, so no SQL Server is needed to run them.

## API Endpoints

| Method | Route | Who can call it |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Any logged-in user |
| GET | /api/users/me | Any logged-in user |
| PUT | /api/users/me | Any logged-in user |
| GET | /api/events | Public |
| GET | /api/events/{id} | Public |
| POST | /api/events | Organiser |
| PUT | /api/events/{id} | Organiser who owns the event |
| DELETE | /api/events/{id} | Organiser who owns the event |
| GET | /api/events/{eventId}/categories | Public |
| POST | /api/events/{eventId}/categories | Organiser who owns the event |
| PUT | /api/categories/{id} | Organiser who owns the event |
| DELETE | /api/categories/{id} | Organiser who owns the event |
| POST | /api/categories/{id}/enrolments | Participant |
| GET | /api/users/me/enrolments | Participant |
| GET | /api/events/{eventId}/enrolments | Organiser who owns the event |
| DELETE | /api/enrolments/{id} | Participant who made the enrolment |
| POST | /api/enrolments/{id}/result | Organiser who owns the event |
| GET | /api/enrolments/{id}/result | The participant, or the event's Organiser |
| GET | /api/users/me/results | Participant |

Every endpoint is described in Swagger, including its purpose, the data it expects, and the responses it returns.

## Authentication

- Passwords are hashed by ASP.NET Core Identity and never stored in plain text.
- Logging in creates a cookie and stores the user's ID and role in a server-side session.
- Protected endpoints use `[Authorize]` and `[Authorize(Roles = "...")]`.
- API requests that are not allowed receive `401` or `403` rather than a redirect to a login page.

## CI/CD

The workflow in `.github/workflows/build-and-test.yml` restores, builds, and runs all unit tests on every push. The earlier `validate-structure.yml` workflow still checks the Part 1 documents.

![Part 2 CI/CD build and tests passing](docs/part2_ci_success.png)

For Part 1, the earlier CI screenshot is below.

![Part 1 CI/CD structure check passing](docs/ci_cd_success.png)

## Differences from the Part 1 Plan

The Part 1 ERD, SQL script and endpoint plan were followed, with these deliberate differences:

- **Users table:** the project uses ASP.NET Core Identity as instructed, so users are stored in `AspNetUsers` (with extra `FullName` and `CreatedAt` columns) instead of a separate `Users` table. Its key is a text ID, so `Events.OrganiserId` and `Enrolments.ParticipantId` are text columns rather than integers. Roles are stored in Identity's role tables.
- **Events table:** `DistanceKm` and `EventType` (Run, Walk or Cycle) were added to `Events` because the Part 2 brief requires them. The ERD, SQL script and data dictionary in `/docs` were updated to match.
- **Login:** the Part 1 plan returned a JWT token. Part 2 requires session management, so login issues a cookie and creates a server-side session instead.
- **Logout:** `POST /api/auth/logout` was added to clear the session.
- **Extra 409 responses:** deleting an event that has categories, deleting a category that has enrolments, cancelling an enrolment that has a result, and recording a second result for an enrolment all return `409 Conflict`.
- **Database:** Part 2 uses Code-First migrations and creates `RaceDayApiDB`. The Part 1 script in `/docs` creates `RaceDayDB` and remains as the Part 1 deliverable.
- **Route entity:** the `Routes` table is mapped by the `RaceRoute` class so it does not clash with ASP.NET's `[Route]` attribute.

## Video Walkthroughs

- **Part 1:** [Watch the Part 1 walkthrough video](https://youtu.be/RfA3ZOpw8js)
- **Part 2:** [Watch the Part 2 walkthrough video](https://youtu.be/GHIwjM5FKQ8)
## AI Tool Disclosure

AI tools (DeepSeek) were used to help plan and assist in debugging and to troubleshoot errors reviewing code. All code was done, run, and tested by me, and I can explain the design decisions and code structure in the accompanying video walkthroughs.
---

## Part 3: MVC Web Application, Azure Blob Storage and Docker

Part 3 is the web front end for RaceDay: an ASP.NET Core MVC app in [`Part3/RaceDay.Web`](Part3/RaceDay.Web) that
consumes the Part 2 API. It has **no database access and no duplicated business logic**. Every piece of data and every
file upload goes through the API, and image storage (Azure Blob Storage) lives inside the API. The MVC app never talks to Azure.

**Demo video (unlisted YouTube):** [PASTE VIDEO LINK HERE]

![Part 3 CI](https://github.com/Mthethwa11/PROG6212-RaceDay-POE/actions/workflows/part3-ci.yml/badge.svg)

![CI run](Part3/docs/ci-green.png)

The full Part 3 documentation is in [`Part3/README.md`](Part3/README.md).

### Architecture

```
Browser  ->  RaceDay.Web (MVC, Part3)  ->  RaceDay API (Part2)  ->  SQL Server
                                                     |
                                                     +-------->  Azure Blob Storage (images)
```

- The MVC app keeps the user's ID and role in its own session and sends the cookies the API issues with every API call.
- Three layouts give each audience its own interface: guest, Participant (top navigation bar) and Organiser (sidebar console).
  Protected pages redirect unauthenticated users to the login page, and the wrong role gets an access-denied page.
- Event banners and profile pictures are uploaded to the **API**, which validates them, stores them in Azure Blob Storage and saves
  only the public URL in the database. The MVC app displays the image from that URL.

### Features

**Organiser:** dashboard (events, upcoming dates, enrolments per event) - create, edit and delete events with a banner image -
category management - enrolments per event with colour-coded status - results entry (finish time and position).

**Participant:** home page with upcoming events and search, type and date filters - event detail with categories and one-click
enrolment - My events with colour-coded status and cancel - My results (event, date, category, finish time and position such as
"47th of 312") - profile page to update details and upload a profile picture.

### Run it with Docker

The container runs the MVC app only. The Part 2 API must be running on your PC (<https://localhost:7297>), because the container
calls it through `host.docker.internal`. Run these from the repository root.

One command (Docker Compose):

```
docker compose -f Part3/docker-compose.yml up --build
```

Or with plain Docker:

```
docker build -t raceday-web Part3/RaceDay.Web
docker run --rm -p 8080:8080 -e Api__BaseUrl=https://host.docker.internal:7297 -e Api__AllowUntrustedCertificate=true --name raceday-web raceday-web
```

Then open <http://localhost:8080>. `Api__AllowUntrustedCertificate=true` is needed only because the API uses the local ASP.NET
development certificate, which a container does not trust. It is off by default and is for local demonstration only.

### Run it locally (Visual Studio)

1. In Package Manager Console (default project `RaceDay`) run `Update-Database`.
2. Add your Azure storage connection string to the API's User Secrets (it is never committed):
   `AzureBlob:ConnectionString`, with containers `event-banners` and `profile-pictures` set to public read for blobs.
3. Set `RaceDay` (API) and `RaceDay.Web` as multiple startup projects. API: <https://localhost:7297> - Web: <https://localhost:7015>.

### CI/CD

`.github/workflows/part3-ci.yml` runs on every push to `Part3/`: it validates the Part 3 repository structure, builds the MVC project
and builds the Docker image. The Part 2 workflows keep building and testing the API.

### Use of AI assistance

I used an AI assistant (ChatGPT & Deepseek) for reviewing, debugging the code during this part of the project.
I ran, tested and reviewed the result myself.