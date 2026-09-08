<p align="center">
  <img src="setup/assets/Logo/landras-logo-svg-128.svg" alt="Landras logo" />
</p>

<h1 align="center">Landras — Cross-platform Git client</h1>

**Landras** is an independent cross-platform fork of
[Git Extensions](https://github.com/gitextensions/gitextensions), the standalone Windows UI
tool for managing git repositories.

## The challenge

Git Extensions is a mature, feature-rich git client — but it is built on Windows Forms, which
ties it to Windows forever. The goal of this fork is to **migrate the UI to
[Avalonia](https://avaloniaui.net/)** so the application runs natively on **Windows, Linux and
macOS**.

This is not a form-by-form port: the presentation layer (~142K lines of WinForms code) will be
rewritten as a new Avalonia (MVVM) shell that reuses the existing core (`GitCommands`,
`GitExtUtils`, the plugin infrastructure and the revision graph model). The original WinForms
application keeps building and running throughout the migration and serves as the functional
reference until the new shell reaches parity.

The journey started on **July 11, 2026**, from the upstream stable tag `v7.2.0`.

## Status

[![Fork CI](https://github.com/madialeva/gitextensionscp/actions/workflows/fork-ci.yml/badge.svg?event=pull_request)](https://github.com/madialeva/gitextensionscp/actions/workflows/fork-ci.yml)

The migration is developed incrementally, phase by phase:

| Phase | Description | Status |
|-------|-------------|--------|
| **0 — Foundations** | Decouple the core from WinForms and prove it builds and passes tests on Linux in CI | ✅ |
| **1 — Walking skeleton** | Avalonia shell that opens repositories and displays a virtualized commit list | ✅ |
| **2 — Vertical slice** | Commit graph and diff viewer (read-only browsing) | |
| **3 — Write operations** | Commit, push/pull, branches, stash… | |
| **4 — Platform & parity** | Localization, settings UI, packaging per OS | |

## Building

Requirements: [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and git.

```powershell
git clone --recurse-submodules https://github.com/madialeva/gitextensionscp.git
cd gitextensionscp
dotnet build
```

On Linux, run the cross-platform verification directly with Bash:

```bash
bash eng/Verify-Linux.sh
bash eng/Verify-Linux.sh Debug
```

The new cross-platform Avalonia shell (under development) is built at
`artifacts/Debug/bin/GitExtensions.Avalonia/net10.0/GitExtensions.Avalonia.dll`
(run with `dotnet run --project src/app/GitExtensions.Avalonia`).

The WinForms app (Windows-only, maintained as reference) is built at
`artifacts/Debug/bin/GitExtensions/net10.0-windows/GitExtensions.exe`.

## Credits

This project stands on the shoulders of the
[Git Extensions](https://github.com/gitextensions/gitextensions) team and its
[contributors](https://github.com/gitextensions/gitextensions/graphs/contributors) — thank you
for two decades of work on an outstanding git client. Upstream resources:

* Original repository: [github.com/gitextensions/gitextensions](https://github.com/gitextensions/gitextensions)
* Online manual: [git-extensions-documentation.readthedocs.org](https://git-extensions-documentation.readthedocs.org/)

Icons by [Yusuke Kamiyamane](http://p.yusukekamiyamane.com/)
([CCA/3.0](http://creativecommons.org/licenses/by/3.0/)).

## License

This project is a fork of [Git Extensions](https://github.com/gitextensions/gitextensions) and
retains its original license — [GNU General Public License v3.0](LICENSE.md).
