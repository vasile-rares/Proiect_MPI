import { useState } from "react";
import { register } from "../services/api";
import { useNavigate, Link } from "react-router-dom";

export default function Register() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleRegister = async (e) => {
    e.preventDefault();
    setError("");

    if (!email || !password) {
      setError("please fill in all fields");
      return;
    }
    if (password !== confirmPassword) {
      setError("passwords don't match");
      return;
    }
    if (password.length < 6) {
      setError("password must be at least 6 characters");
      return;
    }

    setLoading(true);
    try {
      await register({ email, password });
      navigate("/");
    } catch (err) {
      setError(err.message || "registration failed, try again");
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
      <form onSubmit={handleRegister} className="auth-input-group">
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
          autoComplete="new-password"
        />
        <input
          className="auth-input"
          type="password"
          placeholder="confirm password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          autoComplete="new-password"
        />
        <div className="auth-error">{error}</div>
        <button className="auth-btn" type="submit" disabled={loading}>
          {loading ? "..." : "sign up"}
        </button>
      </form>
      <p className="auth-switch">
        already have an account? <Link to="/">sign in</Link>
      </p>
    </div>
  );
}