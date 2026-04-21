const configuredApiUrl = import.meta.env.VITE_API_URL?.trim();
const API_URL = configuredApiUrl ? configuredApiUrl.replace(/\/$/, "") : "/api";
export const PROFILE_UPDATED_EVENT = "keyless:profile-updated";

function parseJwt(token) {
  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join(""),
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

export function getToken() {
  return localStorage.getItem("token");
}

export function getUserId() {
  const token = getToken();
  if (!token) return null;
  const payload = parseJwt(token);
  return payload?.sub || null;
}

export function getUsername() {
  const token = getToken();
  if (!token) return null;
  const payload = parseJwt(token);
  return payload?.unique_name || null;
}

export function isLoggedIn() {
  const token = getToken();
  if (!token) return false;
  const payload = parseJwt(token);
  if (!payload?.exp) return false;
  return payload.exp * 1000 > Date.now();
}

export function logout() {
  localStorage.removeItem("token");
}

function authHeaders() {
  const token = getToken();
  const headers = { "Content-Type": "application/json" };
  if (token) headers["Authorization"] = `Bearer ${token}`;
  return headers;
}

export const login = async (data) => {
  const res = await fetch(`${API_URL}/Authentication/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  const body = await res.text();

  if (!res.ok) {
    let message = "Login failed";
    try {
      const json = JSON.parse(body);
      message = json.message || json.title || message;
    } catch {
      if (body) message = body;
    }
    throw new Error(message);
  }

  return JSON.parse(body);
};

export const register = async (data) => {
  const res = await fetch(`${API_URL}/Authentication/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  const body = await res.text();

  if (!res.ok) {
    let message = "Registration failed";
    try {
      const json = JSON.parse(body);
      message =
        json.message ||
        json.title ||
        (json.errors ? JSON.stringify(json.errors) : message);
    } catch {
      if (body) message = body;
    }
    throw new Error(message);
  }

  return JSON.parse(body);
};

export const getLeaderboard = async (
  scope = "all-time",
  durationInSeconds,
  mode,
  topN = 10,
) => {
  const params = new URLSearchParams({ scope, topN: topN.toString() });
  if (durationInSeconds)
    params.set("durationInSeconds", durationInSeconds.toString());
  if (mode) params.set("mode", mode);

  const res = await fetch(`${API_URL}/StatisticsGame/leaderboard?${params}`);
  if (!res.ok) throw new Error("Failed to fetch leaderboard");
  return res.json();
};

export const saveScore = async (data) => {
  const res = await fetch(`${API_URL}/StatisticsGame`, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });

  const body = await res.text();
  if (!res.ok) {
    throw new Error(body || "Failed to save score");
  }
  try {
    return JSON.parse(body);
  } catch {
    return body;
  }
};

export const getUserProfile = async (id) => {
  const res = await fetch(`${API_URL}/User/${id}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch profile");
  return res.json();
};

export const updateUserProfile = async (id, data) => {
  const res = await fetch(`${API_URL}/User/${id}/update`, {
    method: "PATCH",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const body = await res.text();
    let message = "Failed to update profile";
    try {
      const json = JSON.parse(body);
      message =
        json.message ||
        json.title ||
        (json.errors ? JSON.stringify(json.errors) : message);
    } catch {
      if (body) message = body;
    }
    throw new Error(message);
  }
  return true;
};

export const deleteUserAccount = async (id) => {
  const res = await fetch(`${API_URL}/User/${id}`, {
    method: "DELETE",
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to delete account");
  return true;
};

export const getUserStats = async (id, pageNumber = 1, pageSize = 20) => {
  const params = new URLSearchParams({
    pageNumber: pageNumber.toString(),
    pageSize: pageSize.toString(),
  });
  const res = await fetch(`${API_URL}/StatisticsGame/user/${id}?${params}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch statistics");
  return res.json();
};

export const getStatById = async (id) => {
  const res = await fetch(`${API_URL}/StatisticsGame/${id}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch statistic");
  return res.json();
};

export const getUserStatsAggregate = async (id) => {
  const res = await fetch(`${API_URL}/StatisticsGame/user/${id}/average`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch stats aggregate");
  return res.json();
};
