"""Read-only delivery audit against source manifests and saved runtime evidence."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parents[1]
ASSETS = PROJECT / "Assets/Studies/BoxOrVoid"
def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

levels = read(ROOT / "Research/Levels.json")
resources = read(ROOT / "Research/AssetSources.json")
level_checks = [{"id": item["id"], "match": digest(ASSETS / "Content/Levels" / (item["id"] + ".json")) == item["sha256"] == digest(Path(item["source"]))} for item in levels]
resource_checks = [{"file": item["output"], "match": digest(ASSETS / item["output"]) == item["sha256"]} for item in resources]
run = read(ROOT / "Evidence/Creator/Campaign/report.json")
smoke = read(ROOT / "Evidence/Creator/CampaignSmoke/report.json")
assemblies = read(ROOT / "Evidence/Creator/Campaign/assemblies.json")
creator = [read(ROOT / "Evidence/Creator" / folder / "flow-report.json") for folder in ("Windows", "Windows1920")]
def binaries_match(folder, core_only=False):
    items = read(ROOT / "Evidence/Creator" / folder / "assemblies.json")
    return all(digest(ROOT / "Builds/Windows/BoxOrVoidStudy_Data/Managed" / item["file"]).upper() == item["Hash"] for item in items if not core_only or item["file"] == "BoxOrVoid.Core.dll")
scenes = list(ASSETS.rglob("*.unity"))
checks = {
    "21_original_level_files_match": len(level_checks) == 21 and all(x["match"] for x in level_checks),
    "all_extracted_presentation_assets_match": all(x["match"] for x in resource_checks),
    "one_shared_scene": len(scenes) == 1,
    "no_reference_dll_in_module": not list(ASSETS.rglob("*.dll")),
    "21_windows_replays_reset_undo": run["status"] == "passed" and len(run["levels"]) == 21 and all(x["solved"] and x["resetUndo"] and x["fullUndo"] for x in run["levels"]),
    "final_build_flow_passed": smoke["status"] == "passed" and smoke["flowPassed"] and smoke["midMoveUndoPassed"] and not smoke["errors"],
    "iris_and_authored_title_passed": smoke["transitionPassed"] and smoke["authoredMenuReused"],
    "final_core_matches_21_replay_build": binaries_match("Campaign", core_only=True),
    "final_runtime_matches_smoke_build": binaries_match("CampaignSmoke"),
    "creator_roundtrips_passed_two_resolutions": all(r["status"] == "passed" and r["actualGameplaySolved"] and r["progressIsolated"] for r in creator) and [(r["screenWidth"], r["screenHeight"]) for r in creator] == [(1280, 720), (1920, 1080)],
    "final_runtime_matches_creator_builds": binaries_match("Windows") and binaries_match("Windows1920"),
    "build_success": read(ROOT / "Evidence/Build/windows-build.json")["result"] == "Succeeded",
    "78_editmode_tests_passed": read(ROOT / "Evidence/Creator/editmode-78.json")["data"]["result"]["summary"]["passed"] == 78,
    "workbench_single_scene_preview": read(ROOT / "Evidence/Runtime/workbench.json")["sourceUnchanged"] and read(ROOT / "Evidence/Runtime/workbench.json")["sceneCount"] == 1,
    "reference_sequence_passed": read(ROOT / "Evidence/Runtime/n_tut_2_input_report.json")["status"] == "passed",
    "actual_gameplay_movie_present": (ROOT / "Evidence/WindowsRun03/Box06_gameplay.mp4").is_file(),
}
result = {"passed": all(checks.values()), "checks": checks, "levelCount": len(level_checks), "presentationAssetCount": len(resource_checks), "scene": str(scenes[0].relative_to(PROJECT)), "levelChecks": level_checks, "resourceFailures": [x for x in resource_checks if not x["match"]]}
(ROOT / "Evidence/Creator/delivery-audit.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({"passed": result["passed"], "checks": checks}, ensure_ascii=False, indent=2))
raise SystemExit(0 if result["passed"] else 1)
