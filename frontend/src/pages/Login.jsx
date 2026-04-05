import { useState } from "react";
import { login } from "../services/api";
import { useNavigate, Link } from "react-router-dom";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError("");

    if (!email || !password) {
      setError("please fill in all fields");
      return;
    }

    setLoading(true);
    try {
      const res = await login({ email, password });

      if (res && res.id) {
        localStorage.setItem("userId", res.id);
        navigate("/game");
      } else {
        setError("invalid email or password");
      }
    } catch (err) {
      setError("something went wrong, try again");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div style={{ textAlign: "center", marginBottom: "10px" }}>
        <span style={{ fontSize: "2rem" }}>⌨️</span>
        <h1 className="auth-title">monkeytype</h1>
      </div>
      <form onSubmit={handleLogin} className="auth-input-group">
        <input
          className="auth-input"
          type="email"
          placeholder="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
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