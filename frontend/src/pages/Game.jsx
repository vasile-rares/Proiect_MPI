import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { saveScore } from "../services/api";

const wordBank = [
  "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
  "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
  "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
  "or", "an", "will", "my", "one", "all", "would", "there", "their", "what",
  "so", "up", "out", "if", "about", "who", "get", "which", "go", "me",
  "when", "make", "can", "like", "time", "no", "just", "him", "know", "take",
  "people", "into", "year", "your", "good", "some", "could", "them", "see",
  "other", "than", "then", "now", "look", "only", "come", "its", "over",
  "think", "also", "back", "after", "use", "two", "how", "our", "work",
  "first", "well", "way", "even", "new", "want", "because", "any", "these",
  "give", "day", "most", "us", "great", "between", "need", "large", "often",
  "hand", "high", "place", "hold", "free", "real", "life", "each", "world",
  "next", "still", "found", "city", "live", "where", "long", "been", "should",
  "part", "number", "never", "start", "every", "much", "right", "under",
  "home", "last", "school", "old", "while", "turn", "few", "group", "always",
  "same", "another", "help", "point", "close", "own", "state", "small",
  "open", "must", "run", "play", "move", "try", "change", "line", "before",
  "light", "thought", "head", "many", "begin", "story", "being", "left",
  "read", "something", "follow", "end", "ask", "important", "keep", "let",
  "without", "children", "system", "plan", "night", "might", "second",
  "country", "power", "kind", "learn", "house", "word", "name", "young",
  "together", "social", "problem", "write", "seem", "call", "case", "enough"
];

function generateWords(count = 50) {
  const words = [];
  for (let i = 0; i < count; i++) {
    words.push(wordBank[Math.floor(Math.random() * wordBank.length)]);
  }
  return words;
}

const TIME_OPTIONS = [15, 30, 60, 120];

