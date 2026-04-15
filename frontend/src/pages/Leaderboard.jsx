import { useEffect, useState } from "react";
import { getLeaderboard, getUserId, getUsername } from "../services/api";

const DURATION_OPTIONS = [15, 30, 60, 120];
const SCOPE_OPTIONS = ["all-time", "weekly", "daily"];

export default function Leaderboard() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [duration, setDuration] = useState(30);
  const [scope, setScope] = useState("all-time");
  const myUserId = getUserId();
  const myUsername = getUsername();

  useEffect(() => {
    setLoading(true);
    getLeaderboard(scope, duration, "time", 20)
      .then((res) => {
        setData(Array.isArray(res) ? res : []);
      })
      .catch(() => setData([]))
      .finally(() => setLoading(false));
  }, [scope, duration]);

  const getRankClass = (index) => {
    if (index === 0) return "gold";
    if (index === 1) return "silver";
    if (index === 2) return "bronze";
    return "";
  };

  return (
    <div className="leaderboard-container">
      <h1 className="leaderboard-title">leaderboard</h1>

      <div className="test-config" style={{ marginBottom: "20px" }}>
        <div className="config-group">
          <span className="config-label">scope</span>
          {SCOPE_OPTIONS.map((s) => (
            <button
              key={s}
              className={`config-btn ${scope === s ? "active" : ""}`}
              onClick={() => setScope(s)}
            >
              {s}
            </button>
          ))}
        </div>
        <div className="config-group">
          <span className="config-label">time</span>
          {DURATION_OPTIONS.map((d) => (
            <button
              key={d}
              className={`config-btn ${duration === d ? "active" : ""}`}
              onClick={() => setDuration(d)}
            >
              {d}s
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="leaderboard-empty">loading...</div>
      ) : data.length === 0 ? (
        <div className="leaderboard-empty">no scores yet — be the first!</div>
      ) : (
        <table className="leaderboard-table">
          <thead>
            <tr>
              <th>#</th>
              <th>user</th>
              <th>wpm</th>
              <th>accuracy</th>
              <th>time</th>
              <th>mode</th>
              <th>date</th>
            </tr>
          </thead>
          <tbody>
            {data.map((item, index) => (
              <tr key={index} className={item.userId === myUserId ? "my-row" : ""}>
                <td className={`rank-cell ${getRankClass(index)}`}>
                  {index + 1}
                </td>
                <td className="username-cell">
                  {item.userId === myUserId ? myUsername || "you" : `player ${index + 1}`}
                </td>
                <td className="wpm-cell">
                  {item.wordsPerMinute != null ? Math.round(item.wordsPerMinute) : "—"}
                </td>
                <td className="accuracy-cell">
                  {item.accuracy != null ? `${Math.round(item.accuracy)}%` : "—"}
                </td>
                <td style={{ color: "var(--sub-color)" }}>{item.durationInSeconds}s</td>
                <td style={{ color: "var(--sub-color)" }}>{item.mode || "—"}</td>
                <td style={{ color: "var(--sub-color)", fontSize: "0.8rem" }}>
                  {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}