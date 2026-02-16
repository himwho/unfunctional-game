/**
 * unfunctional - Level 4 Email Support Server
 * =============================================
 *
 * A barebones Node.js server for the Level 4 keypad puzzle.
 *
 * How it works:
 * 1. Player sees sticky note in-game with "rodney@please.nyc"
 * 2. Player alt-tabs and emails that address from their real email client
 * 3. This SMTP server receives the email, generates a 9-digit OTP code
 *    with a 15-second TTL, and auto-replies with the code
 * 4. Player alt-tabs back, types the code into the keypad
 * 5. The game validates the code via HTTP POST to /api/validate
 *
 * Code generation method:
 *   Uses random OTP with TTL -- the same approach as email/SMS-based 2FA.
 *   A cryptographically random 9-digit code is generated per request and
 *   stored in memory with a 15-second expiration. Codes are single-use
 *   and purged on expiry. This is NOT TOTP (which derives codes from a
 *   shared secret + time window); it's a request-scoped random OTP,
 *   which is how most "email me a code" 2FA flows actually work.
 *
 * Components:
 *   - SMTP inbound server (receives mail to rodney@please.nyc)
 *   - Nodemailer outbound (replies with the code)
 *   - Express HTTP API (game client requests codes + validates them)
 *   - In-memory code store with automatic TTL cleanup
 */

require("dotenv").config();

const crypto = require("crypto");
const express = require("express");
const cors = require("cors");
const { SMTPServer } = require("smtp-server");
const { simpleParser } = require("mailparser");
const nodemailer = require("nodemailer");

// =========================================================================
// Configuration
// =========================================================================

const HTTP_PORT = parseInt(process.env.HTTP_PORT || "3000", 10);
const SMTP_PORT = parseInt(process.env.SMTP_PORT || "2525", 10);
const MAIL_DOMAIN = process.env.MAIL_DOMAIN || "please.nyc";
const RODNEY_MAILBOX = process.env.RODNEY_MAILBOX || "rodney";
const CODE_TTL_MS = parseInt(process.env.CODE_TTL_SECONDS || "15", 10) * 1000;
const DEBUG_LOG = process.env.DEBUG_LOG_CODES === "true";

// =========================================================================
// Code Store (in-memory OTP with TTL)
// =========================================================================

/**
 * Stores active OTP codes in a Map.
 * Key: the 9-digit code string
 * Value: { createdAt: number, senderEmail: string }
 *
 * This mirrors how email-based 2FA works:
 *   - A random code is generated per request (not derived from time like TOTP)
 *   - The code is stored server-side with a short TTL
 *   - The code is delivered out-of-band (email reply)
 *   - The code is validated by the relying party (the game) against the store
 *   - The code is consumed on successful validation (single-use)
 */
const activeCodes = new Map();

function generateCode() {
  let code = "";
  for (let i = 0; i < 9; i++) {
    code += crypto.randomInt(0, 10).toString();
  }
  return code;
}

function storeCode(senderEmail) {
  // Prevent duplicate codes (astronomically unlikely with 9 digits, but safe)
  let code;
  do {
    code = generateCode();
  } while (activeCodes.has(code));

  activeCodes.set(code, {
    createdAt: Date.now(),
    senderEmail: senderEmail || "unknown",
  });

  if (DEBUG_LOG) {
    console.log(
      `[CODE] Generated ${code} for ${senderEmail} (expires in ${CODE_TTL_MS / 1000}s)`
    );
  }

  return code;
}

function validateCode(code) {
  const entry = activeCodes.get(code);
  if (!entry) {
    return { valid: false, reason: "invalid" };
  }

  const age = Date.now() - entry.createdAt;
  if (age > CODE_TTL_MS) {
    activeCodes.delete(code);
    return { valid: false, reason: "expired" };
  }

  // Consume the code (single-use, like real 2FA)
  activeCodes.delete(code);
  return { valid: true, senderEmail: entry.senderEmail };
}

// Periodic cleanup of expired codes (every 5 seconds)
setInterval(() => {
  const now = Date.now();
  let purged = 0;
  for (const [code, entry] of activeCodes) {
    if (now - entry.createdAt > CODE_TTL_MS) {
      activeCodes.delete(code);
      purged++;
    }
  }
  if (purged > 0 && DEBUG_LOG) {
    console.log(`[CODE] Purged ${purged} expired code(s). Active: ${activeCodes.size}`);
  }
}, 5000);

