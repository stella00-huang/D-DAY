using System;
using System.Collections.Generic;
using System.Linq;
using DDay;
using UnityEditor;
using UnityEngine;

public static class DDayDay1SeatChecks
{
    [MenuItem("D-DAY/Run DAY 1 Seat Checks")]
    public static void Run()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (!EditorApplication.isPlaying || game == null)
        {
            Debug.LogError("DAY 1 checks require Play Mode and the D-DAY controller.");
            return;
        }

        DDayArtImport.RestoreFullCamera();
        DDayArtImport.PreviewDayOne();
        var results = new List<string>();
        void Check(int number, string label, bool passed) =>
            results.Add($"TEST {number:00} {(passed ? "PASS" : "FAIL")} · {label}");

        Transform Seat(int seat) => game.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "SEAT " + seat + " / fixed desk and chair");
        SpriteRenderer Character(int index) => game.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == SeatRules.Names[index] + " / movable employee");
        Transform Prop(int index) => game.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "PersonalProps / " + SeatRules.Names[index]);
        int PropSeat(int index)
        {
            Transform parent = Prop(index)?.parent?.parent;
            if (parent == null) return -1;
            for (int seat = 0; seat < 8; seat++) if (parent == Seat(seat)) return seat;
            return -1;
        }
        SpriteRenderer Front(int seat) => Seat(seat)?.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r.name == "SEAT LAYER / desk front");
        bool Layered(int animal)
        {
            int seat = game.Seats[animal];
            var character = Character(animal);
            var front = Front(seat);
            return character != null && front != null && character.enabled
                && character.sprite != null && front.sprite != null
                && character.sortingOrder < front.sortingOrder
                && character.transform.parent?.name == "CharacterAnchor " + seat
                && character.bounds.max.y > front.bounds.max.y;
        }

        Check(1, "兔子坐姿层级、头耳高于桌前缘", Layered(0));
        Check(2, "棕熊大体型坐姿层级", Layered(5));
        Check(3, "狐狸独立角色与桌前缘分层", Layered(2));
        Check(4, "七名角色均有独立 Sprite 且位于桌前缘后", Enumerable.Range(0, 7).All(Layered));
        var desks = Enumerable.Range(0, 8).Select(Front).ToArray();
        Check(5, "八张桌使用同一正向 Sprite、同一旋转与缩放",
            desks.All(r => r != null && r.sprite == desks[0].sprite
                && r.transform.rotation == Quaternion.identity && r.transform.localScale == Vector3.one));

        RuleData shiba = game.CurrentDay.rules.First(r => r.animal == "shiba");
        Check(6, "柴犬在右上 Seat 3 靠近打印区", game.Seats[3] == 3
            && SeatRules.FacilityDistance(3, "printer") == 0
            && SeatRules.Satisfied(shiba, game.Seats, game.RabbitSeat));
        int before = game.Satisfied;
        bool movedLeft = game.TryPlaceAnimal(3, 0);
        bool leftSatisfied = SeatRules.Satisfied(shiba, game.Seats, game.RabbitSeat);
        int leftSatisfiedCount = game.Satisfied;
        Check(7, "柴犬移到左上 Seat 0 时打印区规则失败", movedLeft
            && game.Seats[3] == 0 && !leftSatisfied);
        bool movedRight = game.TryPlaceAnimal(3, 3);
        bool rightSatisfied = SeatRules.Satisfied(shiba, game.Seats, game.RabbitSeat);
        Check(8, "柴犬移回 Seat 3 时失败变成功", movedRight && rightSatisfied);
        Check(9, "员工状态由同一规则计算即时更新", !leftSatisfied && rightSatisfied
            && game.CurrentDay.rules.Count(r => r.animal == "shiba") == 1);
        Check(10, "座位满足条数随移动更新", leftSatisfiedCount != before
            && game.Satisfied == before);
        bool facilities = SeatRules.FacilityDistance(0, "toilet") == 0
            && SeatRules.FacilityDistance(1, "coffee") == 0
            && SeatRules.FacilityDistance(2, "coffee") == 0
            && SeatRules.FacilityDistance(3, "printer") == 0
            && SeatRules.FacilityDistance(7, "printer") == 1
            && SeatRules.IsWindowSeat(0) && !SeatRules.IsWindowSeat(4)
            && game.CurrentDay.rules.All(r =>
                r.target != "toilet" && r.target != "coffee" && r.target != "printer"
                || SeatRules.FacilityDistance(game.Seats[SeatRules.Index(r.animal)], r.target) < 99);
        Check(11, "DAY 1 设施规则使用当前八工位映射", facilities);

        var relation = new RuleData { animal = "wolf", kind = "near_person", target = "rabbit", distance = 2 };
        bool initialNear = SeatRules.Satisfied(relation, game.Seats, game.RabbitSeat);
        bool relationMove = game.TryPlaceAnimal(1, 7);
        bool nowFar = !SeatRules.Satisfied(relation, game.Seats, game.RabbitSeat);
        game.TryPlaceAnimal(1, 1);
        Check(12, "人物距离随换位按 Seat Graph 更新", initialNear && relationMove && nowFar);

        bool foxMove = game.TryPlaceAnimal(2, 7);
        Check(13, "狐狸移至空位时个人物件跟随", foxMove && PropSeat(2) == 7);
        bool swap = game.TryPlaceAnimal(2, 3);
        Check(14, "狐狸与柴犬交换时两人物件同步交换", swap
            && game.Seats[2] == 3 && game.Seats[3] == 7
            && PropSeat(2) == 3 && PropSeat(3) == 7);
        int oldFoxSeat = game.Seats[2], oldShibaSeat = game.Seats[3];
        bool invalid = game.TryPlaceAnimal(2, -1);
        Check(15, "无效放置后角色与物件保持原位", !invalid
            && game.Seats[2] == oldFoxSeat && game.Seats[3] == oldShibaSeat
            && PropSeat(2) == oldFoxSeat && PropSeat(3) == oldShibaSeat);
        int empty = Enumerable.Range(0, 8).First(s => !game.Seats.Contains(s));
        Check(16, "空工位没有角色个人物件", Enumerable.Range(0, 7)
            .All(i => PropSeat(i) != empty));

        int count = results.Count(r => r.Contains(" PASS "));
        Debug.Log($"D-DAY DAY 1 seat checks: {count}/16 PASS\n" + string.Join("\n", results));
        DDayArtImport.PreviewDayOne();
    }
}
