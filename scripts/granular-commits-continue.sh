#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
GIT=(git -c user.name=yPerfectBR -c user.email=kauagabriel571@gmail.com)

commit() {
  local msg="$1"
  shift
  [ $# -eq 0 ] && return 0
  "${GIT[@]}" add "$@"
  if ! "${GIT[@]}" diff --cached --quiet; then
    "${GIT[@]}" commit -m "$msg"
    echo "OK: $msg"
  fi
}

commit "Update creative plugin hint to orion:creative-fillers id." src/Orion/Item/ItemRegistry.cs
commit "Expand PluginLoadOrderTests for manifest v2 scenarios." tests/Orion.Game.Tests/PluginLoadOrderTests.cs
commit "Update PluginConflictsTests for softdepend objects." tests/Orion.Game.Tests/PluginConflictsTests.cs
commit "Update ServicesMessengerTests for manifest v2 types." tests/Orion.Game.Tests/ServicesMessengerTests.cs
commit "Point game tests at external Plugins-Orion project paths." tests/Orion.Game.Tests/Orion.Game.Tests.csproj
commit "Fix InventoryTests imports for OrionContainers namespace." tests/Orion.Game.Tests/InventoryTests.cs
commit "Update ContentRegistriesTests for orion:creative-fillers id." tests/Orion.Game.Tests/ContentRegistriesTests.cs
commit "Ensure ItemRegistry load in GameplaySmokeTests." tests/Orion.Game.Tests/GameplaySmokeTests.cs
commit "Fix creative item index in ItemNetworkStackTests." tests/Orion.Game.Tests/ItemNetworkStackTests.cs
commit "Remove first-party plugins from monorepo (externalized)." plugins/
commit "Remove plugin projects from solution file." OrionServerBE.slnx

for f in docs/en_us/plugins/{09,10,11,12,13,14,15,16,17,18,19,20,21}-*.md; do
  [ -f "$f" ] && commit "Add $(basename "$f" .md) (EN)." "$f"
done

for f in docs/pt_br/plugins/{09,10,11,12,13,14,15,16,17,18,19,20,21}-*.md; do
  [ -f "$f" ] && commit "Add $(basename "$f" .md) (PT)." "$f"
done

commit "Update plugin README hub with phases 19-21 (EN)." docs/en_us/plugins/README.md
commit "Update lifecycle manifest doc for v2 supersession (EN)." docs/en_us/plugins/02-lifecycle-manifest.md
commit "Update first-run guide for external plugins (EN)." docs/en_us/first-run.md
commit "Update vision doc for modular engine direction (EN)." docs/en_us/plugins/00-vision-minimal-engine.md
commit "Update platform checklist for SDK train (EN)." docs/en_us/plugins/08-ai-implementation-checklist.md
commit "Update plugin README hub (PT)." docs/pt_br/plugins/README.md
commit "Update first-run guide for external plugins (PT)." docs/pt_br/first-run.md
commit "Update vision doc (PT)." docs/pt_br/plugins/00-vision-minimal-engine.md
commit "Update platform checklist (PT)." docs/pt_br/plugins/08-ai-implementation-checklist.md

# Split EN SDK doc updates that were modified not added
commit "Update SDK overview with plugin neutrality section (EN)." docs/en_us/plugins/09-sdk-overview.md 2>/dev/null || true
commit "Update gameplay services with replacement policy (EN)." docs/en_us/plugins/14-sdk-gameplay-services.md 2>/dev/null || true
commit "Update dogfood spec for orion:* ids (EN)." docs/en_us/plugins/17-sdk-vanilla-dogfood.md 2>/dev/null || true
commit "Add S0/S10 steps to SDK checklist (EN)." docs/en_us/plugins/18-sdk-ai-implementation-checklist.md 2>/dev/null || true

echo "Remaining unstaged:"
git status --short | wc -l
echo "New commits since script start:"
git log --oneline -45 | head -40
