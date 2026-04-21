import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import {
  saveScore,
  getUserId,
  getUserProfile,
  updateUserProfile,
} from "../services/api";

const wordBank = [
  "the",
  "be",
  "to",
  "of",
  "and",
  "a",
  "in",
  "that",
  "have",
  "i",
  "it",
  "for",
  "not",
  "on",
  "with",
  "he",
  "as",
  "you",
  "do",
  "at",
  "this",
  "but",
  "his",
  "by",
  "from",
  "they",
  "we",
  "say",
  "her",
  "she",
  "or",
  "an",
  "will",
  "my",
  "one",
  "all",
  "would",
  "there",
  "their",
  "what",
  "so",
  "up",
  "out",
  "if",
  "about",
  "who",
  "get",
  "which",
  "go",
  "me",
  "when",
  "make",
  "can",
  "like",
  "time",
  "no",
  "just",
  "him",
  "know",
  "take",
  "people",
  "into",
  "year",
  "your",
  "good",
  "some",
  "could",
  "them",
  "see",
  "other",
  "than",
  "then",
  "now",
  "look",
  "only",
  "come",
  "its",
  "over",
  "think",
  "also",
  "back",
  "after",
  "use",
  "two",
  "how",
  "our",
  "work",
  "first",
  "well",
  "way",
  "even",
  "new",
  "want",
  "because",
  "any",
  "these",
  "give",
  "day",
  "most",
  "us",
  "great",
  "between",
  "need",
  "large",
  "often",
  "hand",
  "high",
  "place",
  "hold",
  "free",
  "real",
  "life",
  "each",
  "world",
  "next",
  "still",
  "found",
  "city",
  "live",
  "where",
  "long",
  "been",
  "should",
  "part",
  "number",
  "never",
  "start",
  "every",
  "much",
  "right",
  "under",
  "home",
  "last",
  "school",
  "old",
  "while",
  "turn",
  "few",
  "group",
  "always",
  "same",
  "another",
  "help",
  "point",
  "close",
  "own",
  "state",
  "small",
  "open",
  "must",
  "run",
  "play",
  "move",
  "try",
  "change",
  "line",
  "before",
  "light",
  "thought",
  "head",
  "many",
  "begin",
  "story",
  "being",
  "left",
  "read",
  "something",
  "follow",
  "end",
  "ask",
  "important",
  "keep",
  "let",
  "without",
  "children",
  "system",
  "plan",
  "night",
  "might",
  "second",
  "country",
  "power",
  "kind",
  "learn",
  "house",
  "word",
  "name",
  "young",
  "together",
  "social",
  "problem",
  "write",
  "seem",
  "call",
  "case",
  "enough",
];

const E2E_CONFIG_KEY = "__KEYLESS_E2E__";

function getE2EConfig() {
  if (typeof window === "undefined") {
    return null;
  }

  return window[E2E_CONFIG_KEY] ?? null;
}

function getTimerTickMs() {
  const configuredTick = Number(getE2EConfig()?.timerTickMs);
  return Number.isFinite(configuredTick) && configuredTick > 0
    ? configuredTick
    : 1000;
}

