<!-- MinooTrading SPC fork — security-hardened build of PiranhaCMS/piranha.core v10.x by Kiarash Minoo (https://github.com/MinooTradingSPC/piranha.core) -->

> Piranha CMS is a free, open source, package based CMS framework for .NET

Package for using local authentication for manager interface.
## Login page designs

`Login.cshtml` holds the login page's markup. Its sign-in behaviour (passkey,
authenticator and email-code sign-in, the remembered username and form
validation) is in `piranha.login.js` in Piranha.Manager, which gulp bundles
with the other Manager scripts. A design only changes how the page looks. It's
a key plus the style and script files the page loads for it.

### Pick a design

Under **System → Config** in the Manager, **Login page design** lists the
registered designs. The package ships nine, and each design other than
`Default` has a Manager UI theme with the same key:

| Key | Files | Look |
|---|---|---|
| `Default` | `login-default.min.css` | The form alone in a centred card in the logo's colours, following the browser's light or dark mode |
| `Verdant` | `login-verdant.min.css` | Split screen: a forest-green brand panel with Playfair Display headings beside a sand-coloured form card |
| `Aura` | `login-aura.min.css` | A dark console frame with orange rings, a glass card and mono labels |
| `NeuroSync` | `login-neurosync.min.css`, `login-neurosync.min.js` | Near-black page with an animated WebGL tunnel of light streaks and a frosted-glass form panel |
| `Auralis` | `login-auralis.min.css`, `login-auralis.min.js` | White page with a moving orange and rose WebGL gradient behind glass slices and a cut-corner glass form card |
| `AuralisIndigo` | `login-auralis-indigo.min.css`, `login-auralis-indigo.min.js` | Light hero with an indigo-to-cyan headline beside a WebGL plasma behind glass slices, following the browser's dark mode |
| `Ecosystem` | `login-ecosystem.min.css` | A fanned 3D stack of coloured cards with the sign-in form on the frosted front card, following the browser's dark mode |
| `Lumina` | `login-lumina.min.css`, `login-lumina.min.js` | Two-card brand board: a WebGL noise cover with peach contour lines beside the form card |
| `Nexus` | `login-nexus.min.css`, `login-nexus.min.js` | Black and cyan frame with an animated isometric core scene beside the form |

### Add a client's designs

The designs are the `LoginDesigns` list on the Manager module, so each client
project registers its own at startup, next to the rest of its Piranha setup:

```csharp
var manager = App.Modules.Get<Piranha.Manager.Module>();

manager.LoginDesigns.Add(new Piranha.Manager.LoginDesign
{
    Id = "Acme",                         // stored in the config, keep it stable
    Title = "Acme brand",                // shown in the dropdown
    Styles = { "~/css/acme-login.css" }, // from the client app's wwwroot
    RtlStyles = { "~/css/acme-login.rtl.css" }, // optional, used for right-to-left cultures
    Scripts = { "~/js/acme-login.js" }   // optional, runs after the sign-in scripts
});

// Or replace the built-in design, keeping its key as the fallback.
manager.LoginDesigns.RemoveAll(d => d.Id == Piranha.Manager.LoginDesign.DefaultId);
manager.LoginDesigns.Add(new Piranha.Manager.LoginDesign
{
    Id = Piranha.Manager.LoginDesign.DefaultId,
    Title = "Acme default",
    Styles = { "~/css/acme-default-login.css" }
});
```

A design's style sheet styles the page's existing markup, so start from
[`login-default.scss`](../Piranha.Manager/assets/src/scss/login-default.scss),
which restyles every part of it. Inside Piranha.Manager, name the files
`assets/src/scss/login-{name}.scss` and, if the design needs one,
`assets/src/js/login-{name}.js`: `gulp min:css` and `gulp min:js` build them
into `assets/dist/css/login-{name}.min.css` and
`assets/dist/js/login-{name}.min.js` without any change to the gulpfile. The
sign-in script shows its hidden forms by clearing their inline `display`, so a
design's own `display` rule for `.login-subform` applies. The `<body>` also gets the class
`login-design-{key}` in lower case, for styles that need to tell designs apart.

### Right-to-left cultures

The page sets `lang` and `dir` on `<html>` from the current culture. When the
culture is right-to-left (Persian, Arabic, Hebrew and so on), it loads the
design's `RtlStyles` in place of `Styles`, and falls back to `Styles` if the
design has none. `gulp min:css` builds every `login-{name}.scss` into both
`assets/dist/css/login-{name}.min.css` and the mirrored
`assets/dist/css/login-{name}.rtl.min.css`, and the built-in designs already
register both.

The config page only saves keys from this list. If the saved design is later
removed, the page logs a warning and falls back to `Default`.

Ship a design's files with the client app instead of loading them from a CDN.
A sign-in page shouldn't depend on third-party code, and a Content Security
Policy on the app would block them anyway.