// =========================================================================
// Outbound email transport (Nodemailer)
// =========================================================================

let mailTransport;

if (process.env.SMTP_RELAY_HOST) {
  // Use configured SMTP relay (SES, Mailgun, SendGrid, etc.)
  mailTransport = nodemailer.createTransport({
    host: process.env.SMTP_RELAY_HOST,
    port: parseInt(process.env.SMTP_RELAY_PORT || "587", 10),
    secure: parseInt(process.env.SMTP_RELAY_PORT || "587", 10) === 465,
    auth: {
      user: process.env.SMTP_RELAY_USER,
      pass: process.env.SMTP_RELAY_PASS,
    },
  });
  console.log(`[MAIL] Outbound via relay: ${process.env.SMTP_RELAY_HOST}`);
} else {
  // Direct delivery (works if port 25 outbound is open, often blocked on cloud)
  mailTransport = nodemailer.createTransport({
    direct: true,
    name: MAIL_DOMAIN,
  });
  console.log("[MAIL] Outbound via direct delivery (no relay configured)");
}

async function sendCodeReply(toAddress, code) {
  const ttlSeconds = CODE_TTL_MS / 1000;

  const mailOptions = {
    from: `"Rodney" <${RODNEY_MAILBOX}@${MAIL_DOMAIN}>`,
    to: toAddress,
    subject: "Your door code",
    text: `${code}\n\nThis code expires in ${ttlSeconds} seconds. Hurry up.`,
    html: `
      <div style="font-family: monospace; padding: 20px;">
        <h2 style="margin: 0 0 10px 0;">Your door code:</h2>
        <p style="font-size: 36px; letter-spacing: 6px; font-weight: bold; margin: 10px 0;">
          ${code.slice(0, 3)} ${code.slice(3, 6)} ${code.slice(6, 9)}
        </p>
        <p style="color: #cc3333;">Expires in ${ttlSeconds} seconds. Hurry up.</p>
        <br/>
        <p style="color: #888; font-size: 12px;">&mdash; Rodney</p>
      </div>
    `,
  };

  try {
    const info = await mailTransport.sendMail(mailOptions);
    console.log(`[MAIL] Reply sent to ${toAddress}: ${info.messageId || "ok"}`);
  } catch (err) {
    console.error(`[MAIL] Failed to send reply to ${toAddress}:`, err.message);
    // Code is still stored and valid -- the player might have another way
    // to get it (e.g. the HTTP API debug endpoint)
  }
}

// =========================================================================
// SMTP Inbound Server
// =========================================================================

const smtpServer = new SMTPServer({
  // Allow unauthenticated connections (anyone can email Rodney)
  authOptional: true,
  disabledCommands: ["STARTTLS"], // Keep it simple for a game server

  // Accept any sender
  onAuth(auth, session, callback) {
    callback(null, { user: "anonymous" });
  },

  // Accept mail addressed to rodney@please.nyc (or any @please.nyc)
  onRcptTo(address, session, callback) {
    const recipient = address.address.toLowerCase();
    if (recipient.endsWith(`@${MAIL_DOMAIN}`)) {
      callback();
    } else {
      callback(new Error(`We only accept mail for @${MAIL_DOMAIN}`));
    }
  },

  // Process the incoming message
  onData(stream, session, callback) {
    let rawData = "";
    stream.on("data", (chunk) => {
      rawData += chunk.toString();
    });

    stream.on("end", async () => {
      try {
        const parsed = await simpleParser(rawData);
        const senderAddress =
          parsed.from && parsed.from.value && parsed.from.value[0]
            ? parsed.from.value[0].address
            : null;

        console.log(
          `[SMTP] Received email from ${senderAddress || "unknown"} subject: "${parsed.subject || "(none)}"`
        );

        if (senderAddress) {
          // Generate code and reply
          const code = storeCode(senderAddress);
          await sendCodeReply(senderAddress, code);
        } else {
          console.log("[SMTP] No sender address found, skipping reply");
        }
      } catch (err) {
        console.error("[SMTP] Error processing email:", err.message);
      }

      callback();
    });
  },
});

// =========================================================================
// HTTP API (Express)
// =========================================================================

const app = express();
app.use(cors());
app.use(express.json());

