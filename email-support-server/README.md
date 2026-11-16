# Optional Level 4 email service

This Node service receives SMTP mail for Rodney, generates a random nine-digit single-use
code, replies via SMTP, and exposes HTTP validation to the game. Codes are stored only in
memory. The **current default TTL is 60 seconds**; older comments mentioning 15 seconds are stale.

The game can be played without this service: press F2 in Level 4 to use an on-screen local reply.

## Local setup

```sh
cd email-support-server
cp env.example .env
# Edit .env: use SMTP_PORT=2525 for a non-privileged local port.
npm install
npm start
```

Dependencies currently use version ranges and no committed npm lockfile. Record a reviewed
lockfile and use `npm ci` before establishing a reproducible deployment. Do not commit `.env`.
The existing Makefile contains deployment commands; it is not run by opening the Unity project.

`HTTP_PORT` defaults to 3000. `MAIL_DOMAIN` and `RODNEY_MAILBOX` determine the sender identity.
An outbound SMTP relay must be configured for reliable delivery. The gameplay client points at
`https://premiumdoorcodes.com` in the authored scene; configure `codeServerUrl` for your test service.

## Protocol

- `GET /health`: process status, active code count, uptime.
- `POST /api/request-code` with optional `email`: generate a code and optionally send a reply.
  With debug logging disabled, an empty request returns no usable code to the player.
- `POST /api/validate` with `code`: validate and consume a valid code.
- `GET /api/active-codes`: available only with `DEBUG_LOG_CODES=true`; exposes codes and sender addresses.

Incoming email must include “door”, “code”, or “please” in the fresh subject/body to get a
code. Other messages receive an annoyed generic reply. Quoted reply text is stripped.

## Deployment gaps identified by the source audit

This is a prototype backend. Before public use:

- Bound message size before buffering and parsing, and apply connection/request quotas.
- Validate the supplied email/code input and rate-limit by appropriate sender/client keys.
- Stop auto-responder loops; reject automated/bounce messages and set appropriate response headers.
- Do not trust arbitrary message From headers as proof that a recipient requested a reply.
- Keep debug endpoints disabled and avoid logging active codes or full sender details in production.
- Handle outbound delivery failure explicitly instead of reporting success after a failed send.
- Test code expiry, single-use consumption, duplicate requests, malformed payloads, and SMTP failures.
- Review the Makefile: it disables SSH host verification, lacks pinned installs, and its initial
  TLS configuration references certificate files before they are provisioned. Provisioning and
  current hosting costs have not been verified; do not assume the old “free tier” text is current.

No deployment, real email, account change, or external service test was performed in this development pass.
