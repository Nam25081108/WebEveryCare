-- Chạy khi đang kết nối vào database "postgres", không kết nối vào "sachnha".
-- Dừng backend trước khi chạy để giải phóng các kết nối đang sử dụng database cũ.
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'sachnha'
  AND pid <> pg_backend_pid();

ALTER DATABASE sachnha RENAME TO everycare;
