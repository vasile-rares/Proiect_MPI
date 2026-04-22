# Leaderboard Load Test

This folder contains the load-test runner for the `GET /api/StatisticsGame/leaderboard` endpoint.

## What the runner validates

- It measures leaderboard latency while multiple users submit scores simultaneously.
- It checks the acceptance target at 50 concurrent users.
- It keeps increasing concurrency until the API starts returning 5xx responses, then records that concurrency as the breaking point.
- It generates Markdown and JSON reports that can be handed to DevOps.

The runner marks the target as passing only when all of these conditions are true at the configured target concurrency:

- The leaderboard average response time is under 500 ms.
- The leaderboard p95 response time is under 500 ms.
- No 5xx responses are returned.
- No transport errors are returned.

## Prerequisites

- Start the backend API first. In this workspace you can use the `backend: watch` task.
- If you run the API locally, make sure `Jwt__Key` is set to a strong secret with at least 32 characters. The API refuses to start otherwise.
- Point the test at an isolated environment or database if you do not want the generated users and scores in your normal dev data.

## Run the test

From the `frontend` folder:

```bash
npm run load:leaderboard
```

Example against another environment:

```bash
npm run load:leaderboard -- --api-base-url=https://staging.example.com --target-concurrency=50 --max-concurrency=120 --step=10
```

## Supported options

- `--api-base-url`: Backend base URL. Default: `http://localhost:5232`
- `--scope`: `daily`, `weekly`, or `all-time`. Default: `all-time`
- `--duration-in-seconds`: Duration filter sent to the leaderboard. Default: `15`
- `--mode`: Mode filter sent to the leaderboard. Default: `time`
- `--top-n`: Top-N leaderboard size. Default: `20`
- `--target-concurrency`: Acceptance target. Default: `50`
- `--max-concurrency`: Highest concurrency to probe for the breaking point. Default: `100` or the target concurrency, whichever is higher
- `--step`: Concurrency increment while searching for the breaking point. Default: `10`
- `--iterations-per-user`: Scenario loops per virtual user at each concurrency level. Default: `3`
- `--setup-concurrency`: Registration and warm-up parallelism. Default: `10`
- `--request-timeout-ms`: Per-request timeout. Default: `10000`
- `--threshold-ms`: Leaderboard latency threshold. Default: `500`
- `--output-dir`: Custom folder for generated reports. Default: `load-tests/reports`

## Generated artifacts

Each run writes four files:

- A timestamped Markdown report
- A timestamped JSON report
- `leaderboard-load-report-latest.md`
- `leaderboard-load-report-latest.json`

## DevOps handoff

Share the latest Markdown and JSON reports with DevOps after each meaningful run.
The Markdown file is intended for direct attachment to a ticket or chat thread, while the JSON file preserves raw metrics for follow-up analysis.