// Health check
app.get("/health", (req, res) => {
  res.json({
    status: "ok",
    activeCodes: activeCodes.size,
    uptimeSeconds: Math.floor(process.uptime()),
  });
});

/**
 * POST /api/request-code
 *
 * Called by the in-game "Email Rodney" button as a convenience shortcut.
 * Generates a code and optionally sends an email reply.
 *
 * Body (optional): { "email": "player@example.com" }
 *
 * Response: { "message": "...", "expiresIn": 15 }
 * In debug mode, also returns the code directly for testing.
 */
app.post("/api/request-code", async (req, res) => {
  const email = req.body && req.body.email ? req.body.email : null;
  const code = storeCode(email || "in-game-request");

  // If an email was provided, send the code via email too
  if (email) {
    sendCodeReply(email, code).catch((err) => {
      console.error("[API] Email send failed:", err.message);
    });
  }

  const response = {
    message: email
      ? "Rodney sent you the code. Check your email."
      : "Code generated.",
    expiresIn: CODE_TTL_MS / 1000,
  };

  // In debug mode, include the code in the HTTP response
  // so the game can display it without the player needing real email
  if (DEBUG_LOG) {
    response.code = code;
  }

  res.json(response);
});

/**
 * POST /api/validate
 *
 * Called by the game when the player submits a code on the keypad.
 * Validates the code against the in-memory store.
 *
 * Body: { "code": "123456789" }
 *
 * Response: { "valid": true/false, "message": "..." }
 */
app.post("/api/validate", (req, res) => {
  const code = req.body && req.body.code ? req.body.code.toString().trim() : "";

  if (code.length !== 9) {
    return res.json({
      valid: false,
      message: "Code must be exactly 9 digits.",
    });
  }

  const result = validateCode(code);

  if (result.valid) {
    console.log(`[API] Code ${code} validated successfully`);
    res.json({
      valid: true,
      message: "ACCESS GRANTED",
    });
  } else {
    console.log(`[API] Code ${code} rejected: ${result.reason}`);
    res.json({
      valid: false,
      message:
        result.reason === "expired"
          ? "Code expired. Request a new one."
          : "Invalid code.",
    });
  }
});

/**
 * GET /api/active-codes (debug only)
 *
 * Lists all currently active codes. Only available when DEBUG_LOG_CODES=true.
 */
if (DEBUG_LOG) {
  app.get("/api/active-codes", (req, res) => {
    const codes = [];
    const now = Date.now();
    for (const [code, entry] of activeCodes) {
      const remainingMs = CODE_TTL_MS - (now - entry.createdAt);
      if (remainingMs > 0) {
        codes.push({
          code,
          senderEmail: entry.senderEmail,
          remainingSeconds: Math.round(remainingMs / 1000),
        });
      }
    }
    res.json({ codes });
  });
}

// =========================================================================
// Start servers
// =========================================================================

app.listen(HTTP_PORT, () => {
  console.log(`[HTTP] API server listening on port ${HTTP_PORT}`);
  console.log(`[HTTP] Endpoints:`);
  console.log(`       POST /api/request-code  - generate a new code`);
  console.log(`       POST /api/validate      - validate a code`);
  console.log(`       GET  /health            - health check`);
  if (DEBUG_LOG) {
    console.log(`       GET  /api/active-codes  - list active codes (debug)`);
  }
});

smtpServer.listen(SMTP_PORT, () => {
  console.log(`[SMTP] Mail server listening on port ${SMTP_PORT}`);
  console.log(`[SMTP] Accepting mail for *@${MAIL_DOMAIN}`);
});

smtpServer.on("error", (err) => {
  // Port 25 often requires root or is blocked -- fall back gracefully
  if (err.code === "EACCES") {
    console.error(
      `[SMTP] Cannot bind to port ${SMTP_PORT} (permission denied). ` +
        `Try running with sudo or set SMTP_PORT=2525 for testing.`
    );
  } else if (err.code === "EADDRINUSE") {
    console.error(
      `[SMTP] Port ${SMTP_PORT} already in use. ` +
        `Set SMTP_PORT to another value in .env.`
    );
  } else {
    console.error("[SMTP] Server error:", err.message);
  }
});

console.log("---");
console.log("unfunctional - Level 4 Email Support Server");
console.log(`Code TTL: ${CODE_TTL_MS / 1000} seconds`);
console.log(`Debug logging: ${DEBUG_LOG}`);
console.log("---");
