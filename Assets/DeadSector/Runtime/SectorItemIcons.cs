using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Stylized transparent 48x48 equipment thumbnails drawn with simple
    /// primitives. No licensed game assets, external textures or editor
    /// reimport required. Item IDs select distinctive object silhouettes;
    /// rarity colors are deliberately NOT part of the visual system.
    /// </summary>
    public static class SectorItemIcons
    {
        const int Size = 48;
        static readonly Dictionary<string, Sprite> Sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        static readonly Dictionary<string, Texture2D> Textures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        static readonly Color32 Ink = new Color32(215, 225, 211, 255);
        static readonly Color32 Shadow = new Color32(59, 83, 78, 255);
        static readonly Color32 Olive = new Color32(142, 169, 108, 255);
        static readonly Color32 Metal = new Color32(168, 181, 184, 255);
        static readonly Color32 Blue = new Color32(79, 163, 190, 255);
        static readonly Color32 Warm = new Color32(202, 170, 113, 255);
        static readonly Color32 Red = new Color32(209, 111, 99, 255);

        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = "empty";

            if (Sprites.TryGetValue(id, out Sprite cached))
                return cached;

            Texture2D texture = new Texture2D(
                Size, Size, TextureFormat.RGBA32, false);
            texture.name = "DeadSector_Item_" + id;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] p = new Color32[Size * Size];

            Draw(p, id);
            texture.SetPixels32(p);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture,
                new Rect(0f, 0f, Size, Size),
                new Vector2(.5f, .5f), Size);
            sprite.name = "UI_" + id;
            Sprites[id] = sprite;
            Textures[id] = texture;
            return sprite;
        }

        static void Draw(Color32[] p, string id)
        {
            switch (id)
            {
                case "water":
                    Bottle(p);
                    break;
                case "food":
                    Can(p);
                    break;
                case "bandage":
                    Bandage(p);
                    break;
                case "medkit":
                    Medkit(p);
                    break;
                case "9mm":
                case "556":
                    Ammo(p, id == "556");
                    break;
                case "rifle":
                    Rifle(p);
                    break;
                case "pistol":
                    Pistol(p);
                    break;
                case "knife":
                case "stone_knife":
                    Blade(p, id == "stone_knife");
                    break;
                case "stone_axe":
                case "axe":
                case "stone_pickaxe":
                    Axe(p);
                    break;
                case "spear":
                    Spear(p);
                    break;
                case "torch":
                    Torch(p);
                    break;
                case "wood_club":
                case "fists":
                    Club(p, id == "fists");
                    break;
                case "wood_helmet":
                case "cotton_hood":
                    Helmet(p, id == "cotton_hood");
                    break;
                case "wood_vest":
                case "cotton_shirt":
                    Vest(p, id == "cotton_shirt");
                    break;
                case "wood_leggings":
                case "cotton_pants":
                    Legs(p, id == "cotton_pants");
                    break;
                case "cotton_boots":
                    Boots(p);
                    break;
                case "cotton_bag":
                    Bag(p);
                    break;
                case "stone":
                    Stone(p);
                    break;
                case "scrap":
                    Scrap(p);
                    break;
                case "wood":
                    Wood(p);
                    break;
                case "stick":
                    Stick(p);
                    break;
                case "plant_fiber":
                case "cotton":
                    Plant(p, id == "cotton");
                    break;
                case "cloth":
                    Cloth(p);
                    break;
                case "cord":
                    Cord(p);
                    break;
                default:
                    Crate(p);
                    break;
            }
        }

        static void Bottle(Color32[] p)
        {
            Box(p, 18, 6, 30, 29, Blue);
            Box(p, 15, 12, 33, 27, Blue);
            Box(p, 18, 29, 30, 35, Ink);
            Box(p, 21, 36, 27, 42, Metal);
            Box(p, 16, 15, 32, 23, Shadow);
            Box(p, 16, 17, 32, 21, Ink);
        }

        static void Can(Color32[] p)
        {
            Box(p, 11, 12, 37, 36, Warm);
            Box(p, 11, 12, 37, 16, Metal);
            Box(p, 11, 34, 37, 38, Metal);
            Box(p, 16, 20, 32, 29, Shadow);
            Box(p, 19, 23, 29, 26, Ink);
        }

        static void Bandage(Color32[] p)
        {
            Box(p, 7, 17, 41, 30, Ink);
            Box(p, 17, 7, 30, 41, Ink);
            Box(p, 19, 19, 29, 29, Red);
        }

        static void Medkit(Color32[] p)
        {
            Box(p, 7, 10, 41, 37, Red);
            Box(p, 18, 36, 30, 42, Metal);
            Box(p, 12, 14, 36, 33, Shadow);
            Box(p, 20, 17, 28, 31, Ink);
            Box(p, 16, 21, 32, 27, Ink);
        }

        static void Ammo(Color32[] p, bool rifle)
        {
            for (int i = 0; i < 3; i++)
            {
                int x = 10 + i * 11;
                Box(p, x, 8, x + 8, 29, Warm);
                Box(p, x + 1, 29, x + 7, rifle ? 41 : 36, Metal);
                Box(p, x, 8, x + 8, 11, Shadow);
            }
        }

        static void Rifle(Color32[] p)
        {
            Box(p, 4, 25, 37, 31, Metal);
            Box(p, 27, 30, 43, 33, Metal);
            Box(p, 11, 20, 27, 26, Shadow);
            Box(p, 18, 13, 24, 23, Shadow);
            Box(p, 5, 17, 11, 25, Warm);
            Box(p, 37, 23, 46, 27, Metal);
            Box(p, 22, 14, 26, 20, Ink);
        }

        static void Pistol(Color32[] p)
        {
            Box(p, 11, 22, 41, 33, Metal);
            Box(p, 12, 28, 38, 31, Shadow);
            Box(p, 16, 8, 25, 23, Shadow);
            Box(p, 29, 19, 35, 22, Olive);
        }

        static void Blade(Color32[] p, bool stone)
        {
            Line(p, 12, 8, 27, 29, stone ? Warm : Shadow, 6);
            Line(p, 26, 29, 38, 42, Metal, 7);
            Line(p, 31, 36, 38, 42, Ink, 2);
            Line(p, 9, 27, 32, 16, Warm, 3);
        }

        static void Axe(Color32[] p)
        {
            Line(p, 12, 7, 34, 42, Warm, 6);
            Box(p, 24, 25, 43, 39, Metal);
            Box(p, 38, 25, 44, 39, Ink);
            Line(p, 12, 8, 17, 16, Shadow, 2);
        }

        static void Spear(Color32[] p)
        {
            Line(p, 9, 8, 36, 37, Warm, 4);
            Triangle(p, 28, 37, 44, 45, 40, 29, Metal);
        }

        static void Torch(Color32[] p)
        {
            Line(p, 16, 9, 30, 30, Warm, 7);
            Circle(p, 32, 35, 7, Warm);
            Circle(p, 32, 37, 4, Red);
        }

        static void Club(Color32[] p, bool fist)
        {
            if (fist)
            {
                Circle(p, 24, 25, 12, Olive);
                Box(p, 15, 12, 33, 21, Shadow);
                Box(p, 16, 27, 33, 35, Ink);
            }
            else
            {
                Line(p, 12, 7, 34, 42, Warm, 9);
                Box(p, 21, 25, 37, 38, Shadow);
            }
        }

        static void Helmet(Color32[] p, bool cloth)
        {
            Circle(p, 24, 26, 14, cloth ? Olive : Warm);
            Box(p, 9, 8, 39, 23, new Color32(0, 0, 0, 0));
            Box(p, 8, 19, 40, 24, Metal);
            Box(p, 18, 17, 30, 20, Shadow);
        }

        static void Vest(Color32[] p, bool cloth)
        {
            Box(p, 11, 12, 37, 39, cloth ? Olive : Warm);
            Box(p, 4, 29, 13, 39, cloth ? Olive : Warm);
            Box(p, 35, 29, 44, 39, cloth ? Olive : Warm);
            Box(p, 18, 32, 30, 41, Shadow);
            Box(p, 15, 16, 33, 26, Shadow);
            Box(p, 21, 17, 27, 24, cloth ? Ink : Metal);
        }

        static void Legs(Color32[] p, bool cloth)
        {
            Box(p, 11, 25, 37, 40, cloth ? Olive : Warm);
            Box(p, 11, 8, 22, 27, cloth ? Olive : Warm);
            Box(p, 26, 8, 37, 27, cloth ? Olive : Warm);
            Box(p, 22, 20, 26, 41, Shadow);
        }

        static void Boots(Color32[] p)
        {
            Box(p, 10, 20, 21, 40, Olive);
            Box(p, 26, 20, 37, 40, Olive);
            Box(p, 5, 12, 22, 21, Shadow);
            Box(p, 25, 12, 43, 21, Shadow);
            Box(p, 6, 10, 23, 13, Ink);
            Box(p, 26, 10, 43, 13, Ink);
        }

        static void Bag(Color32[] p)
        {
            Box(p, 12, 11, 36, 37, Olive);
            Box(p, 18, 36, 30, 42, Metal);
            Box(p, 8, 17, 14, 35, Shadow);
            Box(p, 34, 17, 40, 35, Shadow);
            Box(p, 15, 15, 33, 23, Shadow);
            Box(p, 20, 18, 28, 22, Metal);
        }

        static void Stone(Color32[] p)
        {
            Triangle(p, 7, 12, 21, 39, 42, 14, Metal);
            Box(p, 14, 13, 33, 23, Shadow);
        }

        static void Scrap(Color32[] p)
        {
            Box(p, 8, 8, 37, 16, Metal);
            Box(p, 20, 12, 29, 41, Metal);
            Box(p, 12, 30, 41, 38, Shadow);
            Box(p, 17, 32, 23, 37, Ink);
        }

        static void Wood(Color32[] p)
        {
            Box(p, 8, 11, 40, 36, Warm);
            Line(p, 13, 15, 13, 32, Shadow, 2);
            Line(p, 26, 15, 26, 32, Shadow, 2);
            Line(p, 35, 15, 35, 32, Shadow, 2);
        }

        static void Stick(Color32[] p)
        {
            Line(p, 12, 8, 34, 41, Warm, 5);
            Line(p, 17, 8, 40, 36, Shadow, 3);
            Line(p, 10, 19, 32, 42, Olive, 2);
        }

        static void Plant(Color32[] p, bool cotton)
        {
            Line(p, 23, 8, 23, 38, Olive, 4);
            Line(p, 23, 19, 10, 31, Olive, 3);
            Line(p, 23, 27, 37, 36, Olive, 3);
            if (cotton)
            {
                Circle(p, 13, 31, 7, Ink);
                Circle(p, 36, 35, 7, Ink);
                Circle(p, 23, 40, 6, Ink);
            }
            else
            {
                Triangle(p, 5, 27, 22, 31, 19, 18, Olive);
                Triangle(p, 30, 25, 44, 34, 24, 37, Olive);
            }
        }

        static void Cloth(Color32[] p)
        {
            Triangle(p, 10, 7, 15, 41, 38, 33, Ink);
            Triangle(p, 10, 7, 38, 33, 41, 13, Olive);
            Line(p, 14, 12, 34, 29, Shadow, 2);
        }

        static void Cord(Color32[] p)
        {
            Circle(p, 24, 23, 15, Warm);
            Circle(p, 24, 23, 8, Shadow);
            Line(p, 30, 8, 39, 17, Warm, 4);
        }

        static void Crate(Color32[] p)
        {
            Box(p, 10, 9, 38, 39, Shadow);
            Box(p, 12, 11, 36, 37, Olive);
            Line(p, 12, 11, 36, 37, Ink, 3);
            Line(p, 12, 37, 36, 11, Ink, 3);
        }

        static void Box(
            Color32[] p, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(Size, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(Size, x1); x++)
                    p[y * Size + x] = color;
        }

        static void Circle(Color32[] p, int cx, int cy,
            int r, Color32 color)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r)
                        Pixel(p, cx + x, cy + y, color);
        }

        static void Line(Color32[] p, int x0, int y0,
            int x1, int y1, Color32 color, int width)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Box(p, x0 - width / 2, y0 - width / 2,
                    x0 + width / 2 + 1, y0 + width / 2 + 1, color);
                if (x0 == x1 && y0 == y1)
                    break;
                int e = 2 * err;
                if (e > -dy) { err -= dy; x0 += sx; }
                if (e < dx) { err += dx; y0 += sy; }
            }
        }

        static void Triangle(Color32[] p,
            int ax, int ay, int bx, int by, int cx, int cy, Color32 tint)
        {
            int left = Mathf.Max(0, Mathf.Min(ax, Mathf.Min(bx, cx)));
            int right = Mathf.Min(Size - 1, Mathf.Max(ax, Mathf.Max(bx, cx)));
            int low = Mathf.Max(0, Mathf.Min(ay, Mathf.Min(by, cy)));
            int high = Mathf.Min(Size - 1, Mathf.Max(ay, Mathf.Max(by, cy)));
            float area = Cross(ax, ay, bx, by, cx, cy);
            if (Mathf.Abs(area) < .01f)
                return;

            for (int y = low; y <= high; y++)
                for (int x = left; x <= right; x++)
                {
                    float a = Cross(bx, by, cx, cy, x, y) / area;
                    float b = Cross(cx, cy, ax, ay, x, y) / area;
                    float c = 1f - a - b;
                    if (a >= 0f && b >= 0f && c >= 0f)
                        Pixel(p, x, y, tint);
                }
        }

        static float Cross(int ax, int ay, int bx, int by,
            int cx, int cy)
        {
            return (bx - ax) * (cy - ay) -
                (by - ay) * (cx - ax);
        }

        static void Pixel(Color32[] p, int x, int y, Color32 color)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
                p[y * Size + x] = color;
        }

        public static void Release()
        {
            foreach (Sprite sprite in Sprites.Values)
            {
                if (sprite == null) continue;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(sprite);
                else
                    UnityEngine.Object.DestroyImmediate(sprite);
            }
            Sprites.Clear();

            foreach (Texture2D texture in Textures.Values)
            {
                if (texture == null) continue;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(texture);
                else
                    UnityEngine.Object.DestroyImmediate(texture);
            }
            Textures.Clear();
        }
    }
}
