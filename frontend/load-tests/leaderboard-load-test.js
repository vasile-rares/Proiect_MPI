const fs = require("fs/promises");
const path = require("path");
const crypto = require("crypto");

const DEFAULT_API_BASE_URL = "http://localhost:5232";
const DEFAULT_OUTPUT_DIR = path.join(__dirname, "reports");

function parseArgs(argv) {
  const args = {};

  for (let index = 0; index < argv.length; index += 1) {
    const token = argv[index];

    if (!token.startsWith("--")) {
      continue;
    }

    const trimmed = token.slice(2);
    const separatorIndex = trimmed.indexOf("=");
    const rawKey = separatorIndex >= 0 ? trimmed.slice(0, separatorIndex) : trimmed;

    let rawValue = separatorIndex >= 0 ? trimmed.slice(separatorIndex + 1) : undefined;
    if (rawValue === undefined) {
      const nextToken = argv[index + 1];
      if (nextToken && !nextToken.startsWith("--")) {
        rawValue = nextToken;
        index += 1;
      } else {
        rawValue = "true";
      }
    }

    args[toCamelCase(rawKey)] = rawValue;
  }

  return args;
}

function toCamelCase(value) {
  return value.replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
}

function toNpmConfigKey(key) {
  return `npm_config_${key.replace(/([A-Z])/g, "_$1").toLowerCase()}`;
}

function getStringOption(args, key, envKey, fallback) {
  return String(args[key] ?? process.env[envKey] ?? process.env[toNpmConfigKey(key)] ?? fallback).trim();
}

function getIntegerOption(args, key, envKey, fallback, minimumValue) {
  const rawValue = args[key] ?? process.env[envKey] ?? process.env[toNpmConfigKey(key)] ?? fallback;
  const parsedValue = Number.parseInt(String(rawValue), 10);

  if (!Number.isInteger(parsedValue) || parsedValue < minimumValue) {
    throw new Error(`${key} must be an integer greater than or equal to ${minimumValue}.`);
  }

  return parsedValue;
}

function normalizeBaseUrl(rawBaseUrl) {
  return rawBaseUrl.replace(/\/+$/, "");
}

function buildConfig() {
  const args = parseArgs(process.argv.slice(2));
  const targetConcurrency = getIntegerOption(
    args,
    "targetConcurrency",
    "LOAD_TEST_TARGET_CONCURRENCY",
    50,
    1,
  );
  const maxConcurrency = getIntegerOption(
    args,
    "maxConcurrency",
    "LOAD_TEST_MAX_CONCURRENCY",
    Math.max(100, targetConcurrency),
    targetConcurrency,
  );
  const config = {
    apiBaseUrl: normalizeBaseUrl(
      getStringOption(args, "apiBaseUrl", "LOAD_TEST_API_BASE_URL", DEFAULT_API_BASE_URL),
    ),
    scope: getStringOption(args, "scope", "LEADERBOARD_SCOPE", "all-time"),
    durationInSeconds: getIntegerOption(
      args,
      "durationInSeconds",
      "LEADERBOARD_DURATION_SECONDS",
      15,
      1,
    ),
    mode: getStringOption(args, "mode", "LEADERBOARD_MODE", "time"),
    topN: getIntegerOption(args, "topN", "LEADERBOARD_TOP_N", 20, 1),
    targetConcurrency,
    maxConcurrency,
    step: getIntegerOption(args, "step", "LOAD_TEST_STEP", 10, 1),
    iterationsPerUser: getIntegerOption(
      args,
      "iterationsPerUser",
      "LOAD_TEST_ITERATIONS_PER_USER",
      3,
      1,
    ),
    thresholdMs: getIntegerOption(args, "thresholdMs", "LEADERBOARD_THRESHOLD_MS", 500, 1),
    setupConcurrency: getIntegerOption(args, "setupConcurrency", "LOAD_TEST_SETUP_CONCURRENCY", 10, 1),
    requestTimeoutMs: getIntegerOption(
      args,
      "requestTimeoutMs",
      "LOAD_TEST_REQUEST_TIMEOUT_MS",
      10000,
      1,
    ),
    outputDir: path.resolve(
      getStringOption(args, "outputDir", "LOAD_TEST_OUTPUT_DIR", DEFAULT_OUTPUT_DIR),
    ),
    runId: crypto.randomUUID().replace(/-/g, ""),
  };

  validateScope(config.scope);

  return config;
}

