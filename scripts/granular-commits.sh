#!/usr/bin/env bash
# Granular commits for OrionServerBE — no Co-authored-by trailers.
set -euo pipefail
cd "$(dirname "$0")/.."

GIT="git -c user.name=yPerfectBR -c user.email=kauagabriel571@gmail.com"

commit() {
  local msg="$1"
  shift
  if [ $# -eq 0 ]; then return 0; fi
  $GIT add "$@"
  if $GIT diff --cached --quiet; then return 0; fi
  $GIT commit -m "$msg"
  echo "OK: $msg"
}

# --- Containers core abstraction ---
commit "Add IContainer contract in Orion.Containers namespace." \
  src/Orion/Containers/IContainer.cs

commit "Add ContainerType enum under Orion.Containers." \
  src/Orion/Containers/ContainerType.cs

commit "Remove legacy Container implementation from core." \
  src/Orion/Container/Container.cs

commit "Remove legacy ContainerType from core Container folder." \
  src/Orion/Container/ContainerType.cs

commit "Remove EntityContainer from core entity layer." \
  src/Orion/Entity/Container/EntityContainer.cs

commit "Update Entity to use IContainer from plugin runtime." \
  src/Orion/Entity/Entity.cs

commit "Update EntityTrait for external container integration." \
  src/Orion/Entity/Traits/EntityTrait.cs

# --- Gameplay service interfaces ---
commit "Add neutral IInventoryApi gameplay facade." \
  src/Orion/Gameplay/IInventoryApi.cs

commit "Add neutral IBuildingApi gameplay facade." \
  src/Orion/Gameplay/IBuildingApi.cs

commit "Add neutral IMiningApi gameplay facade." \
  src/Orion/Gameplay/IMiningApi.cs

commit "Add neutral IAttributesApi gameplay facade." \
  src/Orion/Gameplay/IAttributesApi.cs

commit "Remove IVanillaInventoryApi from core." \
  src/Orion/Gameplay/IVanillaInventoryApi.cs

commit "Remove IVanillaBuildingApi from core." \
  src/Orion/Gameplay/IVanillaBuildingApi.cs

commit "Remove IVanillaMiningApi from core." \
  src/Orion/Gameplay/IVanillaMiningApi.cs

commit "Remove IVanillaAttributesApi from core." \
  src/Orion/Gameplay/IVanillaAttributesApi.cs

commit "Align IPlayerInventoryService with neutral inventory API." \
  src/Orion/Gameplay/IPlayerInventoryService.cs

commit "Align IPlayerInventoryAccess with container contract." \
  src/Orion/Gameplay/IPlayerInventoryAccess.cs

# --- Network handlers ---
commit "Use IAttributesApi in SetLocalPlayerAsInitialized handler." \
  src/Orion/Network/Handlers/SetLocalPlayerAsInitialized.cs

commit "Expose SessionLookup for plugin packet handlers." \
  src/Orion/Network/SessionLookup.cs

commit "Expose CreativeInventoryLog for plugin diagnostics." \
  src/Orion/Network/Handlers/CreativeInventoryLog.cs

commit "Update InventoryTransaction handler for plugin inventory." \
  src/Orion/Network/Handlers/InventoryTransaction.cs

# --- Player & commands ---
commit "Update Player for container and inventory plugin integration." \
  src/Orion/Player/Player.cs

commit "Update Clear command for inventory service lookup." \
  src/Orion/Commands/List/Operator/Clear.cs

commit "Format softdepend ids in plugins command output." \
  src/Orion/Commands/List/Operator/Plugins.cs

# --- PluginContracts manifest v2 ---
commit "Add PluginDependency and PluginSoftDependency types." \
  src/PluginContracts/PluginDependency.cs

commit "Update IPluginManifest for object-shaped dependencies." \
  src/PluginContracts/IPluginManifest.cs

# --- Manifest parser & load order ---
commit "Add PluginManifestException with fatal error codes." \
  src/Orion/Plugins/PluginManifestException.cs

commit "Implement plugin.json v2 parser and assembly path resolution." \
  src/Orion/Plugins/PluginManifest.cs

commit "Implement manifest v2 load order with version constraints." \
  src/Orion/Plugins/PluginLoadOrder.cs

# --- Plugin host & diagnostics ---
commit "Remove InternalsVisibleTo entries for first-party plugins." \
  src/Orion/Orion.csproj

commit "Stop sharing Server type via McMaster shared assemblies." \
  src/Orion/Plugins/PluginHost.cs

commit "Route plugin diagnostics to LogCategory.Plugins." \
  src/Orion/Plugins/Diagnostics/PluginDiagnostics.cs

commit "Route plugin messenger warnings to LogCategory.Plugins." \
  src/Orion/Plugins/Messaging/PluginMessenger.cs

commit "Route packet pipeline conflicts to LogCategory.Plugins." \
  src/Orion/Plugins/Network/PacketPipeline.cs

commit "Route registry conflict warnings to LogCategory.Plugins." \
  src/Orion/Plugins/Registry/ContentRegistriesCore.cs

commit "Route creative tab registry logs to LogCategory.Plugins." \
  src/Orion/Plugins/Registry/CreativeTabRegistryFacade.cs

# --- Config & logging ---
commit "Add LogCategory.Plugins to server configuration model." \
  src/Config/OrionConfig.cs

commit "Enable Plugins log category in default server.json." \
  config/server.json

commit "Update creative plugin hint to orion:creative-fillers id." \
  src/Orion/Item/ItemRegistry.cs

# --- Tests: manifest v2 ---
commit "Expand PluginLoadOrderTests for manifest v2 scenarios." \
  tests/Orion.Game.Tests/PluginLoadOrderTests.cs

commit "Update PluginConflictsTests for softdepend objects." \
  tests/Orion.Game.Tests/PluginConflictsTests.cs

commit "Update ServicesMessengerTests for manifest v2 types." \
  tests/Orion.Game.Tests/ServicesMessengerTests.cs

commit "Point game tests at external Plugins-Orion project paths." \
  tests/Orion.Game.Tests/Orion.Game.Tests.csproj

commit "Fix InventoryTests imports for OrionContainers namespace." \
  tests/Orion.Game.Tests/InventoryTests.cs

commit "Update ContentRegistriesTests for orion:creative-fillers id." \
  tests/Orion.Game.Tests/ContentRegistriesTests.cs

commit "Ensure ItemRegistry load in GameplaySmokeTests." \
  tests/Orion.Game.Tests/GameplaySmokeTests.cs

commit "Fix creative item index in ItemNetworkStackTests." \
  tests/Orion.Game.Tests/ItemNetworkStackTests.cs

# --- Remove in-tree plugins (moved to Plugins-Orion) ---
commit "Remove first-party plugins from monorepo (externalized)." \
  plugins/

# --- Solution ---
commit "Remove plugin projects from solution file." \
  OrionServerBE.slnx

# --- Docs EN: SDK train ---
commit "Add SDK overview documentation (phase 09, EN)." \
  docs/en_us/plugins/09-sdk-overview.md

commit "Add SDK packages and versioning spec (phase 10, EN)." \
  docs/en_us/plugins/10-sdk-packages-versioning.md

commit "Add Orion.Api surface spec (phase 11, EN)." \
  docs/en_us/plugins/11-sdk-orion-api-surface.md

commit "Add registries and traits SDK spec (phase 12, EN)." \
  docs/en_us/plugins/12-sdk-registries-traits.md

commit "Add events and signals SDK spec (phase 13, EN)." \
  docs/en_us/plugins/13-sdk-events-signals.md

commit "Add gameplay services SDK spec (phase 14, EN)." \
  docs/en_us/plugins/14-sdk-gameplay-services.md

commit "Add protocol escape hatch spec (phase 15, EN)." \
  docs/en_us/plugins/15-sdk-protocol-escape.md

commit "Add external plugin guide (phase 16, EN)." \
  docs/en_us/plugins/16-sdk-external-plugin-guide.md

commit "Add vanilla dogfood migration spec (phase 17, EN)." \
  docs/en_us/plugins/17-sdk-vanilla-dogfood.md

commit "Add SDK AI implementation checklist (phase 18, EN)." \
  docs/en_us/plugins/18-sdk-ai-implementation-checklist.md

commit "Add manifest v2 specification (phase 19, EN)." \
  docs/en_us/plugins/19-manifest-v2.md

commit "Add plugin developer guide (phase 20, EN)." \
  docs/en_us/plugins/20-plugin-developer-guide.md

commit "Add plugin repo layout guide (phase 21, EN)." \
  docs/en_us/plugins/21-plugin-repo-layout.md

# --- Docs PT: SDK train ---
commit "Add SDK overview documentation (phase 09, PT)." \
  docs/pt_br/plugins/09-sdk-overview.md

commit "Add SDK packages spec (phase 10, PT)." \
  docs/pt_br/plugins/10-sdk-packages-versioning.md

commit "Add Orion.Api surface spec (phase 11, PT)." \
  docs/pt_br/plugins/11-sdk-orion-api-surface.md

commit "Add registries SDK spec (phase 12, PT)." \
  docs/pt_br/plugins/12-sdk-registries-traits.md

commit "Add events SDK spec (phase 13, PT)." \
  docs/pt_br/plugins/13-sdk-events-signals.md

commit "Add gameplay services SDK spec (phase 14, PT)." \
  docs/pt_br/plugins/14-sdk-gameplay-services.md

commit "Add protocol escape spec (phase 15, PT)." \
  docs/pt_br/plugins/15-sdk-protocol-escape.md

commit "Add external plugin guide (phase 16, PT)." \
  docs/pt_br/plugins/16-sdk-external-plugin-guide.md

commit "Add dogfood spec (phase 17, PT)." \
  docs/pt_br/plugins/17-sdk-vanilla-dogfood.md

commit "Add SDK checklist (phase 18, PT)." \
  docs/pt_br/plugins/18-sdk-ai-implementation-checklist.md

commit "Add manifest v2 spec (phase 19, PT)." \
  docs/pt_br/plugins/19-manifest-v2.md

commit "Add developer guide (phase 20, PT)." \
  docs/pt_br/plugins/20-plugin-developer-guide.md

commit "Add repo layout guide (phase 21, PT)." \
  docs/pt_br/plugins/21-plugin-repo-layout.md

# --- Docs updates ---
commit "Update plugin README hub with phases 19-21 (EN)." \
  docs/en_us/plugins/README.md

commit "Update lifecycle manifest doc for v2 supersession (EN)." \
  docs/en_us/plugins/02-lifecycle-manifest.md

commit "Update first-run guide for external plugins (EN)." \
  docs/en_us/first-run.md

commit "Update vision doc for modular engine direction (EN)." \
  docs/en_us/plugins/00-vision-minimal-engine.md

commit "Update platform checklist for SDK train (EN)." \
  docs/en_us/plugins/08-ai-implementation-checklist.md

commit "Update plugin README hub (PT)." \
  docs/pt_br/plugins/README.md

commit "Update first-run guide for external plugins (PT)." \
  docs/pt_br/first-run.md

commit "Update vision doc (PT)." \
  docs/pt_br/plugins/00-vision-minimal-engine.md

commit "Update platform checklist (PT)." \
  docs/pt_br/plugins/08-ai-implementation-checklist.md

echo "---"
$GIT log --oneline -60 | wc -l
echo "commits created (showing recent):"
$GIT log --oneline -15
