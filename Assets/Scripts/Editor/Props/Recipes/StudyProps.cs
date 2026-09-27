using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HollowCreek.Editor.Props.Recipes
{
    /// <summary>Предметы из кабинета Элис: сейф, часы, карандаш, ежедневник.</summary>
    static class StudyProps
    {
        // ------------------------------------------------------------------ Сейф

        const float SafeW = 0.70f, SafeH = 0.80f, SafeD = 0.60f, SafeBase = 0.10f;
        const float DoorW = 0.56f, DoorH = 0.66f, DoorT = 0.05f;
        static readonly float DoorY = SafeBase + SafeH / 2;         // центр двери по высоте
        static readonly float FrontZ = SafeD / 2;                   // передняя плоскость корпуса

        /// <summary>
        /// Старинный сейф «Ward &amp; Sons»: эмаль с золотыми линиями, картина на дверце, четыре колеса
        /// кодового замка, ручка-штурвал. Точка отсчёта — середина дна. Дочерние «Door Closed» и
        /// «Door Open» переключаются по состоянию игры.
        /// </summary>
        [PropRecipe]
        static void Safe(PropBuilder b)
        {
            var k = new MeshKit();
            var enamel = k.Mat("SafeEnamel");
            var black = k.Mat("BlackMetal");
            var gold = k.Mat("GoldPaint");
            var brass = k.Mat("Brass");
            var inside = k.Mat("SafeInside");

            // Цоколь и ножки-шары.
            k.Box(new Vector3(0, 0.07f, 0), new Vector3(SafeW + 0.02f, 0.06f, SafeD + 0.02f), black, 0.012f);
            foreach (var x in new[] { -1f, 1f })
            foreach (var z in new[] { -1f, 1f })
                k.Sphere(new Vector3(x * (SafeW / 2 - 0.05f), 0.022f, z * (SafeD / 2 - 0.05f)), 0.022f, brass, 14, 1f);
            // Верхний карниз.
            k.Box(new Vector3(0, SafeBase + SafeH + 0.016f, 0), new Vector3(SafeW + 0.024f, 0.032f, SafeD + 0.024f), black, 0.01f);
            k.Box(new Vector3(0, SafeBase + SafeH + 0.036f, 0), new Vector3(SafeW - 0.02f, 0.012f, SafeD - 0.04f), black, 0.005f);

            // Корпус без передней грани: проём под дверцу обрамлён рамкой, внутри — бордовая отделка.
            var bodyCenter = new Vector3(0, SafeBase + SafeH / 2, 0);
            k.Box(bodyCenter, new Vector3(SafeW, SafeH, SafeD), enamel, 0.03f, Uv.Tiled(2f), 3, skip: new[] { MeshKit.Face.Front });
            var sideW = (SafeW - DoorW) / 2;
            var capH = (SafeH - DoorH) / 2;
            k.Box(new Vector3(DoorW / 2 + sideW / 2, DoorY, FrontZ - 0.02f), new Vector3(sideW, SafeH - 0.002f, 0.04f), enamel, 0.012f, Uv.Tiled(2f));
            k.Box(new Vector3(-DoorW / 2 - sideW / 2, DoorY, FrontZ - 0.02f), new Vector3(sideW, SafeH - 0.002f, 0.04f), enamel, 0.012f, Uv.Tiled(2f));
            k.Box(new Vector3(0, DoorY + DoorH / 2 + capH / 2, FrontZ - 0.02f), new Vector3(DoorW + 0.01f, capH, 0.04f), enamel, 0.012f, Uv.Tiled(2f));
            k.Box(new Vector3(0, DoorY - DoorH / 2 - capH / 2, FrontZ - 0.02f), new Vector3(DoorW + 0.01f, capH, 0.04f), enamel, 0.012f, Uv.Tiled(2f));
            // Внутренние стенки (видны, когда дверца открыта) и полка.
            var inW = DoorW - 0.01f;
            var inH = DoorH - 0.01f;
            var inD = SafeD - 0.09f;
            var inZ = FrontZ - 0.04f - inD / 2;
            k.Box(new Vector3(0, DoorY, inZ - inD / 2), new Vector3(inW, inH, 0.01f), inside);
            k.Box(new Vector3(inW / 2, DoorY, inZ), new Vector3(0.01f, inH, inD), inside);
            k.Box(new Vector3(-inW / 2, DoorY, inZ), new Vector3(0.01f, inH, inD), inside);
            k.Box(new Vector3(0, DoorY + inH / 2, inZ), new Vector3(inW, 0.01f, inD), inside);
            k.Box(new Vector3(0, DoorY - inH / 2, inZ), new Vector3(inW, 0.01f, inD), inside);
            k.Box(new Vector3(0, DoorY + 0.06f, inZ + 0.01f), new Vector3(inW, 0.012f, inD - 0.02f), k.Mat("WoodWalnut"), 0.002f, Uv.Tiled(3f));
            // Внутренний ящичек с латунной ручкой.
            k.Box(new Vector3(0, DoorY - 0.2f, inZ + 0.02f), new Vector3(inW - 0.04f, 0.1f, inD - 0.06f), k.Mat("WoodWalnut"), 0.004f, Uv.Tiled(3f));
            k.Sphere(new Vector3(0, DoorY - 0.2f, inZ + 0.02f + (inD - 0.06f) / 2 + 0.008f), 0.01f, brass, 10);

            // Золотые линии на боковинах и крышке.
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * (SafeW / 2 + 0.001f);
                GoldFrame(k, gold, new Vector3(x, bodyCenter.y, 0), new Vector2(SafeD - 0.12f, SafeH - 0.12f), Vector3.right);
            }

            // Петли на левой (для зрителя) стороне.
            foreach (var y in new[] { DoorY - 0.2f, DoorY + 0.2f })
            {
                k.Cylinder(new Vector3(DoorW / 2 + 0.014f, y, FrontZ + 0.012f), 0.014f, 0.09f, brass, 16, 0.003f);
                k.Sphere(new Vector3(DoorW / 2 + 0.014f, y + 0.052f, FrontZ + 0.012f), 0.009f, brass, 10);
                k.Sphere(new Vector3(DoorW / 2 + 0.014f, y - 0.052f, FrontZ + 0.012f), 0.009f, brass, 10);
            }
            b.Part("Body", k);

            var closed = b.Group("Door Closed", null, new Vector3(DoorW / 2, 0, FrontZ));
            b.Part("Door", SafeDoor(), closed);
            var open = b.Group("Door Open", null, new Vector3(DoorW / 2 + 0.01f, 0, FrontZ + 0.02f), new Vector3(0, 105, 0));
            b.Part("Door", SafeDoor(), open);
            open.gameObject.SetActive(false);
        }

        static void GoldFrame(MeshKit k, int gold, Vector3 center, Vector2 size, Vector3 normal)
        {
            // Две линии: широкая и тонкая, на плоскости с нормалью normal (±X).
            using (k.At(center, new Vector3(0, normal.x > 0 ? 90 : -90, 0)))
            {
                foreach (var (inset, width) in new[] { (0f, 0.006f), (0.018f, 0.002f) })
                {
                    var w = size.x - inset * 2;
                    var h = size.y - inset * 2;
                    k.Box(new Vector3(0, h / 2, 0), new Vector3(w, width, 0.001f), gold);
                    k.Box(new Vector3(0, -h / 2, 0), new Vector3(w, width, 0.001f), gold);
                    k.Box(new Vector3(w / 2, 0, 0), new Vector3(width, h, 0.001f), gold);
                    k.Box(new Vector3(-w / 2, 0, 0), new Vector3(width, h, 0.001f), gold);
                }
            }
        }

        /// <summary>Дверца сейфа; точка отсчёта — ось петель (левый край для зрителя, задняя плоскость).</summary>
        static MeshKit SafeDoor()
        {
            var k = new MeshKit();
            var enamel = k.Mat("SafeEnamel");
            var face = k.Mat("SafeDoor");
            var brass = k.Mat("Brass");
            var dark = k.Mat("BlackMetal");
            var wheels = k.Mat("SafeWheels");
            var cx = -DoorW / 2;                       // центр дверцы относительно петель
            var front = DoorT / 2 + 0.012f;            // передняя плоскость дверцы
            // Сама дверца: лицевая грань с картиной, остальное — эмаль.
            k.Box(new Vector3(cx, DoorY, 0.012f), new Vector3(DoorW - 0.006f, DoorH - 0.006f, DoorT), enamel, 0.012f, Uv.Tiled(2f), 2,
                new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (face, Uv.Fit()) });
            // Задняя толстая плита с ригелями (видна при открытой дверце).
            k.Box(new Vector3(cx, DoorY, -0.03f), new Vector3(DoorW - 0.06f, DoorH - 0.06f, 0.04f), dark, 0.006f);
            foreach (var y in new[] { DoorY - 0.2f, DoorY, DoorY + 0.2f })
                k.Cylinder(new Vector3(cx - DoorW / 2 + 0.01f, y, -0.03f), 0.012f, 0.06f, k.Mat("Steel"), 14, 0.002f);

            var top = DoorY + DoorH / 2;
            // Кодовый замок: латунная планка и четыре колеса с цифрами.
            var lockY = top - 0.444f;
            k.Box(new Vector3(cx, lockY, front + 0.004f), new Vector3(0.30f, 0.064f, 0.008f), brass, 0.004f);
            k.Box(new Vector3(cx, lockY, front + 0.009f), new Vector3(0.22f, 0.036f, 0.004f), dark, 0.002f);
            var shown = new[] { 4, 7, 0, 2 };
            for (var i = 0; i < 4; i++)
            {
                var x = cx + (1.5f - i) * 0.048f;
                var digitAngle = ((shown[i] + 0.5f) / 10f - 0.25f) * 360f;
                using (k.At(new Vector3(x, lockY, front + 0.009f), new Vector3(0, 0, 90)))
                using (k.At(Vector3.zero, new Vector3(0, digitAngle, 0)))
                    k.Cylinder(Vector3.zero, 0.02f, 0.03f, wheels, 40, 0f, dark, null, true, 1f / 0.03f);
                // Ребристые шайбы по бокам колеса.
                foreach (var s in new[] { -1f, 1f })
                    using (k.At(new Vector3(x + s * 0.018f, lockY, front + 0.009f), new Vector3(0, 0, 90)))
                        k.Cylinder(Vector3.zero, 0.021f, 0.004f, brass, 20, 0.001f, faceted: true);
            }
            // Стрелка-указатель над колёсами.
            k.Box(new Vector3(cx, lockY + 0.038f, front + 0.012f), new Vector3(0.2f, 0.003f, 0.004f), brass);

            // Замочная скважина справа.
            var keyX = cx - 0.2f;
            var shield = new List<Vector2>();
            for (var i = 0; i <= 16; i++)
            {
                var a = Mathf.PI * i / 16;
                shield.Add(new Vector2(Mathf.Cos(a) * 0.018f, 0.012f + Mathf.Sin(a) * 0.018f));
            }
            shield.Add(new Vector2(-0.018f, -0.018f));
            shield.Add(new Vector2(0, -0.032f));
            shield.Add(new Vector2(0.018f, -0.018f));
            using (k.At(new Vector3(keyX, lockY, front + 0.002f))) k.Extrude(shield, 0.004f, brass);
            var hole = new List<Vector2>();
            for (var i = 0; i <= 12; i++)
            {
                var a = Mathf.PI * 2 * i / 12;
                hole.Add(new Vector2(Mathf.Cos(a) * 0.005f, 0.008f + Mathf.Sin(a) * 0.005f));
            }
            using (k.At(new Vector3(keyX, lockY, front + 0.0045f))) k.Extrude(hole, 0.001f, dark);
            using (k.At(new Vector3(keyX, lockY, front + 0.0045f)))
                k.Extrude(new[] { new Vector2(-0.003f, 0.006f), new Vector2(0.003f, 0.006f), new Vector2(0.0045f, -0.01f), new Vector2(-0.0045f, -0.01f) }, 0.001f, dark);

            // Ручка-штурвал: ступица, четыре спицы с шарами.
            var hubY = top - 0.554f;
            using (k.At(new Vector3(cx, hubY, front), new Vector3(90, 0, 0)))
            {
                k.Cylinder(new Vector3(0, 0.004f, 0), 0.05f, 0.008f, brass, 32, 0.002f);
                k.Cylinder(new Vector3(0, 0.022f, 0), 0.028f, 0.03f, brass, 24, 0.004f);
                k.Cylinder(new Vector3(0, 0.04f, 0), 0.016f, 0.01f, brass, 20, 0.003f);
            }
            for (var i = 0; i < 4; i++)
            {
                var a = (45 + i * 90) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var start = new Vector3(cx, hubY, front + 0.03f);
                using (k.At(start + dir * 0.06f, new Vector3(0, 0, i * 90 + 45 - 90)))
                    k.Cylinder(Vector3.zero, 0.007f, 0.08f, brass, 14, 0.002f);
                k.Sphere(start + dir * 0.105f, 0.014f, brass, 16);
            }
            return k;
        }

        // ------------------------------------------------------------------ Наручные часы

        /// <summary>
        /// Женские наручные часы: стальной корпус, треснувшее стекло, стрелки на 21:10, кожаный ремешок
        /// с пряжкой и медной стружкой. Лежат циферблатом вверх, «12» смотрит в −Z. Точка отсчёта — на полу.
        /// </summary>
        [PropRecipe]
        static void Watch(PropBuilder b)
        {
            var k = new MeshKit();
            var steel = k.Mat("Chrome");
            var dial = k.Mat("WatchDial");
            var glass = k.Mat("WatchGlass");
            var hands = k.Mat("BlackMetal");
            var leather = k.Mat("Leather");
            var copper = k.Mat("CopperShavings");
            var baseY = 0.0015f;

            // Корпус: тело вращения с ободком.
            var caseProfile = new List<Vector2>
            {
                new(0, 0), new(0.0165f, 0), new(0.0185f, 0.0015f), new(0.0192f, 0.004f), new(0.019f, 0.0075f),
                new(0.0182f, 0.0092f), new(0.0172f, 0.0098f), new(0.0165f, 0.0092f),
            };
            using (k.At(new Vector3(0, baseY, 0))) k.Lathe(caseProfile, steel, 48, 35f);
            // Циферблат и стекло.
            k.Disc(new Vector3(0, baseY + 0.0088f, 0), 0.0166f, dial, 48);
            k.Disc(new Vector3(0, baseY + 0.0104f, 0), 0.0168f, glass, 48);
            k.Disc(new Vector3(0, baseY + 0.0104f, 0), 0.0168f, glass, 48, false);
            // Стрелки (21:10): часовая почти на девяти, минутная на двух, секундная замерла.
            Hand(k, hands, baseY + 0.0091f, 275f, 0.0095f, 0.0016f, 0.0028f);
            Hand(k, hands, baseY + 0.0094f, 60f, 0.0135f, 0.0011f, 0.0020f);
            Hand(k, k.Mat("CeramicRim"), baseY + 0.0097f, 200f, 0.0145f, 0.00035f, 0.0006f, tail: 0.004f);
            k.Cylinder(new Vector3(0, baseY + 0.0098f, 0), 0.0012f, 0.0008f, steel, 12);
            // Заводная головка на «трёх часах» (для зрителя справа — это −X).
            using (k.At(new Vector3(-0.0205f, baseY + 0.0048f, 0), new Vector3(0, 0, 90)))
            {
                k.Cylinder(Vector3.zero, 0.0028f, 0.0035f, steel, 18, 0.0005f, faceted: true);
                k.Cylinder(new Vector3(0, 0.0022f, 0), 0.0012f, 0.002f, steel, 10);
            }
            // Ушки.
            foreach (var x in new[] { -0.0095f, 0.0095f })
            foreach (var z in new[] { -1f, 1f })
                k.Box(new Vector3(x, baseY + 0.0045f, z * 0.0205f), new Vector3(0.0032f, 0.0042f, 0.009f), steel, 0.0012f);
            b.Part("Case", k);

            // Ремешок: две половины, слегка изогнутые на полу.
            var s = new MeshKit();
            var strap = s.Mat("Leather");
            var metal = s.Mat("Chrome");
            var dark = s.Mat("PlasticBlack");
            var width = 0.0155f;
            var thick = 0.0022f;
            var top = new List<Vector3>();
            for (var i = 0; i <= 14; i++)
            {
                var t = i / 14f;
                var z = -0.021f - t * 0.085f;
                var y = baseY + thick / 2 + Mathf.Lerp(0.005f, 0f, Mathf.Clamp01(t * 3.5f));
                top.Add(new Vector3(0.012f * t * t, y, z));
            }
            s.Sweep(top, new Vector2(width, thick), strap, uvScale: 30f);
            var bottom = new List<Vector3>();
            for (var i = 0; i <= 18; i++)
            {
                var t = i / 18f;
                var z = 0.021f + t * 0.115f;
                var y = baseY + thick / 2 + Mathf.Lerp(0.005f, 0f, Mathf.Clamp01(t * 3.5f)) + (t > 0.85f ? (t - 0.85f) * 0.03f : 0);
                bottom.Add(new Vector3(-0.018f * t * t * t, y, z));
            }
            s.Sweep(bottom, new Vector2(width, thick), strap, uvScale: 30f);
            // Дырочки в ремешке.
            for (var i = 0; i < 5; i++)
            {
                var t = 0.45f + i * 0.08f;
                var p = bottom[Mathf.RoundToInt(t * 18)];
                s.Disc(p + new Vector3(0, thick / 2 + 0.0002f, 0), 0.0011f, dark, 10);
            }
            // Пряжка и шлёвка на конце верхней половины.
            var end = top[top.Count - 1];
            using (s.At(end + new Vector3(0, 0.0012f, -0.004f)))
            {
                s.Box(new Vector3(0, 0, -0.004f), new Vector3(width + 0.004f, 0.0016f, 0.0016f), metal, 0.0006f);
                s.Box(new Vector3(0, 0, 0.004f), new Vector3(width + 0.004f, 0.0016f, 0.0016f), metal, 0.0006f);
                s.Box(new Vector3(width / 2 + 0.0012f, 0, 0), new Vector3(0.0016f, 0.0016f, 0.0096f), metal, 0.0006f);
                s.Box(new Vector3(-width / 2 - 0.0012f, 0, 0), new Vector3(0.0016f, 0.0016f, 0.0096f), metal, 0.0006f);
                s.Box(new Vector3(0, 0.0008f, 0), new Vector3(0.0012f, 0.001f, 0.009f), metal, 0.0004f);
            }
            var keeper = top[10];
            s.Box(keeper + new Vector3(0, 0, 0), new Vector3(width + 0.0016f, thick + 0.0016f, 0.004f), strap, 0.0008f);
            b.Part("Strap", s);

            // Медная стружка, застрявшая в ремешке у корпуса.
            var c = new MeshKit();
            var cu = c.Mat("CopperShavings");
            var rnd = new System.Random(5);
            for (var i = 0; i < 9; i++)
            {
                var p = bottom[1 + rnd.Next(4)] + new Vector3((float)(rnd.NextDouble() - 0.5) * width * 0.9f, thick / 2 + 0.0004f, (float)(rnd.NextDouble() - 0.5) * 0.006f);
                using (c.At(p, new Vector3((float)rnd.NextDouble() * 60 - 30, (float)rnd.NextDouble() * 360, 80)))
                    c.Torus(Vector3.zero, 0.0009f + (float)rnd.NextDouble() * 0.0006f, 0.00022f, cu, 10, 4, 0, 200 + (float)rnd.NextDouble() * 200);
            }
            b.Part("Copper Shavings", c);
        }

        /// <summary>Стрелка, лежащая в плоскости циферблата; angle — по часовой от «12» (−Z).</summary>
        static void Hand(MeshKit k, int mat, float y, float angle, float length, float width, float leaf, float tail = 0.002f)
        {
            var outline = new List<Vector2>
            {
                new(0, length), new(leaf / 2, length * 0.72f), new(width / 2, length * 0.3f), new(width / 2, -tail),
                new(-width / 2, -tail), new(-width / 2, length * 0.3f), new(-leaf / 2, length * 0.72f),
            };
            outline.Reverse();
            using (k.At(new Vector3(0, y, 0), new Vector3(-90, angle, 0)))
                k.Extrude(outline, 0.0002f, mat);
        }

        // ------------------------------------------------------------------ Карандаш

        /// <summary>Шестигранный мягкий карандаш «2B» с ластиком. Лежит вдоль Z, грифель — к +Z. Точка отсчёта — центр на полу.</summary>
        [PropRecipe]
        static void Pencil(PropBuilder b)
        {
            var k = new MeshKit();
            var paint = k.Mat("PencilPaint");
            var wood = k.Mat("PencilWood");
            var graphite = k.Mat("Graphite");
            var ferrule = k.Mat("Brass");
            var eraser = k.Mat("Eraser");
            const float r = 0.0038f;
            using (k.At(new Vector3(0, r * 0.87f, 0), new Vector3(90, 0, 0)))
            {
                // Корпус: шесть граней.
                k.Lathe(new List<Vector2> { new(0, -0.068f), new(r, -0.068f), new(r, 0.058f), new(0, 0.058f) }, paint, 6, 10f, 1f, 0f, 360f, true);
                // Заточенный кончик: дерево и толстый грифель.
                var cone = new List<Vector2> { new(r * 0.97f, 0.058f), new(r * 0.8f, 0.062f), new(0.0019f, 0.078f) };
                k.Lathe(cone, wood, 24, 20f);
                k.Lathe(new List<Vector2> { new(0.0019f, 0.078f), new(0.0014f, 0.084f), new(0.0005f, 0.0868f), new(0, 0.0872f) }, graphite, 16, 40f);
                // Металлический колпачок с насечками.
                var cap = new List<Vector2>();
                var y0 = -0.08f;
                cap.Add(new Vector2(0, -0.068f));
                cap.Add(new Vector2(r * 0.95f, -0.068f));
                for (var i = 0; i < 5; i++)
                {
                    var y = -0.069f - i * 0.0022f;
                    cap.Add(new Vector2(r * 1.02f, y));
                    cap.Add(new Vector2(r * 0.96f, y - 0.0011f));
                }
                cap.Add(new Vector2(r * 1.0f, y0));
                cap.Add(new Vector2(0, y0));
                cap.Reverse();
                k.Lathe(cap, ferrule, 24, 30f);
                // Ластик со скруглённым краем.
                k.Lathe(new List<Vector2> { new(0, -0.0875f), new(0.0022f, -0.0874f), new(0.0031f, -0.0862f), new(0.0034f, -0.084f), new(0.0034f, -0.08f) }, eraser, 20, 40f);
            }
            b.Part("Body", k);
            b.Text("Stamp", null, new Vector3(0, r * 0.87f * 2 + 0.0002f, -0.02f), new Vector3(-90, 90, 0), new Vector2(0.06f, 0.004f),
                PropFont.Serif, new Color(0.86f, 0.7f, 0.35f), literal: "HOLLOW CREEK · 2B", fontSize: 0.03f);
        }

        /// <summary>Плоский треугольник толщиной <paramref name="thick"/>, лежащий на высоте y; вершины — в координатах (X, Z).</summary>
        internal static void FlatTriangle(MeshKit k, int mat, float y, float thick, params Vector2[] xz)
        {
            // Extrude кладёт точку (px, py) в (−px, py); поворот (−90,0,0) переводит её в (−px, 0, −py).
            var outline = new List<Vector2>();
            foreach (var p in xz) outline.Add(new Vector2(-p.x, -p.y));
            using (k.At(new Vector3(0, y, 0), new Vector3(-90, 0, 0))) k.Extrude(outline, thick, mat);
        }

        // ------------------------------------------------------------------ Ежедневник

        /// <summary>
        /// Ежедневник в красной коже: скруглённый корешок, латунные уголки, резинка и ленточка-закладка.
        /// Лежит на столе, корешок слева для зрителя (+X). Точка отсчёта — середина дна.
        /// </summary>
        [PropRecipe]
        static void Diary(PropBuilder b)
        {
            var k = new MeshKit();
            var cover = k.Mat("LeatherRed");
            var pages = k.Mat("PageEdges");
            var brass = k.Mat("Brass");
            var band = k.Mat("Rubber");
            const float w = 0.22f, d = 0.30f, t = 0.004f, blockH = 0.03f;
            // Обложки.
            k.Box(new Vector3(0, t / 2, 0), new Vector3(w, t, d), cover, 0.0015f, Uv.Tiled(6f));
            k.Box(new Vector3(0, t + blockH + t / 2, 0), new Vector3(w, t, d), cover, 0.0015f, Uv.Tiled(6f));
            // Блок страниц, утопленный от краёв обложки.
            k.Box(new Vector3(-0.004f, t + blockH / 2, 0), new Vector3(w - 0.014f, blockH, d - 0.01f), pages, 0.001f, Uv.Tiled(30f));
            // Корешок: половина цилиндра по длине книги.
            using (k.At(new Vector3(w / 2 - 0.002f, t + blockH / 2, 0), new Vector3(90, 0, 0)))
                k.Lathe(new List<Vector2> { new((blockH + 2 * t) / 2, -d / 2), new((blockH + 2 * t) / 2, d / 2) }, cover, 16, 50f, 6f, -90f, 90f);
            // Тиснёная рамка на обложке.
            var yTop = t * 2 + blockH + 0.0003f;
            foreach (var inset in new[] { 0.014f, 0.02f })
            {
                var fw = w - inset * 2 - 0.02f;
                var fd = d - inset * 2;
                var dark = k.Mat("Leather");
                k.Box(new Vector3(-0.01f, yTop, fd / 2), new Vector3(fw, 0.0006f, 0.0015f), dark);
                k.Box(new Vector3(-0.01f, yTop, -fd / 2), new Vector3(fw, 0.0006f, 0.0015f), dark);
                k.Box(new Vector3(-0.01f + fw / 2, yTop, 0), new Vector3(0.0015f, 0.0006f, fd), dark);
                k.Box(new Vector3(-0.01f - fw / 2, yTop, 0), new Vector3(0.0015f, 0.0006f, fd), dark);
            }
            // Латунные уголки у открытого края (для зрителя справа — −X), сверху и снизу.
            foreach (var z in new[] { -1f, 1f })
            foreach (var y in new[] { t * 2 + blockH + 0.0006f, -0.0006f })
                FlatTriangle(k, brass, y, 0.0012f,
                    new Vector2(-w / 2 - 0.0005f, z * (d / 2 + 0.0005f)),
                    new Vector2(-w / 2 + 0.028f, z * (d / 2 + 0.0005f)),
                    new Vector2(-w / 2 - 0.0005f, z * (d / 2 - 0.028f)));
            // Резинка поперёк книги у открытого края.
            var bandX = -w / 2 + 0.03f;
            var h = t * 2 + blockH;
            var loop = new List<Vector3>
            {
                new(bandX, h + 0.0012f, -d / 2 - 0.001f), new(bandX, h + 0.0012f, d / 2 + 0.001f),
            };
            k.Sweep(loop, new Vector2(0.006f, 0.0016f), band);
            k.Box(new Vector3(bandX, h / 2, d / 2 + 0.0017f), new Vector3(0.006f, h + 0.004f, 0.0016f), band);
            k.Box(new Vector3(bandX, h / 2, -d / 2 - 0.0017f), new Vector3(0.006f, h + 0.004f, 0.0016f), band);
            b.Part("Book", k);

            // Ленточка-закладка, выглядывающая снизу и лежащая на столе.
            var r = new MeshKit();
            var ribbon = r.Mat("CeramicRim");
            var path = new List<Vector3>
            {
                new(0.02f, t + blockH * 0.6f, d / 2 - 0.02f), new(0.021f, t + blockH * 0.6f, d / 2 + 0.002f),
                new(0.024f, t * 0.8f, d / 2 + 0.012f), new(0.03f, 0.0004f, d / 2 + 0.03f), new(0.045f, 0.0004f, d / 2 + 0.06f),
                new(0.06f, 0.0004f, d / 2 + 0.075f),
            };
            r.Sweep(path, new Vector2(0.007f, 0.0004f), ribbon, uvScale: 1f);
            b.Part("Ribbon", r);
        }
    }
}
