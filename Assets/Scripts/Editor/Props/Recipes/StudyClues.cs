using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HollowCreek.Editor.Props.Recipes
{
    /// <summary>Улики и обстановка кабинета: шкатулка, квитанция, записка, бумаги, ящик, диктофон, подсвечник, портрет, зеркало.</summary>
    static class StudyClues
    {
        static readonly Color InkBlue = new(0.12f, 0.16f, 0.42f);
        static readonly Color InkBlack = new(0.1f, 0.09f, 0.1f);
        static readonly Color InkPurple = new(0.42f, 0.2f, 0.36f);

        // ------------------------------------------------------------------ Фотошкатулка

        /// <summary>
        /// Кедровая шкатулка Рут: латунные уголки и петли, замок и четыре фотографии в рамках на передней стенке
        /// (школа, мост, кладбище, дом), на крышке — снимок женщины с девочкой. Точка отсчёта — середина дна, лицо +Z.
        /// </summary>
        [PropRecipe]
        static void PhotoBox(PropBuilder b)
        {
            var k = new MeshKit();
            var wood = k.Mat("WoodCedar");
            var brass = k.Mat("Brass");
            var photo = k.Mat("Photo");
            var dark = k.Mat("BlackMetal");
            const float w = 0.40f, h = 0.12f, d = 0.26f, lidH = 0.045f;
            k.Box(new Vector3(0, h / 2, 0), new Vector3(w, h, d), wood, 0.004f, Uv.Tiled(4f));
            k.Box(new Vector3(0, h + lidH / 2 + 0.001f, 0), new Vector3(w + 0.008f, lidH, d + 0.008f), wood, 0.008f, Uv.Tiled(4f));
            // Тонкий шов и молдинг по низу.
            k.Box(new Vector3(0, 0.006f, 0), new Vector3(w + 0.01f, 0.012f, d + 0.01f), wood, 0.003f, Uv.Tiled(4f));
            // Латунные уголки (8 штук).
            foreach (var x in new[] { -1f, 1f })
            foreach (var z in new[] { -1f, 1f })
            {
                k.Box(new Vector3(x * (w / 2 - 0.008f), 0.02f, z * (d / 2 - 0.008f)), new Vector3(0.022f, 0.04f, 0.022f), brass, 0.003f);
                k.Box(new Vector3(x * (w / 2 - 0.006f), h + lidH - 0.012f, z * (d / 2 - 0.006f)), new Vector3(0.026f, 0.028f, 0.026f), brass, 0.004f);
            }
            // Петли сзади.
            foreach (var x in new[] { -0.12f, 0.12f })
                using (k.At(new Vector3(x, h + 0.001f, -d / 2 - 0.004f), new Vector3(0, 0, 90)))
                    k.Cylinder(Vector3.zero, 0.005f, 0.05f, brass, 12, 0.001f);
            // Замок: пластина на стыке крышки и корпуса с замочной скважиной.
            var front = d / 2;
            var lockPlate = new List<Vector2>();
            for (var i = 0; i <= 20; i++)
            {
                var a = Mathf.PI * 2 * i / 20;
                lockPlate.Add(new Vector2(Mathf.Cos(a) * 0.026f, Mathf.Sin(a) * 0.032f));
            }
            using (k.At(new Vector3(0, h - 0.004f, front + 0.0015f))) k.Extrude(lockPlate, 0.003f, brass);
            var hole = new List<Vector2>();
            for (var i = 0; i <= 12; i++)
            {
                var a = Mathf.PI * 2 * i / 12;
                hole.Add(new Vector2(Mathf.Cos(a) * 0.0045f, 0.006f + Mathf.Sin(a) * 0.0045f));
            }
            using (k.At(new Vector3(0, h - 0.004f, front + 0.0032f)))
            {
                k.Extrude(hole, 0.0006f, dark);
                k.Extrude(new[] { new Vector2(-0.0025f, 0.004f), new Vector2(0.0025f, 0.004f), new Vector2(0.004f, -0.012f), new Vector2(-0.004f, -0.012f) }, 0.0006f, dark);
            }
            // Четыре фотографии в рамках по сторонам от замка (порядок слева направо — как их видит игрок).
            var xs = new[] { -0.15f, -0.085f, 0.085f, 0.15f };
            for (var i = 0; i < 4; i++)
            {
                var x = -xs[i];     // координата зрителя → локальная X
                var cy = h * 0.5f;
                const float ps = 0.05f;
                k.Box(new Vector3(x, cy, front + 0.001f), new Vector3(ps, ps, 0.002f), photo, 0f, null, 1,
                    new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (photo, Uv.Fit(new Rect(i / 5f + 0.01f, 0.02f, 0.18f, 0.96f))) });
                PhotoFrame(k, brass, new Vector3(x, cy, front + 0.002f), ps + 0.008f, 0.004f);
            }
            // Снимок на крышке.
            var lidTop = h + lidH + 0.001f;
            k.Box(new Vector3(0, lidTop + 0.0008f, 0), new Vector3(0.12f, 0.0016f, 0.12f), photo, 0f, null, 1,
                new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Top] = (photo, Uv.Fit(new Rect(0.81f, 0.02f, 0.18f, 0.96f))) });
            using (k.At(new Vector3(0, lidTop, 0), new Vector3(-90, 0, 0)))
                PhotoFrame(k, brass, Vector3.zero, 0.135f, 0.006f);
            // Латунная табличка под снимком.
            k.Box(new Vector3(0, lidTop + 0.001f, 0.085f), new Vector3(0.08f, 0.002f, 0.018f), brass, 0.001f);
            b.Part("Box", k);
            b.Text("Plate Text", null, new Vector3(0, lidTop + 0.0024f, 0.085f), PropBuilder.OnTop, new Vector2(0.074f, 0.014f),
                PropFont.Serif, new Color(0.25f, 0.18f, 0.08f), "photobox.plate", "Рут", fontSize: 0.1f);
        }

        /// <summary>Квадратная латунная рамка в плоскости XY (лицо +Z).</summary>
        static void PhotoFrame(MeshKit k, int mat, Vector3 c, float size, float bar)
        {
            var half = size / 2 - bar / 2;
            k.Box(c + new Vector3(0, half, 0), new Vector3(size, bar, 0.003f), mat, 0.001f);
            k.Box(c + new Vector3(0, -half, 0), new Vector3(size, bar, 0.003f), mat, 0.001f);
            k.Box(c + new Vector3(half, 0, 0), new Vector3(bar, size - bar * 2, 0.003f), mat, 0.001f);
            k.Box(c + new Vector3(-half, 0, 0), new Vector3(bar, size - bar * 2, 0.003f), mat, 0.001f);
        }

        // ------------------------------------------------------------------ Обрывок квитанции

        /// <summary>Оторванная нижняя часть бланка с подписью Мары. Лежит на полу, верх текста — к −Z.</summary>
        [PropRecipe]
        static void Receipt(PropBuilder b)
        {
            var k = new MeshKit();
            var paper = k.Mat("Receipt");
            var back = k.Mat("PaperAged");
            const float w = 0.095f, h = 0.125f;
            // Контур: ровные низ и бока, рваный верх с косым отрывом.
            var outline = new List<Vector2> { new(-w / 2, -h / 2), new(w / 2, -h / 2), new(w / 2, h / 2 - 0.03f) };
            var rnd = new System.Random(3);
            for (var i = 1; i < 22; i++)
            {
                var t = i / 22f;
                var x = w / 2 - t * w;
                var y = h / 2 - 0.03f + t * 0.03f + (float)(rnd.NextDouble() - 0.5) * 0.004f + Mathf.Sin(t * 9) * 0.002f;
                outline.Add(new Vector2(x, y));
            }
            outline.Add(new Vector2(-w / 2, h / 2));
            // UV: обрывок — нижние 60 % полного бланка (шапка и сумма оторваны).
            using (k.At(new Vector3(0, 0.0004f, 0), new Vector3(-90, 0, 0)))
                k.Extrude(outline, 0.0003f, back, paper, Uv.Fit(new Rect(0, 0, 1, 0.62f)), back);
            b.Part("Paper", k);

            var top = new Vector3(0, 0.0009f, 0);
            b.Text("Header", null, top + new Vector3(0, 0, -0.035f), PropBuilder.OnTop, new Vector2(0.085f, 0.012f),
                PropFont.SansBold, InkPurple, "receipt.print.header", "ОСНОВАНИЕ ПЛАТЕЖА", fontSize: 0.045f,
                align: TextAlignmentOptions.Left);
            b.Text("Printed", null, top + new Vector3(0.002f, 0, -0.022f), PropBuilder.OnTop, new Vector2(0.08f, 0.012f),
                PropFont.Hand, InkBlue, "receipt.hand.deposit", "Задаток получен", fontSize: 0.075f,
                align: TextAlignmentOptions.Left);
            b.Text("Signature Label", null, top + new Vector3(0.005f, 0, 0.03f), PropBuilder.OnTop, new Vector2(0.08f, 0.008f),
                PropFont.Sans, InkPurple, "receipt.print.signature", "Подпись получателя", fontSize: 0.035f,
                align: TextAlignmentOptions.Left);
            var sign = b.Text("Signature", null, top + new Vector3(0.012f, 0, 0.042f), PropBuilder.OnTop + new Vector3(0, -8, 0), new Vector2(0.07f, 0.016f),
                PropFont.Hand, InkBlue, "receipt.hand.signature", "Мара Уорд", fontSize: 0.12f, align: TextAlignmentOptions.Left);
            sign.fontStyle = FontStyles.Italic;
        }

        // ------------------------------------------------------------------ Записка у сейфа

        /// <summary>Листок из блокнота, приколотый кнопкой к стене. Лицо +Z, точка отсчёта — центр листка у стены.</summary>
        [PropRecipe]
        static void SafeNote(PropBuilder b)
        {
            var k = new MeshKit();
            var front = k.Mat("SafeNote");
            var back = k.Mat("Paper");
            var size = new Vector2(0.2f, 0.145f);
            var start = k.Sheet(size, 16, 12, front, back);
            // Нижние уголки чуть отходят от стены, лист слегка волнистый.
            k.Deform(start, p =>
            {
                var fromBottom = Mathf.Clamp01(p.y / size.y + 0.5f);            // 0 — низ, 1 — верх
                var fx = Mathf.Abs(p.x) / (size.x / 2);
                var lift = 0.01f * Mathf.Pow(1 - fromBottom, 3) * fx * fx * fx + 0.0006f * Mathf.Sin(p.x * 60);
                return new Vector3(p.x, p.y, p.z + 0.001f + lift);
            });
            b.Part("Paper", k);

            var pin = new MeshKit();
            var red = pin.Mat("CeramicRim");
            var steel = pin.Mat("Chrome");
            using (pin.At(new Vector3(0, size.y / 2 - 0.015f, 0.002f), new Vector3(90, 0, 0)))
            {
                pin.Lathe(new List<Vector2> { new(0, 0.001f), new(0.007f, 0.001f), new(0.0072f, 0.004f), new(0.0035f, 0.006f), new(0.004f, 0.011f), new(0.0055f, 0.013f), new(0, 0.0138f) }, red, 20, 45f);
                pin.Cylinder(new Vector3(0, -0.004f, 0), 0.0006f, 0.01f, steel, 8);
            }
            b.Part("Pin", pin);

            b.Text("Handwriting", null, new Vector3(0.004f, -0.012f, 0.0045f), new Vector3(0, 0, 2), new Vector2(0.165f, 0.1f),
                PropFont.Hand, InkBlue, "safe_note.hand", "Код — мой любимый праздник.\nСначала месяц, потом день.",
                align: TextAlignmentOptions.TopLeft);
        }

        // ------------------------------------------------------------------ Бумаги на столе

        /// <summary>Куча сброшенных бумаг: папка, листы с машинописью и бланки мэрии, смятые листы, скрепка.</summary>
        [PropRecipe]
        static void Papers(PropBuilder b)
        {
            var rnd = new System.Random(12);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);

            var folder = new MeshKit();
            var card = folder.Mat("Cardboard");
            using (folder.At(new Vector3(0.03f, 0.0008f, 0.01f), new Vector3(0, -6, 0)))
            {
                folder.Box(new Vector3(0, 0, 0), new Vector3(0.24f, 0.0012f, 0.32f), card, 0.0004f);
                // Раскрытая вторая половина папки.
                folder.Box(new Vector3(0.245f, 0, 0), new Vector3(0.24f, 0.0012f, 0.32f), card, 0.0004f);
                folder.Box(new Vector3(0.12f, 0.0012f, 0.12f), new Vector3(0.03f, 0.0006f, 0.06f), card);
            }
            b.Part("Folder", folder);

            var k = new MeshKit();
            var typed = k.Mat("Typed");
            var head = k.Mat("Letterhead");
            var back = k.Mat("Paper");
            var sheet = new Vector2(0.21f, 0.297f);
            for (var i = 0; i < 7; i++)
            {
                var y = 0.002f + i * 0.0009f;
                var pos = new Vector3(R(-0.12f, 0.16f), y, R(-0.06f, 0.06f));
                var yaw = R(-40f, 40f);
                using (k.At(pos, new Vector3(0, yaw, 0)))
                using (k.At(Vector3.zero, new Vector3(-90, 0, 0)))
                {
                    var start = k.Sheet(sheet, 10, 14, i % 3 == 0 ? head : typed, back);
                    var crumple = i == 2 || i == 5;
                    var seed = i * 1.7f;
                    k.Deform(start, p =>
                    {
                        // После поворота листа «вверх» — это +Z локальной системы листа.
                        var bend = 0.004f * Mathf.Sin(p.x * 18 + seed) * Mathf.Sin(p.y * 11 + seed);
                        var curl = 0.01f * Mathf.Pow(Mathf.Clamp01((Mathf.Abs(p.x) - 0.06f) / 0.05f), 2);
                        var c = crumple ? 0.012f * (Mathf.Sin(p.x * 70 + seed) * Mathf.Cos(p.y * 55 - seed) + 0.6f * Mathf.Sin(p.x * 130 + p.y * 90)) : 0;
                        return new Vector3(p.x, p.y, p.z + Mathf.Max(0, bend + curl + c));
                    });
                }
            }
            b.Part("Sheets", k);

            // Смятый в комок лист.
            var ball = new MeshKit();
            var ballMat = ball.Mat("Typed");
            var ballStart = ball.VertexCount;
            ball.Sphere(Vector3.zero, 0.035f, ballMat, 14, 0.8f);
            ball.Deform(ballStart, p =>
            {
                var n = Mathf.Sin(p.x * 180) * Mathf.Cos(p.y * 150) * Mathf.Sin(p.z * 170);
                return p * (1 + 0.28f * n);
            });
            b.Part("Crumpled", ball, null, new Vector3(-0.2f, 0.03f, 0.12f));

            // Скрепка.
            var clip = new MeshKit();
            var steel = clip.Mat("Chrome");
            var path = new List<Vector3>();
            void Arc(float cx, float cz, float r, float from, float to)
            {
                for (var i = 0; i <= 8; i++)
                {
                    var a = Mathf.Lerp(from, to, i / 8f) * Mathf.Deg2Rad;
                    path.Add(new Vector3(cx + Mathf.Cos(a) * r, 0, cz + Mathf.Sin(a) * r));
                }
            }
            path.Add(new Vector3(0.004f, 0, 0.012f));
            Arc(0, -0.012f, 0.004f, 0, -180);
            path.Add(new Vector3(-0.004f, 0, 0.016f));
            Arc(0, 0.016f, 0.0055f, 180, 0);
            path.Add(new Vector3(0.0055f, 0, -0.016f));
            Arc(0, -0.016f, 0.0055f, 0, -180);
            path.Add(new Vector3(-0.0055f, 0, 0.008f));
            var tube = new List<Vector3>();
            foreach (var p in path) tube.Add(p);
            clip.Sweep(tube, new Vector2(0.0008f, 0.0008f), steel);
            b.Part("Paper Clip", clip, null, new Vector3(0.08f, 0.0095f, -0.1f), new Vector3(0, 30, 0));
        }

        // ------------------------------------------------------------------ Ящик стола

        /// <summary>
        /// Выдвинутый и перекошенный ящик: лицевая панель с латунной ручкой, стенки и мелочи внутри.
        /// Точка отсчёта — центр лицевой панели, ящик уходит в −Z.
        /// </summary>
        [PropRecipe]
        static void Drawer(PropBuilder b)
        {
            var k = new MeshKit();
            var wood = k.Mat("WoodOak");
            var inner = k.Mat("WoodPine");
            var brass = k.Mat("Brass");
            var dark = k.Mat("BlackMetal");
            const float w = 0.55f, h = 0.14f, depth = 0.36f;
            // Лицевая панель с фаской и филёнкой.
            k.Box(Vector3.zero, new Vector3(w, h, 0.022f), wood, 0.004f, Uv.Tiled(3f));
            k.Box(new Vector3(0, 0, 0.011f), new Vector3(w - 0.06f, h - 0.05f, 0.004f), wood, 0.002f, Uv.Tiled(3f));
            // Ручка-скоба.
            foreach (var x in new[] { -0.06f, 0.06f })
                using (k.At(new Vector3(x, 0.01f, 0.014f), new Vector3(90, 0, 0)))
                    k.Cylinder(new Vector3(0, 0.012f, 0), 0.006f, 0.024f, brass, 14, 0.002f);
            using (k.At(new Vector3(0, 0.01f, 0.038f), new Vector3(0, 0, 90)))
                k.Cylinder(Vector3.zero, 0.0055f, 0.14f, brass, 16, 0.003f);
            // Замочная скважина.
            using (k.At(new Vector3(0, -0.035f, 0.0135f)))
            {
                var ring = new List<Vector2>();
                for (var i = 0; i <= 16; i++)
                {
                    var a = Mathf.PI * 2 * i / 16;
                    ring.Add(new Vector2(Mathf.Cos(a) * 0.009f, Mathf.Sin(a) * 0.012f));
                }
                k.Extrude(ring, 0.002f, brass);
            }
            using (k.At(new Vector3(0, -0.035f, 0.0148f)))
                k.Extrude(new[] { new Vector2(-0.0015f, 0.004f), new Vector2(0.0015f, 0.004f), new Vector2(0.0025f, -0.006f), new Vector2(-0.0025f, -0.006f) }, 0.0006f, dark);
            // Короб.
            var bh = h - 0.025f;
            k.Box(new Vector3(w / 2 - 0.03f, -0.005f, -depth / 2), new Vector3(0.014f, bh, depth), inner, 0.002f, Uv.Tiled(4f));
            k.Box(new Vector3(-w / 2 + 0.03f, -0.005f, -depth / 2), new Vector3(0.014f, bh, depth), inner, 0.002f, Uv.Tiled(4f));
            k.Box(new Vector3(0, -0.005f, -depth + 0.007f), new Vector3(w - 0.06f, bh, 0.014f), inner, 0.002f, Uv.Tiled(4f));
            k.Box(new Vector3(0, -bh / 2 - 0.002f, -depth / 2), new Vector3(w - 0.06f, 0.008f, depth), inner, 0.001f, Uv.Tiled(4f));
            b.Part("Box", k);

            // Содержимое: карандаши, конверт, ластик, моток бечёвки — всё сдвинуто при обыске.
            var c = new MeshKit();
            var paint = c.Mat("PencilPaint");
            var wood2 = c.Mat("PencilWood");
            var paper = c.Mat("PaperAged");
            var eraser = c.Mat("Eraser");
            var floorY = -bh / 2 + 0.002f;
            foreach (var (x, z, yaw) in new[] { (0.12f, -0.1f, 80f), (0.15f, -0.13f, 70f), (-0.05f, -0.25f, 10f) })
                using (c.At(new Vector3(x, floorY + 0.004f, z), new Vector3(90, yaw, 0)))
                {
                    c.Cylinder(Vector3.zero, 0.0036f, 0.15f, paint, 6, 0f, -1, null, true, 1f, true);
                    c.Lathe(new List<Vector2> { new(0.0035f, 0.075f), new(0.0008f, 0.092f) }, wood2, 12);
                }
            using (c.At(new Vector3(-0.1f, floorY + 0.002f, -0.12f), new Vector3(0, 15, 0)))
                c.Box(Vector3.zero, new Vector3(0.22f, 0.003f, 0.11f), paper, 0.001f);
            c.Box(new Vector3(0.05f, floorY + 0.006f, -0.28f), new Vector3(0.04f, 0.012f, 0.02f), eraser, 0.003f);
            c.Torus(new Vector3(-0.17f, floorY + 0.012f, -0.27f), 0.025f, 0.012f, c.Mat("Cord"), 20, 10);
            b.Part("Contents", c);
        }

        // ------------------------------------------------------------------ Диктофон за кабельной планкой

        /// <summary>
        /// Кабельный лоток под столешницей с проводами; за ним спрятан маленький диктофон, наружу торчит
        /// только решётка микрофона. Точка отсчёта — центр лотка, лицо +Z.
        /// </summary>
        [PropRecipe]
        static void Recorder(PropBuilder b)
        {
            var k = new MeshKit();
            var metal = k.Mat("SteelDark");
            var plastic = k.Mat("PlasticBlack");
            var grill = k.Mat("Mesh");
            var tape = k.Mat("Tape");
            const float len = 1.1f;
            // Лоток: П-образный профиль, открыт вверх (к столешнице).
            k.Box(new Vector3(0, -0.035f, 0), new Vector3(len, 0.004f, 0.07f), metal, 0.001f);
            k.Box(new Vector3(0, -0.012f, 0.033f), new Vector3(len, 0.05f, 0.004f), metal, 0.001f);
            k.Box(new Vector3(0, -0.012f, -0.033f), new Vector3(len, 0.05f, 0.004f), metal, 0.001f);
            // Кронштейны крепления.
            foreach (var x in new[] { -0.45f, 0f, 0.45f })
                k.Box(new Vector3(x, 0.006f, 0), new Vector3(0.02f, 0.012f, 0.08f), metal, 0.001f);
            // Провода, провисающие из лотка.
            var cable = k.Mat("Rubber");
            foreach (var (z, sag, start, end) in new[] { (0.01f, 0.03f, -0.55f, 0.55f), (-0.012f, 0.02f, -0.5f, 0.52f), (0.0f, 0.045f, -0.3f, 0.2f) })
            {
                var path = new List<Vector3>();
                for (var i = 0; i <= 20; i++)
                {
                    var t = i / 20f;
                    var x = Mathf.Lerp(start, end, t);
                    var y = -0.028f - Mathf.Min(sag * Mathf.Sin(t * Mathf.PI), 0.004f);
                    path.Add(new Vector3(x, y, z));
                }
                k.Sweep(path, new Vector2(0.006f, 0.006f), cable, Vector3.forward);
            }
            // Хвост провода, выпавший из лотка.
            var tail = new List<Vector3>();
            for (var i = 0; i <= 12; i++)
            {
                var t = i / 12f;
                tail.Add(new Vector3(0.52f + t * 0.04f, -0.03f - t * t * 0.14f, 0.02f + t * 0.03f));
            }
            k.Sweep(tail, new Vector2(0.006f, 0.006f), cable, Vector3.forward);
            b.Part("Cable Tray", k);

            // Диктофон: корпус внутри лотка, микрофон выглядывает над краем.
            var r = new MeshKit();
            var body = r.Mat("PlasticGrey");
            var black = r.Mat("PlasticBlack");
            var mesh = r.Mat("Mesh");
            var tapeMat = r.Mat("Tape");
            var red = r.Mat("CeramicRim");
            using (r.At(new Vector3(-0.32f, -0.016f, 0.012f), new Vector3(0, 0, -90)))
            {
                r.Box(Vector3.zero, new Vector3(0.036f, 0.1f, 0.015f), body, 0.005f);
                r.Box(new Vector3(0, 0.02f, 0.0078f), new Vector3(0.026f, 0.022f, 0.0012f), black, 0.001f);
                r.Box(new Vector3(0, -0.015f, 0.0078f), new Vector3(0.008f, 0.008f, 0.0015f), red, 0.002f);
                using (r.At(new Vector3(0, 0.058f, 0)))
                {
                    r.Cylinder(Vector3.zero, 0.009f, 0.018f, mesh, 20, 0.004f, -1, null, true, 40f);
                    r.Cylinder(new Vector3(0, -0.01f, 0), 0.0095f, 0.004f, black, 20, 0.001f);
                }
            }
            // Полоска скотча, которой диктофон прижат к лотку.
            r.Box(new Vector3(-0.3f, 0.0f, 0.02f), new Vector3(0.018f, 0.06f, 0.0006f), tapeMat);
            b.Part("Recorder", r);
        }

        // ------------------------------------------------------------------ Подсвечник и пыль

        /// <summary>Тяжёлый медный подсвечник с оплывшей свечой. Точка отсчёта — середина основания.</summary>
        [PropRecipe]
        static void Candlestick(PropBuilder b)
        {
            var k = new MeshKit();
            var copper = k.Mat("Copper");
            var wax = k.Mat("Wax");
            var wick = k.Mat("Wick");
            var profile = new List<Vector2>
            {
                new(0, 0), new(0.058f, 0), new(0.06f, 0.004f), new(0.058f, 0.012f), new(0.05f, 0.016f), new(0.047f, 0.022f),
                new(0.036f, 0.03f), new(0.024f, 0.036f), new(0.016f, 0.045f), new(0.012f, 0.06f), new(0.02f, 0.07f),
                new(0.021f, 0.075f), new(0.012f, 0.085f), new(0.0095f, 0.12f), new(0.0105f, 0.16f), new(0.018f, 0.172f),
                new(0.019f, 0.178f), new(0.011f, 0.19f), new(0.0095f, 0.215f), new(0.013f, 0.225f), new(0.042f, 0.232f),
                new(0.044f, 0.236f), new(0.04f, 0.24f), new(0.017f, 0.242f), new(0.016f, 0.262f), new(0.018f, 0.266f),
                new(0.0125f, 0.268f), new(0.0125f, 0.25f),
            };
            k.Lathe(profile, copper, 36, 38f, 4f);
            // Свеча с неровным оплывшим верхом и потёками.
            k.Lathe(new List<Vector2> { new(0.0122f, 0.25f), new(0.0122f, 0.36f), new(0.0118f, 0.368f), new(0.008f, 0.371f), new(0.004f, 0.366f), new(0, 0.367f) }, wax, 24, 35f);
            foreach (var (a, len) in new[] { (20f, 0.03f), (140f, 0.05f), (250f, 0.022f) })
            {
                var dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                var path = new List<Vector3>();
                for (var i = 0; i <= 6; i++)
                {
                    var t = i / 6f;
                    path.Add(dir * (0.0122f + 0.0012f) + new Vector3(0, 0.368f - t * len, 0));
                }
                k.Sweep(path, new Vector2(0.004f, 0.0022f), wax, dir);
                k.Sphere(path[path.Count - 1], 0.0022f, wax, 8);
            }
            using (k.At(new Vector3(0, 0.372f, 0), new Vector3(0, 0, 8)))
                k.Cylinder(new Vector3(0, 0.004f, 0), 0.0008f, 0.01f, wick, 6);
            b.Part("Body", k);
        }

        /// <summary>Слой пыли на полке 1 × 0,3 м с чистыми кругами от подсвечников (u = 0.2 и 0.8, вдоль X).</summary>
        [PropRecipe]
        static void ShelfDust(PropBuilder b)
        {
            var k = new MeshKit();
            var dust = k.Mat("DustRing");
            using (k.At(new Vector3(0, 0.0015f, 0), new Vector3(-90, 0, 0)))
                k.Sheet(new Vector2(1f, 0.3f), 1, 1, dust, dust, null, 0f);
            b.Part("Dust", k);
        }

        // ------------------------------------------------------------------ Портрет

        /// <summary>Портрет Эвелин Блейк в резной золочёной раме, подпись выцветшими чернилами. Лицо +Z, точка отсчёта — центр у стены.</summary>
        [PropRecipe]
        static void Portrait(PropBuilder b)
        {
            var k = new MeshKit();
            var gilt = k.Mat("BrassDark");
            var gold = k.Mat("GoldPaint");
            var canvas = k.Mat("Portrait");
            var wood = k.Mat("WoodWalnut");
            const float w = 0.70f, h = 0.90f;
            // Ступенчатый профиль рамы: наружный валик, скос, внутренний бортик.
            Frame(k, gilt, w, h, 0.075f, 0.03f, 0.015f, 0.006f);
            Frame(k, gold, w - 0.03f, h - 0.03f, 0.03f, 0.018f, 0.038f, 0.004f);
            Frame(k, wood, w - 0.1f, h - 0.1f, 0.02f, 0.012f, 0.03f, 0.003f);
            Frame(k, gold, w - 0.13f, h - 0.13f, 0.008f, 0.008f, 0.026f, 0.002f);
            // Розетки в углах.
            foreach (var x in new[] { -1f, 1f })
            foreach (var y in new[] { -1f, 1f })
                using (k.At(new Vector3(x * (w / 2 - 0.035f), y * (h / 2 - 0.035f), 0.045f), new Vector3(90, 0, 0)))
                {
                    k.Cylinder(Vector3.zero, 0.028f, 0.008f, gilt, 16, 0.003f);
                    k.Sphere(new Vector3(0, 0.006f, 0), 0.012f, gold, 12, 0.6f);
                    for (var i = 0; i < 8; i++)
                    {
                        var a = i * Mathf.PI / 4;
                        k.Sphere(new Vector3(Mathf.Cos(a) * 0.02f, 0.004f, Mathf.Sin(a) * 0.02f), 0.006f, gold, 8, 0.6f);
                    }
                }
            // Картуш сверху.
            using (k.At(new Vector3(0, h / 2 + 0.01f, 0.03f), new Vector3(90, 0, 0)))
                k.Cylinder(Vector3.zero, 0.05f, 0.02f, gilt, 24, 0.008f);
            k.Sphere(new Vector3(0, h / 2 + 0.01f, 0.045f), 0.02f, gold, 14, 0.7f);
            // Холст и задник.
            k.Box(new Vector3(0, 0, 0.012f), new Vector3(w - 0.14f, h - 0.14f, 0.004f), wood, 0f, null, 1,
                new Dictionary<MeshKit.Face, (int, Uv)> { [MeshKit.Face.Front] = (canvas, Uv.Fit()) });
            k.Box(new Vector3(0, 0, 0.004f), new Vector3(w - 0.02f, h - 0.02f, 0.008f), wood, 0.002f);
            // Проволока подвеса над рамой.
            k.Sweep(new List<Vector3> { new(0.2f, h / 2 - 0.1f, 0.002f), new(0.05f, h / 2 + 0.06f, 0.002f), new(0, h / 2 + 0.07f, 0.002f), new(-0.05f, h / 2 + 0.06f, 0.002f), new(-0.2f, h / 2 - 0.1f, 0.002f) },
                new Vector2(0.002f, 0.002f), k.Mat("Steel"), Vector3.forward);
            k.Sphere(new Vector3(0, h / 2 + 0.072f, 0.004f), 0.006f, k.Mat("Brass"), 10);
            b.Part("Frame", k);
            b.Text("Name", null, new Vector3(0, -h / 2 + 0.02f, 0.0415f), Vector3.zero, new Vector2(0.3f, 0.03f),
                PropFont.Hand, new Color(0.18f, 0.13f, 0.09f, 0.75f), "portrait.name", "Эвелин Блейк", fontSize: 0.22f);
        }

        /// <summary>Прямоугольная рама из четырёх брусков в плоскости XY (лицо +Z).</summary>
        internal static void Frame(MeshKit k, int mat, float w, float h, float bar, float depth, float z, float radius)
        {
            k.Box(new Vector3(0, h / 2 - bar / 2, z), new Vector3(w, bar, depth), mat, radius);
            k.Box(new Vector3(0, -h / 2 + bar / 2, z), new Vector3(w, bar, depth), mat, radius);
            k.Box(new Vector3(w / 2 - bar / 2, 0, z), new Vector3(bar, h - bar * 2, depth), mat, radius);
            k.Box(new Vector3(-w / 2 + bar / 2, 0, z), new Vector3(bar, h - bar * 2, depth), mat, radius);
        }

        // ------------------------------------------------------------------ Зеркало

        /// <summary>Рама старого зеркала из тёмного дерева с резным навершием. Лицо +Z, точка отсчёта — центр у стены.</summary>
        [PropRecipe]
        static void MirrorFrame(PropBuilder b)
        {
            var k = new MeshKit();
            var wood = k.Mat("WoodWalnut");
            var brass = k.Mat("BrassDark");
            const float w = 0.90f, h = 1.50f;
            Frame(k, wood, w, h, 0.09f, 0.045f, 0.022f, 0.012f);
            Frame(k, wood, w - 0.06f, h - 0.06f, 0.03f, 0.02f, 0.05f, 0.006f);
            Frame(k, brass, w - 0.17f, h - 0.17f, 0.012f, 0.008f, 0.047f, 0.003f);
            // Навершие-фронтон.
            var crest = new List<Vector2>();
            for (var i = 0; i <= 24; i++)
            {
                var t = i / 24f;
                var x = Mathf.Lerp(-0.34f, 0.34f, t);
                var y = 0.02f + 0.13f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.5f) + 0.025f * Mathf.Sin(t * Mathf.PI * 6);
                crest.Add(new Vector2(x, y));
            }
            crest.Add(new Vector2(0.34f, 0));
            crest.Add(new Vector2(-0.34f, 0));
            crest.Reverse();
            using (k.At(new Vector3(0, h / 2 - 0.01f, 0.02f))) k.Extrude(crest, 0.03f, wood);
            k.Sphere(new Vector3(0, h / 2 + 0.16f, 0.03f), 0.03f, brass, 14);
            // Точёные «пуговицы» по бокам.
            foreach (var x in new[] { -1f, 1f })
            foreach (var y in new[] { -0.45f, 0f, 0.45f })
                k.Sphere(new Vector3(x * (w / 2 - 0.045f), y, 0.05f), 0.016f, brass, 12, 0.7f);
            // Задник.
            k.Box(new Vector3(0, 0, 0.004f), new Vector3(w - 0.04f, h - 0.04f, 0.008f), wood);
            b.Part("Frame", k);
        }

        /// <summary>Стекло зеркала (0,72 × 1,30 м).</summary>
        [PropRecipe]
        static void MirrorGlass(PropBuilder b)
        {
            var k = new MeshKit();
            k.Box(Vector3.zero, new Vector3(0.72f, 1.30f, 0.006f), k.Mat("MirrorGlass"));
            b.Part("Glass", k);
        }

        /// <summary>Запотевшее стекло с потёками — на нём проступает надпись.</summary>
        [PropRecipe]
        static void MirrorGlassFogged(PropBuilder b)
        {
            var k = new MeshKit();
            k.Box(Vector3.zero, new Vector3(0.72f, 1.30f, 0.006f), k.Mat("MirrorGlass"));
            var fog = k.Mat("MirrorFog");
            using (k.At(new Vector3(0, 0, 0.0045f))) k.Sheet(new Vector2(0.72f, 1.30f), 1, 1, fog, fog, null, 0f);
            b.Part("Glass", k);
        }
    }
}

