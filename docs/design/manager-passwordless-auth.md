# Manager Passwordless Authentication: Design

Status: Draft
Ticket: [#171](https://github.com/MinooTradingSPC/piranha.core/issues/171) (parent design ticket)

## 1. Scope and constraints

The Manager area has two independent sign-in paths today, and they are not
equally capable of supporting this design:

- **`Piranha.Manager.LocalAuth`** (`ISecurity.SignIn(context, username, password)`)
  is a single-credential-pair interface with no concept of a user record.
  The host application supplies its own `ISecurity` implementation (there is
  no shipped implementation in this repo), and it has nowhere to persist a
  passkey credential, a TOTP secret, or a recovery method per user.
- **`Piranha.AspNetCore.Identity.*`** wraps standard ASP.NET Core Identity
  (`AddIdentity`, `SignInManager`, EF-backed `Users` table across SQLite,
  SQL Server, MySQL, PostgreSQL) and already has per-user storage, a
  `Lockout` option, and a two-factor token provider abstraction.

**Decision:** this flow targets `Piranha.AspNetCore.Identity.*` only.
`LocalAuth` keeps its existing username/password page unchanged as a
lightweight fallback for hosts that don't need multi-user identity. See
[§8 Open questions](#8-open-questions) for the option to later add a
minimal `ISecurity` extension if that's ever needed.

## 2. Login UX flow

```
┌─────────────┐     email      ┌──────────────────────┐
│  Enter email │ ─────────────▶ │ POST /manager/auth/   │
│   (step 1)   │                │ options               │
└─────────────┘                └──────────┬────────────┘
                                           │ always 200, same shape
                                           ▼
                                ┌──────────────────────┐
                                │ Method selection      │
                                │ (step 2)              │
                                │ preferred method shown │
                                │ first, others as       │
                                │ "Try another way"      │
                                └──────────┬────────────┘
                     ┌─────────────────────┼─────────────────────┐
                     ▼                     ▼                     ▼
              ┌─────────────┐      ┌─────────────┐       ┌─────────────┐
              │  Passkey     │      │  TOTP code   │       │ Email OTP /  │
              │  (WebAuthn   │      │  (6-digit,   │       │ magic link   │
              │  assertion)  │      │  30s window) │       │ (recovery)   │
              └──────┬──────┘      └──────┬──────┘       └──────┬──────┘
                     └─────────────────────┼─────────────────────┘
                                           ▼
                                ┌──────────────────────┐
                                │ POST /manager/auth/   │
                                │ verify                │
                                │ → session cookie      │
                                └──────────────────────┘
```

1. **Email entry:** the user enters their email on `~/manager/login`. No
   password field is shown by default.
2. **Method discovery:** the client calls `POST /manager/auth/options`
   (§3). The response always has the same shape and timing characteristics
   regardless of whether the email matches a user (§4), so it can't be used
   to enumerate accounts.
3. **Method selection:** the UI shows the *strongest available* method for
   that response as the default action, with a "Try another way" link that
   expands the rest:
   - Passkey (if the response says a resident credential may exist for this
     browser) is offered first: a single click plus a platform biometric
     check, no manual code entry.
   - TOTP is offered next if the account has an authenticator registered.
   - Password is offered only if the account has a password set (legacy
     compatibility, §6.4) and no passkey/TOTP method is registered.
   - Email OTP / magic link is always offered last, framed as "Email me a
     code." It's the universal fallback, and the recovery path when every
     other method is unavailable (lost device, new browser).
4. **Verification:** whichever method is chosen posts to
   `POST /manager/auth/verify` with a method discriminator and its payload
   (WebAuthn assertion, TOTP code, password, or OTP code). Success sets the
   existing Manager auth cookie via the existing
   `AuthController.SetAuthCookie` redirect step; failure re-renders the
   *same* method-selection screen with a generic error (§4) and counts
   against the rate limit (§5).
5. **No stored methods:** if discovery finds a user with *no* method
   registered yet (fresh Identity install, or an admin created via seed),
   the response still returns the email-OTP option only. First sign-in is
   always by emailed code, and the Manager should prompt the user to
   register a passkey/TOTP immediately after that first session starts.

## 3. Backend contract

### `POST /manager/auth/options`

```jsonc
// Request
{ "email": "user@example.com" }

// Response — 200, always, regardless of whether the email is known
{
  "token": "opaque-flow-token",     // ties options → verify, short-lived (§4)
  "methods": ["passkey", "totp", "email-otp"]   // order = preference; subset of
                                                 // ["passkey", "totp", "password", "email-otp"]
}
```

- Always returns `200` with the same JSON shape and a normalized response
  time (§4): never `404`, never a distinguishable error for "unknown
  email".
- `token` is a short-lived (2–5 min), single-use, signed opaque value (e.g.
  ASP.NET Core Data Protection–protected payload) binding the flow to the
  submitted email server-side, so `verify` doesn't need to re-accept the
  email or trust the client's claim of which methods are "available".
- For an unknown email, `methods` is exactly `["email-otp"]`. Requesting an
  email-OTP for that address looks identical, from the outside, to a real
  request (§4 covers the server-side handling), but no email actually goes
  out to an address that doesn't exist.

### `POST /manager/auth/verify`

```jsonc
// Request
{
  "token": "opaque-flow-token",
  "method": "totp",                 // "passkey" | "totp" | "password" | "email-otp"
  "code": "123456"                  // shape depends on method:
                                     //   passkey  → WebAuthn AuthenticatorAssertionResponse
                                     //   totp     → 6-digit code
                                     //   password → password string
                                     //   email-otp→ 6-digit code from the emailed message
}

// Response — success
{ "succeeded": true, "returnUrl": "/manager/login/auth?returnUrl=..." }

// Response — failure (any reason)
{ "succeeded": false, "message": "The code you entered is incorrect or has expired." }
```

- One generic failure message for *every* failure reason (expired token,
  wrong code, unknown method, locked-out account, revoked passkey). See §4.
- On success, redirects through the existing
  `AuthController.SetAuthCookie` step unchanged (it already only depends on
  the ASP.NET auth cookie being set, which `SignInManager.SignInAsync`
  still does).
- `passkey` verification is a standard WebAuthn assertion check
  (`Fido2NetLib` or equivalent). §6 covers this as a new dependency.
- `email-otp` requires a companion `POST /manager/auth/options/resend` with
  its own rate limit (§5), since the user may need a second code.

## 4. Anti-enumeration

The two places an attacker could learn whether `email` belongs to a real
account are the `options` response and its timing:

- **Response shape:** `options` never branches on "user found / not
  found" in a way that reaches the client. An unknown email gets
  `methods: ["email-otp"]`, identical to a known email whose *only*
  registered method is email-OTP. The client cannot distinguish "this
  account only has email recovery" from "this account doesn't exist".
- **Timing:** the handler does constant-shape work either way. It always
  attempts a user lookup, always computes a methods list (empty user →
  hardcoded `["email-otp"]`), and always issues a flow token. It must
  *not* skip a hashing/lookup step for the not-found path in a way that's
  measurably faster; pad or normalize to the slower branch's typical
  latency if profiling shows a gap.
- **Email sending:** for a real, known email, `options` (or `verify` with
  method `email-otp`) triggers the actual email send. For an unknown
  email, no email is sent, but the HTTP response and timing are the same.
  The difference is invisible to the caller, and only observable by
  controlling the actual mailbox.
- **Verify failures:** as in §3, every `verify` failure returns the same
  generic message and (as much as possible) the same latency, whether the
  token expired, the code was wrong, or the account doesn't exist.

## 5. Rate limiting and lockout

Two layers, since they defend against different attackers:

### 5.1 Per-IP (bot/scraping defense)

Use the built-in ASP.NET Core rate-limiting middleware
(`Microsoft.AspNetCore.RateLimiting`, available since net8.0, matching this
repo's `Directory.Build.props` target range) with a fixed-window or
token-bucket limiter scoped to `/manager/auth/*`:

- `options`: e.g. 20 requests / 5 min / IP.
- `verify`: e.g. 10 requests / 5 min / IP, tighter since this is where
  guessing happens.
- `options/resend`: e.g. 3 requests / 10 min / IP.

Exceeding the limit returns `429` with `Retry-After`. This can be
distinguishable from a normal failure, since IP-level throttling isn't the
enumeration surface (§4 concerns per-email information leakage, not
per-IP abuse signaling).

### 5.2 Per-account lockout (credential-stuffing / targeted defense)

Reuse ASP.NET Core Identity's existing `IdentityOptions.Lockout` rather
than inventing a parallel mechanism:

- Every failed `verify` attempt for a given user calls
  `UserManager.AccessFailedAsync`, same as the password flow does today.
- Once `MaxFailedAccessAttempts` is reached, `IsLockedOutAsync` gates
  *all* methods for that account (passkey, TOTP, password, email-otp). A
  locked account still gets the same normal-looking generic failure
  message (§4) rather than a distinct "account locked" message, so
  lockout state doesn't leak to an outside caller.
- Recommended defaults for the Manager (admin-facing, low volume, higher
  stakes than a public site): `MaxFailedAccessAttempts = 5`,
  `DefaultLockoutTimeSpan = 15 minutes`, doubling on repeated lockouts
  within a rolling 24h window (needs a small custom policy layered over
  the default, since stock Identity doesn't escalate lockout duration).

## 6. Method dependencies and rollout order

The methods aren't independent: each has a prerequisite relationship that
constrains implementation order into child tickets.

1. **Email OTP is the foundation, not an afterthought.** It's the only
   method that works for a brand-new account (no passkey/TOTP registered
   yet) and the only recovery path when every other method is unavailable.
   It must ship first; passkey and TOTP registration flows both assume
   email-OTP already exists as the "verify it's really you before you
   register a stronger method" step.
2. **TOTP builds on Identity's existing token-provider abstraction**
   (`UserManager.GenerateTwoFactorTokenAsync` /
   `RegisterTwoFactorProviderAsync`). It needs no new external dependency,
   so it's the natural second ticket after email-OTP.
3. **Passkey/WebAuthn is the only method needing a new third-party
   dependency** (`Fido2NetLib` or equivalent; ASP.NET Core Identity has
   no built-in WebAuthn support) plus new EF tables/migrations across all
   four identity providers (SQLite/SQL Server/MySQL/PostgreSQL, via
   `generate-migrations.ps1`) to store credential public keys and sign
   counters. This is the largest child ticket and should land last, after
   the discovery contract (§3) has already been proven out by TOTP.
4. **Password fallback exists purely for legacy compatibility.** It should
   only appear in `options.methods` for accounts that already have a
   password set (migrated from the current username/password flow), and it
   should never be offered to a newly created account. New accounts get
   nudged toward TOTP/passkey immediately after their first email-OTP
   sign-in (§2 step 5).
5. **Ordering rule for `options.methods`:** passkey > totp > password >
   email-otp, strongest-first, except that email-otp is always present as
   a fallback entry unless the account has explicitly disabled recovery
   (out of scope for this ticket; see §7).

## 7. Non-goals / follow-up tickets

This ticket only defines the design; the following are separate child
tickets once this is agreed:

- `POST /manager/auth/options` + `/verify` scaffolding and the
  anti-enumeration/rate-limiting middleware (§3, §4, §5.1).
- Email-OTP method (§6.1), including the email template and
  `options/resend` endpoint.
- TOTP method (§6.2) and its Manager settings-page registration UI
  (QR code enrollment, backup codes).
- Passkey/WebAuthn method (§6.3): dependency choice, EF migrations across
  all four identity providers, and browser-side `navigator.credentials`
  integration in the Manager login page.
- Per-account lockout escalation policy (§5.2) beyond stock Identity
  defaults.
- Disabling recovery / "no email fallback" mode for high-security
  deployments (mentioned in §6.5 as explicitly out of scope here).

## 8. Open questions

- Does `LocalAuth` need *any* passwordless support, or does it stay
  password-only indefinitely for hosts that don't adopt
  `Piranha.AspNetCore.Identity`? (§1 assumes the latter.)
- Should the per-IP limits in §5.1 be configurable via `ManagerOptions`,
  or fixed constants? Hosts behind a shared corporate NAT may need higher
  per-IP thresholds.
- Backup codes for TOTP/passkey loss (in addition to email-otp recovery)
  aren't addressed here. Worth a decision before the TOTP child ticket
  starts.
