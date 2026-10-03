# Спецификация реализации

Ветка `spec`: ваш исходный код плюс заглушки `NotImplementedException("TODO")` и красные тесты. Ветка `reference`: готовая реализация.
Всего тестов: **136** (из них 11 — ваши исходные, они остаются зелёными).

Красных на ветке `spec`: **109** (на сборке .NET 8, где я проверял). Ещё один тест — `There_is_exactly_one_openapi_document_generator` —
красный только на вашей сборке с .NET 9: он ждёт, что `/openapi/v1.json` не существует, а в исходном коде есть `AddOpenApi`;
при моей проверке на .NET 8 этот вызов пришлось убрать, поэтому тест там зелёный.

## Что оставлено готовым

Модели и DTO (`Task` с `CreatedAt`/`UpdatedAt`, `TaskHistoryEntry`, `TaskQuery`, `PagedResult`, `UpdateTaskDto`; у `UpdateStatusDto.Status` теперь `?`),
интерфейс `ITaskRepository` с новыми методами, `CreateTaskDtoValidator`, `Dockerfile`, CI, README, примеры `.http`. Новые зависимости тестового проекта:
`Microsoft.AspNetCore.Mvc.Testing` и `Microsoft.Extensions.TimeProvider.Testing`. Из двух исходных тестовых файлов удалена неиспользуемая строка
`using TaskManagementService.Services;` (класса `TaskStatusService` больше нет).

## Порядок и тесты

Запуск набора: `dotnet test --filter "FullyQualifiedName~TaskStatusRulesTests"`.

| # | Что реализовать | Файл | Красные тесты |
|---|---|---|---|
| 1 | `TaskStatusRules` (3 метода) | `Domain/TaskStatusRules.cs` | `TaskStatusRulesTests` (26) |
| 2 | потокобезопасный репозиторий | `Repositories/InMemoryTaskRepository.cs` | `TaskRepositoryExtendedTests` (39 из 40) |
| 3 | правила валидаторов | `Validators/UpdateTaskDtoValidator.cs`, `TaskQueryValidator.cs`, `UpdateStatusDtoValidator.cs` | `ValidatorTests` (9 из 20) |
| 4 | `ValidationFilter`, `Program.cs` (JSON, ProblemDetails, 500, DI, `/health`) | `Filters/ValidationFilter.cs`, `Program.cs` | `ApiTests` (формат ошибок, health, 500, OpenAPI) |
| 5 | действия контроллера | `Controllers/TasksController.cs` | `ApiTests` (35 из 39 красные) |

Критерий готовности: `dotnet test` — 136 зелёных.

## Тонкости, которых не видно из сигнатур

* Ключ ошибок валидации — имя свойства в camelCase; пустое тело даёт ключ `""`.
* 409 содержит `allowedNext` — массив строк; для `Done` он пустой.
* `GetAllAsync` возвращает копии, отсортированные по id; `QueryAsync` зажимает `pageSize` в 1..100 и `page` в ≥1 (валидатор отдельно отвечает 400 на такие значения до репозитория).
* После `DeleteAsync` идентификаторы не переиспользуются.
* `Create` игнорирует `Id`, `Status` и временные метки, присланные вызывающим.
* История — копия: изменение возвращённого списка не меняет хранилище.
