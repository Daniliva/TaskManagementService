# Task Management Service

Lightweight REST API for task management with status rules and validation.

## Features
- Create task (default: `Backlog`)
- List all tasks
- Get task by ID
- Update status: `Backlog → InWork → Testing → Done` only

## Tech Stack
- C# / .NET 6+
- ASP.NET Core
- In-Memory storage
- FluentValidation
- xUnit

## How to Run
1. `dotnet restore`
2. `dotnet run`

Swagger: `https://localhost:****/swagger`

## Example (cURL)
```bash
curl -X POST "https://localhost:****/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{"title":"Task","description":"Desc"}'
## Tests
dotnet test