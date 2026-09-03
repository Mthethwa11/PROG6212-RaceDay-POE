# Changelog

All notable changes to the RaceDay project are documented here.

## Part 1 — System Planning and Database

### Added
- Entity Relationship Diagram (`docs/raceday_erd.drawio`) covering all 6 entities: Users, Events, Categories, Routes, Enrolments, Results.
- API Endpoint Plan (`docs/api_endpoint_plan.md`) covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results.
- SQL database script (`docs/raceday_schema.sql`) creating and seeding the full `RaceDayDB` schema.
- Data dictionary (`docs/data_dictionary.md`) documenting every table and column.
- Assumptions document (`docs/assumptions.md`) recording design decisions made during planning.
- GitHub Actions workflow (`.github/workflows/validate-structure.yml`) validating that all required Part 1 files are present.
- README with system description, roles, setup instructions, and CI/CD status.
- MIT License.
- `.gitattributes` for consistent line endings across environments.

### Fixed
- Corrected `raceday_schema.sql` after an earlier commit accidentally contained unrelated DATA6222 Art Gallery script content.
- Fixed a YAML syntax error (missing `run: |` block indicator) in the CI workflow.
