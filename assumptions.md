# RaceDay — Design Assumptions

This document lists assumptions made while planning the RaceDay system in Part 1, before any application code was written.

## Users and Roles

- A single `Users` table holds both Organisers and Participants, distinguished by the `Role` column, rather than two separate tables. This avoids duplicating shared fields (name, email, password) and matches the brief's requirement that both roles share the same authentication flow.
- Email is treated as the unique identifier for login, since the brief does not require a separate username field.

## Events and Categories

- Only an Organiser can create, edit, or delete an Event they own. Other Organisers cannot modify events they did not create.
- An Event can have multiple Categories (e.g. "5km Fun Run", "10km Race"), but a Category always belongs to exactly one Event.

## Routes

- Each Category has exactly one Route (a 1:1 relationship), since route/elevation information is specific to a distance, not shared across an entire event.

## Enrolments and Results

- A Participant can only enrol once per Category — enforced with a unique constraint on (ParticipantId, CategoryId) — to prevent duplicate entries.
- Cancelling an enrolment is modelled as a DELETE rather than a status update, since the brief does not require retaining cancelled enrolment history for reporting.
- A Result can only exist if an Enrolment exists first, and each Enrolment has at most one Result (1:1), since a participant only finishes a race once per category they entered.

## Out of Scope for Part 1

- No password hashing algorithm has been implemented yet — this is a Part 2 concern once the API is being built. The `PasswordHash` column exists in the schema to reflect the intended final structure.
- No pagination, filtering, or sorting logic is defined for list endpoints in this plan — these will be considered as implementation detail in Part 2 if needed.
