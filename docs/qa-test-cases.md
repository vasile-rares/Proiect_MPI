# QA Test Cases

This document groups the requested QA scenarios in a Jira-ready format and shows where automated regression coverage exists in the repository.

## Repository Initialization

These checks are intended as repository-level QA checks and should be executed manually because they validate the top-level repository contract rather than runtime behavior.

| ID | Title | Preconditions | Steps | Expected Result | Coverage |
| --- | --- | --- | --- | --- | --- |
| TC1 | Repository structure is correctly initialized | Repository is created. | 1. Inspect the repository root. 2. Verify that the top-level structure is logical for the project type. 3. For full-stack repositories, verify clear FE/BE separation. | Root contains the expected logical folders and the frontend/backend split is easy to identify. | Manual |
| TC2 | Dependency management is standardized | Repository is initialized. | 1. Verify the presence of package manifest files. 2. Verify that a lock file is committed. | `package.json` and a lock file are present in the expected scope for the JavaScript application. | Manual |
| TC3 | Git configuration excludes unwanted files | Repository is initialized. | 1. Open `.gitignore`. 2. Verify entries for dependency folders, build outputs, environment files, and OS-generated files. | `.gitignore` excludes `node_modules`, build folders, `.env`, and common system files. | Manual |
| TC4 | Base documentation is available | Repository is accessible. | 1. Open the main project documentation. 2. Check for project description and setup steps. | A README is available with project overview and setup guidance. | Manual |
| TC5 | License file is present | Repository is accessible. | 1. Inspect the repository root. 2. Open the license file. | `LICENSE` exists and contains a standard license. | Manual |

## Regression: AsNoTracking and Upsert

Automated coverage lives in [backend/Keyless.Tests/UnitTests/UserStatsAggregateRepositoryTests.cs](../backend/Keyless.Tests/UnitTests/UserStatsAggregateRepositoryTests.cs).

| ID | Title | Preconditions | Steps | Expected Result | Coverage |
| --- | --- | --- | --- | --- | --- |
| TC1 | Upsert after tracked entity retrieval | Entity is retrieved without `AsNoTracking`. | 1. Fetch aggregate from DB. 2. Modify entity. 3. Call `Upsert`. | Entity is updated and no duplicate row is inserted. | Automated |
| TC2 | Upsert after `AsNoTracking` retrieval | Entity is retrieved using `AsNoTracking()`. | 1. Fetch aggregate using `AsNoTracking`. 2. Modify entity. 3. Call `Upsert`. | Existing entity is still updated correctly and no duplicate insert occurs. | Automated |
| TC3 | Verify update vs insert decision | Existing entity already exists in database. | 1. Retrieve entity. 2. Modify fields. 3. Execute `Upsert`. | System performs update only; primary key stays unchanged. | Automated |
| TC4 | Data integrity after repeated upserts | Multiple upserts run for the same entity. | 1. Execute repeated upsert operations for the same `UserId`. 2. Query the aggregate table. | Exactly one record exists for the entity and it matches the latest update. | Automated |

## Regression: Username Not Updating In Header After Profile Edit

Automated coverage lives in [frontend/e2e/register-and-profile.spec.js](../frontend/e2e/register-and-profile.spec.js).

| ID | Title | Preconditions | Steps | Expected Result | Coverage |
| --- | --- | --- | --- | --- | --- |
| TC1 | Username updates in profile but not in header | User is logged in. | 1. Navigate to Profile. 2. Change username. 3. Save changes. | Profile reflects the new username. Header keeps the old username if the bug is present. | Regression reference |
| TC2 | Header reflects updated username after profile change | User updates username successfully. | 1. Edit username in Profile. 2. Save changes. 3. Observe the header. | Header updates immediately with the new username without logout/login. | Automated |
| TC3 | Token refresh after username update | System refreshes JWT after profile update. | 1. Update username. 2. Backend returns updated token. 3. Frontend replaces stored token. | Decoded token contains the new username and the header stays consistent. | Optional, architecture-specific |
| TC4 | Auth state synchronization across the UI | App uses centralized profile/auth synchronization. | 1. Update username in Profile. 2. Navigate across protected pages. 3. Reload the page. | Header and profile stay synchronized with the updated username across the app. | Automated |

## Regression: Last Word Not Counted When Timer Expires

Automated coverage lives in [frontend/e2e/typing-happy-path.spec.js](../frontend/e2e/typing-happy-path.spec.js).

| ID | Title | Preconditions | Steps | Expected Result | Coverage |
| --- | --- | --- | --- | --- | --- |
| TC1 | Last word typed correctly but not submitted | Typing test is active. | 1. Type the final word completely. 2. Do not press Space. 3. Let timer reach 0. | Bug reproduction shows correct characters counted but correct words undercounted. | Regression reference |
| TC2 | Completed last word is counted on timeout | Fix is applied in finish logic. | 1. Type the final word fully and correctly. 2. Do not press Space. 3. Wait for timer to expire. | Final word is counted as correct and results stay consistent. | Automated |
| TC3 | Incomplete last word is not counted | Typing test is active. | 1. Type only part of the final word. 2. Let timer reach 0. | Final word is not counted as correct. | Automated |
| TC4 | Character and word scoring remain consistent | Multiple correct words are typed and the last edge-case word occurs at timeout. | 1. Submit earlier correct words. 2. Complete the final word without pressing Space. 3. Let timer expire. | Word count and character accuracy remain aligned with the typed input. | Automated |