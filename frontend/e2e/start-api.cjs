const path = require("path");
const { spawn } = require("child_process");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const apiDirectory = path.join(workspaceRoot, "backend", "Keyless.API");
const connectionString =
  process.env.E2E_POSTGRES_CONNECTION_STRING ||
  process.env.ConnectionStrings__DefaultConnection ||
  "Host=127.0.0.1;Port=55432;Database=keyless_e2e;Username=postgres;Password=postgres";

// In CI the backend is pre-built; set DOTNET_NO_BUILD=1 to skip rebuild and
// avoid hitting the Playwright webServer timeout on the first cold run.
const dotnetRunArgs = ["run", "--no-launch-profile"];
if (process.env.DOTNET_NO_BUILD === "1") {
  dotnetRunArgs.push("--no-build");
}

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
