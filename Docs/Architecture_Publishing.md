# Architecture viewer publishing

Option B: a README preview plus a self-contained Archify viewer on GitHub Pages. The viewer is the site homepage.

## Review the branch

- Open `site/index.html` in Chrome or Edge; no server, npm install or Unity installation is required.
- Switch themes, search for `EventBus`, select a node and open its SRC references.
- Source links are pinned to main commit `42f3a2b8d6bc3e4b847abec1f34cae841fda204d`.
- GitHub's **Architecture site** workflow checks feature branches and uploads `architecture-site-preview`. Download, unzip, then open `index.html`.
- Feature-branch and pull-request runs never deploy. The public URL becomes available after publishing.

## Publish after review

1. Merge the reviewed branch into `main`.
2. Set **Settings → Pages → Build and deployment → Source → GitHub Actions**.
3. If the initial main run happened before Pages was enabled, run **Actions → Architecture site → Run workflow** on `main`.
4. Verify https://bibi45c.github.io/UEA-CMP-SEM2-GAME-Turn_Based_Strategy/
5. In **About → gear icon**, set Website to that URL and Description to the selected wording below.

Selected Description (option 2):

> A Unity 6 tactical RPG with hex-grid combat, scoring-based enemy AI, and data-driven abilities. Includes an interactive architecture map.

Optional Topics: `unity`, `csharp`, `tactical-rpg`, `hex-grid`, `turn-based`, `game-development`.

Description and Website are repository metadata; merging a branch does not set them. This review branch leaves both unchanged until publication is verified.

Only `site/` is deployed, excluding Unity Assets, builds, third-party packs and local configuration. Official Actions are pinned to verified commits. Only the main deploy job receives Pages/OIDC write permissions.

## Update the diagram

Editable source: `Docs/Architecture_Main_Archify.json`. Generated HTML is committed, so deployment does not download or execute Archify. This artifact used Archify `2.17.0-dev.1`.

From the repository root, set `$archifyDir` to your installed skill directory:

```powershell
node "$archifyDir/bin/archify.mjs" validate architecture Docs/Architecture_Main_Archify.json --quality showcase --repo-root . --json
node "$archifyDir/bin/archify.mjs" deliver architecture Docs/Architecture_Main_Archify.json site/index.html --quality showcase --repo-root . --json
node "$archifyDir/bin/archify.mjs" visual-check site/index.html --json
```

Inspect both themes and desktop captures. Copy the accepted light 1440×900 capture to `Docs/Architecture_Preview.png`. Update `Docs/Architecture_Validation.json` from the new delivery and browser receipts, including preview SHA-256. Automated browser checks do not establish perceptual review.

Run `node scripts/check-architecture-site.mjs` before committing. CI verifies JSON/HTML/preview hashes and compares referenced source blobs to the pinned revision. It does not rerun Archify/browser checks or prove that every gameplay change is represented. Reinspect the map whenever runtime responsibilities change.

A source revision stays pinned until explicitly refreshed; the documentation merge commit does not become the evidence revision.

## Scope and rollback

The map covers exploration and tactical combat at module level. Opening cutscenes, office interactions, settings, editor tooling and individual class dependencies are omitted. `GameOutline.md` retains older design wording; current `ActionSystem` uses an AP pool, and the map describes the implementation.

Only documentation and static publishing files change. Unity play-mode checks are not applicable; browser review and branch CI are the acceptance checks.

To withdraw publication, disable Pages in Settings. Revert the feature commit/merge to undo the repository changes. Description and Website must be restored separately if changed.

The generated viewer is distributed with Archify's MIT license in `site/ARCHIFY-LICENSE.txt`. Source: https://github.com/tt-a1i/archify.
