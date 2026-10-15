# Node upgrade for Angular 22

CK.TS.Angular.Engine now targets Angular 22. The Angular 22 CLI refuses to start on an older Node:

> The Angular CLI requires a minimum Node.js version of v22.22.3 or v24.15.0 or v26.0.0.

## Requirements

| Tool | Required | Recommended |
| --- | --- | --- |
| Node.js | `^22.22.3`, `^24.15.0` or `^26.0.0` | 24.21.0 (LTS) |
| nvm-windows | 1.2.2 or later | 1.2.2 |
| Yarn | Provided by corepack (`packageManager` field) | - |

Node must be managed by nvm-windows only. A Node installed with the official MSI installer conflicts with nvm.

## Is my setup outdated?

Run in PowerShell:

```powershell
node -v
nvm version
Get-Package -Name "Node.js*" -ErrorAction SilentlyContinue
```

Your setup needs the upgrade if any of these is true:

- `node -v` prints a version below the requirements above (for example `v22.18.0` or `v20.x`).
- `nvm version` prints `1.1.x` or lower.
- `Get-Package` returns a `Node.js` entry: Node was installed with the MSI installer, outside nvm.

If none is true, you are done.

## Upgrade steps

### 1. Install nvm-windows 1.2.2

Download `nvm-setup.exe` (1.2.2) from https://github.com/coreybutler/nvm-windows/releases and run it. Keep the default paths. Your Node versions already installed with nvm are kept.

`winget upgrade CoreyButler.NVMforWindows` does not work when nvm was installed manually; use the installer.

Check in a new terminal:

```powershell
nvm version   # 1.2.2
```

### 2. Uninstall the MSI Node

Skip this step if `Get-Package` returned nothing.

Settings > Apps > **Node.js** > Uninstall.

### 3. Install and activate Node 24

```powershell
nvm install 24.21.0 64
nvm use 24.21.0
```

Give the exact version and the `64` architecture. If `nvm use` cannot create the link, run it again in an elevated PowerShell.

### 4. Enable Yarn for the new Node

Each Node version has its own global tools, so the Yarn shim must be created again:

```powershell
corepack enable
```

### 5. Restart your tools

Close all instances of Visual Studio, Rider, VS Code and every terminal, then reopen them. A running process keeps the PATH it started with. If a build still cannot find `node`, sign out of Windows and sign in again.

### 6. Verify

```powershell
node -v                     # v24.21.0
(Get-Command node).Source   # C:\nvm4w\nodejs\node.exe
yarn --version              # resolves (4.x inside a repository)
```

## Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Version 24.x.x-arm64.msi is not available` on an x64 machine | nvm-windows older than 1.2.2 | Step 1, then `nvm install 24.21.0 64` |
| `An error occurred trying to start process 'node'` in the build log | The IDE or terminal was started before the switch | Step 5 |
| `'yarn' is not recognized as an internal or external command` | No Yarn shim for the new Node | Step 4 |
| A global npm tool fails or is missing | It was installed under the previous Node | Reinstall it with `npm i -g <tool>` |

Build servers and CI agents need the same upgrade.
