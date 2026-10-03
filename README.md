# Task Management Service

Лёгкий REST API задач на ASP.NET Core: создание, поиск и фильтры, правка, удаление, смена статуса по цепочке и история переходов.

## Возможности

| Метод и путь | Что делает |
|---|---|
| `POST /api/tasks` | создать задачу (статус всегда `Backlog`) → 201 + `Location` |
| `GET /api/tasks?status=&search=&page=&pageSize=` | список с фильтром по статусу, поиском по названию и описанию и пагинацией (`pageSize` 1–100, по умолчанию 20) |
| `GET /api/tasks/{id}` | задача или 404 |
| `PUT /api/tasks/{id}` | заменить название и описание |
| `DELETE /api/tasks/{id}` | удалить → 204 |
| `PATCH /api/tasks/{id}/status` | сменить статус: только `Backlog → InWork → Testing → Done`; запрещённый переход → 409 со списком допустимых |
| `GET /api/tasks/{id}/history` | история переходов, от старых к новым |
| `GET /health` | проверка живости |

Статусы передаются строками (`"InWork"`, регистр не важен). Ошибки — `application/problem+json`: 400 (проверка ввода, список ошибок по полям),
404, 409, 500 (без подробностей).

## Технологии

C# / .NET 9, ASP.NET Core, FluentValidation, Swagger (Swashbuckle), хранилище в памяти (потокобезопасное), xUnit.

## Запуск

```bash
dotnet restore
dotnet run --project TaskManagementService
```

Swagger (в Development): `https://localhost:<порт>/swagger`

## Пример

```bash
curl -X POST "https://localhost:<порт>/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{"title":"Task","description":"Desc"}'

curl -X PATCH "https://localhost:<порт>/api/tasks/1/status" \
  -H "Content-Type: application/json" \
  -d '{"status":"InWork"}'
```

## Тесты

```bash
dotnet test
```

Данные хранятся в памяти и пропадают при перезапуске (постоянное хранение — в плане, см. `docs/PLAN.md`).
