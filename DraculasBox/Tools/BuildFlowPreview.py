"""Encode Player-captured completion frames using their actual capture timestamps."""
import argparse
import json
import pathlib
import subprocess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder", type=pathlib.Path)
    parser.add_argument("--ffmpeg", default="ffmpeg")
    args = parser.parse_args()
    folder = args.folder.resolve()
    report = json.loads((folder / "report.json").read_text(encoding="utf-8-sig"))
    times = report["completionTimes"]
    if report["status"] != "passed" or len(times) < 2:
        raise SystemExit("A passing Player replay with captured completion frames is required.")
    lines = ["ffconcat version 1.0"]
    for i, time in enumerate(times):
        name = f"completion_{i:02d}.png"
        if not (folder / name).is_file():
            raise SystemExit(f"Missing frame: {name}")
        duration = times[i + 1] - time if i + 1 < len(times) else .15
        lines += [f"file '{name}'", f"duration {max(.001, duration):.6f}"]
    lines.append(f"file 'completion_{len(times)-1:02d}.png'")
    (folder / "completion.ffconcat").write_text("\n".join(lines)+"\n", encoding="utf-8")
    subprocess.run([str(pathlib.Path(args.ffmpeg).resolve()) if pathlib.Path(args.ffmpeg).is_file() else args.ffmpeg,
                    "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", "completion.ffconcat",
                    "-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-crf", "18", "-movflags", "+faststart",
                    "WinFlow.mp4"], cwd=folder, check=True)
    print(json.dumps({"output": str(folder / "WinFlow.mp4"), "frames": len(times),
                      "capturedSeconds": round(times[-1] - times[0], 3), "audio": False}))


if __name__ == "__main__":
    main()
