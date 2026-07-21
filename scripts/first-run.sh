#!/usr/bin/env bash
# OrionServerBE — first-run bootstrap: check toolchain, optional install, create local dirs/config.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOTNET_CHANNEL="10.0"
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
REQUIRED_SDK_MAJOR=10
NOCONFIRM=false

usage() {
  cat <<'EOF'
Uso: ./first-run.sh [opções]

Verifica dependências para compilar e rodar o OrionServer, oferece instalação
automática quando algo estiver faltando e cria pastas/config locais.

Opções:
  -y, --noconfirm, --yes   Instala dependências sem perguntar
  -h, --help               Mostra esta ajuda

Pastas criadas (se não existirem): config/, plugins/, resource_packs/, worlds/, logs/
Arquivos de config criados apenas se ausentes: config/server.json, config/permissions.json
EOF
}

for arg in "$@"; do
  case "$arg" in
    -y|--noconfirm|--yes) NOCONFIRM=true ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Opção desconhecida: $arg" >&2; usage >&2; exit 1 ;;
  esac
done

info()  { printf '→ %s\n' "$*"; }
ok()    { printf '✓ %s\n' "$*"; }
warn()  { printf '! %s\n' "$*" >&2; }

confirm() {
  local prompt="$1"
  if $NOCONFIRM; then
    return 0
  fi
  local answer
  read -rp "$prompt [S/n] " answer
  case "${answer:-S}" in
    S|s|Y|y|"") return 0 ;;
    *) return 1 ;;
  esac
}

have_cmd() {
  command -v "$1" >/dev/null 2>&1
}

detect_pkg_mgr() {
  if have_cmd apt-get; then echo apt
  elif have_cmd pacman; then echo pacman
  elif have_cmd dnf; then echo dnf
  elif have_cmd zypper; then echo zypper
  elif have_cmd apk; then echo apk
  elif have_cmd brew; then echo brew
  else echo none
  fi
}

ensure_path_has_dotnet() {
  if [[ -x "$DOTNET_INSTALL_DIR/dotnet" ]]; then
    export PATH="$DOTNET_INSTALL_DIR:$PATH"
    export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
  fi
}

dotnet_sdk_ok() {
  ensure_path_has_dotnet
  if ! have_cmd dotnet; then
    return 1
  fi
  dotnet --list-sdks 2>/dev/null | awk '{print $1}' | grep -q "^${REQUIRED_SDK_MAJOR}\."
}

install_dotnet_sdk() {
  info "Instalando .NET SDK ${DOTNET_CHANNEL} em $DOTNET_INSTALL_DIR ..."
  mkdir -p "$DOTNET_INSTALL_DIR"
  local installer=/tmp/dotnet-install.sh
  if have_cmd curl; then
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$installer"
  elif have_cmd wget; then
    wget -qO "$installer" https://dot.net/v1/dotnet-install.sh
  else
    warn "curl ou wget é necessário para baixar o instalador do .NET."
    return 1
  fi
  chmod +x "$installer"
  bash "$installer" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_INSTALL_DIR"
  rm -f "$installer"
  ensure_path_has_dotnet
  if ! dotnet_sdk_ok; then
    warn "Instalação do .NET concluída, mas SDK ${REQUIRED_SDK_MAJOR}.x não foi detectado."
    warn "Adicione ao seu shell: export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\""
    return 1
  fi
  ok ".NET SDK instalado: $(dotnet --version)"
  if ! grep -q "$DOTNET_INSTALL_DIR" "$HOME/.bashrc" 2>/dev/null; then
    warn "Para persistir o PATH, adicione em ~/.bashrc ou ~/.zshrc:"
    warn "  export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\""
    warn "  export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\""
  fi
}

install_with_pkg_mgr() {
  local pkg_mgr="$1"
  shift
  local packages=("$@")
  case "$pkg_mgr" in
    apt)
      sudo apt-get update -qq
      sudo apt-get install -y "${packages[@]}"
      ;;
    pacman)
      sudo pacman -S --needed --noconfirm "${packages[@]}"
      ;;
    dnf)
      sudo dnf install -y "${packages[@]}"
      ;;
    zypper)
      sudo zypper install -y "${packages[@]}"
      ;;
    apk)
      sudo apk add "${packages[@]}"
      ;;
    brew)
      brew install "${packages[@]}"
      ;;
    *)
      return 1
      ;;
  esac
}