function validateScope(scope) {
  const validScopes = new Set(["daily", "weekly", "all-time"]);
  if (!validScopes.has(scope)) {
    throw new Error("scope must be one of: daily, weekly, all-time.");
  }
}

function createMetricBucket() {
  return {
    durationsMs: [],
    status2xx: 0,
    status4xx: 0,
    status5xx: 0,
    otherStatuses: 0,
    transportErrors: 0,
    failures: [],
  };
}

function recordMetric(bucket, operation, result) {
  bucket.durationsMs.push(result.durationMs);

  if (result.isTransportError) {
    bucket.transportErrors += 1;
    pushFailure(bucket, operation, result);
    return;
  }

  if (result.status >= 200 && result.status < 300) {
    bucket.status2xx += 1;
    return;
  }

  if (result.status >= 400 && result.status < 500) {
    bucket.status4xx += 1;
  } else if (result.status >= 500) {
    bucket.status5xx += 1;
  } else {
    bucket.otherStatuses += 1;
  }

  pushFailure(bucket, operation, result);
}

function pushFailure(bucket, operation, result) {
  if (bucket.failures.length >= 8) {
    return;
  }

  bucket.failures.push({
    operation,
    status: result.status,
    durationMs: roundNumber(result.durationMs),
    error: result.errorMessage || null,
    body: truncateText(result.bodyText || "", 200),
  });
}

function mergeBuckets(targetBucket, sourceBucket) {
  targetBucket.durationsMs.push(...sourceBucket.durationsMs);
  targetBucket.status2xx += sourceBucket.status2xx;
  targetBucket.status4xx += sourceBucket.status4xx;
  targetBucket.status5xx += sourceBucket.status5xx;
  targetBucket.otherStatuses += sourceBucket.otherStatuses;
  targetBucket.transportErrors += sourceBucket.transportErrors;

  for (const failure of sourceBucket.failures) {
    if (targetBucket.failures.length >= 12) {
      break;
    }

    targetBucket.failures.push(failure);
  }
}

function summarizeBucket(bucket) {
  const durations = [...bucket.durationsMs].sort((left, right) => left - right);
  const averageMs = durations.length === 0
    ? null
    : durations.reduce((sum, value) => sum + value, 0) / durations.length;

  return {
    requestCount: durations.length,
    minMs: durations.length === 0 ? null : roundNumber(durations[0]),
    averageMs: averageMs === null ? null : roundNumber(averageMs),
    p50Ms: durations.length === 0 ? null : roundNumber(percentile(durations, 0.5)),
    p95Ms: durations.length === 0 ? null : roundNumber(percentile(durations, 0.95)),
    maxMs: durations.length === 0 ? null : roundNumber(durations[durations.length - 1]),
    status2xx: bucket.status2xx,
    status4xx: bucket.status4xx,
    status5xx: bucket.status5xx,
    otherStatuses: bucket.otherStatuses,
    transportErrors: bucket.transportErrors,
    failures: bucket.failures,
  };
}

function percentile(sortedValues, ratio) {
  if (sortedValues.length === 1) {
    return sortedValues[0];
  }

  const position = (sortedValues.length - 1) * ratio;
  const lowerIndex = Math.floor(position);
  const upperIndex = Math.ceil(position);

  if (lowerIndex === upperIndex) {
    return sortedValues[lowerIndex];
  }

  const lowerValue = sortedValues[lowerIndex];
  const upperValue = sortedValues[upperIndex];
  const weight = position - lowerIndex;

  return lowerValue + ((upperValue - lowerValue) * weight);
}

function roundNumber(value) {
  return Number(value.toFixed(2));
}

function truncateText(value, maxLength) {
  if (value.length <= maxLength) {
    return value;
  }

  return `${value.slice(0, maxLength - 3)}...`;
}

function createCredentials(runId, userIndex) {
  const suffix = `${runId}_${String(userIndex).padStart(4, "0")}`;
  const username = `load_${suffix}`;
  const email = `${username}@example.com`;

  return {
    username,
    email,
    verifyEmail: email,
    password: "Keyless!123",
    verifyPassword: "Keyless!123",
  };
}

