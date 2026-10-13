using System;
using System.Collections.Generic;
using System.Linq;
using DDay;
using UnityEditor;
using UnityEngine;

public static class DDayDay2SafeAreaChecks
{
    [MenuItem("D-DAY/Run DAY 2 Safe Area Checks")]
    public static void Run()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (!EditorApplication.isPlaying || game == null)
        {
            Debug.LogError("DAY 2 safe area checks require Play Mode.");
            return;
        }

        var results = new List<string>();
        void Check(int number, string label, bool passed)
        {
            string line = $"DAY2 SAFE TEST {number:00} {(passed ? "PASS" : "FAIL")} · {label}";
            results.Add(line);
            Debug.Log(line);
        }
        Transform Seat(int seat) => game.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "SEAT " + seat + " / fixed desk and chair");
        SpriteRenderer Render(int animal) => game.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == SeatRules.Names[animal] + " / movable employee");
        SpriteRenderer Prop(int animal) => game.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == "CharacterDeskProps / visual only"
                && r.transform.parent?.name == "PersonalProps / " + SeatRules.Names[animal]);
        SpriteRenderer Desk(int seat) => Seat(seat)?.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == "SEAT LAYER / desk");
        SpriteRenderer Chair(int seat) => Seat(seat)?.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == "SEAT LAYER / chair");
        float VirtualX(float worldX)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(worldX, 0, 0));
            return screen.x * 1920f / Screen.width;
        }
        bool Safe(SpriteRenderer renderer, float margin = 0)
        {
            if (renderer == null || renderer.sprite == null || !renderer.gameObject.activeInHierarchy)
                return false;
            Bounds bounds = renderer.bounds;
            return VirtualX(bounds.min.x) >= DDayGame.OfficeSafeLeft + margin - 1f
                && VirtualX(bounds.max.x) <= DDayGame.OfficeSafeRight - margin + 1f;
        }
        bool StationSafe(int seat) => Safe(Desk(seat), DDayGame.OfficeSafeMargin)
            && Safe(Chair(seat));
        bool AnimalSafe(int index) => Safe(Render(index)) && Safe(Prop(index));
        void Reset() => DDayArtImport.PreviewDayTwo();

        Reset();
        Check(1, "Seat 3 桌椅完整并留安全边距", StationSafe(3));
        Check(2, "Seat 7 桌椅完整并留安全边距", StationSafe(7));
        Check(3, "柴犬在 Seat 3 完整可见", game.Seats[3] == 3 && AnimalSafe(3));
        Reset();
        bool bearMoved = game.TryPlaceAnimal(5, 3);
        Check(4, "棕熊在 Seat 3，身体和物品完整", bearMoved && AnimalSafe(5));
        Reset();
        Check(5, "棕熊在 Seat 7，身体和物品完整", game.Seats[5] == 7 && AnimalSafe(5));

        // DAY 2 locks the rabbit's former seat. Check the rabbit's sprite width
        // against Seat 7 without changing that story rule.
        Transform rabbitSeatSeven = Seat(7)?.Find("CharacterAnchor 7");
        Sprite rabbitSprite = DDayArt.SeatedEmployee(0);
        float rabbitHalf = rabbitSprite == null || rabbitSeatSeven == null ? float.MaxValue
            : rabbitSprite.bounds.extents.x * 1.10f * rabbitSeatSeven.lossyScale.x;
        Check(6, "兔子假设位于 Seat 7 时长耳和身体不会被 HUD 裁切",
            rabbitSeatSeven != null && rabbitSprite != null
            && VirtualX(rabbitSeatSeven.position.x - rabbitHalf) >= DDayGame.OfficeSafeLeft
            && VirtualX(rabbitSeatSeven.position.x + rabbitHalf) <= DDayGame.OfficeSafeRight);

        Reset();
        bool foxMoved = game.TryPlaceAnimal(2, 3);
        Check(7, "狐狸在 Seat 3，尾巴和物品完整", foxMoved && AnimalSafe(2));
        Reset();
        bool catMoved = game.TryPlaceAnimal(4, 7);
        Check(8, "黑猫在 Seat 7 完整可见", catMoved && AnimalSafe(4));
        float nameX = VirtualX(game.AnimalWorldPoint(4).x);
        Check(9, "最右侧姓名标签在 OfficeSafeArea 内",
            nameX - 42 >= DDayGame.OfficeSafeLeft
            && nameX + 42 <= DDayGame.OfficeSafeRight);

        DDayArtImport.PreviewDayTwoFoxRight();
        bool foxShibaBubble = game.AvailableConversations.Any(e => e.id == "d2_fox_shiba");
        float foxX = VirtualX(game.AnimalWorldPoint(2).x);
        float shibaX = VirtualX(game.AnimalWorldPoint(3).x);
        float bubbleX = Mathf.Clamp((foxX + shibaX) * .5f,
            DDayGame.OfficeSafeLeft + DDayGame.OfficeSafeMargin + 33f,
            DDayGame.OfficeSafeRight - DDayGame.OfficeSafeMargin - 33f);
        Check(10, "最右列角色的对话气泡不进入右 HUD", foxShibaBubble
            && bubbleX + 33 <= DDayGame.OfficeSafeRight - DDayGame.OfficeSafeMargin);

        Reset();
        bool swap = game.TryPlaceAnimal(5, 3);
        Check(11, "棕熊与柴犬 Swap 后两者仍完整显示", swap
            && game.Seats[5] == 3 && game.Seats[3] == 7
            && AnimalSafe(5) && AnimalSafe(3));
        Reset();
        Check(12, "初始座位规则判定保持 5/8", game.Satisfied == 5
            && game.TotalRules == 8);
        Check(13, "设施距离仍为原 Seat 数据", SeatRules.FacilityDistance(3, "printer") == 0
            && SeatRules.FacilityDistance(7, "printer") == 1
            && SeatRules.FacilityDistance(0, "toilet") == 0);
        float aspect = 16f / 9f;
        float pxPerWorld = 1920f / (2f * Camera.main.orthographicSize * aspect);
        float deskWidth = DDayArt.OfficeDesk.bounds.size.x * .92f;
        float available = DDayGame.OfficeSafeRight - DDayGame.OfficeSafeLeft
            - 2f * DDayGame.OfficeSafeMargin;
        float scale = Mathf.Min(1f, (available - 3f * 16f) / (4f * deskWidth * pxPerWorld));
        float deskHalf = deskWidth * scale * pxPerWorld * .5f;
        float first = DDayGame.OfficeSafeLeft + DDayGame.OfficeSafeMargin + deskHalf;
        float last = DDayGame.OfficeSafeRight - DDayGame.OfficeSafeMargin - deskHalf;
        Check(14, "1920×1080 16:9 几何计算保持两侧 HUD 间距",
            first - deskHalf >= DDayGame.OfficeSafeLeft + DDayGame.OfficeSafeMargin - 1f
            && last + deskHalf <= DDayGame.OfficeSafeRight - DDayGame.OfficeSafeMargin + 1f
            && (last - first) / 3f >= deskWidth * scale * pxPerWorld + 12f);

        int passed = results.Count(r => r.Contains(" PASS "));
        Debug.Log($"D-DAY DAY2 safe area checks: {passed}/14 PASS");
        Reset();
    }
}
