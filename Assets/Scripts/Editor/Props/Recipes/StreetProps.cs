using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HollowCreek.Editor.Props.Recipes
{
    /// <summary>Улица: афиша на доске объявлений, открытка Рут, памятник основательнице.</summary>
    static class StreetProps
    {
        // ------------------------------------------------------------------ Афиша

        /// <summary>
        /// Доска объявлений с отсыревшей афишей «Ночи масок»: кнопки, загнутый угол, обрывки старых объявлений.
        /// Точка отсчёта — середина задней стороны доски (у столба), лицо +Z.
        /// </summary>
        [PropRecipe]
        static void Poster(PropBuilder b)
        {
            var k = new MeshKit();
            var board = k.Mat("WoodPine");
            var trim = k.Mat("WoodCedar");
            var old = k.Mat("PaperAged");
            const float bw = 0.56f, bh = 0.74f, bt = 0.022f;
            k.Box(new Vector3(0, 0, bt / 2), new Vector3(bw, bh, bt), board, 0.003f, Uv.Tiled(2f));
            StudyClues.Frame(k, trim, bw + 0.03f, bh + 0.03f, 0.03f, 0.032f, 0.016f, 0.004f);
            // Козырёк от дождя.
            using (k.At(new Vector3(0, bh / 2 + 0.03f, 0.03f), new Vector3(35, 0, 0)))
                k.Box(Vector3.zero, new Vector3(bw + 0.08f, 0.012f, 0.08f), trim, 0.003f, Uv.Tiled(2f));
            // Обрывки старых объявлений по углам.
            var rnd = new System.Random(4);
            foreach (var (x, y, rot) in new[] { (0.22f, 0.3f, 8f), (-0.23f, -0.31f, -12f), (0.24f, -0.28f, 20f) })
            {
                var pts = new List<Vector2>();
                for (var i = 0; i < 9; i++)
                {
                    var a = Mathf.PI * 2 * i / 9;
                    var r = 0.035f + (float)rnd.NextDouble() * 0.03f;
                    pts.Add(new Vector2(Mathf.Cos(a) * r * 1.3f, Mathf.Sin(a) * r));
                }
                using (k.At(new Vector3(-x, y, bt + 0.0006f), new Vector3(0, 0, rot))) k.Extrude(pts, 0.0005f, old);
            }
            b.Part("Board", k);

            // Сама афиша: лист с волной и загнутым нижним правым углом.
            var p = new MeshKit();
            var art = p.Mat("Poster");
            var back = p.Mat("PaperAged");
            var size = new Vector2(0.45f, 0.6f);
            var start = p.Sheet(size, 18, 24, art, back);
            p.Deform(start, v =>
            {
                var wave = 0.002f * Mathf.Sin(v.y * 22) * Mathf.Sin(v.x * 14 + 1);
                // Для зрителя правый нижний угол — это −X, −Y.
                var cx = Mathf.Clamp01((-v.x - 0.1f) / 0.125f);
                var cy = Mathf.Clamp01((-v.y - 0.17f) / 0.13f);
                var curl = 0.05f * Mathf.Pow(cx * cy, 1.6f);
                return new Vector3(v.x, v.y, v.z + bt + 0.0015f + wave + curl);
            });
            // Кнопки по углам (одной не хватает — угол отошёл).
            var brass = p.Mat("Brass");
            foreach (var (x, y) in new[] { (0.2f, 0.275f), (-0.2f, 0.275f), (0.2f, -0.275f) })
                using (p.At(new Vector3(-x, y, bt + 0.002f), new Vector3(90, 0, 0)))
                {
                    p.Cylinder(Vector3.zero, 0.007f, 0.0015f, brass, 16, 0.0006f);
                    p.Sphere(new Vector3(0, 0.001f, 0), 0.0045f, brass, 10, 0.5f);
                }
            b.Part("Sheet", p);

            b.Text("Title", null, new Vector3(0, 0.155f, bt + 0.004f), Vector3.zero, new Vector2(0.38f, 0.25f),
                PropFont.Serif, new Color(0.16f, 0.09f, 0.07f), "poster.sign", "НОЧЬ\nМАСОК\n<size=45%>31 ОКТЯБРЯ</size>",
                lineSpacing: -12f);
            b.Text("Procession", null, new Vector3(0, -0.268f, bt + 0.004f), Vector3.zero, new Vector2(0.38f, 0.03f),
                PropFont.SansBold, new Color(0.95f, 0.8f, 0.55f), "poster.procession", "Шествие фонарей от площади до старого моста");
        }

        // ------------------------------------------------------------------ Открытка

        /// <summary>
        /// Старая открытка, лежащая обратной стороной вверх: детский почерк Рут, адрес, марка со штемпелем
        /// и приписка Элис на полях. Точка отсчёта — на земле, верх текста — к −Z.
        /// </summary>
        [PropRecipe]
        static void Postcard(PropBuilder b)
        {
            var k = new MeshKit();
            var backSide = k.Mat("PostcardBack");
            var picture = k.Mat("PostcardFront");
            var size = new Vector2(0.15f, 0.1f);
            using (k.At(new Vector3(0, 0.0008f, 0), new Vector3(-90, 0, 0)))
            {
                var start = k.Sheet(size, 16, 10, backSide, picture, null, 0.0005f);
                k.Deform(start, v =>
                {
                    // Открытка отсырела и выгнулась: края приподняты, правый верхний угол загнут.
                    var bow = 0.004f * (v.x * v.x / 0.0056f + v.y * v.y / 0.0025f);
                    var corner = 0.006f * Mathf.Pow(Mathf.Clamp01((-v.x - 0.04f) / 0.035f) * Mathf.Clamp01((v.y - 0.02f) / 0.03f), 1.5f);
                    return new Vector3(v.x, v.y, v.z + bow + corner);
                });
            }
            b.Part("Card", k);
            var ink = new Color(0.2f, 0.16f, 0.32f);
            var top = new Vector3(0, 0.0022f, 0);
            // Левая половина — письмо (x зрителя < 0 → локальная +X).
            b.Text("Message", null, top + new Vector3(0.034f, 0, 0.004f), PropBuilder.OnTop, new Vector2(0.074f, 0.074f),
                PropFont.Hand, ink, "postcard.hand",
                "Мы вышли из школы, перешли ручей по мосту, оставили свечу у могилы и вернулись домой. Запомни наш путь.\n— Рут",
                align: TextAlignmentOptions.TopLeft);
            b.Text("Address", null, top + new Vector3(-0.038f, 0, 0.018f), PropBuilder.OnTop, new Vector2(0.056f, 0.03f),
                PropFont.Hand, ink, "postcard.address", "Чарли Блейку\nХоллоу-Крик", align: TextAlignmentOptions.TopLeft);
            b.Text("Alice Note", null, top + new Vector3(0.001f, 0, -0.041f), PropBuilder.OnTop + new Vector3(0, -3, 0), new Vector2(0.12f, 0.012f),
                PropFont.Hand, new Color(0.08f, 0.2f, 0.55f), "postcard.alice", "Ключ от шкатулки — в сейфе!",
                align: TextAlignmentOptions.Left);
        }

        // ------------------------------------------------------------------ Памятник

        /// <summary>
        /// Гранитный обелиск на ступенчатом постаменте с бронзовой табличкой, венком и фонарями к празднику.
        /// Точка отсчёта — центр основания на земле, лицо +Z.
        /// </summary>
        [PropRecipe]
        static void Monument(PropBuilder b)
        {
            var k = new MeshKit();
            var stone = k.Mat("Granite");
            var darkStone = k.Mat("GraniteDark");
            var bronze = k.Mat("Bronze");
            // Ступени.
            k.Box(new Vector3(0, 0.08f, 0), new Vector3(2.2f, 0.16f, 2.2f), darkStone, 0.02f, Uv.Tiled(1f));
            k.Box(new Vector3(0, 0.23f, 0), new Vector3(1.8f, 0.14f, 1.8f), stone, 0.02f, Uv.Tiled(1f));
            // Цоколь, тумба и карниз.
            k.Box(new Vector3(0, 0.36f, 0), new Vector3(1.4f, 0.12f, 1.4f), darkStone, 0.015f, Uv.Tiled(1f));
            k.Box(new Vector3(0, 0.79f, 0), new Vector3(1.2f, 0.74f, 1.2f), stone, 0.012f, Uv.Tiled(1f));
            k.Box(new Vector3(0, 1.2f, 0), new Vector3(1.36f, 0.08f, 1.36f), darkStone, 0.02f, Uv.Tiled(1f));
            k.Box(new Vector3(0, 1.27f, 0), new Vector3(1.1f, 0.06f, 1.1f), stone, 0.015f, Uv.Tiled(1f));
            // Обелиск: четырёхгранный, сужается кверху, с пирамидкой.
            using (k.At(new Vector3(0, 1.3f, 0), new Vector3(0, 45, 0)))
            {
                k.Lathe(new List<Vector2> { new(0, 0), new(0.42f, 0), new(0.42f, 0.08f), new(0.34f, 0.12f), new(0.24f, 2.6f), new(0.27f, 2.62f), new(0.27f, 2.66f), new(0, 3.0f) },
                    stone, 4, 10f, 0.6f, 0f, 360f, true);
            }
            // Бронзовая табличка.
            k.Box(new Vector3(0, 0.82f, 0.6f + 0.008f), new Vector3(0.72f, 0.4f, 0.016f), bronze, 0.006f);
            StudyClues.Frame(k, bronze, 0.72f, 0.4f, 0.025f, 0.012f, 0.6f + 0.02f, 0.004f);
            foreach (var x in new[] { -0.32f, 0.32f })
            foreach (var y in new[] { 0.64f, 1.0f })
                k.Sphere(new Vector3(x, y, 0.625f), 0.012f, bronze, 10, 0.6f);
            b.Part("Stone", k);

            // Праздничный венок у подножия с лентой.
            var w = new MeshKit();
            var leaves = w.Mat("PencilPaint");
            var ribbon = w.Mat("CeramicRim");
            var rnd = new System.Random(9);
            using (w.At(new Vector3(0.38f, 0.5f, 0.76f), new Vector3(80, 0, 0), new Vector3(0.75f, 0.75f, 0.75f)))
            {
                w.Torus(Vector3.zero, 0.2f, 0.045f, leaves, 28, 10);
                for (var i = 0; i < 36; i++)
                {
                    var a = i / 36f * Mathf.PI * 2;
                    var p = new Vector3(Mathf.Cos(a) * 0.2f, 0.03f, Mathf.Sin(a) * 0.2f);
                    w.Sphere(p + new Vector3((float)rnd.NextDouble() * 0.02f, 0, (float)rnd.NextDouble() * 0.02f), 0.028f, leaves, 8, 0.6f);
                }
                foreach (var a in new[] { 20f, 150f, 260f })
                {
                    var r = a * Mathf.Deg2Rad;
                    w.Sphere(new Vector3(Mathf.Cos(r) * 0.2f, 0.05f, Mathf.Sin(r) * 0.2f), 0.022f, w.Mat("Copper"), 10);
                }
                // Бант и концы ленты внизу.
                w.Box(new Vector3(0, 0.05f, 0.2f), new Vector3(0.08f, 0.02f, 0.05f), ribbon, 0.008f);
                w.Sweep(new List<Vector3> { new(0, 0.05f, 0.2f), new(0.03f, 0.04f, 0.3f), new(0.05f, 0.035f, 0.38f) }, new Vector2(0.04f, 0.004f), ribbon);
                w.Sweep(new List<Vector3> { new(0, 0.05f, 0.2f), new(-0.03f, 0.04f, 0.3f), new(-0.06f, 0.035f, 0.37f) }, new Vector2(0.04f, 0.004f), ribbon);
            }
            b.Part("Wreath", w);

            // Фонарики на ступенях.
            var l = new MeshKit();
            var metal = l.Mat("BlackMetal");
            var glass = l.Mat("Glass");
            var wax = l.Mat("Wax");
            foreach (var (x, z) in new[] { (0.75f, 1.0f), (-0.8f, 0.95f), (0.95f, -0.2f) })
            {
                var baseY = z > 0.9f ? 0.16f : 0.3f;
                using (l.At(new Vector3(x, baseY, z)))
                {
                    l.Box(new Vector3(0, 0.01f, 0), new Vector3(0.12f, 0.02f, 0.12f), metal, 0.004f);
                    foreach (var cx in new[] { -1f, 1f })
                    foreach (var cz in new[] { -1f, 1f })
                        l.Box(new Vector3(cx * 0.055f, 0.1f, cz * 0.055f), new Vector3(0.012f, 0.18f, 0.012f), metal, 0.002f);
                    l.Box(new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.16f, 0.1f), glass);
                    l.Lathe(new List<Vector2> { new(0.075f, 0.19f), new(0.04f, 0.24f), new(0, 0.25f) }, metal, 4, 10f, 1f, 45f, 405f, true);
                    l.Torus(new Vector3(0, 0.27f, 0), 0.02f, 0.004f, metal, 12, 6);
                    l.Cylinder(new Vector3(0, 0.06f, 0), 0.022f, 0.08f, wax, 14, 0.003f);
                }
            }
            b.Part("Lanterns", l);

            b.Text("Plaque", null, new Vector3(0, 0.82f, 0.6f + 0.0175f), Vector3.zero, new Vector2(0.62f, 0.3f),
                PropFont.Serif, new Color(0.86f, 0.72f, 0.42f), literal: "HOLLOW CREEK\n<size=60%>FOUNDED 1891</size>");
        }
    }
}