function decodeJwtSubject(token) {
  const parts = token.split(".");
  if (parts.length < 2) {
    throw new Error("Received an invalid JWT token from the API.");
  }

  const payload = JSON.parse(Buffer.from(parts[1], "base64url").toString("utf8"));
  if (!payload.sub) {
    throw new Error("JWT token does not contain a sub claim.");
  }

  return payload.sub;
}

function buildLeaderboardUrl(config) {
  const params = new URLSearchParams({
    scope: config.scope,
    durationInSeconds: String(config.durationInSeconds),
    mode: config.mode,
    topN: String(config.topN),
  });

  return `${config.apiBaseUrl}/api/StatisticsGame/leaderboard?${params.toString()}`;
}

function buildScorePayload(user, config, iteration) {
  const baseCorrectCharacters = 260 + ((user.sequenceSeed + iteration) % 40) * 5;
  const incorrectCharacters = (user.sequenceSeed + iteration) % 4;
  const extraCharacters = iteration % 2;
  const missedCharacters = (user.sequenceSeed + iteration + 1) % 3;

  return {
    userId: user.userId,
    correctCharacters: baseCorrectCharacters,
    incorrectCharacters,
    extraCharacters,
    missedCharacters,
    durationInSeconds: config.durationInSeconds,
    mode: config.mode,
  };
}

async function performTimedRequest(url, options, timeoutMs) {
  const controller = new AbortController();
  const timeoutHandle = setTimeout(() => controller.abort(), timeoutMs);
  const startedAt = process.hrtime.bigint();

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
    });
    const bodyText = await response.text();
    return {
      ok: response.ok,
      status: response.status,
      bodyText,
      durationMs: Number(process.hrtime.bigint() - startedAt) / 1_000_000,
      isTransportError: false,
      errorMessage: null,
    };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      bodyText: "",
      durationMs: Number(process.hrtime.bigint() - startedAt) / 1_000_000,
      isTransportError: true,
      errorMessage: error.name === "AbortError" ? "Request timed out." : error.message,
    };
  } finally {
    clearTimeout(timeoutHandle);
  }
}

function parseJsonResponse(result, context) {
  try {
    return JSON.parse(result.bodyText);
  } catch (error) {
    throw new Error(`${context} returned invalid JSON: ${error.message}`);
  }
}

async function ensureApiIsReachable(config) {
  const result = await performTimedRequest(buildLeaderboardUrl(config), {
    method: "GET",
  }, config.requestTimeoutMs);

  if (result.isTransportError) {
    throw new Error(`Could not reach the API at ${config.apiBaseUrl}: ${result.errorMessage}`);
  }

  if (!result.ok) {
    throw new Error(
      `Pre-flight request to ${buildLeaderboardUrl(config)} failed with status ${result.status}.`,
    );
  }
}

async function mapWithConcurrency(items, concurrency, worker) {
  const results = new Array(items.length);
  let nextIndex = 0;

  async function runWorker() {
    while (true) {
      const currentIndex = nextIndex;
      nextIndex += 1;

      if (currentIndex >= items.length) {
        return;
      }

      results[currentIndex] = await worker(items[currentIndex], currentIndex);
    }
  }

  const workerCount = Math.min(concurrency, items.length);
  await Promise.all(Array.from({ length: workerCount }, () => runWorker()));
  return results;
}

async function registerVirtualUsers(config) {
  const userIndexes = Array.from({ length: config.maxConcurrency }, (_, index) => index);

  return mapWithConcurrency(userIndexes, config.setupConcurrency, async (userIndex) => {
    const credentials = createCredentials(config.runId, userIndex);
    const result = await performTimedRequest(
      `${config.apiBaseUrl}/api/Authentication/register`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(credentials),
      },
      config.requestTimeoutMs,
    );

    if (!result.ok) {
      throw new Error(
        `User registration failed for ${credentials.username} with status ${result.status}: ${truncateText(result.bodyText, 300)}`,
      );
    }

    const payload = parseJsonResponse(result, "Registration");
    return {
      userId: decodeJwtSubject(payload.token),
      token: payload.token,
      username: credentials.username,
      sequenceSeed: userIndex,
    };
  });
}

async function submitScore(user, config, iteration) {
  return performTimedRequest(
    `${config.apiBaseUrl}/api/StatisticsGame`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${user.token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(buildScorePayload(user, config, iteration)),
    },
    config.requestTimeoutMs,
  );
}

