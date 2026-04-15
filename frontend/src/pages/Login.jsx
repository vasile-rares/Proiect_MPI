import { useState } from "react";
import { login } from "../services/api";
import { useNavigate, Link } from "react-router-dom";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError("");

    if (!username || !password) {
      setError("please fill in all fields");
      return;
    }

    setLoading(true);
    try {
      const res = await login({ username, password });

      if (res?.token) {
        localStorage.setItem("token", res.token);
        navigate("/game");
      } else {
        setError("invalid username or password");
      }
    } catch (err) {
      setError(err.message || "something went wrong, try again");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div style={{ textAlign: "center", marginBottom: "10px" }}>
        <span style={{ fontSize: "2.4rem", color: "var(--main-color)" }}>⌘</span>
        <h1 className="auth-title">keyless</h1>
      </div>
      <form onSubmit={handleLogin} className="auth-input-group">
        <input
          className="auth-input"
          type="text"
          placeholder="username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoComplete="username"
        />
        <input
          className="auth-input"
          type="password"
          placeholder="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
        />
        <div className="auth-error">{error}</div>
        <button className="auth-btn" type="submit" disabled={loading}>
          {loading ? "..." : "sign in"}
        </button>
      </form>
      <p className="auth-switch">
        don't have an account? <Link to="/register">sign up</Link>
      </p>
    </div>
  );
}