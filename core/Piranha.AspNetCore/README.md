<!-- MinooTrading SPC fork — security-hardened build of PiranhaCMS/piranha.core v10.x by Kiarash Minoo (https://github.com/MinooTradingSPC/piranha.core) -->

> Piranha CMS is a free, open source, package based CMS framework for .NET

Package containing helpers, extension methods and middleware for building a Piranha CMS application using ASP.NET MVC or ASP.NET Razor Pages.

## Site themes

A site theme adds style sheets and scripts to the public site's layout. Piranha
ships none, so **System → Config** in the Manager only shows the **Site theme**
dropdown after the application registers at least one at startup:

```csharp
App.SiteThemes.Add(new Piranha.Models.SiteTheme
{
    Id = "Acme",                               // stored in the config, keep it stable
    Title = "Acme brand",                      // shown in the dropdown
    Styles = { "~/css/acme.css" },             // from the app's wwwroot
    RtlStyles = { "~/css/acme.rtl.css" },      // optional, used for right-to-left cultures
    Scripts = { "~/js/acme.js" }               // optional
});
```

The layout loads the selected theme with three html helpers:

```cshtml
<head>
    <link rel="stylesheet" href="~/css/site.css">
    @Html.SiteThemeStyles()
</head>
<body class="@Html.SiteThemeClass()">
    @RenderBody()
    @Html.SiteThemeScripts()
</body>
```

`SiteThemeClass()` returns `site-theme-{key}` in lower case. Razor Pages that
inherit `SinglePage<T>`, `SinglePost<T>` or `ArchivePage<T>` can also read the
selected theme from `Model.Theme`, and `Html.SiteTheme()` returns it in any
view. The dropdown's **Default** option, an unregistered key and an empty list
all give `null`, so the helpers render nothing and the site keeps its own look.