const path = require("path");
const { spawn } = require("child_process");
const { Client } = require("pg");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const apiDirectory = path.join(workspaceRoot, "backend", "Keyless.API");
const connectionString =
  process.env.E2E_POSTGRES_CONNECTION_STRING ||
  process.env.ConnectionStrings__DefaultConnection ||
  "Host=127.0.0.1;Port=55432;Database=keyless_e2e;Username=postgres;Password=postgres";

function parseConnectionString(rawConnectionString) {
  if (
    rawConnectionString.startsWith("postgres://") ||
    rawConnectionString.startsWith("postgresql://")
  ) {
    const url = new URL(rawConnectionString);

    return {
      host: url.hostname,
      port: url.port ? Number(url.port) : 5432,
      database:
        decodeURIComponent(url.pathname.replace(/^\//, "")) || "postgres",
      user: decodeURIComponent(url.username),
      password: decodeURIComponent(url.password),
    };
  }

  const segments = rawConnectionString
    .split(";")
    .map((segment) => segment.trim())
    .filter(Boolean);

  const values = Object.create(null);

  for (const segment of segments) {
    const separatorIndex = segment.indexOf("=");
    if (separatorIndex === -1) {
      continue;
    }

    const key = segment
      .slice(0, separatorIndex)
      .trim()
      .toLowerCase()
      .replace(/[\s_]/g, "");
    const value = segment.slice(separatorIndex + 1).trim();
    values[key] = value;
  }

  return {
    host: values.host || values.server || "127.0.0.1",
    port: values.port ? Number(values.port) : 5432,
    database: values.database || values.initialcatalog || "postgres",
    user:
      values.username ||
      values.userid ||
      values.user ||
      values.uid ||
      "postgres",
    password: values.password || values.pwd || "",
  };
}

function quoteIdentifier(identifier) {
  return `"${identifier.replace(/"/g, '""')}"`;
}

async function ensureDatabaseExists(rawConnectionString) {
  const config = parseConnectionString(rawConnectionString);
  const adminClient = new Client({
    host: config.host,
    port: config.port,
    database: "postgres",
    user: config.user,
    password: config.password,
  });

  await adminClient.connect();

  try {
    const result = await adminClient.query(
      "SELECT 1 FROM pg_database WHERE datname = $1",
      [config.database],
    );

    if (result.rowCount === 0) {
      await adminClient.query(
        `CREATE DATABASE ${quoteIdentifier(config.database)}`,
      );
    }
  } finally {
    await adminClient.end();
  }
}

// In CI the backend is pre-built; set DOTNET_NO_BUILD=1 to skip rebuild and
// avoid hitting the Playwright webServer timeout on the first cold run.
const dotnetRunArgs = ["run", "--no-launch-profile"];
if (process.env.DOTNET_NO_BUILD === "1") {
  dotnetRunArgs.push("--no-build");
}

async function main() {
  await ensureDatabaseExists(connectionString);

  const apiProcess = spawn("dotnet", dotnetRunArgs, {
    cwd: apiDirectory,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      ASPNETCORE_URLS: process.env.ASPNETCORE_URLS || "http://localhost:5232",
      ConnectionStrings__DefaultConnection: connectionString,
    },
    stdio: "inherit",
    shell: true,
  });

  apiProcess.on("exit", (code) => {
    process.exit(code ?? 0);
  });

  for (const signal of ["SIGINT", "SIGTERM"]) {
    process.on(signal, () => {
      apiProcess.kill(signal);
    });
  }
}

if (require.main === module) {
  main().catch((error) => {
    console.error("Failed to prepare the E2E PostgreSQL database.");
    console.error(error);
    process.exit(1);
  });
}

module.exports = {
  ensureDatabaseExists,
  parseConnectionString,
};
