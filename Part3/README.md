# RaceDay - Part 3: MVC Web Application

**Module:** PROG6212 (Programming 2B)
**Student:** [S`bongakonke Mnqobi Mthethwa] &nbsp;|&nbsp; **Student number:** [ST10469763]

RaceDay is an event management system for runs, walks and cycle races.
Part 3 is the ASP.NET Core MVC web application (`Part3/RaceDay.Web`). It has **no database access and no
duplicated business logic**: every piece of data and every file upload goes through the Part 2 API.
Image storage (Azure Blob Storage) lives inside the API; the MVC app never talks to Azure.

**Demo video (unlisted YouTube):** [PASTE VIDEO LINK HERE]

**CI status:** ![CI screenshot](docs/ci-green.png)
<!-- Replace docs/ci-green.png with the screenshot of the green GitHub Actions run, saved in Part3/docs/ -->

---

## Architecture

```
Browser  ->  RaceDay.Web (MVC, Part3)  ->  RaceDay API (Part2)  ->  SQL Server
                                                     |
                                                     +-------->  Azure Blob Storage (images)
```

- **RaceDay.Web** keeps the user's ID and role in its own session. After login it stores the cookies the API
  issues and sends them with every API call (`Services/ApiClient.cs`).
- **Role-based navigation:** three layouts - guest, Participant (top navigation bar) and Organiser (sidebar console).
  `[RoleAuthorize]` redirects unauthenticated users to the login page and sends the wrong role to an access-denied page.
- **Azure Blob Storage** is integrated into the **API** (`ImagesController`, `BlobStorageService`). The MVC app posts the
  image file to the API, the API validates it and stores it in Blob Storage, and only the public URL is saved in the database.
  The MVC app then displays the image from that URL.

## Features

**Everyone**
- Register (choosing Organiser or Participant) and log in through the API; log out ends the session and redirects to login.
- Browse upcoming events with search, event type and date filters.

**Organiser**
- Dashboard: events created, upcoming events with dates, total enrolments per event.
- Create, edit and delete events (name, description, date, location, distance, event type) with a banner image upload.
- Category management for each event (name, distance, price, maximum participants).
- Enrolments page per event: participant, category and colour-coded status.
- Results entry: finish time and finishing position per participant (after the event date), with the option to correct a result.

**Participant**
- Event detail page with banner and categories; enrol by choosing a category (recorded immediately).
- My events: every enrolment with its colour-coded status, and cancel for upcoming events.
- My results: event, date, category, finish time and position such as "47th of 312".

## Running it locally (Visual Studio)

Prerequisites: Visual Studio 2022, .NET 8 SDK, SQL Server Express (`localhost\SQLEXPRESS01`), SSMS (optional).

1. **Database:** open the solution `Part2/RaceDay/RaceDay.sln`. In *Package Manager Console* (default project `RaceDay`) run
   `Update-Database` to create/update `RaceDayApiDB`.
2. **Azure Blob Storage (API):** create a storage account with two containers, `event-banners` and `profile-pictures`, both with
   public read access for blobs. Then right-click the API project (`RaceDay`) > *Manage User Secrets* and add:
   ```json
   {
     "AzureBlob": {
       "ConnectionString": "<your storage account connection string>"
     }
   }
   ```
   The connection string is **never committed to the repository**.
3. **Run both projects:** right-click the solution > *Configure Startup Projects* > *Multiple startup projects* and set
   `RaceDay` (API) and `RaceDay.Web` to *Start*.
   - API: <https://localhost:7297> (Swagger at `/swagger`)
   - Web app: <https://localhost:7015>

## Running the MVC app in Docker

The container runs only the MVC app. **The Part 2 API must be running on your PC** (step 3 above) because the
container calls it. Inside the container the API is reached at `host.docker.internal`.

Run these from the repository root.

**One command (Docker Compose):**
```
docker compose -f Part3/docker-compose.yml up --build
```

**Or with plain Docker:**
```
docker build -t raceday-web Part3/RaceDay.Web
docker run --rm -p 8080:8080 -e Api__BaseUrl=https://host.docker.internal:7297 -e Api__AllowUntrustedCertificate=true --name raceday-web raceday-web
```

Then open <http://localhost:8080>. It behaves the same as the local app.

Notes:
- `Api__BaseUrl` sets the API address; it overrides `Api:BaseUrl` in `appsettings.json`.
- `Api__AllowUntrustedCertificate=true` is needed only because the API uses the local ASP.NET development
  certificate, which a container does not trust. It is **off by default** and is for local demonstration only.
- Stop the container with `Ctrl+C` (or `docker stop raceday-web`).

## Continuous integration

`.github/workflows/part3-ci.yml` runs on every push to `Part3/`:
1. validates the Part 3 repository structure (required files and folders, and that the MVC project has no EF Core, SQL or Azure Storage references),
2. restores and builds `RaceDay.Web`,
3. builds the Docker image.

The Part 2 workflows (`validate-structure.yml`, `build-and-test.yml`) continue to build and test the API.

## Project structure (Part 3)

```
Part3/
  README.md
  docker-compose.yml
  RaceDay.Web/
    Dockerfile
    Program.cs
    Controllers/    Account, Home, Events, MyEvents, MyResults, Organiser*, ...
    Models/         view models and API DTOs
    Services/       ApiClient (all API calls), session helpers
    Filters/        RoleAuthorize (login and role protection)
    Views/          Razor views, one layout per role
    wwwroot/        CSS (RaceDay theme) and static files
```

## Demo accounts and data

The demo video uses throwaway accounts created only for the demonstration. No real personal data is used.

## Use of AI assistance

I used an AI assistant (ChatGPT & Deepseek) for reviewing, debugging thr code during this part of the project.
I ran, tested and reviewed the result myself.
