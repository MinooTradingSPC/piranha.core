<!-- MinooTrading SPC fork — security-hardened build of PiranhaCMS/piranha.core v10.x by Kiarash Minoo (https://github.com/MinooTradingSPC/piranha.core) -->

> Piranha CMS is a free, open source, package based CMS framework for .NET

Package containing the administrative and editorial interface for Piranha CMS. This package needs to be hosted in an ASP.NET web application.

## UI themes

A UI theme adds its own style sheets and scripts to every Manager page, on top
of the built-in ones. Pick one under **System → Config** in the **UI theme**
dropdown. The package ships eight, each matching the login page design with the
same key, and each styles both the light and the dark mode:

| Key | Files | Look |
|---|---|---|
| `Aura` | `manager-aura.min.css` | Deep slate console with a dot grid, faint orange rings and pill controls |
| `Auralis` | `manager-auralis.min.css` | Warm off-white with an orange and rose glow, Geist type and cut-corner buttons |
| `AuralisIndigo` | `manager-auralis-indigo.min.css` | White glass panels with an indigo and cyan glow and Geist type |
| `Ecosystem` | `manager-ecosystem.min.css` | Frosted white cards on grey with a slate menu and orange gradient buttons |
| `Lumina` | `manager-lumina.min.css` | Near-black brand board with a fine grid, gradient hairlines and peach accents |
| `NeuroSync` | `manager-neurosync.min.css` | Near-black with a still fan of light streaks and terracotta accents |
| `Nexus` | `manager-nexus.min.css` | Black and cyan control room with uppercase mono labels and 4px corners |
| `Verdant` | `manager-verdant.min.css` | Sand page, deep forest menu, Playfair Display titles and obsidian pills |

Every theme also has a `.rtl.min.css` mirror, used for right-to-left cultures.
Inside Piranha.Manager, name a theme's file `assets/src/scss/manager-{name}.scss`
and, if it needs one, `assets/src/js/manager-{name}.js`: `gulp min:css` and
`gulp min:js` build them into `assets/dist/` without any change to the gulpfile.
The design references live under `.claude/designs/login` and
`.claude/designs/manager` in the repository.

A client project can add its own themes at startup, or clear the list to hide
the dropdown:

```csharp
var manager = App.Modules.Get<Piranha.Manager.Module>();

manager.Themes.Add(new Piranha.Manager.ManagerTheme
{
    Id = "Acme",                                   // stored in the config, keep it stable
    Title = "Acme brand",                          // shown in the dropdown
    Styles = { "~/css/acme-manager.css" },         // from the client app's wwwroot
    RtlStyles = { "~/css/acme-manager.rtl.css" },  // optional, used for right-to-left cultures
    Scripts = { "~/js/acme-manager.js" }           // optional, runs after the Manager scripts
});
```

The dropdown's **Default** option keeps the built-in look. The theme's styles
load after `full.min.css` and before the styles in `Module.Styles`, and the
`<body>` gets the class `manager-theme-{key}` in lower case. The theme doesn't
replace the light and dark switch: style both `[data-bs-theme="light"]` and
`[data-bs-theme="dark"]` if the theme changes colours.

The config page only saves registered keys. If the saved theme is later
removed, the Manager falls back to the built-in look.