function generateWords(count = 50) {
  const configuredWords = getE2EConfig()?.words;
  if (Array.isArray(configuredWords) && configuredWords.length > 0) {
    return Array.from(
      { length: count },
      (_, index) => configuredWords[index % configuredWords.length],
    );
  }

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
  const [, setCurrentCharIndex] = useState(0);
  const [charStates, setCharStates] = useState({});
  const [extraChars, setExtraChars] = useState({});
  const [isFocused, setIsFocused] = useState(false);
  const [stats, setStats] = useState(null);
  const typingRef = useRef(null);
  const timerRef = useRef(null);
  const wordsRef = useRef(null);
  const startTimeRef = useRef(null);
  const correctCharsRef = useRef(0);
  const incorrectCharsRef = useRef(0);
  const extraCharsRef = useRef(0);
  const missedCharsRef = useRef(0);
  const correctWordsRef = useRef(0);
  const charsTypedPerWordRef = useRef({});
  const currentWordIndexRef = useRef(0);
  const currentCharIndexRef = useRef(0);
  const wordsListRef = useRef(words);
  const charStatesRef = useRef({});
  const extraCharsMapRef = useRef({});
  const startedRef = useRef(false);
  const finishedRef = useRef(false);
  const scrollLineRef = useRef(0);
  const timerTickMsRef = useRef(getTimerTickMs());

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
    incorrectCharsRef.current = 0;
    extraCharsRef.current = 0;
    missedCharsRef.current = 0;
    correctWordsRef.current = 0;
    charsTypedPerWordRef.current = {};
    currentWordIndexRef.current = 0;
    currentCharIndexRef.current = 0;
    wordsListRef.current = newWords;
    charStatesRef.current = {};
    extraCharsMapRef.current = {};
    startedRef.current = false;
    finishedRef.current = false;
    scrollLineRef.current = 0;
    startTimeRef.current = null;
    if (wordsRef.current) wordsRef.current.style.transform = "translateY(0px)";
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
    incorrectCharsRef.current = 0;
    extraCharsRef.current = 0;
    missedCharsRef.current = 0;
    correctWordsRef.current = 0;
    charsTypedPerWordRef.current = {};
    currentWordIndexRef.current = 0;
    currentCharIndexRef.current = 0;
    wordsListRef.current = newWords;
    charStatesRef.current = {};
    extraCharsMapRef.current = {};
    startedRef.current = false;
    finishedRef.current = false;
    scrollLineRef.current = 0;
    startTimeRef.current = null;
    if (wordsRef.current) wordsRef.current.style.transform = "translateY(0px)";
    setTimeout(() => typingRef.current?.focus(), 50);
  };

  const finishTest = useCallback(async () => {
    if (finishedRef.current) {
      return;
    }

    clearInterval(timerRef.current);
    finishedRef.current = true;
    setFinished(true);

    const currentWi = currentWordIndexRef.current;
    const currentCi = currentCharIndexRef.current;
    const wordsList = wordsListRef.current;
    const currentWord = wordsList[currentWi];
    const currentWordExtras = extraCharsMapRef.current[currentWi] || "";

    const currentWordCompletedCorrectly = Boolean(
      currentWord &&
      currentCi === currentWord.length &&
      currentWordExtras.length === 0 &&
      currentWord
        .split("")
        .every(
          (ch, i) => charStatesRef.current[`${currentWi}-${i}`] === "correct",
        ),
    );

    if (currentWordCompletedCorrectly) {
      correctWordsRef.current++;
    }

    if (currentWord && currentCi < currentWord.length) {
      missedCharsRef.current += currentWord.length - currentCi;
    }

    const totalTyped =
      correctCharsRef.current +
      incorrectCharsRef.current +
      extraCharsRef.current;
    const totalMeasured = totalTyped + missedCharsRef.current;
    const elapsed = (Date.now() - startTimeRef.current) / 1000 / 60;
    const wpm =
      elapsed > 0 ? Math.round(correctCharsRef.current / 5 / elapsed) : 0;
    const rawWpm = elapsed > 0 ? Math.round(totalTyped / 5 / elapsed) : 0;
    const accuracy =
      totalMeasured > 0
        ? Math.round((correctCharsRef.current / totalMeasured) * 100)
        : 0;
    const consistency = rawWpm > 0 ? Math.round((wpm / rawWpm) * 100) : 0;

    const computedStats = {
      wpm,
      rawWpm,
      accuracy,
      consistency,
      correct: correctCharsRef.current,
      incorrect: incorrectCharsRef.current,
      extra: extraCharsRef.current,
      missed: missedCharsRef.current,
      words: correctWordsRef.current,
    };

    const userId = getUserId();
    if (userId) {
      try {
        await saveScore({
          userId,
          correctCharacters: correctCharsRef.current,
          incorrectCharacters: incorrectCharsRef.current,
          extraCharacters: extraCharsRef.current,
          missedCharacters: missedCharsRef.current,
          durationInSeconds: timeOption,
          mode: "time",
        });
        getUserProfile(userId)
          .then((prof) => {
            updateUserProfile(userId, {
              username: prof.username,
              email: prof.email,
              testsStarted: prof.testsStarted || 0,
              testsCompleted: (prof.testsCompleted || 0) + 1,
              biography: prof.biography,
            }).catch(() => {});
          })
          .catch(() => {});
      } catch (e) {
        console.error("Failed to save score:", e);
      }
    }

    setStats(computedStats);
  }, [timeOption]);

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
      }, timerTickMsRef.current);
    }
    return () => clearInterval(timerRef.current);
  }, [started, finished, finishTest]);

  const handleKeyDown = useCallback(
    (e) => {
      if (finishedRef.current) return;

      if (e.key === "Tab") {
        e.preventDefault();
        resetTest();
        return;
      }

      if (e.ctrlKey || e.altKey || e.metaKey) return;
      if (e.key === "Shift" || e.key === "CapsLock") return;

      if (!startedRef.current && e.key.length === 1) {
        startedRef.current = true;
        setStarted(true);
        startTimeRef.current = Date.now();
        const uid = getUserId();
        if (uid) {
          getUserProfile(uid)
            .then((prof) => {
              updateUserProfile(uid, {
                username: prof.username,
                email: prof.email,
                testsStarted: (prof.testsStarted || 0) + 1,
                testsCompleted: prof.testsCompleted || 0,
                biography: prof.biography,
              }).catch(() => {});
            })
            .catch(() => {});
        }
      }

      const wi = currentWordIndexRef.current;
      const ci = currentCharIndexRef.current;
      const wordsList = wordsListRef.current;
      const currentWord = wordsList[wi];
      const extras = extraCharsMapRef.current[wi] || "";

      if (e.key === "Backspace") {
        e.preventDefault();
        if (extras.length > 0) {
          const newExtras = extras.slice(0, -1);
          extraCharsMapRef.current = {
            ...extraCharsMapRef.current,
            [wi]: newExtras,
          };
          setExtraChars({ ...extraCharsMapRef.current });
          extraCharsRef.current = Math.max(0, extraCharsRef.current - 1);
        } else if (ci > 0) {
          const newIndex = ci - 1;
          const key = `${wi}-${newIndex}`;
          if (charStatesRef.current[key] === "correct") {
            correctCharsRef.current = Math.max(0, correctCharsRef.current - 1);
          } else if (charStatesRef.current[key] === "incorrect") {
            incorrectCharsRef.current = Math.max(
              0,
              incorrectCharsRef.current - 1,
            );
          }
          const nextStates = { ...charStatesRef.current };
          delete nextStates[key];
          charStatesRef.current = nextStates;
          setCharStates(nextStates);
          currentCharIndexRef.current = newIndex;
          setCurrentCharIndex(newIndex);
          charsTypedPerWordRef.current[wi] = newIndex;
        }
        return;
      }

      if (e.key === " ") {
        e.preventDefault();
        if (ci === 0) return;

        const missed = Math.max(0, currentWord.length - ci);
        missedCharsRef.current += missed;

        const wordCorrect =
          currentWord.split("").every((ch, i) => {
            return charStatesRef.current[`${wi}-${i}`] === "correct";
          }) &&
          extras.length === 0 &&
          missed === 0;

        if (wordCorrect) correctWordsRef.current++;

        const nextWi = wi + 1;
        currentWordIndexRef.current = nextWi;
        currentCharIndexRef.current = 0;
        setCurrentWordIndex(nextWi);
        setCurrentCharIndex(0);

        if (nextWi >= wordsList.length - 10) {
          const newWords = [...wordsList, ...generateWords(30)];
          wordsListRef.current = newWords;
          setWords(newWords);
        }
        return;
      }

      if (e.key.length === 1) {
        e.preventDefault();
        if (ci < currentWord.length) {
          const isCorrect = e.key === currentWord[ci];
          const key = `${wi}-${ci}`;
          charStatesRef.current = {
            ...charStatesRef.current,
            [key]: isCorrect ? "correct" : "incorrect",
          };
          setCharStates({ ...charStatesRef.current });
          if (isCorrect) {
            correctCharsRef.current++;
          } else {
            incorrectCharsRef.current++;
          }
          const nextCi = ci + 1;
          currentCharIndexRef.current = nextCi;
          setCurrentCharIndex(nextCi);
          charsTypedPerWordRef.current[wi] = nextCi;
        } else {
          const newExtras = extras + e.key;
          extraCharsMapRef.current = {
            ...extraCharsMapRef.current,
            [wi]: newExtras,
          };
          setExtraChars({ ...extraCharsMapRef.current });
          extraCharsRef.current++;
        }
      }
    },
    [resetTest],
  );

  // Smooth scroll: keep active word on the first visible line
  useEffect(() => {
    if (!wordsRef.current || finished) return;
    const container = wordsRef.current;
    const activeEl = container.querySelector(".word.active");
    if (!activeEl) return;
    const firstWordEl = container.querySelector(".word");
    if (!firstWordEl) return;
    const lineHeight = parseFloat(getComputedStyle(container).lineHeight) || 48;
    const lineIndex = Math.round(
      (activeEl.offsetTop - firstWordEl.offsetTop) / lineHeight,
    );
    if (lineIndex > scrollLineRef.current) {
      scrollLineRef.current = lineIndex;
      container.style.transform = `translateY(-${lineIndex * lineHeight}px)`;
    }
  }, [currentWordIndex, finished]);

  useEffect(() => {
    const handleGlobalKeyDown = () => {
      if (finished) return;
      const tag = document.activeElement?.tagName;
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") return;
      if (typingRef.current && document.activeElement !== typingRef.current) {
        typingRef.current.focus();
      }
    };
    document.addEventListener("keydown", handleGlobalKeyDown);
    return () => document.removeEventListener("keydown", handleGlobalKeyDown);
  }, [finished]);

  if (finished && stats) {
    return (
      <div className="results-container" data-testid="results-view">
        <div className="results-grid">
          <div className="results-card primary">
            <span className="results-card-label">wpm</span>
            <span
              className="results-card-value accent"
              data-testid="result-wpm"
            >
              {stats.wpm}
            </span>
          </div>
          <div className="results-card primary">
            <span className="results-card-label">accuracy</span>
            <span
              className="results-card-value accent"
              data-testid="result-accuracy"
            >
              {stats.accuracy}%
            </span>
          </div>
          <div className="results-card primary">
            <span className="results-card-label">consistency</span>
            <span
              className="results-card-value accent"
              data-testid="result-consistency"
            >
              {stats.consistency}%
            </span>
          </div>
          <div className="results-card secondary">
            <span className="results-card-label">raw wpm</span>
            <span className="results-card-value">{stats.rawWpm}</span>
          </div>
          <div className="results-card secondary">
            <span className="results-card-label">correct words</span>
            <span
              className="results-card-value"
              data-testid="result-correct-words"
            >
              {stats.words}
            </span>
          </div>
          <div className="results-card secondary">
            <span className="results-card-label">time</span>
            <span className="results-card-value">{timeOption}s</span>
          </div>
          <div className="results-card wide">
            <span className="results-card-label">characters</span>
            <div className="results-chars">
              <span className="results-char correct">
                {stats.correct}
                <small>correct</small>
              </span>
              <span className="results-char-sep">/</span>
              <span className="results-char incorrect">
                {stats.incorrect}
                <small>incorrect</small>
              </span>
              <span className="results-char-sep">/</span>
              <span className="results-char extra">
                {stats.extra}
                <small>extra</small>
              </span>
              <span className="results-char-sep">/</span>
              <span className="results-char missed">
                {stats.missed}
                <small>missed</small>
              </span>
            </div>
          </div>
        </div>
        <div className="result-actions">
          <button
            className="result-btn"
            onClick={resetTest}
            data-testid="result-next-test"
          >
            ⟳ next test
          </button>
          <button
            className="result-btn"
            onClick={() => navigate("/leaderboard")}
            data-testid="result-leaderboard"
          >
            ♛ leaderboard
          </button>
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
              data-testid={`time-option-${t}`}
              className={`config-btn ${timeOption === t ? "active" : ""}`}
              onClick={() => changeTimeOption(t)}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      <div className="timer-display" data-testid="timer-display">
        {started ? timeLeft : ""}
      </div>

      <div
        className="typing-area"
        data-testid="typing-area"
        tabIndex={0}
        ref={typingRef}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        onKeyDown={handleKeyDown}
      >
        {!isFocused && !finished && (
          <div className="typing-focus-warning">
            click here or press any key to start
          </div>
        )}
        <div className="words-wrapper">
          <div
            className={`words-display ${!isFocused ? "words-blurred" : ""}`}
            data-testid="words-display"
            ref={wordsRef}
            style={{ transition: "transform 0.15s ease" }}
          >
            {words.map((word, wi) => {
              const extras = extraChars[wi] || "";
              let wordClass = "word";
              if (wi === currentWordIndex) {
                wordClass += " active";
              } else if (wi < currentWordIndex) {
                const hasIncorrect = word
                  .split("")
                  .some((ch, ci) => charStates[`${wi}-${ci}`] === "incorrect");
                const hasMissed = word
                  .split("")
                  .some((ch, ci) => !charStates[`${wi}-${ci}`]);
                const hasExtra = extras.length > 0;
                if (hasIncorrect || hasMissed || hasExtra) {
                  wordClass += " word-error";
                }
              }
              return (
                <span
                  key={wi}
                  className={wordClass}
                  data-testid={
                    wi === currentWordIndex ? "active-word" : undefined
                  }
                >
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
      </div>

      <div
        style={{
          color: "var(--sub-color)",
          fontSize: "0.7rem",
          marginTop: "30px",
          textAlign: "center",
        }}
      >
        <span style={{ opacity: 0.6 }}>tab</span> — restart test
      </div>
    </>
  );
}
