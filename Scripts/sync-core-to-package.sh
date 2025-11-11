#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PACKAGE_ROOT="${REPO_ROOT}/Packages/com.arandra.gsheet-to-data"
PACKAGE_RUNTIME="${PACKAGE_ROOT}/Runtime"
PACKAGE_EDITOR="${PACKAGE_ROOT}/Editor"
MANIFEST_PATH="${PACKAGE_ROOT}/.sync-manifest.json"
SRC_CORE="${REPO_ROOT}/Core/GSheetToDataCore"
SRC_SERIALIZABLE="${REPO_ROOT}/Core/SerializableTypes"

if [[ ! -d "${SRC_CORE}" || ! -d "${SRC_SERIALIZABLE}" ]]; then
  echo "Core source folders not found. Did you init/update the submodules?" >&2
  exit 1
fi

mkdir -p "${PACKAGE_RUNTIME}"
mkdir -p "${PACKAGE_EDITOR}"

copy_tree() {
  local src="$1"
  local dst="$2"

  echo "Syncing ${src} -> ${dst}"
  mkdir -p "${dst}"

  if command -v rsync >/dev/null 2>&1; then
    rsync -a \
      --exclude '.git/' \
      --exclude '.vs/' \
      --exclude 'bin/' \
      --exclude 'obj/' \
      --exclude '*.user' \
      --exclude '*.csproj.nuget.*' \
      "${src}/" "${dst}/"
  else
    cp -R "${src}/." "${dst}"
    rm -rf "${dst}/.git" "${dst}/.vs" "${dst}/bin" "${dst}/obj"
    find "${dst}" -name '*.user' -delete || true
    find "${dst}" -name '*.csproj.nuget.*' -delete || true
  fi
}

copy_tree "${SRC_SERIALIZABLE}" "${PACKAGE_RUNTIME}/SerializableTypes"
copy_tree "${SRC_CORE}" "${PACKAGE_EDITOR}/GSheetToDataCore"

if [[ -d "${PACKAGE_RUNTIME}/GSheetToDataCore" ]]; then
  echo "Removing legacy Runtime/GSheetToDataCore copy"
  rm -rf "${PACKAGE_RUNTIME}/GSheetToDataCore"
fi

PYTHON_BIN="${PYTHON_BIN:-python3}"
if ! command -v "${PYTHON_BIN}" >/dev/null 2>&1; then
  PYTHON_BIN="python"
fi

if command -v "${PYTHON_BIN}" >/dev/null 2>&1; then
  "${PYTHON_BIN}" <<PY
import hashlib
import json
import pathlib

manifest_path = pathlib.Path("${MANIFEST_PATH}")
package_root = manifest_path.parent
pairs = [
    ("Runtime/SerializableTypes", pathlib.Path("${SRC_SERIALIZABLE}")),
    ("Editor/GSheetToDataCore", pathlib.Path("${SRC_CORE}")),
]

def list_dest():
    output = []
    for dest_rel, _ in pairs:
        dest_path = package_root / dest_rel
        if not dest_path.exists():
            continue
        for file_path in dest_path.rglob("*"):
            if file_path.is_file() and not file_path.name.endswith(".meta"):
                output.append(file_path.relative_to(package_root).as_posix())
    return output

old = []
if manifest_path.exists():
    data = json.loads(manifest_path.read_text())
    if isinstance(data, list):
        old = data
    else:
        old = data.get("files", [])
else:
    old = list_dest()

current = []
for dest_rel, src_path in pairs:
    if not src_path.exists():
        continue
    for file_path in src_path.rglob("*"):
        if file_path.is_file() and not file_path.name.endswith(".meta"):
            rel_inside = file_path.relative_to(src_path).as_posix()
            current.append(f"{dest_rel}/{rel_inside}")

old_set = set(old)
current_set = set(current)
removed = sorted(old_set - current_set)

for rel in removed:
    path = package_root / rel
    if path.exists():
        path.unlink()
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        meta.unlink()

manifest_path.write_text(json.dumps(sorted(current_set), indent=2) + "\n")

def guid_for(rel):
    return hashlib.md5(rel.encode("utf-8")).hexdigest()

def write_meta(meta_path, content):
    meta_path.parent.mkdir(parents=True, exist_ok=True)
    meta_path.write_text(content)

def ensure_file_meta(asset_path, rel):
    meta_path = asset_path.with_name(asset_path.name + ".meta")
    if meta_path.exists():
        return
    guid = guid_for(rel)
    if asset_path.suffix == ".asmdef":
        template = f"""fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    elif asset_path.suffix == ".cs":
        template = f"""fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    else:
        template = f"""fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    write_meta(meta_path, template)

def ensure_folder_meta(folder_path, rel):
    meta_path = folder_path.with_name(folder_path.name + ".meta")
    if meta_path.exists():
        return
    guid = guid_for(rel)
    template = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    write_meta(meta_path, template)

for dest_rel, _ in pairs:
    dest_path = package_root / dest_rel
    if not dest_path.exists():
        continue
    ensure_folder_meta(dest_path, dest_path.relative_to(package_root).as_posix())
    for folder in sorted([p for p in dest_path.rglob("*") if p.is_dir()]):
        rel = folder.relative_to(package_root).as_posix()
        ensure_folder_meta(folder, rel)
    for file_path in dest_path.rglob("*"):
        if not file_path.is_file() or file_path.name.endswith(".meta"):
            continue
        rel = file_path.relative_to(package_root).as_posix()
        ensure_file_meta(file_path, rel)

for root_name in ("Editor", "Runtime"):
    root_path = package_root / root_name
    if root_path.exists():
        ensure_folder_meta(root_path, root_name)
PY
else
  echo "Warning: python interpreter not found; skipping deletion tracking"
fi

echo "Done. SerializableTypes synced to Runtime/, GSheetToDataCore synced to Editor/."
