import { useEffect, useState } from "react";
import { getLeaderboard } from "../services/api";

export default function Leaderboard() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getLeaderboard()
      .then((res) => {
        if (Array.isArray(res)) setData(res);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const getRankClass = (index) => {
    if (index === 0) return "gold";
    if (index === 1) return "silver";
    if (index === 2) return "bronze";
    return "";
  };

  return (
    <div className="leaderboard-container">
      <h1 className="leaderboard-title">leaderboard</h1>

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
            </tr>
          </thead>
          <tbody>
            {data.map((item, index) => (
              <tr key={index}>
                <td className={`rank-cell ${getRankClass(index)}`}>
                  {index + 1}
                </td>
                <td className="username-cell">{item.username || item.email || "anonymous"}</td>
                <td className="wpm-cell">{item.wpm}</td>
                <td className="accuracy-cell">{item.accuracy != null ? `${item.accuracy}%` : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}