import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getUserId, getUserStats, getStatById } from "../services/api";

export default function History() {
  const navigate = useNavigate();
  const userId = getUserId();

  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [selectedGame, setSelectedGame] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const pageSize = 15;

  useEffect(() => {
    if (!userId) {
      navigate("/");
      return;
    }
    setLoading(true);
    getUserStats(userId, pageNumber, pageSize)
      .then((res) => {
        setData(res.items || []);
        setTotalCount(res.totalCount || 0);
      })
      .catch(() => setData([]))
      .finally(() => setLoading(false));
  }, [userId, pageNumber, navigate]);

  const totalPages = Math.ceil(totalCount / pageSize);

  const viewDetail = async (id) => {
    setDetailLoading(true);
    try {
      const stat = await getStatById(id);
      setSelectedGame(stat);
    } catch {
      setSelectedGame(null);
    } finally {
      setDetailLoading(false);
    }
  };

  const formatDate = (dateStr) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div className="history-container" data-testid="history-view">
      <h1 className="leaderboard-title">game history</h1>

      {/* Game Detail Modal */}
      {selectedGame && (
        <div className="modal-overlay" onClick={() => setSelectedGame(null)} data-testid="history-detail-modal-overlay">
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2 className="modal-title" data-testid="history-detail-title">game details</h2>
            <div className="detail-grid" data-testid="history-detail-modal">
              <div className="detail-item">
                <span className="detail-label">wpm</span>
                <span className="detail-value highlight" data-testid="history-detail-wpm">{Math.round(selectedGame.wordsPerMinute)}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">raw wpm</span>
                <span className="detail-value">{Math.round(selectedGame.rawWordsPerMinute)}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">accuracy</span>
                <span className="detail-value highlight">{Math.round(selectedGame.accuracy)}%</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">consistency</span>
                <span className="detail-value">{Math.round(selectedGame.consistency)}%</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">correct</span>
                <span className="detail-value" style={{ color: "var(--correct-color)" }}>
                  {selectedGame.correctCharacters}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">incorrect</span>
                <span className="detail-value" style={{ color: "var(--error-color)" }}>
                  {selectedGame.incorrectCharacters}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">extra</span>
                <span className="detail-value" style={{ color: "var(--error-extra-color)" }}>
                  {selectedGame.extraCharacters}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">missed</span>
                <span className="detail-value" style={{ color: "var(--sub-color)" }}>
                  {selectedGame.missedCharacters}
                </span>
              </div>
              <div className="detail-item">
                <span className="detail-label">duration</span>
                <span className="detail-value" data-testid="history-detail-duration">{selectedGame.durationInSeconds}s</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">mode</span>
                <span className="detail-value" data-testid="history-detail-mode">{selectedGame.mode}</span>
              </div>
              <div className="detail-item full-width">
                <span className="detail-label">date</span>
                <span className="detail-value">{formatDate(selectedGame.createdAt)}</span>
              </div>
            </div>
            <div className="modal-actions">
              <button className="result-btn" onClick={() => setSelectedGame(null)} data-testid="history-detail-close">close</button>
            </div>
          </div>
        </div>
      )}

      {loading ? (
        <div className="leaderboard-empty" data-testid="history-loading">loading...</div>
      ) : data.length === 0 ? (
        <div className="leaderboard-empty" data-testid="history-empty">
          no games yet —{" "}
          <span
            style={{ color: "var(--main-color)", cursor: "pointer" }}
            onClick={() => navigate("/game")}
          >
            take a test!
          </span>
        </div>
      ) : (
        <>
          <table className="leaderboard-table" data-testid="history-table">
            <thead>
              <tr>
                <th>#</th>
                <th>wpm</th>
                <th>raw</th>
                <th>accuracy</th>
                <th>consistency</th>
                <th>time</th>
                <th>date</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.map((item, index) => (
                <tr key={item.id || index} data-testid={`history-row-${index}`}>
                  <td className="rank-cell" data-testid={`history-rank-${index}`}>{(pageNumber - 1) * pageSize + index + 1}</td>
                  <td className="wpm-cell" data-testid={`history-wpm-${index}`}>{Math.round(item.wordsPerMinute)}</td>
                  <td style={{ color: "var(--text-color)" }}>{Math.round(item.rawWordsPerMinute)}</td>
                  <td className="accuracy-cell">{Math.round(item.accuracy)}%</td>
                  <td className="accuracy-cell">{Math.round(item.consistency)}%</td>
                  <td style={{ color: "var(--sub-color)" }} data-testid={`history-duration-${index}`}>{item.durationInSeconds}s</td>
                  <td style={{ color: "var(--sub-color)", fontSize: "0.8rem" }}>
                    {formatDate(item.createdAt)}
                  </td>
                  <td>
                    <button
                      className="detail-btn"
                      onClick={() => viewDetail(item.id)}
                      disabled={detailLoading}
                      data-testid={`history-detail-button-${index}`}
                    >
                      ⋯
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="pagination">
              <button
                className="config-btn"
                disabled={pageNumber <= 1}
                onClick={() => setPageNumber((p) => p - 1)}
              >
                ← prev
              </button>
              <span className="pagination-info">
                {pageNumber} / {totalPages}
              </span>
              <button
                className="config-btn"
                disabled={pageNumber >= totalPages}
                onClick={() => setPageNumber((p) => p + 1)}
              >
                next →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