async function fetchLeaderboard(config) {
  return performTimedRequest(buildLeaderboardUrl(config), {
    method: "GET",
  }, config.requestTimeoutMs);
}

async function warmUpEnvironment(users, config) {
  await mapWithConcurrency(users, config.setupConcurrency, async (user, index) => {
    const result = await submitScore(user, config, index);
    if (!result.ok) {
      throw new Error(
        `Warm-up score submission failed for ${user.username} with status ${result.status}: ${truncateText(result.bodyText, 300)}`,
      );
    }
  });

  const leaderboardResult = await fetchLeaderboard(config);
  if (!leaderboardResult.ok) {
    throw new Error(
      `Warm-up leaderboard request failed with status ${leaderboardResult.status}: ${truncateText(leaderboardResult.bodyText, 300)}`,
    );
  }
}

async function waitForStart(startAtMs) {
  const delayMs = startAtMs - Date.now();
  if (delayMs <= 0) {
    return;
  }

  await new Promise((resolve) => setTimeout(resolve, delayMs));
}

async function runVirtualUser(user, config, startAtMs) {
  const submissionMetrics = createMetricBucket();
  const leaderboardMetrics = createMetricBucket();

  await waitForStart(startAtMs);

  for (let iteration = 0; iteration < config.iterationsPerUser; iteration += 1) {
    const submissionResult = await submitScore(user, config, iteration);
    recordMetric(submissionMetrics, "submit-score", submissionResult);

    const leaderboardResult = await fetchLeaderboard(config);
    recordMetric(leaderboardMetrics, "get-leaderboard", leaderboardResult);
  }

  return {
    submissionMetrics,
    leaderboardMetrics,
  };
}

async function runConcurrencyLevel(users, concurrency, config) {
  const activeUsers = users.slice(0, concurrency);
  const startAtMs = Date.now() + 250;
  const workerResults = await Promise.all(
    activeUsers.map((user) => runVirtualUser(user, config, startAtMs)),
  );

  const submissionMetrics = createMetricBucket();
  const leaderboardMetrics = createMetricBucket();

  for (const workerResult of workerResults) {
    mergeBuckets(submissionMetrics, workerResult.submissionMetrics);
    mergeBuckets(leaderboardMetrics, workerResult.leaderboardMetrics);
  }

  const submissions = summarizeBucket(submissionMetrics);
  const leaderboard = summarizeBucket(leaderboardMetrics);
  const total5xx = submissions.status5xx + leaderboard.status5xx;
  const totalTransportErrors = submissions.transportErrors + leaderboard.transportErrors;

  return {
    concurrency,
    iterationsPerUser: config.iterationsPerUser,
    submissions,
    leaderboard,
    total5xx,
    totalTransportErrors,
    leaderboardMeetsThreshold:
      leaderboard.averageMs !== null &&
      leaderboard.p95Ms !== null &&
      leaderboard.averageMs <= config.thresholdMs &&
      leaderboard.p95Ms <= config.thresholdMs,
  };
}

function buildConcurrencyLevels(config) {
  const levels = new Set();

  for (let concurrency = config.step; concurrency <= config.maxConcurrency; concurrency += config.step) {
    levels.add(concurrency);
  }

  levels.add(config.targetConcurrency);

  return [...levels].sort((left, right) => left - right);
}

function summarizeRun(levels, config) {
  const targetLevel = levels.find((level) => level.concurrency === config.targetConcurrency) || null;
  const breakingLevel = levels.find((level) => level.total5xx > 0) || null;

  const targetPassed = Boolean(
    targetLevel &&
    targetLevel.leaderboardMeetsThreshold &&
    targetLevel.total5xx === 0 &&
    targetLevel.totalTransportErrors === 0,
  );

  return {
    thresholdMs: config.thresholdMs,
    targetConcurrency: config.targetConcurrency,
    targetPassed,
    targetLevel,
    breakingPointConcurrency: breakingLevel ? breakingLevel.concurrency : null,
    maxExecutedConcurrency: levels.length === 0 ? 0 : levels[levels.length - 1].concurrency,
    devOpsActions: buildDevOpsActions(targetLevel, breakingLevel, config),
  };
}

