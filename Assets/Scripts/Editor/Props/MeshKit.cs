using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCreek.Editor.Props
{
    /// <summary>Как раскладывать текстуру по грани.</summary>
    public readonly struct Uv
    {
        public readonly bool World;
        public readonly float Scale;
        public readonly Rect Rect;

        Uv(bool world, float scale, Rect rect)
        {
            World = world;
            Scale = scale;
            Rect = rect;
        }

        /// <summary>Текстура повторяется: <paramref name="scale"/> повторов на метр.</summary>
        public static Uv Tiled(float scale = 1f) => new(true, scale, new Rect(0, 0, 1, 1));

        /// <summary>Вся текстура (или её часть <paramref name="rect"/>) растягивается на грань.</summary>
        public static Uv Fit(Rect? rect = null) => new(false, 1f, rect ?? new Rect(0, 0, 1, 1));
    }

    /// <summary>
    /// Собирает меш из простых форм: скруглённых коробок, тел вращения, выдавленных контуров и листов.
    /// Соглашения: метры; лицевая сторона предмета смотрит в +Z, зритель стоит со стороны +Z,
    /// поэтому «вправо» для него — это −X. Каждый материал — отдельный подмеш.
    /// </summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> positions = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uvs = new();
        readonly List<List<int>> triangles = new();
        readonly List<string> materials = new();
        readonly Stack<Matrix4x4> stack = new();
        Matrix4x4 matrix = Matrix4x4.identity;

        public IReadOnlyList<string> Materials => materials;
        public int VertexCount => positions.Count;

        /// <summary>Индекс подмеша для материала с этим ключом (см. <see cref="PropMaterials"/>).</summary>
        public int Mat(string key)
        {
            var index = materials.IndexOf(key);
            if (index >= 0) return index;
            materials.Add(key);
            triangles.Add(new List<int>());
            return materials.Count - 1;
        }

        // ---------- Трансформации ----------

        /// <summary>Сдвинуть/повернуть/масштабировать всё, что строится внутри using-блока.</summary>
        public IDisposable At(Vector3 position, Vector3 euler = default, Vector3? scale = null)
        {
            stack.Push(matrix);
            matrix *= Matrix4x4.TRS(position, Quaternion.Euler(euler), scale ?? Vector3.one);
            return new Scope(this);
        }

        sealed class Scope : IDisposable
        {
            readonly MeshKit kit;
            bool done;
            public Scope(MeshKit kit) => this.kit = kit;

            public void Dispose()
            {
                if (done) return;
                done = true;
                kit.matrix = kit.stack.Pop();
            }
        }

        // ---------- Низкий уровень ----------

        public int Vertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            positions.Add(matrix.MultiplyPoint3x4(p));
            normals.Add(matrix.inverse.transpose.MultiplyVector(n).normalized);
            uvs.Add(uv);
            return positions.Count - 1;
        }

        /// <summary>Треугольник; порядок обхода подбирается по нормалям вершин.</summary>
        public void Tri(int mat, int a, int b, int c)
        {
            var face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            var n = normals[a] + normals[b] + normals[c];
            var list = triangles[mat];
            if (Vector3.Dot(face, n) >= 0)
            {
                list.Add(a); list.Add(b); list.Add(c);
            }
            else
            {
                list.Add(a); list.Add(c); list.Add(b);
            }
        }

        public void Quad(int mat, int a, int b, int c, int d)
        {
            Tri(mat, a, b, c);
            Tri(mat, a, c, d);
        }

        // ---------- Коробка со скруглёнными рёбрами ----------

        static readonly (Vector3 n, Vector3 u, Vector3 v)[] Faces =
        {
            (Vector3.forward, Vector3.left, Vector3.up),    // лицевая: зритель справа видит −X
            (Vector3.back, Vector3.right, Vector3.up),
            (Vector3.right, Vector3.forward, Vector3.up),
            (Vector3.left, Vector3.back, Vector3.up),
            (Vector3.up, Vector3.left, Vector3.back),       // верх: текст читается со стороны +Z
            (Vector3.down, Vector3.left, Vector3.forward),
        };

        /// <summary>Индексы граней для <see cref="Box"/>: какие грани рисовать и каким материалом.</summary>
        public enum Face { Front, Back, Right, Left, Top, Bottom }

        /// <summary>
        /// Коробка с центром <paramref name="center"/>. <paramref name="radius"/> — радиус скругления рёбер.
        /// <paramref name="faceMats"/> позволяет задать грани свой материал и UV (например, лицевой — текстуру).
        /// </summary>
        public void Box(Vector3 center, Vector3 size, int mat, float radius = 0f, Uv? uv = null, int segments = 2,
            IDictionary<Face, (int mat, Uv uv)> faceMats = null, ICollection<Face> skip = null)
        {
            var half = size * 0.5f;
            radius = Mathf.Min(radius, half.x * 0.99f, half.y * 0.99f, half.z * 0.99f);
            var inner = half - Vector3.one * radius;
            for (var f = 0; f < 6; f++)
            {
                var face = (Face)f;
                if (skip != null && skip.Contains(face)) continue;
                var (n, u, v) = Faces[f];
                var faceMat = mat;
                var faceUv = uv ?? Uv.Tiled();
                if (faceMats != null && faceMats.TryGetValue(face, out var custom)) (faceMat, faceUv) = custom;

                var hu = Abs(Vector3.Dot(half, u));
                var hv = Abs(Vector3.Dot(half, v));
                var hn = Abs(Vector3.Dot(half, n));
                var iu = hu - radius;
                var iv = hv - radius;
                var us = Coords(iu, radius, radius > 0 ? segments : 0);
                var vs = Coords(iv, radius, radius > 0 ? segments : 0);
                var start = positions.Count;
                foreach (var cv in vs)
                foreach (var cu in us)
                {
                    var flat = n * hn + u * cu + v * cv;
                    var clamped = new Vector3(Clamp(flat.x, inner.x), Clamp(flat.y, inner.y), Clamp(flat.z, inner.z));
                    var dir = radius > 0 ? (flat - clamped).normalized : n;
                    var p = radius > 0 ? clamped + dir * radius : flat;
                    Vector2 t;
                    if (faceUv.World)
                        t = new Vector2(cu, cv) * faceUv.Scale;
                    else
                    {
                        var r = faceUv.Rect;
                        t = new Vector2(r.x + (cu + hu) / (2 * hu) * r.width, r.y + (cv + hv) / (2 * hv) * r.height);
                    }
                    Vertex(center + p, dir, t);
                }
                var w = us.Count;
                for (var j = 0; j < vs.Count - 1; j++)
                for (var i = 0; i < w - 1; i++)
                {
                    var a = start + j * w + i;
                    Quad(faceMat, a, a + 1, a + w + 1, a + w);
                }
            }
        }

        static float Abs(float x) => Mathf.Abs(x);
        static float Clamp(float x, float limit) => Mathf.Clamp(x, -limit, limit);

        /// <summary>Координаты вдоль ребра: плоская часть и скругления на концах (равные углы).</summary>
        static List<float> Coords(float inner, float radius, int segments)
        {
            var list = new List<float>();
            for (var k = segments; k >= 1; k--) list.Add(-inner - radius * Mathf.Tan(Mathf.PI / 4 * k / segments));
            list.Add(-inner);
            list.Add(inner);
            for (var k = 1; k <= segments; k++) list.Add(inner + radius * Mathf.Tan(Mathf.PI / 4 * k / segments));
            return list;
        }

        // ---------- Тело вращения ----------

        /// <summary>
        /// Тело вращения вокруг локальной оси Y. Профиль — точки (радиус, высота) снизу вверх по внешней поверхности.
        /// Рёбра профиля с углом меньше <paramref name="smoothAngle"/> сглаживаются.
        /// </summary>
        public void Lathe(IList<Vector2> profile, int mat, int segments = 24, float smoothAngle = 40f,
            float uvScale = 1f, float fromDeg = 0f, float toDeg = 360f, bool faceted = false)
        {
            if (faceted)
            {
                LatheFaceted(profile, mat, segments, uvScale, fromDeg, toDeg);
                return;
            }
            var segNormals = new Vector2[profile.Count - 1];
            for (var i = 0; i < profile.Count - 1; i++)
            {
                var d = profile[i + 1] - profile[i];
                segNormals[i] = new Vector2(d.y, -d.x).normalized;
            }
            var along = 0f;
            for (var i = 0; i < profile.Count - 1; i++)
            {
                var p0 = profile[i];
                var p1 = profile[i + 1];
                var len = (p1 - p0).magnitude;
                if (len < 1e-6f) continue;
                var n0 = segNormals[i];
                var n1 = segNormals[i];
                if (i > 0 && Vector2.Angle(segNormals[i - 1], segNormals[i]) < smoothAngle)
                    n0 = (segNormals[i - 1] + segNormals[i]).normalized;
                if (i < profile.Count - 2 && Vector2.Angle(segNormals[i], segNormals[i + 1]) < smoothAngle)
                    n1 = (segNormals[i] + segNormals[i + 1]).normalized;
                var start = positions.Count;
                for (var s = 0; s <= segments; s++)
                {
                    var angle = Mathf.Lerp(fromDeg, toDeg, (float)s / segments) * Mathf.Deg2Rad;
                    var c = Mathf.Cos(angle);
                    var sn = Mathf.Sin(angle);
                    var uCoord = (float)s / segments;
                    Vertex(new Vector3(p0.x * c, p0.y, p0.x * sn), new Vector3(n0.x * c, n0.y, n0.x * sn), new Vector2(uCoord, along * uvScale));
                    Vertex(new Vector3(p1.x * c, p1.y, p1.x * sn), new Vector3(n1.x * c, n1.y, n1.x * sn), new Vector2(uCoord, (along + len) * uvScale));
                }
                for (var s = 0; s < segments; s++)
                {
                    var a = start + s * 2;
                    Quad(mat, a, a + 1, a + 3, a + 2);
                }
                along += len;
            }
        }

        /// <summary>Тело вращения с плоскими гранями по окружности (шестигранный карандаш, гайка).</summary>
        void LatheFaceted(IList<Vector2> profile, int mat, int segments, float uvScale, float fromDeg, float toDeg)
        {
            var along = 0f;
            for (var i = 0; i < profile.Count - 1; i++)
            {
                var p0 = profile[i];
                var p1 = profile[i + 1];
                var d = p1 - p0;
                var len = d.magnitude;
                if (len < 1e-6f) continue;
                var n2 = new Vector2(d.y, -d.x).normalized;
                for (var s = 0; s < segments; s++)
                {
                    var a0 = Mathf.Lerp(fromDeg, toDeg, (float)s / segments) * Mathf.Deg2Rad;
                    var a1 = Mathf.Lerp(fromDeg, toDeg, (float)(s + 1) / segments) * Mathf.Deg2Rad;
                    var am = (a0 + a1) / 2;
                    var n = new Vector3(n2.x * Mathf.Cos(am), n2.y, n2.x * Mathf.Sin(am)).normalized;
                    var u0 = (float)s / segments;
                    var u1 = (float)(s + 1) / segments;
                    var v0 = along * uvScale;
                    var v1 = (along + len) * uvScale;
                    var a = Vertex(new Vector3(p0.x * Mathf.Cos(a0), p0.y, p0.x * Mathf.Sin(a0)), n, new Vector2(u0, v0));
                    var b = Vertex(new Vector3(p1.x * Mathf.Cos(a0), p1.y, p1.x * Mathf.Sin(a0)), n, new Vector2(u0, v1));
                    var c = Vertex(new Vector3(p1.x * Mathf.Cos(a1), p1.y, p1.x * Mathf.Sin(a1)), n, new Vector2(u1, v1));
                    var e = Vertex(new Vector3(p0.x * Mathf.Cos(a1), p0.y, p0.x * Mathf.Sin(a1)), n, new Vector2(u1, v0));
                    Quad(mat, a, b, c, e);
                }
                along += len;
            }
        }

        /// <summary>
        /// Протянуть прямоугольное сечение (ширина × толщина) вдоль ломаной: ремешки, ленты, резинки, провода.
        /// <paramref name="up"/> — куда смотрит «толщина» (для лежащего ремешка — вверх).
        /// </summary>
        public void Sweep(IList<Vector3> path, Vector2 section, int mat, Vector3? up = null, float uvScale = 10f,
            bool caps = true, IList<Vector3> ups = null)
        {
            var count = path.Count;
            var right = new Vector3[count];
            var norm = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var t = (i == 0 ? path[1] - path[0] : i == count - 1 ? path[i] - path[i - 1] : path[i + 1] - path[i - 1]).normalized;
                var u = ups != null ? ups[i] : up ?? Vector3.up;
                var r = Vector3.Cross(u, t).normalized;
                if (r.sqrMagnitude < 1e-6f) r = Vector3.Cross(Vector3.forward, t).normalized;
                right[i] = r;
                norm[i] = Vector3.Cross(t, r).normalized;
            }
            var hw = section.x / 2;
            var ht = section.y / 2;
            // Четыре стороны: верх, низ, правая, левая — каждая своей полосой вершин (жёсткие рёбра).
            var sides = new (float sx, float sy, float ex, float ey, int axis)[]
            {
                (-hw, ht, hw, ht, 0), (hw, -ht, -hw, -ht, 1), (hw, ht, hw, -ht, 2), (-hw, -ht, -hw, ht, 3),
            };
            foreach (var (sx, sy, ex, ey, axis) in sides)
            {
                var start = positions.Count;
                var along = 0f;
                for (var i = 0; i < count; i++)
                {
                    if (i > 0) along += (path[i] - path[i - 1]).magnitude;
                    var n = axis switch { 0 => norm[i], 1 => -norm[i], 2 => right[i], _ => -right[i] };
                    Vertex(path[i] + right[i] * sx + norm[i] * sy, n, new Vector2(0, along * uvScale));
                    Vertex(path[i] + right[i] * ex + norm[i] * ey, n, new Vector2(1, along * uvScale));
                }
                for (var i = 0; i < count - 1; i++)
                {
                    var a = start + i * 2;
                    Quad(mat, a, a + 1, a + 3, a + 2);
                }
            }
            if (!caps) return;
            foreach (var (i, sign) in new[] { (0, -1f), (count - 1, 1f) })
            {
                var t = (sign < 0 ? path[0] - path[1] : path[count - 1] - path[count - 2]).normalized;
                var s = positions.Count;
                Vertex(path[i] - right[i] * hw - norm[i] * ht, t, new Vector2(0, 0));
                Vertex(path[i] + right[i] * hw - norm[i] * ht, t, new Vector2(1, 0));
                Vertex(path[i] + right[i] * hw + norm[i] * ht, t, new Vector2(1, 1));
                Vertex(path[i] - right[i] * hw + norm[i] * ht, t, new Vector2(0, 1));
                Quad(mat, s, s + 1, s + 2, s + 3);
            }
        }

        /// <summary>Шар (для ножек, набалдашников, шляпок гвоздей).</summary>
        public void Sphere(Vector3 center, float radius, int mat, int segments = 16, float squash = 1f)
        {
            var profile = new List<Vector2>();
            var rings = Mathf.Max(4, segments / 2);
            for (var k = 0; k <= rings; k++)
            {
                var a = -Mathf.PI / 2 + Mathf.PI * k / rings;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * squash));
            }
            using (At(center)) Lathe(profile, mat, segments, 80f, 1f);
        }

        /// <summary>Цилиндр вдоль оси Y с фаской на торцах. Торцы — с плоской UV-проекцией (для циферблатов и крышек).</summary>
        public void Cylinder(Vector3 center, float radius, float height, int mat, int segments = 24, float bevel = 0f,
            int capMat = -1, Uv? capUv = null, bool caps = true, float sideUvScale = 1f, bool faceted = false)
        {
            var h = height / 2;
            bevel = Mathf.Min(bevel, radius * 0.5f, h * 0.9f);
            var side = new List<Vector2>();
            if (bevel > 0)
            {
                side.Add(new Vector2(radius - bevel, -h));
                for (var k = 1; k <= 3; k++)
                {
                    var a = Mathf.PI / 2 * k / 3;
                    side.Add(new Vector2(radius - bevel + bevel * Mathf.Sin(a), -h + bevel - bevel * Mathf.Cos(a)));
                }
                side.Add(new Vector2(radius, h - bevel));
                for (var k = 1; k <= 3; k++)
                {
                    var a = Mathf.PI / 2 * k / 3;
                    side.Add(new Vector2(radius - bevel + bevel * Mathf.Cos(a), h - bevel + bevel * Mathf.Sin(a)));
                }
            }
            else
            {
                side.Add(new Vector2(radius, -h));
                side.Add(new Vector2(radius, h));
            }
            using (At(center)) Lathe(side, mat, segments, 50f, sideUvScale, 0f, 360f, faceted);
            if (!caps) return;
            var capR = radius - bevel;
            using (At(center))
            {
                Disc(new Vector3(0, h, 0), capR, capMat < 0 ? mat : capMat, segments, true, capUv);
                Disc(new Vector3(0, -h, 0), capR, capMat < 0 ? mat : capMat, segments, false, capUv);
            }
        }

        /// <summary>Круг в плоскости XZ; вверх (+Y) или вниз. UV: вписанный квадрат текстуры (верх текстуры — к −Z).</summary>
        public void Disc(Vector3 center, float radius, int mat, int segments = 24, bool up = true, Uv? uv = null)
        {
            var n = up ? Vector3.up : Vector3.down;
            var r = (uv ?? Uv.Fit()).Rect;
            Vector2 Map(float x, float z) =>
                new(r.x + (0.5f - x / (2 * radius)) * r.width, r.y + (0.5f - z / (2 * radius)) * r.height);
            var c = Vertex(center, n, Map(0, 0));
            var first = positions.Count;
            for (var s = 0; s <= segments; s++)
            {
                var a = (float)s / segments * Mathf.PI * 2;
                var x = Mathf.Cos(a) * radius;
                var z = Mathf.Sin(a) * radius;
                Vertex(center + new Vector3(x, 0, z), n, Map(x, z));
            }
            for (var s = 0; s < segments; s++) Tri(mat, c, first + s, first + s + 1);
        }

        /// <summary>Тор (кольцо) вокруг оси Y: ручки, заводная головка, кольца.</summary>
        public void Torus(Vector3 center, float radius, float tube, int mat, int segments = 24, int tubeSegments = 10,
            float fromDeg = 0f, float toDeg = 360f)
        {
            var profile = new List<Vector2>();
            for (var k = 0; k <= tubeSegments; k++)
            {
                var a = -Mathf.PI / 2 + Mathf.PI * 2 * k / tubeSegments;
                profile.Add(new Vector2(radius + Mathf.Cos(a) * tube, Mathf.Sin(a) * tube));
            }
            using (At(center)) Lathe(profile, mat, segments, 60f, 1f, fromDeg, toDeg);
        }

        // ---------- Выдавленный контур ----------

        /// <summary>
        /// Плоская фигура по контуру (точки в координатах зрителя: x вправо, y вверх), выдавленная на
        /// <paramref name="depth"/> вдоль Z. Лицевая грань (+Z) получает всю текстуру <paramref name="frontUv"/>.
        /// </summary>
        public void Extrude(IList<Vector2> outline, float depth, int mat, int frontMat = -1, Uv? frontUv = null,
            int backMat = -1, bool sides = true)
        {
            var pts = new List<Vector2>(outline);
            if (SignedArea(pts) < 0) pts.Reverse();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var p in pts)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            var size = max - min;
            var r = (frontUv ?? Uv.Fit()).Rect;
            Vector2 Map(Vector2 p) => new(r.x + (p.x - min.x) / size.x * r.width, r.y + (p.y - min.y) / size.y * r.height);
            var tris = Triangulate(pts);
            var h = depth / 2;

            var front = positions.Count;
            foreach (var p in pts) Vertex(new Vector3(-p.x, p.y, h), Vector3.forward, Map(p));
            for (var i = 0; i < tris.Count; i += 3)
                Tri(frontMat < 0 ? mat : frontMat, front + tris[i], front + tris[i + 1], front + tris[i + 2]);

            var back = positions.Count;
            foreach (var p in pts)
            {
                var m = Map(p);
                Vertex(new Vector3(-p.x, p.y, -h), Vector3.back, new Vector2(1 - m.x, m.y));
            }
            for (var i = 0; i < tris.Count; i += 3)
                Tri(backMat < 0 ? mat : backMat, back + tris[i], back + tris[i + 1], back + tris[i + 2]);

            if (!sides) return;
            var along = 0f;
            for (var i = 0; i < pts.Count; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Count];
                var d = b - a;
                // Внешняя нормаль ребра при обходе против часовой — (dy, −dx) в координатах зрителя; x зрителя = −X.
                var n3 = new Vector3(-d.y, -d.x, 0).normalized;
                var len = d.magnitude;
                var s = positions.Count;
                Vertex(new Vector3(-a.x, a.y, h), n3, new Vector2(along, 0));
                Vertex(new Vector3(-b.x, b.y, h), n3, new Vector2(along + len, 0));
                Vertex(new Vector3(-b.x, b.y, -h), n3, new Vector2(along + len, depth));
                Vertex(new Vector3(-a.x, a.y, -h), n3, new Vector2(along, depth));
                Quad(mat, s, s + 1, s + 2, s + 3);
                along += len;
            }
        }

        static float SignedArea(IList<Vector2> p)
        {
            var a = 0f;
            for (var i = 0; i < p.Count; i++)
            {
                var q = p[(i + 1) % p.Count];
                a += p[i].x * q.y - q.x * p[i].y;
            }
            return a / 2;
        }

        /// <summary>Триангуляция простого многоугольника (обход против часовой) методом отсечения ушей.</summary>
        static List<int> Triangulate(IList<Vector2> p)
        {
            var result = new List<int>();
            var idx = new List<int>();
            for (var i = 0; i < p.Count; i++) idx.Add(i);
            var guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                var cut = false;
                for (var i = 0; i < idx.Count; i++)
                {
                    var i0 = idx[(i + idx.Count - 1) % idx.Count];
                    var i1 = idx[i];
                    var i2 = idx[(i + 1) % idx.Count];
                    var a = p[i0];
                    var b = p[i1];
                    var c = p[i2];
                    if (Cross(b - a, c - b) <= 1e-9f) continue; // вогнутая вершина
                    var inside = false;
                    foreach (var j in idx)
                    {
                        if (j == i0 || j == i1 || j == i2) continue;
                        if (InTriangle(p[j], a, b, c))
                        {
                            inside = true;
                            break;
                        }
                    }
                    if (inside) continue;
                    result.Add(i0); result.Add(i1); result.Add(i2);
                    idx.RemoveAt(i);
                    cut = true;
                    break;
                }
                if (!cut) break;
            }
            if (idx.Count == 3)
            {
                result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]);
            }
            return result;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var d1 = Cross(b - a, p - a);
            var d2 = Cross(c - b, p - b);
            var d3 = Cross(a - c, p - c);
            return d1 >= 0 && d2 >= 0 && d3 >= 0;
        }

        // ---------- Лист (бумага, ткань) ----------

        /// <summary>
        /// Двусторонний лист в плоскости XY с лицом в +Z, разбитый на сетку — его можно изогнуть через <see cref="Deform"/>.
        /// </summary>
        public int Sheet(Vector2 size, int nx, int ny, int frontMat, int backMat, Uv? frontUv = null, float thickness = 0.0004f)
        {
            var start = positions.Count;
            var r = (frontUv ?? Uv.Fit()).Rect;
            for (var side = 0; side < 2; side++)
            {
                var s0 = positions.Count;
                var z = side == 0 ? thickness / 2 : -thickness / 2;
                var n = side == 0 ? Vector3.forward : Vector3.back;
                for (var j = 0; j <= ny; j++)
                for (var i = 0; i <= nx; i++)
                {
                    var fx = (float)i / nx;
                    var fy = (float)j / ny;
                    var uv = new Vector2(r.x + fx * r.width, r.y + fy * r.height);
                    if (side == 1) uv.x = r.x + (1 - fx) * r.width;
                    Vertex(new Vector3((0.5f - fx) * size.x, (fy - 0.5f) * size.y, z), n, uv);
                }
                for (var j = 0; j < ny; j++)
                for (var i = 0; i < nx; i++)
                {
                    var a = s0 + j * (nx + 1) + i;
                    Quad(side == 0 ? frontMat : backMat, a, a + 1, a + nx + 2, a + nx + 1);
                }
            }
            return start;
        }

        /// <summary>Изменить вершины, добавленные начиная с <paramref name="start"/>, и пересчитать их нормали.</summary>
        public void Deform(int start, Func<Vector3, Vector3> f)
        {
            for (var i = start; i < positions.Count; i++) positions[i] = f(positions[i]);
            var acc = new Vector3[positions.Count - start];
            foreach (var list in triangles)
                for (var t = 0; t < list.Count; t += 3)
                {
                    int a = list[t], b = list[t + 1], c = list[t + 2];
                    if (a < start || b < start || c < start) continue;
                    var n = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
                    acc[a - start] += n;
                    acc[b - start] += n;
                    acc[c - start] += n;
                }
            for (var i = 0; i < acc.Length; i++)
                if (acc[i].sqrMagnitude > 1e-12f) normals[start + i] = acc[i].normalized;
        }

        // ---------- Итог ----------

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = triangles.Count;
            for (var i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
