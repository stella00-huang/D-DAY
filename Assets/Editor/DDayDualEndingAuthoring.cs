using System;
using System.IO;
using DDay;
using UnityEditor;
using UnityEngine;

public static class DDayDualEndingAuthoring
{
    public const string BEPath = "Assets/Resources/DDay/Endings/BE/BEEndingConfig.asset";
    public static readonly string[] HEText = {
        "自聚餐结束后，SISY、METIS、CASSIA 没日没夜地核对那些被隐藏的真相。原来，这一隅之地竟然萌生过许多罪恶的源泉，而它们也在不知不觉中成为了链条中的一份子。X 留下的所谓的“遗书”到底是真心想要揭露这犯罪链还是蓄意报复 APIS 老板，结果也不得而知了。庆幸地是，METIS 愿意为其它动物做担保，除了 THANE，毕竟它确实参与了犯罪行为。",
        "在故事的结尾，马老板和 THANE 同时被调查人员带走。APIS 的业务同时也公之于众了。对于这个真相，各位众说纷纭，有人说马老板就是幕后凶手，有人说马老板只是个傀儡。但这一切纭纭随着马老板和 THANE 被关进监狱后如风一般都消散了……",
        "对 SISY 而言，自己从毕业后就入职的公司，陪伴自己多年的同事，甚至可以称之为伙伴的马老板和 THANE 竟然走上的是这条不归路。该说自己是聪明还是笨呢，或许是那股努力上进的蛮劲让它只顾得推动眼前名为巨石的工作吧，只是从今天开始，这块硕大的巨石终于顺着细缝裂开了。\n\n在 SISY 走神的瞬间，其余五名伙伴推搡了它的背。那一刻，SISY 不由得踉跄了一下，它心里苦笑道“难道这就是生活的推背感吗？”。有只温暖的手轻轻地抚上，坚定地握上它的手时，它抬头看向身边狼和眼前打闹的伙伴们，心里不再感到不安和害怕。\n\n这一刻，SISY 终于不是被推着走了。"
    };
    public static readonly string[] BEText = {
        "自聚餐结束后，SISY、METIS、CASSIA 便开始没日没夜地核对那些被隐藏的真相。只是它们不知道，有些秘密尚未被揭开，危险便已悄然降临。马老板早已向 THANE 下达了处理 SISY 的命令，而这道命令并没有因为它侥幸逃过第一次死亡而撤销。THANE 望着手机上熟悉的来电，沉默了许久。它曾经也想过，自己究竟是从什么时候开始，成为这条犯罪链上不可或缺的一环的呢？",
        "那天晚上，ORIN 像往常一样留在办公室等 SISY。它们已经商量好了离开 APIS 的计划，甚至开始畅想未来的生活。与此同时，THANE 以工作为由将 SISY 带离了办公室。SISY 并未察觉异样，毕竟这位宽厚的棕熊一直是它信任的伙伴之一。只是这一次，SISY 再也没有回来。ORIN 等到办公室的灯一盏盏熄灭，才终于意识到，那些原本触手可及的未来，竟如泡影一般消散了。",
        "第二天，APIS 依旧照常运转。打印机吐出新的文件，咖啡机发出熟悉的声响，马老板仍然站在落地窗前，俯瞰着这座逐渐苏醒的城市。有人试图追查 SISY 的下落，有人对公司的秘密愈发怀疑，也有人选择沉默。只是那张空荡荡的工位，似乎并没有影响这台庞大机器的运作。说来也奇怪，SISY 曾经总觉得自己是被生活推着走的，工作、责任、旁人的期待，无一不催促着它向前。直到不久前，它才终于明白，原来自己也可以选择另一条道路。\n\n只是这一次，再也没有谁能推着 SISY 向前走了。"
    };

    [MenuItem("D-DAY/Endings/Configure Formal Narration")]
    public static void Configure()
    {
        var he = AssetDatabase.LoadAssetAtPath<HEEndingConfig>(HEEndingAuthoring.ConfigPath);
        if (he == null) throw new InvalidOperationException("Existing HE configuration is missing.");
        var textures = new Texture2D[3];
        string[] paths = { "Assets/Art/Endings/BE/BE_01_Order.png", "Assets/Art/Endings/BE/BE_02_SISY.png", "Assets/Art/Endings/BE/BE_03_APIS.png" };
        for (int i = 0; i < 3; i++)
        {
            AssetDatabase.ImportAsset(paths[i]);
            var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing BE illustration: " + paths[i]);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.isReadable = false; importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
        }
        var be = AssetDatabase.LoadAssetAtPath<HEEndingConfig>(BEPath);
        if (be == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BEPath)); AssetDatabase.Refresh();
            be = ScriptableObject.CreateInstance<HEEndingConfig>();
            be.shots = new[] {
                new HEEndingShot { id="BE_01_Order", image=textures[0], duration=10,
                    startScale=1, endScale=1.035f, endOffset=new Vector2(.007f,0),
                    atmosphere=HEAtmosphere.CorridorPhone, lightPosition=new Vector2(.44f,.48f), lightIntensity=.028f, dustIntensity=.014f },
                new HEEndingShot { id="BE_02_SISY", image=textures[1], duration=10,
                    startScale=1.025f, endScale=1.025f, startOffset=new Vector2(.009f,0), endOffset=new Vector2(-.009f,0),
                    atmosphere=HEAtmosphere.RainNight, lightPosition=new Vector2(.25f,.65f), lightIntensity=.018f, dustIntensity=.010f },
                new HEEndingShot { id="BE_03_APIS", image=textures[2], duration=12,
                    startScale=1.035f, endScale=1,
                    atmosphere=HEAtmosphere.Morning, lightPosition=new Vector2(.91f,.63f), lightIntensity=.024f, dustIntensity=.024f,
                    fadeIn=2, fadeOut=2 }
            };
            AssetDatabase.CreateAsset(be, BEPath);
        }
        // HE camera, artwork, light and audio parameters stay exactly as already authored.
        ConfigureText(he, HEText, "如露亦如电，应作如是观。", "HE_Formal_v1");
        ConfigureText(be, BEText, "一切有为法，如梦幻泡影。", "BE_Formal_v1");
        AssetDatabase.SaveAssets(); Selection.activeObject = be;
        Debug.Log("D-DAY formal endings: HE=" + he.Duration.ToString("0.0") + "s; BE=" + be.Duration.ToString("0.0") + "s. Audio slots remain optional.");
    }
    static void ConfigureText(HEEndingConfig config, string[] text, string title, string id)
    {
        config.openingTitle = title; config.presentationId = id;
        config.endingTitle = "D-DAY"; config.endingSubtitle = "";
        for (int i = 0; i < 3; i++) config.shots[i].narration = text[i];
        EditorUtility.SetDirty(config);
    }
    [MenuItem("D-DAY/Endings/Preview HE (Play Mode)")]
    static void PreviewHE() => UnityEngine.Object.FindFirstObjectByType<DDayGame>()?.GetComponent<HEEndingPlayer>()?.PlayPreview();
    [MenuItem("D-DAY/Endings/Preview BE (Play Mode)")]
    static void PreviewBE() => UnityEngine.Object.FindFirstObjectByType<DDayGame>()?.GetComponent<HEEndingPlayer>()?.PlayBEPreview();
    [MenuItem("D-DAY/Endings/Preview HE (Play Mode)", true)]
    [MenuItem("D-DAY/Endings/Preview BE (Play Mode)", true)]
    static bool CanPreview() => Application.isPlaying && UnityEngine.Object.FindFirstObjectByType<DDayGame>() != null;
}
