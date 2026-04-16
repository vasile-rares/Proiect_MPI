const { test, expect } = require("@playwright/test");
const {
  createCredentials,
  registerUser,
  enableDeterministicGame,
  loginThroughUi,
  completeTypingRun,
  getToken,
  fetchUserStats,
} = require("./helpers");

test("login and complete the typing test happy path", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await registerUser(request, credentials);
  await enableDeterministicGame(page);
  await loginThroughUi(page, baseURL, credentials);

  const resultsView = await completeTypingRun(page, 15);

  await expect(resultsView).toBeVisible();
  await expect(page.getByTestId("result-next-test")).toBeVisible();
  await expect(page.getByTestId("result-leaderboard")).toBeVisible();

  const wpm = Number(await page.getByTestId("result-wpm").textContent());
  expect(wpm).toBeGreaterThan(0);
  await expect(page.getByTestId("result-accuracy")).toContainText("%");
  await expect(page.getByTestId("result-consistency")).toContainText("%");

  const token = await getToken(page);
  const statsBody = await fetchUserStats(request, token);
  expect(Array.isArray(statsBody.items)).toBeTruthy();
  expect(statsBody.items.length).toBeGreaterThan(0);
});

test("completed word counts even if the timer expires before space is pressed", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await registerUser(request, credentials);
  await enableDeterministicGame(page, {
    words: ["focus"],
    timerTickMs: 80,
  });
  await loginThroughUi(page, baseURL, credentials);

  await page.getByTestId("time-option-15").click();
  await page.getByTestId("typing-area").click();
  await page.keyboard.type("focus");

  await expect(page.getByTestId("results-view")).toBeVisible();
  await expect(page.getByTestId("result-correct-words")).toHaveText("1");
});