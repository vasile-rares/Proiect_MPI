const { test, expect } = require("@playwright/test");
const {
  createCredentials,
  registerUser,
  enableDeterministicGame,
  loginThroughUi,
  completeTypingRun,
  getToken,
} = require("./helpers");

test("saved run appears in leaderboard for the logged in user", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await registerUser(request, credentials);
  await enableDeterministicGame(page);
  await loginThroughUi(page, baseURL, credentials);
  await completeTypingRun(page, 15);

  await page.getByTestId("result-leaderboard").click();
  await expect(page).toHaveURL(/\/leaderboard$/);
  await page.getByTestId("leaderboard-duration-15").click();
  await expect(page.getByTestId("leaderboard-table")).toBeVisible();
  await expect(page.getByTestId("leaderboard-my-row")).toBeVisible();
  await expect(page.getByTestId("leaderboard-my-username")).toHaveText(credentials.username);
  await expect(page.getByTestId("leaderboard-my-duration")).toHaveText("15s");

  const myWpm = Number(await page.getByTestId("leaderboard-my-wpm").textContent());
  expect(myWpm).toBeGreaterThan(0);
});

test("logout removes access to protected routes", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await page.goto((baseURL || "http://localhost:3000") + "/history");
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByTestId("login-form")).toBeVisible();

  await registerUser(request, credentials);
  await enableDeterministicGame(page);
  await loginThroughUi(page, baseURL, credentials);

  const tokenBeforeLogout = await getToken(page);
  expect(tokenBeforeLogout).toBeTruthy();

  await page.getByTestId("nav-logout").click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByTestId("login-form")).toBeVisible();

  const tokenAfterLogout = await page.evaluate(() => window.localStorage.getItem("token"));
  expect(tokenAfterLogout).toBeNull();

  await page.goto((baseURL || "http://localhost:3000") + "/profile");
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByTestId("login-form")).toBeVisible();
});