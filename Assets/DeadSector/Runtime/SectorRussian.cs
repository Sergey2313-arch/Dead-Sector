using System;
using System.Collections.Generic;

namespace DeadSector
{
    // Display-only Russian text. IDs, recipe IDs and save JSON remain unchanged.
    public static class SectorRussian
    {
        static readonly Dictionary<string, string> Items = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "water", "Вода в бутылке" }, { "food", "Консервы" },
            { "bandage", "Бинт" }, { "medkit", "Аптечка" },
            { "scrap", "Металлолом" }, { "wood", "Доски" },
            { "cloth", "Ткань" }, { "stone", "Камень" },
            { "stick", "Сухая палка" }, { "plant_fiber", "Растительные волокна" },
            { "cotton", "Хлопок" }, { "cord", "Верёвка" },
            { "knife", "Полевой нож" }, { "stone_knife", "Каменный нож" },
            { "stone_axe", "Каменный топор" }, { "wood_club", "Дубина" },
            { "axe", "Топорик" }, { "spear", "Самодельное копьё" },
            { "torch", "Факел" }, { "pistol", "Пистолет" },
            { "rifle", "Винтовка" }, { "9mm", "Патроны 9 мм" },
            { "556", "Патроны 5,56 мм" },
            { "wood_helmet", "Деревянный шлем" },
            { "wood_vest", "Деревянный нагрудник" },
            { "wood_leggings", "Деревянные поножи" },
            { "cotton_hood", "Хлопковый капюшон" },
            { "cotton_shirt", "Хлопковая рубашка" },
            { "cotton_pants", "Хлопковые брюки" },
            { "cotton_boots", "Тканевые обмотки" },
            { "cotton_bag", "Хлопковый рюкзак" }
        };

        static readonly Dictionary<string, string> Recipes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "cord", "Скрутить верёвку" }, { "stone_knife", "Каменный нож" },
            { "stone_axe", "Каменный топор" }, { "wood_club", "Деревянная дубина" },
            { "spear", "Деревянное копьё" }, { "torch", "Факел" },
            { "wood_helmet", "Деревянный шлем" },
            { "wood_vest", "Деревянный нагрудник" },
            { "wood_leggings", "Деревянные поножи" },
            { "cloth_cotton", "Соткать ткань" },
            { "cotton_hood", "Хлопковый капюшон" },
            { "cotton_shirt", "Хлопковая рубашка" },
            { "cotton_pants", "Хлопковые брюки" },
            { "cotton_boots", "Тканевые обмотки" },
            { "cotton_bag", "Хлопковый рюкзак" },
            { "bandage", "Изготовить бинт" },
            { "medkit", "Собрать аптечку" },
            { "hatchet", "Металлический топорик" }
        };

        public static string ItemName(string id, string fallback = "")
        {
            return id != null && Items.TryGetValue(id, out string name)
                ? name : fallback;
        }

        public static string RecipeName(string id, string fallback = "")
        {
            return id != null && Recipes.TryGetValue(id, out string name)
                ? name : fallback;
        }

        public static string Kind(SectorItemKind type)
        {
            switch (type)
            {
                case SectorItemKind.Food: return "Еда";
                case SectorItemKind.Drink: return "Напитки";
                case SectorItemKind.Medical: return "Медицина";
                case SectorItemKind.Material: return "Материалы";
                case SectorItemKind.Melee: return "Ближний бой";
                case SectorItemKind.Firearm: return "Огнестрельное оружие";
                case SectorItemKind.Ammunition: return "Боеприпасы";
                case SectorItemKind.Armor: return "Экипировка";
                default: return "Предмет";
            }
        }
    }
}