function buildDevOpsActions(targetLevel, breakingLevel, config) {
  const actions = [];

  if (!targetLevel) {
    actions.push(
      `Breaking point was reached before the ${config.targetConcurrency}-user target. Review API CPU and memory limits before the next run.`,
    );
  } else if (!targetLevel.leaderboardMeetsThreshold || targetLevel.totalTransportErrors > 0) {
    actions.push(
      `At ${config.targetConcurrency} concurrent users the leaderboard exceeded ${config.thresholdMs}ms average/p95 or transport errors occurred. Review server sizing and connection pool limits.`,
    );
  } else {
    actions.push(
      `At ${config.targetConcurrency} concurrent users the leaderboard stayed under ${config.thresholdMs}ms average and p95 with no 5xx responses. Keep this report as the current capacity baseline.`,
    );
  }

  if (breakingLevel) {
    actions.push(
      `First 5xx responses appeared at ${breakingLevel.concurrency} concurrent users. Share this concurrency level with DevOps so they can tune compute, database resources, or autoscaling rules.`,
    );
  } else {
    actions.push(
      `No 5xx responses were observed up to ${config.maxConcurrency} concurrent users. DevOps can use this run to delay scaling changes or to size a higher target for the next test.`,
    );
  }

  actions.push(
    "Include the generated Markdown and JSON reports in the DevOps handoff so infrastructure changes can be tied back to measured latency and failure thresholds.",
  );

  return actions;
}

function buildReport(config, levels) {
  return {
    generatedAtUtc: new Date().toISOString(),
    configuration: {
      apiBaseUrl: config.apiBaseUrl,
      scope: config.scope,
      durationInSeconds: config.durationInSeconds,
      mode: config.mode,
      topN: config.topN,
      targetConcurrency: config.targetConcurrency,
      maxConcurrency: config.maxConcurrency,
      step: config.step,
      iterationsPerUser: config.iterationsPerUser,
      thresholdMs: config.thresholdMs,
      requestTimeoutMs: config.requestTimeoutMs,
    },
    summary: summarizeRun(levels, config),
    levels,
  };
}

function formatMs(value) {
  return value === null ? "-" : `${value.toFixed(2)} ms`;
}

function renderLevelRow(level) {
  return `| ${level.concurrency} | ${formatMs(level.leaderboard.averageMs)} | ${formatMs(level.leaderboard.p95Ms)} | ${formatMs(level.leaderboard.maxMs)} | ${level.leaderboard.status5xx} | ${level.submissions.status5xx} | ${level.totalTransportErrors} |`;
}

function renderFailures(level) {
  const combinedFailures = [
    ...level.leaderboard.failures.map((failure) => ({ ...failure, source: "leaderboard" })),
    ...level.submissions.failures.map((failure) => ({ ...failure, source: "submit-score" })),
  ];

  if (combinedFailures.length === 0) {
    return "No failures captured.";
  }

  return combinedFailures
    .slice(0, 8)
    .map((failure) => `- ${failure.source}: status=${failure.status}, duration=${failure.durationMs}ms, error=${failure.error || "-"}, body=${failure.body || "-"}`)
    .join("\n");
}

