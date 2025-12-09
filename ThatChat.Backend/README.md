# ThatChat.Backend

Простое чат приложение

Структура:

- `Api` - api эндпоинты
- `Data` - всё связанное с бд
- `Dto` - dto которые отдаются и принимаются по api
- `Hubs` - signalr логика
- `Logs` - логи, которые сохраняются сюда (по тз надо)
- `Migrations` - миграции бд от .net
- `Services` - сервисы (основная логика)

Тесты в [`ThatChat.Backend.Tests`](../ThatChat.Backend.Tests)

Фронт в другом репозитории

По функционалу:

- Глобальный чат, приватные чаты (1 на 1)
- Отправка сообщений (через signalr)
- Отправка файлов (сохраняются в `wwwroot/uploads`, название рандомное, отдаётся ссылка)
- Бд - sqlite (создание структуры происходит при dev)
- Логи (apache-like) идут в `Logs/`

Требуемые env или appsettings (при env будет другая структура) (примерные значения):

```json
{
    "ConnectionStrings": {
        "AppDbContext": "Data Source=TempAppDbContext.db;"
    },
    "CorsOrigins": [
        "http://localhost:5173",
        "http://localhost:4173",
        "http://192.168.3.111:5173",
        "http://192.168.3.111:4173"
    ]
}
```


