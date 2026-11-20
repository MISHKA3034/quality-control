#!/usr/bin/env bash
# Запуск API и клиента при каждом старте Codespace.
cd "$(dirname "$0")/.."

echo "Ожидание SQL Server..."
for i in $(seq 1 90); do
  (exec 3<>/dev/tcp/127.0.0.1/1433) 2>/dev/null && break
  sleep 2
done

# API (миграции и демо-данные применяются при старте). Перезапуск, если БД ещё не готова принимать вход.
(
  for attempt in 1 2 3 4 5; do
    dotnet run --project src/QualityControl.API --launch-profile http && break
    echo "API завершился, повтор через 10 с (попытка $attempt)"; sleep 10
  done
) > /tmp/askk-api.log 2>&1 &

cd client && npm run dev -- --host 0.0.0.0 > /tmp/askk-client.log 2>&1 &
wait
