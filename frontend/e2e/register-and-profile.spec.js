const { test, expect } = require("@playwright/test");
const {
  createCredentials,
  enableDeterministicGame,
  getToken,
  fetchUserProfile,
} = require("./helpers");

test("user can register through the UI and enter the game authenticated", async ({ page, baseURL }) => {
  const credentials = createCredentials();

  await enableDeterministicGame(page);
  await page.goto((baseURL || "http://localhost:3000") + "/register");
  await page.getByTestId("register-username").fill(credentials.username);
  await page.getByTestId("register-email").fill(credentials.email);
  await page.getByTestId("register-confirm-email").fill(credentials.verifyEmail);
  await page.getByTestId("register-password").fill(credentials.password);
  await page.getByTestId("register-confirm-password").fill(credentials.verifyPassword);
  await page.getByTestId("register-submit").click();

  await expect(page).toHaveURL(/\/game$/);
  await expect(page.getByTestId("typing-area")).toBeVisible();
  await expect(page.getByTestId("nav-history")).toBeVisible();
  await expect(page.getByTestId("nav-profile")).toBeVisible();

  const token = await page.evaluate(() => window.localStorage.getItem("token"));
  expect(token).toBeTruthy();
});

test("user can edit profile details and changes persist", async ({ page, request, baseURL }) => {
  const credentials = createCredentials();
  const updatedProfile = {
    username: `${credentials.username}_edit`,
    email: `updated_${credentials.email}`,
    biography: "Typing daily with focused E2E coverage.",
  };

  await page.goto((baseURL || "http://localhost:3000") + "/register");
  await page.getByTestId("register-username").fill(credentials.username);
  await page.getByTestId("register-email").fill(credentials.email);
  await page.getByTestId("register-confirm-email").fill(credentials.verifyEmail);
  await page.getByTestId("register-password").fill(credentials.password);
  await page.getByTestId("register-confirm-password").fill(credentials.verifyPassword);
  await page.getByTestId("register-submit").click();
  await expect(page).toHaveURL(/\/game$/);

  await page.getByTestId("nav-profile").click();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByTestId("profile-view")).toBeVisible();
  await page.getByTestId("profile-edit-button").click();
  await expect(page.getByTestId("profile-edit-form")).toBeVisible();

  await page.getByTestId("profile-edit-username").fill(updatedProfile.username);
  await page.getByTestId("profile-edit-email").fill(updatedProfile.email);
  await page.getByTestId("profile-edit-biography").fill(updatedProfile.biography);
  await page.getByTestId("profile-save-button").click();

  await expect(page.getByTestId("profile-success")).toHaveText("Profile updated successfully");
  await expect(page.getByTestId("profile-username-value")).toHaveText(updatedProfile.username);
  await expect(page.getByTestId("profile-email-value")).toHaveText(updatedProfile.email);
  await expect(page.getByTestId("profile-biography-value")).toContainText(updatedProfile.biography);
  await expect(page.getByTestId("nav-profile")).toContainText(updatedProfile.username);

  await page.reload();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByTestId("nav-profile")).toContainText(updatedProfile.username);
  await expect(page.getByTestId("profile-username-value")).toHaveText(updatedProfile.username);

  const token = await getToken(page);
  const persistedProfile = await fetchUserProfile(request, token);
  expect(persistedProfile.username).toBe(updatedProfile.username);
  expect(persistedProfile.email).toBe(updatedProfile.email);
  expect(persistedProfile.biography).toBe(updatedProfile.biography);
});