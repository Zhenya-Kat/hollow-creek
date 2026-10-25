"""Package successful standalone builds without Unity debug/backup folders."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Builds" / "Release"
VERSION = re.search(r"^  bundleVersion: (.+)$", (ROOT / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8"), re.M)[1].strip()


def files(platform):
    base = OUT / platform
    report = json.loads((base / "build-report.json").read_text(encoding="utf-8"))
    if report["result"] != "Succeeded" or report["errors"]:
        raise RuntimeError(f"{platform} did not build successfully")
    for path in sorted(base.rglob("*")):
        if not path.is_file():
            continue
        if any("DoNotShip" in p or "DontShip" in p for p in path.parts):
            continue
        if path.name in {"build-report.json", "build-error.txt"}:
            continue
        yield path, path.relative_to(base)


def main():
    instructions = (
        f"Hollow Creek: Ночь масок — {VERSION}\n\n"
        "Windows: распакуйте весь архив и запустите HollowCreek.exe.\n"
        "Linux x86-64: распакуйте tar.gz, запустите ./HollowCreek.x86_64.\n"
        "Сохраняйте папку HollowCreek_Data и остальные файлы рядом с игрой.\n\n"
        "WASD — ходьба, Shift — бег, мышь — осмотреться, E/ЛКМ — действие,\n"
        "Tab/J — дневник, Esc — пауза/назад. Поддерживается геймпад.\n"
        "Исходники: https://github.com/Zhenya-Kat/hollow-creek\n"
    )
    for platform in ("Windows", "Linux"):
        (OUT / platform / "README.txt").write_text(instructions, encoding="utf-8")
        shutil.copyfile(ROOT / "docs" / "CREDITS.md", OUT / platform / "CREDITS.md")
    archives = []
    windows = OUT / f"HollowCreek-{VERSION}-Windows-x86_64.zip"
    with zipfile.ZipFile(windows, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for source, relative in files("Windows"):
            archive.write(source, Path(f"HollowCreek-{VERSION}-Windows") / relative)
    archives.append(windows)
    linux = OUT / f"HollowCreek-{VERSION}-Linux-x86_64.tar.gz"
    with tarfile.open(linux, "w:gz", compresslevel=6) as archive:
        for source, relative in files("Linux"):
            info = archive.gettarinfo(str(source), arcname=str(Path(f"HollowCreek-{VERSION}-Linux") / relative))
            info.uid = info.gid = 0
            info.uname = info.gname = ""
            info.mode = 0o755 if source.name in {"HollowCreek.x86_64", "UnityCrashHandler64"} else 0o644
            with source.open("rb") as stream:
                archive.addfile(info, stream)
    archives.append(linux)
    sums = []
    for archive in archives:
        with archive.open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        sums.append(f"{digest}  {archive.name}")
        print(f"{archive.name}: {archive.stat().st_size:,} bytes")
    (OUT / "SHA256SUMS.txt").write_text("\n".join(sums) + "\n", encoding="ascii")
    # Validate archive structure and CRCs, including the Linux executable permission.
    with zipfile.ZipFile(windows) as archive:
        assert archive.testzip() is None
        assert any(n.endswith("/HollowCreek.exe") for n in archive.namelist())
        assert any("StreamingAssets/aa/" in n for n in archive.namelist())
    with tarfile.open(linux) as archive:
        members = archive.getmembers()
        executable = next(m for m in members if m.name.endswith("/HollowCreek.x86_64"))
        assert executable.mode & 0o111
        assert any("StreamingAssets/aa/" in m.name for m in members)
    print("Archive verification passed")


if __name__ == "__main__":
    main()
