SELECT 'CREATE DATABASE keyless_e2e'
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_database
    WHERE datname = 'keyless_e2e'
)\gexec