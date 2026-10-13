"""Attach the D-DAY runtime to the user-created scene through UnitySkills.

The scene is intentionally saved by the user in Unity Editor (Auto mode blocks
scene_save). This script never deletes or overwrites other scene objects.
"""
import json
import urllib.request

BASE = "http://localhost:8090"
SCENE = "Assets/Scenes/TodayWhereToSit.unity"


def call(skill, args=None, dry=False):
    url = BASE + "/skill/" + skill + ("?mode=dryRun" if dry else "")
    request = urllib.request.Request(url,
        json.dumps(args or {}, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(request, timeout=120) as response:
        result = json.load(response)
    if dry:
        if not result.get("valid") or not result.get("authorization", {}).get("allowed"):
            raise RuntimeError((skill, result))
    elif result.get("status") != "success" or result.get("result", {}).get("failCount", 0):
        raise RuntimeError((skill, result))
    return result


def main():
    scene = call("scene_get_info")["result"]
    if scene["scenePath"] != SCENE:
        raise RuntimeError("Open " + SCENE + " first; active=" + scene["scenePath"])

    sprites = ["WolfIdle", "FoxIdle", "ShibaIdle", "PandaIdle", "CoffeeMachine"]
    batch = {"items": json.dumps([{
        "assetPath": "Assets/Resources/DDay/Pixel/" + name + ".png",
        "textureType": "Sprite", "filterMode": "Point",
        "compression": "Uncompressed", "mipmapEnabled": False,
        "alphaIsTransparency": True, "spritePixelsPerUnit": 24,
    } for name in sprites])}
    call("texture_set_settings_batch", batch, dry=True)
    print("import:", call("texture_set_settings_batch", batch)["result"].get("successCount"))

    found = call("gameobject_find", {"name": "D-DAY", "limit": 10})["result"]
    if not any(obj["name"] == "D-DAY" for obj in found.get("objects", [])):
        args = {"name": "D-DAY"}
        call("gameobject_create", args, dry=True)
        print("create:", call("gameobject_create", args)["result"]["path"])
    target = {"name": "D-DAY", "componentType": "DDay.DDayGame"}
    components = call("component_list", {"name": "D-DAY"})["result"]
    existing = str(components)
    if "DDayGame" not in existing:
        call("component_add", target, dry=True)
        print("component:", call("component_add", target)["result"])
    print("scene:", call("scene_get_info")["result"])


if __name__ == "__main__":
    main()
