const fs = require("fs");
const path = require("path");
const { spawn } = require("child_process");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const apiDirectory = path.join(workspaceRoot, "backend", "Keyless.API");
const databasePath = path.join(
  workspaceRoot,
  "backend",
  "Keyless.Infrastructure",
  "Keyless.E2E.db",
);

for (const filePath of [
  databasePath,
  `${databasePath}-shm`,
  `${databasePath}-wal`,
]) {
  try {
    fs.rmSync(filePath, { force: true });
  } catch {
    // Best-effort cleanup for repeatable local runs.
  }
}

const apiProcess = spawn("dotnet", ["run", "--no-launch-profile"], {
  cwd: apiDirectory,
  env: {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: "Development",
    ASPNETCORE_URLS: process.env.ASPNETCORE_URLS || "http://localhost:5232",
    ConnectionStrings__DefaultConnection:
      "Data Source=../Keyless.Infrastructure/Keyless.E2E.db",
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