export default function Game() {
  const navigate = useNavigate();
  const [words, setWords] = useState(() => generateWords());
  const [timeOption, setTimeOption] = useState(30);
  const [timeLeft, setTimeLeft] = useState(30);
  const [started, setStarted] = useState(false);
  const [finished, setFinished] = useState(false);
  const [currentWordIndex, setCurrentWordIndex] = useState(0);
  const [currentCharIndex, setCurrentCharIndex] = useState(0);
  const [charStates, setCharStates] = useState({});
  const [extraChars, setExtraChars] = useState({});
  const [isFocused, setIsFocused] = useState(false);
  const [stats, setStats] = useState(null);
  const typingRef = useRef(null);
  const timerRef = useRef(null);
  const wordsRef = useRef(null);
  const startTimeRef = useRef(null);
  const correctCharsRef = useRef(0);
  const totalCharsRef = useRef(0);
  const correctWordsRef = useRef(0);

  const resetTest = useCallback(() => {
    clearInterval(timerRef.current);
    const newWords = generateWords();
    setWords(newWords);
    setTimeLeft(timeOption);
    setStarted(false);
    setFinished(false);
    setCurrentWordIndex(0);
    setCurrentCharIndex(0);
    setCharStates({});
    setExtraChars({});
    setStats(null);
    correctCharsRef.current = 0;
    totalCharsRef.current = 0;
    correctWordsRef.current = 0;
    startTimeRef.current = null;
    setTimeout(() => typingRef.current?.focus(), 50);
  }, [timeOption]);

  const changeTimeOption = (t) => {
    setTimeOption(t);
    setTimeLeft(t);
    clearInterval(timerRef.current);
    const newWords = generateWords();
    setWords(newWords);
    setStarted(false);
    setFinished(false);
    setCurrentWordIndex(0);
    setCurrentCharIndex(0);
    setCharStates({});
    setExtraChars({});
    setStats(null);
    correctCharsRef.current = 0;
    totalCharsRef.current = 0;
    correctWordsRef.current = 0;
    startTimeRef.current = null;
    setTimeout(() => typingRef.current?.focus(), 50);
  };

  const finishTest = useCallback(async () => {
    clearInterval(timerRef.current);
    setFinished(true);

    const elapsed = (Date.now() - startTimeRef.current) / 1000 / 60;
    const wpm = Math.round((correctCharsRef.current / 5) / elapsed);
    const accuracy = totalCharsRef.current > 0
      ? Math.round((correctCharsRef.current / totalCharsRef.current) * 100)
      : 0;

    setStats({ wpm, accuracy, correct: correctCharsRef.current, total: totalCharsRef.current, words: correctWordsRef.current });

    const userId = localStorage.getItem("userId");
    if (userId) {
      try {
        await saveScore({ userId, wpm, accuracy });
      } catch (e) {
        console.error("Failed to save score:", e);
      }
    }
  }, []);

  useEffect(() => {
    if (started && !finished) {
      timerRef.current = setInterval(() => {
        setTimeLeft((prev) => {
          if (prev <= 1) {
            finishTest();
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    }
    return () => clearInterval(timerRef.current);
  }, [started, finished, finishTest]);

  const handleKeyDown = useCallback((e) => {
    if (finished) return;

    if (e.key === "Tab") {
      e.preventDefault();
      resetTest();
      return;
    }

    if (e.key === "Escape") {
      e.preventDefault();
      resetTest();
      return;
    }

    if (e.ctrlKey || e.altKey || e.metaKey) return;
    if (e.key === "Shift" || e.key === "CapsLock") return;

    if (!started && e.key.length === 1) {
      setStarted(true);
      startTimeRef.current = Date.now();
    }

    const currentWord = words[currentWordIndex];
    const extras = extraChars[currentWordIndex] || "";

    if (e.key === "Backspace") {
      e.preventDefault();
      if (extras.length > 0) {
        setExtraChars((prev) => ({
          ...prev,
          [currentWordIndex]: extras.slice(0, -1),
        }));
        totalCharsRef.current = Math.max(0, totalCharsRef.current - 1);
      } else if (currentCharIndex > 0) {
        const newIndex = currentCharIndex - 1;
        const key = `${currentWordIndex}-${newIndex}`;
        if (charStates[key] === "correct") {
          correctCharsRef.current = Math.max(0, correctCharsRef.current - 1);
        }
        totalCharsRef.current = Math.max(0, totalCharsRef.current - 1);
        setCharStates((prev) => {
          const next = { ...prev };
          delete next[key];
          return next;
        });
        setCurrentCharIndex(newIndex);
      }
      return;
    }

    if (e.key === " ") {
      e.preventDefault();
      if (currentCharIndex === 0) return;

      const wordCorrect = currentWord.split("").every((ch, i) => {
        return charStates[`${currentWordIndex}-${i}`] === "correct";
      }) && extras.length === 0;

      if (wordCorrect) correctWordsRef.current++;
      totalCharsRef.current++;

      setCurrentWordIndex((prev) => prev + 1);
      setCurrentCharIndex(0);

      if (currentWordIndex >= words.length - 10) {
        setWords((prev) => [...prev, ...generateWords(30)]);
      }
      return;
    }

    if (e.key.length === 1) {
      e.preventDefault();
      if (currentCharIndex < currentWord.length) {
        const isCorrect = e.key === currentWord[currentCharIndex];
        const key = `${currentWordIndex}-${currentCharIndex}`;
        setCharStates((prev) => ({
          ...prev,
          [key]: isCorrect ? "correct" : "incorrect",
        }));
        if (isCorrect) correctCharsRef.current++;
        totalCharsRef.current++;
        setCurrentCharIndex((prev) => prev + 1);
      } else {
        setExtraChars((prev) => ({
          ...prev,
          [currentWordIndex]: (prev[currentWordIndex] || "") + e.key,
        }));
        totalCharsRef.current++;
      }
    }
  }, [finished, started, words, currentWordIndex, currentCharIndex, charStates, extraChars, resetTest]);

  useEffect(() => {
    if (wordsRef.current && !finished) {
      const activeWord = wordsRef.current.querySelector(".word.active");
      if (activeWord) {
        const container = wordsRef.current;
        const wordTop = activeWord.offsetTop;
        const lineHeight = activeWord.offsetHeight;
        if (wordTop > lineHeight * 1.5) {
          container.style.transform = `translateY(-${wordTop - lineHeight * 0.5}px)`;
        }
      }
    }
  }, [currentWordIndex, finished]);

  if (finished && stats) {
    return (
      <div className="results-container">
        <div className="results-stats">
          <div className="stat-block">
            <span className="stat-label">wpm</span>
            <span className="stat-value">{stats.wpm}</span>
          </div>
          <div className="stat-block">
            <span className="stat-label">accuracy</span>
            <span className="stat-value">{stats.accuracy}%</span>
          </div>
          <div className="stat-block">
            <span className="stat-label">correct words</span>
            <span className="stat-value secondary">{stats.words}</span>
          </div>
          <div className="stat-block">
            <span className="stat-label">characters</span>
            <span className="stat-value secondary">{stats.correct}/{stats.total}</span>
          </div>
        </div>
        <div className="result-actions">
          <button className="result-btn" onClick={resetTest}>⟳ next test</button>
          <button className="result-btn" onClick={() => navigate("/leaderboard")}>♛ leaderboard</button>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="test-config">
        <div className="config-group">
          <span className="config-label">time</span>
          {TIME_OPTIONS.map((t) => (
            <button
              key={t}
              className={`config-btn ${timeOption === t ? "active" : ""}`}
              onClick={() => changeTimeOption(t)}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      <div className="timer-display">
        {started ? timeLeft : ""}
      </div>

      <div
        className="typing-area"
        tabIndex={0}
        ref={typingRef}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        onKeyDown={handleKeyDown}
      >
        {!isFocused && !finished && (
          <div className="typing-focus-warning">Click here or press any key to focus</div>
        )}
        <div
          className={`words-display ${!isFocused ? "words-blurred" : ""}`}
          ref={wordsRef}
          style={{ transition: "transform 0.15s ease" }}
        >
          {words.map((word, wi) => {
            const extras = extraChars[wi] || "";
            return (
              <span key={wi} className={`word ${wi === currentWordIndex ? "active" : ""}`}>
                {word.split("").map((ch, ci) => {
                  const state = charStates[`${wi}-${ci}`];
                  let className = "letter";
                  if (state) className += ` ${state}`;
                  return (
                    <span key={ci} className={className}>
                      {ch}
                    </span>
                  );
                })}
                {extras.split("").map((ch, ei) => (
                  <span key={`extra-${ei}`} className="letter extra">
                    {ch}
                  </span>
                ))}
              </span>
            );
          })}
        </div>
      </div>

      <div style={{ color: "var(--sub-color)", fontSize: "0.7rem", marginTop: "30px", textAlign: "center" }}>
        <span style={{ opacity: 0.6 }}>tab</span> — restart test &nbsp;|&nbsp; <span style={{ opacity: 0.6 }}>esc</span> — restart test
      </div>
    </>
  );
}