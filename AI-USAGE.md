# AI Usage

## AI Strategy
- I used GitHub Copilot to scaffold the layered .NET projects, draft the domain-service behavior, and create the initial unit tests.
- I provided the AI with the assignment requirements, the target project structure, and the expected endpoints and behaviors so it could generate consistent code.
- I broke the work into backend domain logic, infrastructure adapters, API wiring, tests, and frontend implementation so the context stayed focused and reviewable.

## Human Audit
- I accepted the AI-generated service structure because it matched the requested DDD layering and made the hold lifecycle explicit.
- I rejected the initial boilerplate API and the overly generic weather-forecast endpoint, replacing it with inventory-hold-specific controllers and startup seeding.
- I also corrected the AI-generated infrastructure assumptions around RabbitMQ and Redis by aligning them with the actual package APIs in the environment.

## Verification
- I used the AI-generated test scaffolding as a starting point and validated each behavior by running the NUnit suite.
- I verified the implementation by running `dotnet test InventoryHold.UnitTests/InventoryHold.UnitTests.csproj` and confirming 6 tests passed.
- I also checked the generated backend build and prepared the Docker and Vite frontend entry points for local validation.
