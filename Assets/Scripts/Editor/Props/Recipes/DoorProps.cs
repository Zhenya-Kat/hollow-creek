using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HollowCreek.Editor.Props.Recipes
{
    /// <summary>
    /// Входные двери изнутри помещений. Проём в стене — 1,4 × 2,4 м, стена толщиной 0,15 м.
    /// Точка отсчёта — середина проёма на полу в плоскости стены, лицо (+Z) смотрит в комнату.
    /// </summary>
    static class DoorProps
    {
        const float OpeningW = 1.4f, OpeningH = 2.4f, Wall = 0.15f;
        const float LeafW = 0.96f, LeafH = 2.18f;

        /// <summary>Коробка проёма: боковые вставки с филёнками, перемычка и наличник со стороны комнаты.</summary>
        static void Surround(MeshKit k, int wood, int trim, int panel)
        {
            var fill = (OpeningW - LeafW) / 2 - 0.02f;
            foreach (var s in new[] { -1f, 1f })
            {
                var x = s * (LeafW / 2 + 0.02f + fill / 2);
                k.Box(new Vector3(x, OpeningH / 2, 0), new Vector3(fill, OpeningH, Wall + 0.01f), wood, 0.004f, Uv.Tiled(2f));
                k.Box(new Vector3(x, 0.55f, Wall / 2 + 0.008f), new Vector3(fill - 0.07f, 0.7f, 0.012f), panel, 0.01f, Uv.Tiled(2f));
                k.Box(new Vector3(x, 1.55f, Wall / 2 + 0.008f), new Vector3(fill - 0.07f, 0.9f, 0.012f), panel, 0.01f, Uv.Tiled(2f));
                // Коробка вокруг полотна.
                k.Box(new Vector3(s * (LeafW / 2 + 0.01f), OpeningH / 2 - 0.1f, 0), new Vector3(0.02f, LeafH + 0.04f, Wall + 0.02f), trim, 0.003f, Uv.Tiled(2f));
            }
            k.Box(new Vector3(0, LeafH + 0.02f + (OpeningH - LeafH - 0.02f) / 2, 0), new Vector3(LeafW + 0.04f, OpeningH - LeafH - 0.02f, Wall + 0.01f), wood, 0.004f, Uv.Tiled(2f));
            // Наличник: две стойки и верх с карнизиком.
            var cz = Wall / 2 + 0.012f;
            foreach (var s in new[] { -1f, 1f })
                k.Box(new Vector3(s * (OpeningW / 2 + 0.04f), OpeningH / 2 + 0.03f, cz), new Vector3(0.1f, OpeningH + 0.06f, 0.025f), trim, 0.006f, Uv.Tiled(2f));
            k.Box(new Vector3(0, OpeningH + 0.08f, cz), new Vector3(OpeningW + 0.2f, 0.12f, 0.025f), trim, 0.006f, Uv.Tiled(2f));
            k.Box(new Vector3(0, OpeningH + 0.15f, cz + 0.01f), new Vector3(OpeningW + 0.26f, 0.03f, 0.045f), trim, 0.008f, Uv.Tiled(2f));
            // Плинтусные «башмаки» у наличника.
            foreach (var s in new[] { -1f, 1f })
                k.Box(new Vector3(s * (OpeningW / 2 + 0.04f), 0.08f, cz + 0.005f), new Vector3(0.11f, 0.16f, 0.035f), trim, 0.004f, Uv.Tiled(2f));
            // Порог.
            k.Box(new Vector3(0, 0.01f, 0), new Vector3(LeafW + 0.04f, 0.02f, Wall + 0.06f), trim, 0.004f, Uv.Tiled(2f));
        }

        // ------------------------------------------------------------------ Дом Элис

        /// <summary>Шестифиленчатая дверь из тёмного дерева: латунная ручка, накладка, петли, прорезь для почты, глазок и коврик.</summary>
        [PropRecipe]
        static void HouseDoor(PropBuilder b)
        {
            var k = new MeshKit();
            var wood = k.Mat("WoodDoor");
            var trim = k.Mat("WoodWalnut");
            var brass = k.Mat("Brass");
            var dark = k.Mat("BlackMetal");
            Surround(k, trim, trim, wood);

            // Полотно (закрыто, лицом в комнату).
            var leafZ = 0.015f;
            var front = leafZ + 0.0225f;
            k.Box(new Vector3(0, 0.01f + LeafH / 2, leafZ), new Vector3(LeafW, LeafH, 0.045f), wood, 0.004f, Uv.Tiled(1.2f));
            foreach (var (y0, y1) in new[] { (0.24f, 0.86f), (1.06f, 1.62f), (1.76f, 2.06f) })
            foreach (var s in new[] { -1f, 1f })
            {
                var h = y1 - y0;
                var c = new Vector3(s * 0.205f, (y0 + y1) / 2, front);
                // Фаска филёнки и выпуклое поле.
                k.Box(c, new Vector3(0.33f, h, 0.006f), wood, 0.003f, Uv.Tiled(1.2f));
                k.Box(c + new Vector3(0, 0, 0.006f), new Vector3(0.25f, h - 0.08f, 0.008f), wood, 0.012f, Uv.Tiled(1.2f));
            }
            // Петли (со стороны зрителя слева, +X).
            foreach (var y in new[] { 0.3f, 1.1f, 1.9f })
            {
                k.Box(new Vector3(LeafW / 2 - 0.02f, y, front + 0.001f), new Vector3(0.05f, 0.1f, 0.003f), brass, 0.001f);
                k.Cylinder(new Vector3(LeafW / 2 + 0.004f, y, front + 0.004f), 0.008f, 0.1f, brass, 12, 0.002f);
                k.Sphere(new Vector3(LeafW / 2 + 0.004f, y + 0.056f, front + 0.004f), 0.006f, brass, 8);
            }
            // Ручка-кноб с розеткой справа и замочная накладка.
            var hx = -(LeafW / 2 - 0.09f);
            using (k.At(new Vector3(hx, 1.0f, front), new Vector3(90, 0, 0)))
            {
                k.Lathe(new List<Vector2> { new(0, 0), new(0.032f, 0), new(0.033f, 0.004f), new(0.028f, 0.008f), new(0.012f, 0.012f), new(0.009f, 0.04f),
                    new(0.02f, 0.048f), new(0.03f, 0.058f), new(0.031f, 0.07f), new(0.022f, 0.08f), new(0, 0.083f) }, brass, 28, 35f);
            }
            var plate = new List<Vector2>();
            for (var i = 0; i <= 10; i++)
            {
                var a = Mathf.PI * i / 10;
                plate.Add(new Vector2(Mathf.Cos(a) * 0.022f, 0.05f + Mathf.Sin(a) * 0.022f));
            }
            for (var i = 0; i <= 10; i++)
            {
                var a = Mathf.PI + Mathf.PI * i / 10;
                plate.Add(new Vector2(Mathf.Cos(a) * 0.022f, -0.05f + Mathf.Sin(a) * 0.022f));
            }
            using (k.At(new Vector3(hx, 0.86f, front + 0.002f))) k.Extrude(plate, 0.004f, brass);
            using (k.At(new Vector3(hx, 0.86f, front + 0.0045f)))
            {
                var hole = new List<Vector2>();
                for (var i = 0; i <= 12; i++)
                {
                    var a = Mathf.PI * 2 * i / 12;
                    hole.Add(new Vector2(Mathf.Cos(a) * 0.005f, 0.006f + Mathf.Sin(a) * 0.005f));
                }
                k.Extrude(hole, 0.001f, dark);
                k.Extrude(new[] { new Vector2(-0.003f, 0.004f), new Vector2(0.003f, 0.004f), new Vector2(0.004f, -0.014f), new Vector2(-0.004f, -0.014f) }, 0.001f, dark);
            }
            // Прорезь для почты и глазок.
            k.Box(new Vector3(0, 0.94f, front + 0.003f), new Vector3(0.28f, 0.07f, 0.006f), brass, 0.004f);
            k.Box(new Vector3(0, 0.94f, front + 0.0065f), new Vector3(0.22f, 0.02f, 0.002f), dark);
            using (k.At(new Vector3(0, 1.55f, front), new Vector3(90, 0, 0)))
            {
                k.Cylinder(new Vector3(0, 0.004f, 0), 0.012f, 0.008f, brass, 16, 0.002f);
                k.Disc(new Vector3(0, 0.0085f, 0), 0.006f, k.Mat("Glass"), 12);
            }
            // Нижняя латунная планка от ботинок.
            k.Box(new Vector3(0, 0.1f, front + 0.002f), new Vector3(LeafW - 0.04f, 0.16f, 0.004f), brass, 0.002f);
            b.Part("Door", k);

            // Коврик у двери.
            var m = new MeshKit();
            var mat = m.Mat("Cardboard");
            var border = m.Mat("LeatherRed");
            m.Box(new Vector3(0, 0.008f, 0.45f), new Vector3(0.9f, 0.016f, 0.55f), border, 0.006f, Uv.Tiled(4f));
            m.Box(new Vector3(0, 0.012f, 0.45f), new Vector3(0.8f, 0.016f, 0.45f), mat, 0.004f, Uv.Tiled(4f));
            b.Part("Mat", m);
        }

        // ------------------------------------------------------------------ Закусочная

        /// <summary>Алюминиевая остеклённая дверь закусочной: наклейка с совой, штанга, доводчик, колокольчик, табличка.</summary>
        [PropRecipe]
        static void DinerDoor(PropBuilder b)
        {
            var k = new MeshKit();
            var alu = k.Mat("Aluminium");
            var glass = k.Mat("DoorGlass");
            var decal = k.Mat("DoorDecal");
            var dark = k.Mat("PlasticBlack");
            var chrome = k.Mat("Chrome");
            // Боковые остеклённые вставки в алюминиевой раме.
            var fill = (OpeningW - LeafW) / 2 - 0.02f;
            foreach (var s in new[] { -1f, 1f })
            {
                var x = s * (LeafW / 2 + 0.02f + fill / 2);
                using (k.At(new Vector3(x, OpeningH / 2, 0)))
                {
                    StudyClues.Frame(k, alu, fill, OpeningH, 0.05f, Wall + 0.01f, 0, 0.004f);
                    k.Box(new Vector3(0, -OpeningH / 2 + 0.15f, 0), new Vector3(fill - 0.06f, 0.2f, 0.04f), alu, 0.004f);
                    k.Box(new Vector3(0, 0.1f, 0), new Vector3(fill - 0.08f, 2.1f, 0.01f), glass);
                }
            }
            // Перемычка над дверью.
            k.Box(new Vector3(0, LeafH + 0.02f + (OpeningH - LeafH - 0.02f) / 2, 0), new Vector3(LeafW + 0.04f, OpeningH - LeafH - 0.02f, Wall + 0.01f), alu, 0.004f);
            foreach (var s in new[] { -1f, 1f })
                k.Box(new Vector3(s * (LeafW / 2 + 0.01f), OpeningH / 2 - 0.1f, 0), new Vector3(0.02f, LeafH + 0.04f, Wall + 0.02f), alu, 0.003f);
            k.Box(new Vector3(0, 0.008f, 0), new Vector3(LeafW + 0.04f, 0.016f, Wall + 0.08f), alu, 0.003f);

            // Полотно: рама и стекло.
            var leafZ = 0.01f;
            using (k.At(new Vector3(0, 0.01f + LeafH / 2, leafZ)))
            {
                k.Box(new Vector3(0, LeafH / 2 - 0.05f, 0), new Vector3(LeafW, 0.1f, 0.045f), alu, 0.004f);
                k.Box(new Vector3(0, -LeafH / 2 + 0.125f, 0), new Vector3(LeafW, 0.25f, 0.045f), alu, 0.004f);
                k.Box(new Vector3(LeafW / 2 - 0.05f, 0, 0), new Vector3(0.1f, LeafH - 0.35f, 0.045f), alu, 0.004f);
                k.Box(new Vector3(-LeafW / 2 + 0.05f, 0, 0), new Vector3(0.1f, LeafH - 0.35f, 0.045f), alu, 0.004f);
                k.Box(new Vector3(0, 0.075f, 0), new Vector3(LeafW - 0.2f, LeafH - 0.35f, 0.008f), glass);
                // Наклейка с совой: на стекле, видна зеркально (читается с улицы).
                using (k.At(new Vector3(0, 0.25f, 0.0045f), new Vector3(0, 180, 0)))
                    k.Sheet(new Vector2(0.56f, 0.56f), 1, 1, decal, decal, null, 0f);
            }
            // Штанга «от себя».
            foreach (var x in new[] { -0.3f, 0.3f })
                using (k.At(new Vector3(x, 1.05f, leafZ + 0.0225f), new Vector3(90, 0, 0)))
                    k.Cylinder(new Vector3(0, 0.03f, 0), 0.012f, 0.06f, chrome, 14, 0.003f);
            using (k.At(new Vector3(0, 1.05f, leafZ + 0.08f), new Vector3(0, 0, 90)))
                k.Cylinder(Vector3.zero, 0.016f, 0.72f, chrome, 20, 0.006f);
            // Доводчик.
            k.Box(new Vector3(0.25f, LeafH - 0.06f, leafZ + 0.05f), new Vector3(0.3f, 0.06f, 0.06f), dark, 0.01f);
            k.Box(new Vector3(0.05f, LeafH + 0.03f, leafZ + 0.07f), new Vector3(0.3f, 0.02f, 0.02f), dark, 0.005f);
            // Колокольчик на пружине над дверью.
            var brass = k.Mat("Brass");
            var spring = new List<Vector3>();
            for (var i = 0; i <= 12; i++)
            {
                var t = i / 12f;
                spring.Add(new Vector3(-0.2f, LeafH + 0.15f - Mathf.Sin(t * Mathf.PI / 2) * 0.1f, Wall / 2 + 0.02f + t * 0.1f));
            }
            k.Sweep(spring, new Vector2(0.006f, 0.003f), brass);
            var bellTop = spring[spring.Count - 1];
            using (k.At(bellTop + new Vector3(0, -0.06f, 0)))
                k.Lathe(new List<Vector2> { new(0.035f, 0), new(0.03f, 0.006f), new(0.022f, 0.03f), new(0.016f, 0.05f), new(0.008f, 0.058f), new(0, 0.06f) }, brass, 20, 40f);
            b.Part("Door", k);

            // Табличка-перевёртыш на шнурке: с этой стороны — «CLOSED».
            var s2 = new MeshKit();
            var card = s2.Mat("PlasticBeige");
            var cord = s2.Mat("Cord");
            s2.Box(new Vector3(0, 1.5f, leafZ + 0.03f), new Vector3(0.3f, 0.12f, 0.004f), card, 0.01f);
            s2.Sweep(new List<Vector3> { new(-0.13f, 1.56f, leafZ + 0.028f), new(0, 1.72f, leafZ + 0.026f), new(0.13f, 1.56f, leafZ + 0.028f) }, new Vector2(0.003f, 0.003f), cord, Vector3.forward);
            s2.Sphere(new Vector3(0, 1.72f, leafZ + 0.026f), 0.006f, s2.Mat("Chrome"), 8);
            b.Part("Sign", s2);
            b.Text("Sign Text", null, new Vector3(0, 1.5f, leafZ + 0.0325f), Vector3.zero, new Vector2(0.26f, 0.09f),
                PropFont.Serif, new Color(0.62f, 0.1f, 0.1f), literal: "CLOSED");
        }
    }
}