ensure_command() {
  local cmd="$1"
  local label="$2"
  shift 2
  local -a pkg_names=("$@")

  if have_cmd "$cmd"; then
    ok "$label disponível ($cmd)"
    return 0
  fi

  warn "$label não encontrado ($cmd)."
  local pkg_mgr
  pkg_mgr="$(detect_pkg_mgr)"
  if [[ "$pkg_mgr" == none ]]; then
    warn "Nenhum gerenciador de pacotes suportado detectado. Instale $label manualmente."
    return 1
  fi

  if ! confirm "Instalar $label via $pkg_mgr (${pkg_names[*]})?"; then
    warn "$label não instalado — algumas etapas podem falhar."
    return 1
  fi

  install_with_pkg_mgr "$pkg_mgr" "${pkg_names[@]}"
  if have_cmd "$cmd"; then
    ok "$label instalado."
  else
    warn "Não foi possível instalar $label automaticamente."
    return 1
  fi
}

check_toolchain() {
  echo
  echo "╔══════════════════════════════════════════╗"
  echo "║     OrionServer — Verificação de stack   ║"
  echo "╚══════════════════════════════════════════╝"
  echo

  local pkg_mgr
  pkg_mgr="$(detect_pkg_mgr)"
  info "Gerenciador de pacotes detectado: ${pkg_mgr:-nenhum}"

  case "$pkg_mgr" in
    apt)    ensure_command curl  "curl"  curl ;;
    pacman) ensure_command curl  "curl"  curl ;;
    dnf)    ensure_command curl  "curl"  curl ;;
    zypper) ensure_command curl  "curl"  curl ;;
    apk)    ensure_command curl  "curl"  curl ;;
    brew)   ensure_command curl  "curl"  curl ;;
    none)   have_cmd curl || have_cmd wget || warn "curl/wget ausente — instalação automática do .NET pode falhar." ;;
  esac

  ensure_command git "Git" git || true

  ensure_path_has_dotnet
  if dotnet_sdk_ok; then
    ok ".NET SDK disponível ($(dotnet --version))"
  else
    warn ".NET SDK ${REQUIRED_SDK_MAJOR}.x não encontrado."
    if confirm "Baixar e instalar .NET SDK ${DOTNET_CHANNEL} automaticamente?"; then
      install_dotnet_sdk
    else
      warn "Sem .NET SDK ${REQUIRED_SDK_MAJOR}.x não é possível compilar o OrionServer."
      exit 1
    fi
  fi
}

create_layout() {
  echo
  info "Criando estrutura local ..."
  mkdir -p "$ROOT/config" "$ROOT/plugins" "$ROOT/resource_packs" "$ROOT/worlds" "$ROOT/logs"

  if [[ ! -f "$ROOT/config/permissions.json" ]]; then
    cp "$SCRIPT_DIR/fixtures/permissions.json" "$ROOT/config/permissions.json"
    ok "config/permissions.json criado (operators vazio)"
  else
    info "config/permissions.json já existe — mantido"
  fi

  if [[ ! -f "$ROOT/config/server.json" ]]; then
    cp "$SCRIPT_DIR/fixtures/server.json" "$ROOT/config/server.json"
    ok "config/server.json criado (sem pregen / threadingAreas)"
  else
    info "config/server.json já existe — mantido"
  fi

  ok "Pastas prontas: config/, plugins/, resource_packs/, worlds/, logs/"
}

print_thanks() {
  cat <<'EOF'

╔══════════════════════════════════════════╗
║           Obrigado por usar o            ║
║              OrionServer!                ║
╚══════════════════════════════════════════╝

Próximos passos:
  1. dotnet build src/Orion/Orion.csproj
  2. Coloque plugins em plugins/ (cada pasta com plugin.json + DLL)

Para mecânicas vanilla básicas (inventário, blocos, mineração, atributos, etc.),
use os plugins oficiais publicados na organização OrionBedrock:

  https://github.com/OrionBedrock

Repositórios recomendados:
  • orion-containers, orion-inventory, orion-block-containers
  • orion-attributes, orion-building, orion-mining, orion-minimal-items

Clone os repos individuais e compile com dotnet build, ou use o repositório
local Plugins-Orion com build-plugins.sh para copiar as DLLs para plugins/.

Documentação: docs/pt_br/first-run.md
EOF
}

main() {
  check_toolchain
  create_layout
  print_thanks
}

main "$@"