function renderMarkdownReport(report) {
  const summary = report.summary;
  const targetLevel = summary.targetLevel;
  const targetStatus = summary.targetPassed ? "PASS" : "FAIL";
  const breakingPointText = summary.breakingPointConcurrency === null
    ? `No 5xx responses observed up to ${summary.maxExecutedConcurrency} concurrent users.`
    : `First 5xx responses observed at ${summary.breakingPointConcurrency} concurrent users.`;

  const lines = [
    "# Leaderboard Load Test Report",
    "",
    `Generated at (UTC): ${report.generatedAtUtc}`,
    `API base URL: ${report.configuration.apiBaseUrl}`,
    "",
    "## Acceptance Summary",
    "",
    `- Target concurrency: ${summary.targetConcurrency} users`,
    `- Target status: ${targetStatus}`,
    `- Leaderboard threshold: ${summary.thresholdMs} ms for both average and p95`,
    targetLevel
      ? `- Leaderboard at target: avg=${formatMs(targetLevel.leaderboard.averageMs)}, p95=${formatMs(targetLevel.leaderboard.p95Ms)}, 5xx=${targetLevel.total5xx}, transportErrors=${targetLevel.totalTransportErrors}`
      : `- Leaderboard at target: target was not executed because the test hit a breaking point earlier.`,
    `- Breaking point: ${breakingPointText}`,
    "",
    "## Test Configuration",
    "",
    `- Scope: ${report.configuration.scope}`,
    `- Duration filter: ${report.configuration.durationInSeconds}s`,
    `- Mode: ${report.configuration.mode}`,
    `- Top N: ${report.configuration.topN}`,
    `- Iterations per user: ${report.configuration.iterationsPerUser}`,
    `- Request timeout: ${report.configuration.requestTimeoutMs} ms`,
    "",
    "## Results by Concurrency",
    "",
    "| Users | Leaderboard avg | Leaderboard p95 | Leaderboard max | Leaderboard 5xx | Submit 5xx | Transport errors |",
    "| --- | --- | --- | --- | --- | --- | --- |",
    ...report.levels.map(renderLevelRow),
    "",
    "## DevOps Handoff",
    "",
    ...summary.devOpsActions.map((action) => `- ${action}`),
  ];

  const failingLevels = report.levels.filter((level) => level.total5xx > 0 || level.totalTransportErrors > 0);
  if (failingLevels.length > 0) {
    lines.push("", "## Failure Samples", "");
    for (const level of failingLevels) {
      lines.push(`### ${level.concurrency} Concurrent Users`, "", renderFailures(level), "");
    }
  }

  return `${lines.join("\n").trim()}\n`;
}

async function writeReportFiles(report, config) {
  await fs.mkdir(config.outputDir, { recursive: true });

  const timestamp = report.generatedAtUtc.replace(/[:.]/g, "-");
  const jsonReportPath = path.join(config.outputDir, `leaderboard-load-report-${timestamp}.json`);
  const markdownReportPath = path.join(config.outputDir, `leaderboard-load-report-${timestamp}.md`);
  const latestJsonReportPath = path.join(config.outputDir, "leaderboard-load-report-latest.json");
  const latestMarkdownReportPath = path.join(config.outputDir, "leaderboard-load-report-latest.md");

  const jsonContent = JSON.stringify(report, null, 2);
  const markdownContent = renderMarkdownReport(report);

  await Promise.all([
    fs.writeFile(jsonReportPath, jsonContent, "utf8"),
    fs.writeFile(markdownReportPath, markdownContent, "utf8"),
    fs.writeFile(latestJsonReportPath, jsonContent, "utf8"),
    fs.writeFile(latestMarkdownReportPath, markdownContent, "utf8"),
  ]);

  return {
    jsonReportPath,
    markdownReportPath,
    latestJsonReportPath,
    latestMarkdownReportPath,
  };
}

function printLevelSummary(level) {
  console.log(
    [
      `Users=${level.concurrency}`,
      `leaderboardAvg=${formatMs(level.leaderboard.averageMs)}`,
      `leaderboardP95=${formatMs(level.leaderboard.p95Ms)}`,
      `leaderboard5xx=${level.leaderboard.status5xx}`,
      `submit5xx=${level.submissions.status5xx}`,
      `transportErrors=${level.totalTransportErrors}`,
    ].join(" | "),
  );
}

async function main() {
  const config = buildConfig();

  console.log(`Preparing leaderboard load test against ${config.apiBaseUrl}.`);
  await ensureApiIsReachable(config);

  console.log(`Registering ${config.maxConcurrency} virtual users.`);
  const users = await registerVirtualUsers(config);

  console.log("Seeding users and warming up the leaderboard query.");
  await warmUpEnvironment(users, config);

  const levels = [];
  const plannedLevels = buildConcurrencyLevels(config);

  for (const concurrency of plannedLevels) {
    console.log(`Running ${concurrency} concurrent users.`);
    const level = await runConcurrencyLevel(users, concurrency, config);
    levels.push(level);
    printLevelSummary(level);

    if (level.total5xx > 0) {
      console.log(`Breaking point detected at ${concurrency} concurrent users.`);
      break;
    }
  }

  const report = buildReport(config, levels);
  const reportPaths = await writeReportFiles(report, config);

  console.log(`Markdown report: ${reportPaths.latestMarkdownReportPath}`);
  console.log(`JSON report: ${reportPaths.latestJsonReportPath}`);

  if (!report.summary.targetPassed) {
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error("Leaderboard load test failed.");
  console.error(error.message);
  process.exitCode = 1;
});