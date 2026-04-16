const { expect } = require("@playwright/test");

const API_BASE_URL = process.env.PLAYWRIGHT_API_BASE_URL || "http://localhost:5232";
const TEST_PASSWORD = "Keyless!123";

function createCredentials() {
  const suffix = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
  return {
    username: `e2e_${suffix}`,
    email: `e2e_${suffix}@example.com`,
    verifyEmail: `e2e_${suffix}@example.com`,
    password: TEST_PASSWORD,
    verifyPassword: TEST_PASSWORD,
  };
}

function decodeJwt(token) {
  const [, payload] = token.split(".");
  return JSON.parse(Buffer.from(payload, "base64url").toString("utf8"));
}

async function registerUser(request, credentials) {
  const registerResponse = await request.post(`${API_BASE_URL}/api/Authentication/register`, {
    data: credentials,
  });

  expect(registerResponse.ok(), await registerResponse.text()).toBeTruthy();
}

async function enableDeterministicGame(page) {
  await page.addInitScript(() => {
    window.__KEYLESS_E2E__ = {
      words: ["focus", "speed", "rhythm", "flow"],
      timerTickMs: 20,
    };
  });
}

async function loginThroughUi(page, baseURL, credentials) {
  await page.goto(baseURL || "http://localhost:3000");
  await page.getByTestId("login-username").fill(credentials.username);
  await page.getByTestId("login-password").fill(credentials.password);
  await page.getByTestId("login-submit").click();
  await expect(page).toHaveURL(/\/game$/);
}

async function completeTypingRun(page, duration = 15) {
  await page.getByTestId(`time-option-${duration}`).click();
  await page.getByTestId("typing-area").click();

  const resultsView = page.getByTestId("results-view");

  for (let index = 0; index < 20; index++) {
    if (await resultsView.isVisible().catch(() => false)) {
      break;
    }

    const activeWord = await page.evaluate(() => {
      const activeWordElement = document.querySelector('[data-testid="active-word"]');
      return activeWordElement?.textContent?.trim() || null;
    });

    if (!activeWord) {
      await page.waitForTimeout(50);
      continue;
    }

    await page.keyboard.type(activeWord);
    await page.keyboard.press("Space");
    await page.waitForTimeout(25);
  }

  await expect(resultsView).toBeVisible();
  return resultsView;
}

async function getToken(page) {
  const token = await page.evaluate(() => window.localStorage.getItem("token"));
  expect(token).toBeTruthy();
  return token;
}

async function fetchUserStats(request, token) {
  const payload = decodeJwt(token);
  const statsResponse = await request.get(
    `${API_BASE_URL}/api/StatisticsGame/user/${payload.sub}?pageNumber=1&pageSize=20`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  expect(statsResponse.ok()).toBeTruthy();
  return statsResponse.json();
}

async function fetchUserProfile(request, token) {
  const payload = decodeJwt(token);
  const profileResponse = await request.get(`${API_BASE_URL}/api/User/${payload.sub}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  expect(profileResponse.ok()).toBeTruthy();
  return profileResponse.json();
}

module.exports = {
  API_BASE_URL,
  createCredentials,
  decodeJwt,
  registerUser,
  enableDeterministicGame,
  loginThroughUi,
  completeTypingRun,
  getToken,
  fetchUserStats,
  fetchUserProfile,
};