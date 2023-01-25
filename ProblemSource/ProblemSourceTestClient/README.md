
Month with most syncs is March 2020:
```SQL
WITH cte AS (
SELECT CONCAT(DATEPART(year, updated_at), '-', DATEPART(month, updated_at))  AS yearMonth
  FROM [trainingdb].[dbo].[phases]
WHERE updated_at > '2017-03-01' and updated_at < '2020-04-01'
)
SELECT COUNT(*), yearMonth FROM cte GROUP BY yearMonth ORDER BY yearMonth
```

Within that month, 9 AM was most synced hour - 70453 syncs total during month at that hour:
```SQL
WITH cte AS (
SELECT DATEPART(hour, updated_at)  AS dayHour
  FROM [trainingdb].[dbo].[phases]
WHERE updated_at > '2020-03-01' and updated_at < '2020-04-01'
)
SELECT COUNT(*), dayHour FROM cte GROUP BY dayHour ORDER BY dayHour
```

On the 27th, 8-9, 4927 phases were synced = 1.4 / second :
```SQL
WITH cte AS (
SELECT CONCAT(DATEPART(day, updated_at), '-', DATEPART(hour, updated_at))  AS dayHour--, account_id
  FROM [trainingdb].[dbo].[phases]
WHERE updated_at > '2020-03-01' and updated_at < '2020-04-01'
)
SELECT COUNT(*) as cnt, dayHour FROM cte GROUP BY dayHour ORDER BY cnt DESC
```

On the 27th, 8-9, 427 distinct trainings were syncing, so by approximation each training synced 11.5 times during the hour
```SQL
WITH cte AS (
SELECT CONCAT(DATEPART(day, updated_at), '-', DATEPART(hour, updated_at))  AS dayHour, account_id
  FROM [trainingdb].[dbo].[phases]
  WHERE updated_at > '2020-03-01' and updated_at < '2020-04-01'
)
SELECT COUNT(DISTINCT(account_id)) as cnt, dayHour FROM cte GROUP BY dayHour ORDER BY cnt DESC
```
