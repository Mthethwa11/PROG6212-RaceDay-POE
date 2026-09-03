# RaceDay — API Endpoint Plan

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as an Organiser or Participant. | None (public) | `{ fullName, email, password, role }` | 201 Created — user object (no password) · 400 Bad Request — validation errors · 409 Conflict — email already registered |
| POST | /api/auth/login | Authenticates a user and issues a JWT. | None (public) | `{ email, password }` | 200 OK — `{ token, user }` · 401 Unauthorized — invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's profile. | Any (logged in) | None | 200 OK — user profile · 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's profile details. | Any (logged in) | `{ fullName, email }` | 200 OK — updated profile · 400 Bad Request — validation errors |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events. | None (public) | None | 200 OK — array of events |
| GET | /api/events/{id} | Gets details of a specific event, including its categories. | None (public) | None | 200 OK — event object · 404 Not Found |
| POST | /api/events | Creates a new event. | Organiser | `{ name, description, eventDate, location }` | 201 Created — event object · 400 Bad Request |
| PUT | /api/events/{id} | Updates an event's details. | Organiser (owner) | `{ name, description, eventDate, location }` | 200 OK — updated event · 403 Forbidden — not owner · 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event. | Organiser (owner) | None | 204 No Content · 403 Forbidden · 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories for an event. | None (public) | None | 200 OK — array of categories |
| POST | /api/events/{eventId}/categories | Adds a category to an event. | Organiser (owner) | `{ name, distanceKm, price, maxParticipants }` | 201 Created — category object · 403 Forbidden · 404 Not Found |
| PUT | /api/categories/{id} | Updates a category. | Organiser (owner) | `{ name, distanceKm, price, maxParticipants }` | 200 OK — updated category · 403 Forbidden · 404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category. | Organiser (owner) | None | 204 No Content · 403 Forbidden · 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrolments | Enrols the logged-in participant into a category. | Participant | None | 201 Created — enrolment record · 404 Not Found — category does not exist · 409 Conflict — category full or already enrolled |
| GET | /api/users/me/enrolments | Lists the logged-in participant's own enrolments. | Participant | None | 200 OK — array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event (organiser view). | Organiser (owner) | None | 200 OK — array of enrolments · 403 Forbidden |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant (owner) | None | 204 No Content · 403 Forbidden · 404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/result | Captures a participant's finish result for an enrolment. | Organiser (event owner) | `{ finishTimeSeconds, position }` | 201 Created — result record · 403 Forbidden · 404 Not Found |
| GET | /api/enrolments/{id}/result | Gets the result for a specific enrolment. | Any (logged in — owner or organiser) | None | 200 OK — result object · 404 Not Found |
| GET | /api/users/me/results | Gets the logged-in participant's personal performance history. | Participant | None | 200 OK — array of results |
