const API_URL = "http://localhost:5232/api";

export const login = async (data) => {
  const res = await fetch(`${API_URL}/Authentication/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  return res.json();
};

export const register = async (data) => {
  const res = await fetch(`${API_URL}/Authentication/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  const body = await res.text();

  if (!res.ok) {
    let message = "registration failed";
    try {
      const json = JSON.parse(body);
      message = json.message || json.title || json.errors
        ? JSON.stringify(json.errors || json.message || json.title)
        : message;
    } catch {
      if (body) message = body;
    }
    throw new Error(message);
  }

  try {
    return JSON.parse(body);
  } catch {
    return body;
  }
};

export const getLeaderboard = async () => {
  const res = await fetch(`${API_URL}/StatisticsGame/leaderboard`);
  return res.json();
};

export const saveScore = async (data) => {
  const res = await fetch(`${API_URL}/StatisticsGame`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  return res.json();
};