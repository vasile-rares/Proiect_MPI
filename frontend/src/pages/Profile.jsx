import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  getUserId,
  getUserProfile,
  updateUserProfile,
  deleteUserAccount,
  getUserStatsAggregate,
  logout,
  PROFILE_UPDATED_EVENT,
} from "../services/api";

export default function Profile() {
  const navigate = useNavigate();
  const userId = getUserId();

  const [profile, setProfile] = useState(null);
  const [aggregate, setAggregate] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [saving, setSaving] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Edit form state
  const [editUsername, setEditUsername] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editBiography, setEditBiography] = useState("");

  useEffect(() => {
    if (!userId) {
      navigate("/");
      return;
    }
    setLoading(true);
    Promise.all([
      getUserProfile(userId).catch(() => null),
      getUserStatsAggregate(userId).catch(() => null),
    ]).then(([prof, agg]) => {
      setProfile(prof);
      setAggregate(agg);
      if (prof) {
        setEditUsername(prof.username || "");
        setEditEmail(prof.email || "");
        setEditBiography(prof.biography || "");
      }
      setLoading(false);
    });
  }, [userId, navigate]);

  const handleEdit = () => {
    setEditing(true);
    setError("");
    setSuccess("");
  };

  const handleCancel = () => {
    setEditing(false);
    setError("");
    setSuccess("");
    if (profile) {
      setEditUsername(profile.username || "");
      setEditEmail(profile.email || "");
      setEditBiography(profile.biography || "");
    }
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!editUsername || editUsername.length < 3) {
      setError("Username must be at least 3 characters");
      return;
    }
    if (!editEmail || !editEmail.includes("@")) {
      setError("Please enter a valid email");
      return;
    }

    setSaving(true);
    try {
      await updateUserProfile(userId, {
        username: editUsername,
        email: editEmail,
        testsStarted: profile.testsStarted,
        testsCompleted: profile.testsCompleted,
        biography: editBiography || null,
      });

      const refreshedProfile = await getUserProfile(userId).catch(() => null);
      const nextProfile = refreshedProfile || {
        ...profile,
        username: editUsername,
        email: editEmail,
        biography: editBiography || null,
      };

      setProfile(nextProfile);
      window.dispatchEvent(new CustomEvent(PROFILE_UPDATED_EVENT, {
        detail: {
          username: nextProfile.username,
        },
      }));
      setEditing(false);
      setSuccess("Profile updated successfully");
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    try {
      await deleteUserAccount(userId);
      logout();
      navigate("/");
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) {
    return (
      <div className="profile-container" data-testid="profile-view">
        <div className="leaderboard-empty" data-testid="profile-loading">loading...</div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="profile-container" data-testid="profile-view">
        <div className="leaderboard-empty" data-testid="profile-empty">could not load profile</div>
      </div>
    );
  }

  return (
    <div className="profile-container" data-testid="profile-view">
      <h1 className="profile-title">profile</h1>

      {/* Stats Aggregate Section */}
      {aggregate && (
        <>
          <div className="profile-stats-grid">
          <div className="profile-stat-card">
            <span className="profile-stat-label">games played</span>
            <span className="profile-stat-value">{aggregate.gamesCount}</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">best wpm</span>
            <span className="profile-stat-value accent">{Math.round(aggregate.highestWordsPerMinute)}</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">avg wpm</span>
            <span className="profile-stat-value">{Math.round(aggregate.averageWordsPerMinute)}</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">best raw wpm</span>
            <span className="profile-stat-value accent">{Math.round(aggregate.highestRawWordsPerMinute || 0)}</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">avg raw wpm</span>
            <span className="profile-stat-value">{Math.round(aggregate.averageRawWordsPerMinute || 0)}</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">best accuracy</span>
            <span className="profile-stat-value accent">{Math.round(aggregate.highestAccuracy)}%</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">avg accuracy</span>
            <span className="profile-stat-value">{Math.round(aggregate.averageAccuracy)}%</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">best consistency</span>
            <span className="profile-stat-value accent">{Math.round(aggregate.highestConsistency || 0)}%</span>
          </div>
          <div className="profile-stat-card">
            <span className="profile-stat-label">avg consistency</span>
            <span className="profile-stat-value">{Math.round(aggregate.averageConsistency)}%</span>
          </div>
        </div>
        {aggregate.updatedAt && (
          <div style={{ color: "var(--sub-color)", fontSize: "0.65rem", textAlign: "right", marginTop: "6px" }}>
            last updated: {new Date(aggregate.updatedAt).toLocaleDateString()}
          </div>
        )}
        </>
      )}

      {/* Profile Info / Edit Form */}
      <div className="profile-card">
        {!editing ? (
          <>
            <div className="profile-field">
              <span className="profile-field-label">username</span>
              <span className="profile-field-value" data-testid="profile-username-value">{profile.username}</span>
            </div>
            <div className="profile-field">
              <span className="profile-field-label">email</span>
              <span className="profile-field-value" data-testid="profile-email-value">{profile.email}</span>
            </div>
            <div className="profile-field">
              <span className="profile-field-label">biography</span>
              <span className="profile-field-value" data-testid="profile-biography-value">
                {profile.biography || <span style={{ color: "var(--sub-color)" }}>no biography set</span>}
              </span>
            </div>
            <div className="profile-field">
              <span className="profile-field-label">tests started</span>
              <span className="profile-field-value" data-testid="profile-tests-started">{profile.testsStarted}</span>
            </div>
            <div className="profile-field">
              <span className="profile-field-label">tests completed</span>
              <span className="profile-field-value" data-testid="profile-tests-completed">{profile.testsCompleted}</span>
            </div>
          </>
        ) : (
          <form id="profile-edit-form" onSubmit={handleSave} className="profile-edit-form" data-testid="profile-edit-form">
            <div className="profile-field">
              <span className="profile-field-label">username</span>
              <input
                data-testid="profile-edit-username"
                className="auth-input"
                type="text"
                value={editUsername}
                onChange={(e) => setEditUsername(e.target.value)}
                placeholder="username"
              />
            </div>
            <div className="profile-field">
              <span className="profile-field-label">email</span>
              <input
                data-testid="profile-edit-email"
                className="auth-input"
                type="email"
                value={editEmail}
                onChange={(e) => setEditEmail(e.target.value)}
                placeholder="email"
              />
            </div>
            <div className="profile-field">
              <span className="profile-field-label">biography</span>
              <textarea
                data-testid="profile-edit-biography"
                className="auth-input profile-textarea"
                value={editBiography}
                onChange={(e) => setEditBiography(e.target.value)}
                placeholder="tell us about yourself..."
                rows={3}
              />
            </div>
          </form>
        )}

        {error && <div className="auth-error" data-testid="profile-error">{error}</div>}
        {success && <div className="profile-success" data-testid="profile-success">{success}</div>}

        <div className="profile-actions">
          {!editing ? (
            <div key="view-actions" className="profile-actions-group">
              <button type="button" className="result-btn" onClick={handleEdit} data-testid="profile-edit-button">✎ edit profile</button>
              <button type="button" className="result-btn" onClick={() => navigate("/history")} data-testid="profile-history-button">📊 game history</button>
              <button
                type="button"
                className="result-btn profile-delete-btn"
                onClick={() => setShowDeleteConfirm(true)}
                data-testid="profile-delete-button"
              >
                ✕ delete account
              </button>
            </div>
          ) : (
            <div key="edit-actions" className="profile-actions-group">
              <button
                className="auth-btn"
                type="submit"
                form="profile-edit-form"
                disabled={saving}
                style={{ flex: 1 }}
                data-testid="profile-save-button"
              >
                {saving ? "saving..." : "save changes"}
              </button>
              <button type="button" className="result-btn" onClick={handleCancel} data-testid="profile-cancel-button">cancel</button>
            </div>
          )}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="modal-overlay" onClick={() => setShowDeleteConfirm(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2 className="modal-title">delete account?</h2>
            <p className="modal-text">
              this action cannot be undone. all your data will be permanently deleted.
            </p>
            <div className="modal-actions">
              <button className="auth-btn profile-delete-confirm" onClick={handleDelete}>
                yes, delete my account
              </button>
              <button className="result-btn" onClick={() => setShowDeleteConfirm(false)}>
                cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
