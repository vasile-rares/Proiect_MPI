import { BrowserRouter, Routes, Route, Link, useNavigate, useLocation } from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Game from "./pages/Game";
import Leaderboard from "./pages/Leaderboard";
import "./App.css";

function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const isLoggedIn = localStorage.getItem("userId");

  const handleLogout = () => {
    localStorage.removeItem("userId");
    navigate("/");
  };

  if (location.pathname === "/" || location.pathname === "/register") {
    return null;
  }

  return (
    <header className="header">
      <Link to="/game" className="logo">
        <span className="logo-icon">⌨️</span>
        <span className="logo-text">monkey<span>type</span></span>
      </Link>
      <nav className="nav-links">
        <Link
          to="/game"
          className={`nav-link ${location.pathname === "/game" ? "active" : ""}`}
        >
          ⟳ test
        </Link>
        <Link
          to="/leaderboard"
          className={`nav-link ${location.pathname === "/leaderboard" ? "active" : ""}`}
        >
          ♛ leaderboard
        </Link>
        {isLoggedIn && (
          <button className="nav-link" onClick={handleLogout}>
            ⏻ logout
          </button>
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
          </Routes>
        </main>
        <footer className="footer">
          monkeytype clone — built with React & .NET
        </footer>
      </div>
    </BrowserRouter>
  );
}

export default App;