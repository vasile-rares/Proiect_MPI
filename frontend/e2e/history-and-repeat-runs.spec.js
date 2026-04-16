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

test("saved run appears in history and can be inspected", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await registerUser(request, credentials);
  await enableDeterministicGame(page);
  await loginThroughUi(page, baseURL, credentials);
  await completeTypingRun(page, 15);

  await page.getByTestId("nav-history").click();
  await expect(page).toHaveURL(/\/history$/);
  await expect(page.getByTestId("history-table")).toBeVisible();
  await expect(page.getByTestId("history-row-0")).toBeVisible();
  await expect(page.getByTestId("history-duration-0")).toHaveText("15s");

  await page.getByTestId("history-detail-button-0").click();
  await expect(page.getByTestId("history-detail-modal")).toBeVisible();
  await expect(page.getByTestId("history-detail-duration")).toHaveText("15s");
  await expect(page.getByTestId("history-detail-mode")).toHaveText("time");
  await expect(page.getByTestId("history-detail-wpm")).not.toHaveText("0");
  await page.getByTestId("history-detail-close").click();
  await expect(page.getByTestId("history-detail-modal")).not.toBeVisible();
});

test("same user can complete two consecutive typing runs", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();

  await registerUser(request, credentials);
  await enableDeterministicGame(page);
  await loginThroughUi(page, baseURL, credentials);
  await completeTypingRun(page, 15);
  await page.getByTestId("result-next-test").click();
  await expect(page.getByTestId("typing-area")).toBeVisible();

  await completeTypingRun(page, 15);

  const token = await getToken(page);
  const statsBody = await fetchUserStats(request, token);
  expect(Array.isArray(statsBody.items)).toBeTruthy();
  expect(statsBody.items.length).toBeGreaterThanOrEqual(2);

  await page.getByTestId("nav-history").click();
  await expect(page.getByTestId("history-table")).toBeVisible();
  await expect(page.getByTestId("history-row-0")).toBeVisible();
  await expect(page.getByTestId("history-row-1")).toBeVisible();
});