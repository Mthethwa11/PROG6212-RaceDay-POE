# RaceDay — Data Dictionary

## Users
| Column | Type | Description |
|---|---|---|
| UserId | INT (PK) | Unique identifier for the user |
| FullName | VARCHAR(100) | The user's full name |
| Email | VARCHAR(150) | Unique login email |
| PasswordHash | VARCHAR(255) | Hashed password (never stored in plain text) |
| Role | VARCHAR(20) | Either 'Organiser' or 'Participant' |
| CreatedAt | DATETIME | When the account was created |

## Events
| Column | Type | Description |
|---|---|---|
| EventId | INT (PK) | Unique identifier for the event |
| OrganiserId | INT (FK) | The Organiser who created this event |
| Name | VARCHAR(150) | Event name |
| Description | VARCHAR(MAX) | Event description |
| EventDate | DATE | Date the event takes place |
| Location | VARCHAR(150) | Where the event is held |

## Categories
| Column | Type | Description |
|---|---|---|
| CategoryId | INT (PK) | Unique identifier for the category |
| EventId | INT (FK) | The event this category belongs to |
| Name | VARCHAR(100) | e.g. "10km Run" |
| DistanceKm | DECIMAL(5,2) | Distance in kilometres |
| Price | DECIMAL(8,2) | Entry price |
| MaxParticipants | INT | Capacity limit |

## Routes
| Column | Type | Description |
|---|---|---|
| RouteId | INT (PK) | Unique identifier for the route |
| CategoryId | INT (FK, UNIQUE) | The category this route belongs to (1:1) |
| RouteName | VARCHAR(100) | Route name |
| MapUrl | VARCHAR(255) | Link to a map of the route |
| ElevationGainM | INT | Total elevation gain in metres |

## Enrolments
| Column | Type | Description |
|---|---|---|
| EnrolmentId | INT (PK) | Unique identifier for the enrolment |
| ParticipantId | INT (FK) | The participant who enrolled |
| CategoryId | INT (FK) | The category they enrolled into |
| EnrolmentDate | DATETIME | When they enrolled |
| Status | VARCHAR(20) | 'Confirmed' or 'Cancelled' |

## Results
| Column | Type | Description |
|---|---|---|
| ResultId | INT (PK) | Unique identifier for the result |
| EnrolmentId | INT (FK, UNIQUE) | The enrolment this result belongs to (1:1) |
| FinishTimeSeconds | INT | Finish time in seconds |
| Position | INT | Finishing position |
