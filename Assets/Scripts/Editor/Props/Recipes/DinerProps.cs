using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HollowCreek.Editor.Props.Recipes
{
    /// <summary>Закусочная «Ночная сова»: касса, объявление, витрина, щипцы, кофе и пирог Мары, жалюзи.</summary>
    static class DinerProps
    {
        // ------------------------------------------------------------------ Касса

        /// <summary>
        /// Электронная касса: денежный ящик, наклонная клавиатура с 30 клавишами, принтер чеков с лентой,
        /// дисплей для кассира и дисплей на стойке для гостей. Лицо (+Z) — к кассиру, задняя стенка — к гостям.
        /// Точка отсчёта — середина дна.
        /// </summary>
        [PropRecipe]
        static void Register(PropBuilder b)
        {
            var k = new MeshKit();
            var body = k.Mat("PlasticBeige");
            var dark = k.Mat("PlasticGrey");
            var black = k.Mat("PlasticBlack");
            var keys = k.Mat("RegisterKeys");
            var lcd = k.Mat("Lcd");
            var chrome = k.Mat("Chrome");
            const float w = 0.42f, d = 0.40f;
            // Денежный ящик с щелью, замком и ручкой-выемкой.
            k.Box(new Vector3(0, 0.05f, 0), new Vector3(w, 0.1f, d), body, 0.008f);
            k.Box(new Vector3(0, 0.09f, d / 2 + 0.001f), new Vector3(w - 0.02f, 0.004f, 0.004f), black);
            k.Box(new Vector3(0, 0.05f, d / 2 + 0.002f), new Vector3(0.12f, 0.02f, 0.004f), dark, 0.004f);
            using (k.At(new Vector3(0.16f, 0.05f, d / 2 + 0.002f), new Vector3(90, 0, 0)))
                k.Cylinder(Vector3.zero, 0.008f, 0.006f, chrome, 14, 0.002f);
            // Ножки.
            foreach (var x in new[] { -1f, 1f })
            foreach (var z in new[] { -1f, 1f })
                k.Cylinder(new Vector3(x * (w / 2 - 0.03f), -0.003f, z * (d / 2 - 0.03f)), 0.012f, 0.006f, black, 10);

            // Задняя башня (к гостям): принтер, крышка рулона, место для объявления сзади.
            const float towerD = 0.14f, towerTop = 0.33f;
            k.Box(new Vector3(0, 0.1f + (towerTop - 0.1f) / 2, -d / 2 + towerD / 2), new Vector3(w, towerTop - 0.1f, towerD), body, 0.01f);
            k.Box(new Vector3(0.1f, towerTop + 0.002f, -d / 2 + towerD / 2), new Vector3(0.16f, 0.006f, 0.1f), dark, 0.004f);
            k.Box(new Vector3(0.1f, towerTop + 0.004f, -d / 2 + towerD - 0.012f), new Vector3(0.12f, 0.004f, 0.006f), black);
            // Чековая лента, свисающая из щели.
            var paper = k.Mat("Paper");
            var strip = new List<Vector3>();
            for (var i = 0; i <= 14; i++)
            {
                var t = i / 14f;
                strip.Add(new Vector3(0.1f, towerTop + 0.006f + Mathf.Sin(t * Mathf.PI) * 0.03f - t * t * 0.02f, -d / 2 + towerD - 0.012f + t * 0.1f));
            }
            k.Sweep(strip, new Vector2(0.056f, 0.0004f), paper, uvScale: 1f);
            // Дисплей кассира на лицевой стороне башни.
            k.Box(new Vector3(-0.08f, towerTop - 0.06f, -d / 2 + towerD + 0.001f), new Vector3(0.2f, 0.06f, 0.006f), black, 0.004f);
            k.Box(new Vector3(-0.08f, towerTop - 0.06f, -d / 2 + towerD + 0.0045f), new Vector3(0.17f, 0.04f, 0.001f), black, 0f, null, 1,
                new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (lcd, Uv.Fit()) });

            // Наклонная клавиатура.
            var kbCenter = new Vector3(0, 0.13f, 0.06f);
            using (k.At(kbCenter, new Vector3(12, 0, 0)))
            {
                k.Box(Vector3.zero, new Vector3(w - 0.01f, 0.05f, 0.26f), body, 0.008f);
                k.Box(new Vector3(0, 0.026f, 0), new Vector3(w - 0.05f, 0.004f, 0.22f), dark, 0.004f);
                const int cols = 6, rows = 5;
                var kw = (w - 0.07f) / cols;
                var kd = 0.2f / rows;
                for (var r = 0; r < rows; r++)
                for (var c = 0; c < cols; c++)
                {
                    // Столбец c слева направо для кассира: у него «вправо» — это −X.
                    var x = (w - 0.07f) / 2 - kw * (c + 0.5f);
                    var z = -0.1f + kd * (r + 0.5f);
                    var uv = new Rect(c / (float)cols, 1 - (r + 1) / (float)rows, 1f / cols, 1f / rows);
                    k.Box(new Vector3(x, 0.034f, z), new Vector3(kw - 0.006f, 0.012f, kd - 0.006f), body, 0.002f, null, 1,
                        new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Top] = (keys, Uv.Fit(uv)) });
                }
            }
            // Ключ режимов справа от клавиатуры.
            using (k.At(new Vector3(-0.19f, 0.165f, -0.04f), new Vector3(0, 0, 0)))
            {
                k.Cylinder(Vector3.zero, 0.014f, 0.01f, chrome, 16, 0.002f);
                k.Box(new Vector3(0, 0.012f, 0), new Vector3(0.004f, 0.014f, 0.016f), black, 0.001f);
            }
            b.Part("Body", k);

            // Дисплей для гостей на стойке — повернут к задней стороне.
            var p = new MeshKit();
            var pb = p.Mat("PlasticBeige");
            var pl = p.Mat("Lcd");
            var pk = p.Mat("PlasticBlack");
            p.Cylinder(new Vector3(-0.15f, towerTop + 0.06f, -d / 2 + 0.05f), 0.012f, 0.12f, pb, 12);
            p.Box(new Vector3(-0.15f, towerTop + 0.14f, -d / 2 + 0.05f), new Vector3(0.18f, 0.06f, 0.05f), pb, 0.01f);
            using (p.At(new Vector3(-0.15f, towerTop + 0.14f, -d / 2 + 0.024f), new Vector3(0, 180, 0)))
            {
                p.Box(Vector3.zero, new Vector3(0.15f, 0.04f, 0.002f), pk);
                p.Box(new Vector3(0, 0, 0.0012f), new Vector3(0.13f, 0.028f, 0.001f), pk, 0f, null, 1,
                    new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (pl, Uv.Fit()) });
            }
            b.Part("Customer Display", p);
        }

        // ------------------------------------------------------------------ Объявление у кассы

        /// <summary>Листок «Дорогие гости!», приклеенный скотчем. Лицо +Z, точка отсчёта — центр у поверхности.</summary>
        [PropRecipe]
        static void OutageNotice(PropBuilder b)
        {
            var k = new MeshKit();
            var front = k.Mat("Outage");
            var back = k.Mat("Paper");
            var size = new Vector2(0.21f, 0.28f);
            var start = k.Sheet(size, 10, 14, front, back);
            k.Deform(start, v =>
            {
                var lift = 0.004f * Mathf.Pow(Mathf.Clamp01((-v.y / size.y) + 0.1f), 2) + 0.0008f * Mathf.Sin(v.x * 50);
                return new Vector3(v.x, v.y, v.z + 0.0008f + lift);
            });
            var tape = k.Mat("Tape");
            foreach (var s in new[] { -1f, 1f })
                using (k.At(new Vector3(s * 0.095f, size.y / 2 - 0.01f, 0.0022f), new Vector3(0, 0, s * 35)))
                    k.Box(Vector3.zero, new Vector3(0.06f, 0.02f, 0.0004f), tape);
            b.Part("Paper", k);
            var ink = new Color(0.12f, 0.12f, 0.14f);
            b.Text("Header", null, new Vector3(-0.02f, size.y / 2 - 0.026f, 0.0035f), Vector3.zero, new Vector2(0.14f, 0.03f),
                PropFont.SansBold, new Color(0.96f, 0.95f, 0.9f), "outage.print.header", "ОБЪЯВЛЕНИЕ", fontSize: 0.2f);
            b.Text("Body", null, new Vector3(0, -0.02f, 0.0035f), Vector3.zero, new Vector2(0.18f, 0.2f),
                PropFont.Sans, ink, "outage.print.body",
                "Дорогие гости!\n\nВчера с <b>20:42</b> до <b>21:32</b> на всей Мейпл-стрит не было света. Касса и кофемашина не работали — простите за ожидание.\n\n<i>Ваша «Ночная сова»</i>",
                align: TextAlignmentOptions.TopLeft);
        }

        // ------------------------------------------------------------------ Витрина

        /// <summary>
        /// Настольная витрина для выпечки: деревянное основание, хромированный каркас, наклонное переднее стекло,
        /// стеклянная полка, раздвижные дверцы сзади и ценники. Лицо (+Z) — к гостям. Точка отсчёта — середина дна.
        /// </summary>
        [PropRecipe]
        static void PastryShowcase(PropBuilder b)
        {
            var k = new MeshKit();
            var wood = k.Mat("WoodWalnut");
            var chrome = k.Mat("Chrome");
            var glass = k.Mat("Glass");
            var tags = k.Mat("PriceTag");
            const float w = 1.2f, h = 0.5f, d = 0.6f, baseH = 0.05f;
            k.Box(new Vector3(0, baseH / 2, 0), new Vector3(w, baseH, d), wood, 0.006f, Uv.Tiled(3f));
            k.Box(new Vector3(0, baseH + 0.002f, 0), new Vector3(w - 0.02f, 0.004f, d - 0.02f), k.Mat("Paper"), 0.001f);
            // Каркас.
            var frontBottom = new Vector3(0, baseH, d / 2 - 0.01f);
            var frontTop = new Vector3(0, h, d / 2 - 0.18f);
            foreach (var s in new[] { -1f, 1f })
            {
                var x = s * (w / 2 - 0.01f);
                k.Sweep(new List<Vector3> { new(x, frontBottom.y, frontBottom.z), new(x, frontTop.y, frontTop.z) }, new Vector2(0.016f, 0.016f), chrome, Vector3.forward);
                k.Box(new Vector3(x, (baseH + h) / 2, -d / 2 + 0.01f), new Vector3(0.016f, h - baseH, 0.016f), chrome, 0.003f);
                // Боковое стекло — трапеция.
                var side = new List<Vector2> { new(-d / 2 + 0.02f, baseH), new(d / 2 - 0.02f, baseH), new(d / 2 - 0.18f, h - 0.01f), new(-d / 2 + 0.02f, h - 0.01f) };
                using (k.At(new Vector3(x, 0, 0), new Vector3(0, s > 0 ? -90 : 90, 0)))
                {
                    var pts = new List<Vector2>();
                    foreach (var p in side) pts.Add(new Vector2(s > 0 ? p.x : -p.x, p.y));
                    k.Extrude(pts, 0.006f, glass);
                }
            }
            k.Box(new Vector3(0, h, frontTop.z), new Vector3(w, 0.016f, 0.016f), chrome, 0.003f);
            k.Box(new Vector3(0, h, -d / 2 + 0.01f), new Vector3(w, 0.016f, 0.016f), chrome, 0.003f);
            k.Box(new Vector3(0, baseH + 0.008f, frontBottom.z), new Vector3(w, 0.016f, 0.016f), chrome, 0.003f);
            // Стекла: верх, наклонное переднее, раздвижные дверцы сзади.
            var topDepth = frontTop.z - (-d / 2 + 0.01f);
            k.Box(new Vector3(0, h + 0.004f, (-d / 2 + 0.01f + frontTop.z) / 2), new Vector3(w - 0.02f, 0.006f, topDepth), glass);
            var slant = frontTop - frontBottom;
            var angle = Mathf.Atan2(slant.z, slant.y) * Mathf.Rad2Deg;
            using (k.At((frontTop + frontBottom) / 2, new Vector3(angle, 0, 0)))
                k.Box(Vector3.zero, new Vector3(w - 0.02f, slant.magnitude, 0.006f), glass);
            foreach (var (x, z) in new[] { (-0.28f, -d / 2 + 0.018f), (0.28f, -d / 2 + 0.03f) })
            {
                k.Box(new Vector3(x, (baseH + h) / 2, z), new Vector3(w / 2 + 0.02f, h - baseH - 0.02f, 0.006f), glass);
                k.Box(new Vector3(x + (x > 0 ? -0.2f : 0.2f), (baseH + h) / 2, z - 0.008f), new Vector3(0.012f, 0.08f, 0.01f), chrome, 0.004f);
            }
            // Полка со хромированной кромкой.
            k.Box(new Vector3(0, 0.29f, -0.07f), new Vector3(w - 0.04f, 0.008f, 0.3f), glass);
            k.Box(new Vector3(0, 0.29f, 0.08f), new Vector3(w - 0.04f, 0.012f, 0.008f), chrome, 0.002f);
            foreach (var s in new[] { -1f, 1f })
                k.Box(new Vector3(s * (w / 2 - 0.03f), 0.17f, -0.07f), new Vector3(0.01f, 0.24f, 0.01f), chrome);
            // Ценники перед выпечкой.
            var tagXs = new[] { -0.42f, -0.14f, 0.14f, 0.42f };
            for (var i = 0; i < 4; i++)
            {
                using (k.At(new Vector3(-tagXs[i], baseH + 0.025f, d / 2 - 0.06f), new Vector3(-15, 0, 0)))
                {
                    k.Box(Vector3.zero, new Vector3(0.07f, 0.04f, 0.002f), tags, 0f, null, 1,
                        new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (tags, Uv.Fit(new Rect(i / 4f, 0, 0.25f, 1))) });
                    k.Box(new Vector3(0, -0.028f, -0.006f), new Vector3(0.012f, 0.02f, 0.012f), chrome, 0.002f);
                }
            }
            b.Part("Showcase", k);
        }

        /// <summary>Щипцы для выпечки, брошенные на прилавке. Точка отсчёта — на поверхности.</summary>
        [PropRecipe]
        static void Tongs(PropBuilder b)
        {
            var k = new MeshKit();
            var steel = k.Mat("Chrome");
            foreach (var s in new[] { -1f, 1f })
            {
                var path = new List<Vector3>();
                for (var i = 0; i <= 12; i++)
                {
                    var t = i / 12f;
                    path.Add(new Vector3(s * (0.004f + t * 0.022f), 0.006f + Mathf.Sin(t * Mathf.PI) * 0.01f, -0.1f + t * 0.2f));
                }
                k.Sweep(path, new Vector2(0.014f, 0.0012f), steel);
                // Зубчатые захваты.
                using (k.At(path[path.Count - 1] + new Vector3(0, 0, 0.018f)))
                    for (var j = 0; j < 4; j++)
                        k.Box(new Vector3((j - 1.5f) * 0.004f, 0, 0), new Vector3(0.003f, 0.0015f, 0.036f), steel, 0.0006f);
            }
            k.Torus(new Vector3(0, 0.008f, -0.105f), 0.008f, 0.0015f, steel, 16, 6);
            b.Part("Tongs", k);
        }

        // ------------------------------------------------------------------ Кофе и пирог Мары

        /// <summary>Кружка остывшего кофе со следом помады, блюдце и ложка. Точка отсчёта — на столе.</summary>
        [PropRecipe]
        static void DinerMug(PropBuilder b)
        {
            var k = new MeshKit();
            var ceramic = k.Mat("Ceramic");
            var rim = k.Mat("CeramicRim");
            var coffee = k.Mat("Coffee");
            var lipstick = k.Mat("Lipstick");
            var steel = k.Mat("Chrome");
            // Блюдце.
            k.Lathe(new List<Vector2> { new(0, 0.001f), new(0.045f, 0.001f), new(0.05f, 0.004f), new(0.07f, 0.009f), new(0.074f, 0.013f), new(0.071f, 0.014f),
                new(0.05f, 0.009f), new(0.036f, 0.007f), new(0.034f, 0.009f), new(0, 0.009f) }, ceramic, 40, 45f);
            // Кружка: снаружи, по краю и внутри.
            var y0 = 0.009f;
            var outer = new List<Vector2> { new(0, y0), new(0.03f, y0), new(0.034f, y0 + 0.004f), new(0.037f, y0 + 0.02f), new(0.038f, y0 + 0.07f), new(0.0385f, y0 + 0.088f) };
            k.Lathe(outer, ceramic, 40, 40f);
            k.Lathe(new List<Vector2> { new(0.0385f, y0 + 0.088f), new(0.0385f, y0 + 0.094f) }, rim, 40, 40f);
            k.Lathe(new List<Vector2> { new(0.0385f, y0 + 0.094f), new(0.0375f, y0 + 0.097f), new(0.0335f, y0 + 0.096f) }, ceramic, 40, 60f);
            k.Lathe(new List<Vector2> { new(0.0335f, y0 + 0.096f), new(0.033f, y0 + 0.06f), new(0.031f, y0 + 0.012f), new(0, y0 + 0.01f) }, ceramic, 40, 40f);
            k.Disc(new Vector3(0, y0 + 0.074f, 0), 0.0331f, coffee, 40);
            // Кольцо засохшего кофе на стенке.
            k.Torus(new Vector3(0, y0 + 0.08f, 0), 0.0332f, 0.0005f, coffee, 40, 4);
            // Ручка.
            using (k.At(new Vector3(-0.043f, y0 + 0.05f, 0), new Vector3(90, 0, 0)))
                k.Torus(Vector3.zero, 0.021f, 0.0055f, ceramic, 24, 10, 70, 290);
            // След помады на краю (со стороны, противоположной ручке).
            k.Torus(new Vector3(0, y0 + 0.0965f, 0), 0.036f, 0.0018f, lipstick, 48, 6, -18, 22);
            // Ложка на блюдце.
            using (k.At(new Vector3(0.048f, 0.012f, 0.02f), new Vector3(0, 70, 0)))
            {
                var handle = new List<Vector3>();
                for (var i = 0; i <= 10; i++)
                {
                    var t = i / 10f;
                    handle.Add(new Vector3(0, 0.002f + t * 0.006f, -0.01f - t * 0.075f));
                }
                k.Sweep(handle, new Vector2(0.006f, 0.0012f), steel);
                using (k.At(new Vector3(0, 0.002f, 0.004f), Vector3.zero, new Vector3(0.9f, 0.35f, 1.4f)))
                    k.Sphere(Vector3.zero, 0.011f, steel, 16);
            }
            b.Part("Mug", k);
        }

        /// <summary>Нетронутый кусок вишнёвого пирога на тарелке с вилкой. Точка отсчёта — на столе.</summary>
        [PropRecipe]
        static void PieSlice(PropBuilder b)
        {
            var k = new MeshKit();
            var ceramic = k.Mat("Ceramic");
            var rim = k.Mat("CeramicRim");
            var crust = k.Mat("PieCrust");
            var filling = k.Mat("PieFilling");
            var steel = k.Mat("Chrome");
            k.Lathe(new List<Vector2> { new(0, 0.001f), new(0.06f, 0.001f), new(0.07f, 0.005f), new(0.1f, 0.012f), new(0.105f, 0.016f), new(0.1f, 0.017f),
                new(0.075f, 0.01f), new(0.065f, 0.008f), new(0, 0.008f) }, ceramic, 48, 45f);
            k.Torus(new Vector3(0, 0.0152f, 0), 0.098f, 0.0012f, rim, 48, 4);
            // Кусок пирога: клин с корочкой сверху и снизу и начинкой по бокам.
            var wedge = new List<Vector2> { new(0, -0.05f), new(0.04f, 0.05f), new(-0.04f, 0.05f) };
            using (k.At(new Vector3(0, 0.008f + 0.02f, 0.005f), new Vector3(-90, 20, 0)))
                k.Extrude(wedge, 0.04f, filling, crust, null, crust);
            // Бортик корочки и решётка сверху.
            using (k.At(new Vector3(0, 0.008f, 0.005f), new Vector3(0, 20, 0)))
            {
                k.Sweep(new List<Vector3> { new(-0.042f, 0.042f, -0.05f), new(0, 0.044f, -0.052f), new(0.042f, 0.042f, -0.05f) }, new Vector2(0.012f, 0.01f), crust);
                for (var i = 0; i < 3; i++)
                {
                    var z = -0.03f + i * 0.025f;
                    var half = 0.04f * (0.05f - z) / 0.1f;
                    k.Box(new Vector3(0, 0.041f, z), new Vector3(half * 2, 0.004f, 0.007f), crust, 0.002f);
                }
                // Вытекшая начинка на тарелку.
                k.Sphere(new Vector3(0.02f, 0.001f, 0.04f), 0.012f, filling, 12, 0.25f);
            }
            // Вилка.
            using (k.At(new Vector3(-0.06f, 0.011f, -0.01f), new Vector3(0, -30, 0)))
            {
                var handle = new List<Vector3>();
                for (var i = 0; i <= 10; i++)
                {
                    var t = i / 10f;
                    handle.Add(new Vector3(0, 0.001f + t * 0.006f, -t * 0.09f));
                }
                k.Sweep(handle, new Vector2(0.008f, 0.0015f), steel);
                for (var j = 0; j < 4; j++)
                    k.Box(new Vector3((j - 1.5f) * 0.0045f, 0.0008f, 0.022f), new Vector3(0.0022f, 0.0012f, 0.04f), steel, 0.0005f);
                k.Box(new Vector3(0, 0.0008f, 0.003f), new Vector3(0.02f, 0.0014f, 0.01f), steel, 0.001f);
            }
            b.Part("Pie", k);
        }

        // ------------------------------------------------------------------ Жалюзи и подоконник

        /// <summary>Приподнятые жалюзи со шнурами и подоконник для окна 2,2 × 1,4 м. Лицо +Z, точка отсчёта — центр окна.</summary>
        [PropRecipe]
        static void DinerBlinds(PropBuilder b)
        {
            var k = new MeshKit();
            var slat = k.Mat("Blinds");
            var cord = k.Mat("Cord");
            var sill = k.Mat("WoodOak");
            const float w = 2.3f;
            k.Box(new Vector3(0, 0.74f, 0.04f), new Vector3(w, 0.045f, 0.05f), slat, 0.008f);
            // Собранные наверху ламели.
            for (var i = 0; i < 14; i++)
                using (k.At(new Vector3(0, 0.7f - i * 0.02f, 0.04f), new Vector3(-20, 0, 0)))
                    k.Box(Vector3.zero, new Vector3(w - 0.04f, 0.003f, 0.028f), slat, 0.001f);
            k.Box(new Vector3(0, 0.7f - 14 * 0.02f - 0.005f, 0.04f), new Vector3(w - 0.02f, 0.012f, 0.03f), slat, 0.004f);
            // Лесенки-шнуры и шнур подъёма с кисточкой.
            foreach (var x in new[] { -0.8f, 0f, 0.8f })
                k.Box(new Vector3(x, 0.56f, 0.057f), new Vector3(0.004f, 0.32f, 0.002f), cord);
            var pull = new List<Vector3> { new(-1.05f, 0.72f, 0.07f), new(-1.06f, 0.2f, 0.075f), new(-1.06f, -0.2f, 0.075f) };
            k.Sweep(pull, new Vector2(0.004f, 0.004f), cord, Vector3.forward);
            using (k.At(new Vector3(-1.06f, -0.245f, 0.075f))) k.Lathe(new List<Vector2> { new(0, 0), new(0.01f, 0.01f), new(0.008f, 0.04f), new(0, 0.045f) }, k.Mat("WoodWalnut"), 12, 40f);
            // Подоконник.
            k.Box(new Vector3(0, -0.73f, 0.07f), new Vector3(w + 0.1f, 0.04f, 0.16f), sill, 0.008f, Uv.Tiled(2f));
            k.Box(new Vector3(0, -0.77f, 0.03f), new Vector3(w, 0.05f, 0.03f), sill, 0.004f, Uv.Tiled(2f));
            b.Part("Blinds", k);
        }
    }
}
