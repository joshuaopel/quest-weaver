#!/usr/bin/env python3
"""Build QuestWeaver.unitypackage from unity/QuestWeaver/ — no Unity needed.

A .unitypackage is a gzipped tar: one folder per asset GUID containing
  pathname   the project-relative path (e.g. Assets/QuestWeaver/...)
  asset      the file bytes (omitted for folders)
  asset.meta the Unity .meta file
GUIDs here are deterministic (md5 of the path) so re-imports update in place
instead of duplicating.

Usage: python3 unity/build_unitypackage.py   (from the repo root or unity/)
"""

import hashlib, io, os, tarfile, time

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "QuestWeaver")
OUT = os.path.join(HERE, "QuestWeaver.unitypackage")

META_FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_SCRIPT = """fileFormatVersion: 2
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

META_TEXT = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid_for(path):            # stable across rebuilds
    return hashlib.md5(("questweaver:" + path).encode()).hexdigest()


def meta_for(path, is_dir):
    g = guid_for(path)
    if is_dir:
        return META_FOLDER.format(guid=g)
    if path.endswith(".cs"):
        return META_SCRIPT.format(guid=g)
    return META_TEXT.format(guid=g)


def add_bytes(tar, name, data):
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mtime = int(time.time())
    info.mode = 0o644
    info.uname = info.gname = ""
    tar.addfile(info, io.BytesIO(data))


def add_dir(tar, name):
    info = tarfile.TarInfo(name)
    info.type = tarfile.DIRTYPE
    info.mode = 0o755
    info.mtime = int(time.time())
    info.uname = info.gname = ""
    tar.addfile(info)


def add_asset(tar, project_path, file_path=None):
    # mirror the layout of packages Unity itself exports:
    # ./<guid>/ (dir), ./<guid>/pathname, ./<guid>/asset.meta, ./<guid>/asset
    is_dir = file_path is None
    g = guid_for(project_path)
    add_dir(tar, f"./{g}/")
    add_bytes(tar, f"./{g}/pathname", project_path.encode())
    add_bytes(tar, f"./{g}/asset.meta", meta_for(project_path, is_dir).encode())
    if not is_dir:
        with open(file_path, "rb") as f:
            add_bytes(tar, f"./{g}/asset", f.read())


def main():
    entries = []                                   # (project_path, file_path|None)
    entries.append(("Assets/QuestWeaver", None))
    for root, dirs, files in os.walk(SRC):
        rel_root = os.path.relpath(root, SRC).replace("\\", "/")
        base = "Assets/QuestWeaver" if rel_root == "." else f"Assets/QuestWeaver/{rel_root}"
        for d in sorted(dirs):
            entries.append((f"{base}/{d}", None))
        for f in sorted(files):
            if f.endswith(".meta"):
                continue
            entries.append((f"{base}/{f}", os.path.join(root, f)))

    with tarfile.open(OUT, "w:gz", format=tarfile.GNU_FORMAT) as tar:
        add_dir(tar, "./")
        for project_path, file_path in entries:
            add_asset(tar, project_path, file_path)

    n = sum(1 for _, fp in entries if fp)
    print(f"wrote {OUT} ({os.path.getsize(OUT)} bytes, {n} assets)")


if __name__ == "__main__":
    main()
