import { useEffect, useState } from "react";
import { BrowserRouter, Routes, Route, Link, useNavigate, useLocation, Navigate } from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Game from "./pages/Game";
import Leaderboard from "./pages/Leaderboard";
import Profile from "./pages/Profile";
import History from "./pages/History";
import {
  isLoggedIn as checkAuth,
  logout,
  getUsername,
  getUserId,
  getUserProfile,
  PROFILE_UPDATED_EVENT,
} from "./services/api";
import "./App.css";

function ProtectedRoute({ children }) {
  return checkAuth() ? children : <Navigate to="/" replace />;
}

function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const loggedIn = checkAuth();
  const userId = getUserId();
  const [username, setUsername] = useState(() => getUsername());

  useEffect(() => {
    let isCancelled = false;

    if (!loggedIn || !userId) {
      setUsername(null);
      return undefined;
    }

    const tokenUsername = getUsername();
    setUsername(tokenUsername);

    getUserProfile(userId)
      .then((profile) => {
        if (!isCancelled) {
          setUsername(profile?.username || tokenUsername);
        }
      })
      .catch(() => {
        if (!isCancelled) {
          setUsername(tokenUsername);
        }
      });

    const handleProfileUpdated = (event) => {
      if (!isCancelled) {
        setUsername(event.detail?.username || tokenUsername);
      }
    };

    window.addEventListener(PROFILE_UPDATED_EVENT, handleProfileUpdated);

    return () => {
      isCancelled = true;
      window.removeEventListener(PROFILE_UPDATED_EVENT, handleProfileUpdated);
    };
  }, [loggedIn, userId]);

  const handleLogout = () => {
    logout();
    setUsername(null);
    navigate("/");
  };

  if (location.pathname === "/" || location.pathname === "/register") {
    return null;
  }

  return (
    <header className="header" data-testid="app-header">
      <Link to="/game" className="logo" data-testid="nav-logo">
        <span className="logo-icon">⌘</span>
        <span className="logo-text">key<span>less</span></span>
      </Link>
      <nav className="nav-links">
        <Link
          to="/game"
          data-testid="nav-game"
          className={`nav-link ${location.pathname === "/game" ? "active" : ""}`}
        >
          ⟳ test
        </Link>
        <Link
          to="/leaderboard"
          data-testid="nav-leaderboard"
          className={`nav-link ${location.pathname === "/leaderboard" ? "active" : ""}`}
        >
          ♛ leaderboard
        </Link>
        {loggedIn && (
          <Link
            to="/history"
            data-testid="nav-history"
            className={`nav-link ${location.pathname === "/history" ? "active" : ""}`}
          >
            📊 history
          </Link>
        )}
        {loggedIn && (
          <Link
            to="/profile"
            data-testid="nav-profile"
            className={`nav-link ${location.pathname === "/profile" ? "active" : ""}`}
          >
            ⚙ {username || "profile"}
          </Link>
        )}
        {loggedIn && (
          <button className="nav-link" onClick={handleLogout} data-testid="nav-logout">
            ⏻ logout
          </button>
        )}
        {!loggedIn && (
          <Link
            to="/"
            data-testid="nav-login"
            className={`nav-link ${location.pathname === "/" ? "active" : ""}`}
          >
            ⏻ login
          </Link>
        )}
      </nav>
    </header>
  );
}

function App() {
  return (
    <BrowserRouter>
      <div className="app-container">
        <Header />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/game" element={<Game />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
            <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
            <Route path="/history" element={<ProtectedRoute><History /></ProtectedRoute>} />
          </Routes>
        </main>
        <footer className="footer">
          keyless — built with React & .NET
        </footer>
      </div>
    </BrowserRouter>
  );
}

export default App;