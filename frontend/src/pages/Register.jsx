import { useState } from "react";
import { register } from "../services/api";
import { useNavigate, Link } from "react-router-dom";

export default function Register() {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [confirmEmail, setConfirmEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleRegister = async (e) => {
    e.preventDefault();
    setError("");

    // 🔍 VALIDARI
    if (!username || !email || !confirmEmail || !password || !confirmPassword) {
      setError("please fill in all fields");
      return;
    }

    if (username.length < 3) {
      setError("username must be at least 3 characters");
      return;
    }

    if (!email.includes("@")) {
      setError("invalid email");
      return;
    }

    if (email !== confirmEmail) {
      setError("emails don't match");
      return;
    }

    if (password !== confirmPassword) {
      setError("passwords don't match");
      return;
    }

    if (password.length < 8) {
      setError("password must be at least 8 characters");
      return;
    }

    if (!/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password) || !/[^A-Za-z0-9]/.test(password)) {
      setError("password needs uppercase, lowercase, digit, and special character");
      return;
    }

    // 🚀 REQUEST
    setLoading(true);
    try {
      const res = await register({
        username,
        email,
        verifyEmail: confirmEmail,
        password,
        verifyPassword: confirmPassword,
      });

      // 🔐 salvam token
      if (res?.token) {
        localStorage.setItem("token", res.token);
      }

      navigate("/game");
    } catch (err) {
      console.error(err);
      setError(err.message || "registration failed, try again");
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

      <form onSubmit={handleRegister} className="auth-input-group">
        <input
          className="auth-input"
          type="text"
          placeholder="username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />

        <input
          className="auth-input"
          type="email"
          placeholder="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />

        <input
          className="auth-input"
          type="email"
          placeholder="confirm email"
          value={confirmEmail}
          onChange={(e) => setConfirmEmail(e.target.value)}
        />

        <input
          className="auth-input"
          type="password"
          placeholder="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />

        <input
          className="auth-input"
          type="password"
          placeholder="confirm password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
        />

        {error && <div className="auth-error">{error}</div>}

        <button className="auth-btn" type="submit" disabled={loading}>
          {loading ? "creating account..." : "sign up"}
        </button>
      </form>

      <p className="auth-switch">
        already have an account? <Link to="/">sign in</Link>
      </p>
    </div>
  );
}