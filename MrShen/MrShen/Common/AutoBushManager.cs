﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;
using Color = SharpDX.Color;

namespace MrShen.Common
{
    internal class EnemyHeros
    {
        public AIHeroClient Player;
        public int LastSeen;

        public EnemyHeros(AIHeroClient player)
        {
            Player = player;
        }
    }
    // TODO: Add Support Corki Q, Ashe E, Quinn W, Kalista W, Jinx E

    internal class AutoBushHelper
    {
        public static List<EnemyHeros> EnemyInfo = new List<EnemyHeros>();

        public AutoBushHelper()
        {
            var champions = ObjectManager.Get<AIHeroClient>().ToList();

            EnemyInfo = HeroManager.Enemies.Select(e => new EnemyHeros(e)).ToList();
            Game.OnUpdate += Game_OnGameUpdate;

        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            var time = Environment.TickCount;
            foreach (EnemyHeros enemyInfo in EnemyInfo.Where(x => x.Player.IsVisible))
            {
                enemyInfo.LastSeen = time;
            }
        }
        public EnemyHeros GetPlayerInfo(AIHeroClient enemy)
        {
            return AutoBushHelper.EnemyInfo.Find(x => x.Player.NetworkId == enemy.NetworkId);
        }

        public float GetTargetHealth(EnemyHeros playerHeros, int additionalTime)
        {
            if (playerHeros.Player.IsVisible) return playerHeros.Player.Health;

            var predictedhealth = playerHeros.Player.Health
                                  + playerHeros.Player.HPRegenRate
                                  * ((Environment.TickCount - playerHeros.LastSeen + additionalTime) / 1000f);

            return predictedhealth > playerHeros.Player.MaxHealth ? playerHeros.Player.MaxHealth : predictedhealth;
        }

        public static T GetSafeMenuItem<T>(MenuItem item)
        {
            if (item != null) return item.GetValue<T>();

            return default(T);
        }

    }

    internal class AutoBushManager
    {
        private static Spell ChampionSpell;
        private static Menu Config => Modes.MenuConfig.LocalMenu;
        static readonly List<KeyValuePair<int, String>> _wards = new List<KeyValuePair<int, String>> //insertion order
        {
            new KeyValuePair<int, String>(3340, "Warding Totem Trinket"),
            new KeyValuePair<int, String>(3361, "Greater Stealth Totem Trinket"),
            new KeyValuePair<int, String>(3205, "Quill Coat"),
            new KeyValuePair<int, String>(3207, "Spirit Of The Ancient Golem"),
            new KeyValuePair<int, String>(3154, "Wriggle's Lantern"),
            new KeyValuePair<int, String>(2049, "Sight Stone"),
            new KeyValuePair<int, String>(2045, "Ruby Sightstone"),
            new KeyValuePair<int, String>(3160, "Feral Flare"),
            new KeyValuePair<int, String>(2050, "Explorer's Ward"),
            new KeyValuePair<int, String>(2044, "Stealth Ward"),
        };

        static int[] wardIds = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };

        private static int lastTimeWarded;

        private static Menu menu;
        public static void Initialize(Menu ParentMenu)
        {
            menu = ParentMenu.AddSubMenu(new Menu("Auto Bush Revealer", "AutoBushRevealer").SetFontStyle(FontStyle.Regular, Color.Aquamarine));

            var useWardsMenu = new Menu("Use Wards: ", "AutoBushUseWards");
            menu.AddSubMenu(useWardsMenu);

            foreach (var ward in _wards)
            {
                useWardsMenu.AddItem(new MenuItem("AutoBush." + ward.Key, ward.Value).SetValue(true));
            }

            var useMenuItemName = "Use." + ObjectManager.Player.CharacterData.CharacterName;
            var useMenuItemText = "Use " + ObjectManager.Player.CharacterData.CharacterName;

            switch (ObjectManager.Player.CharacterData.CharacterName)
            {
                case "Corki":
                    {
                        menu.AddItem(new MenuItem(useMenuItemName, useMenuItemText + " Q").SetValue(true));
                        break;
                    }
                case "Ashe":
                    {
                        menu.AddItem(new MenuItem(useMenuItemName, useMenuItemText + " E").SetValue(true));
                        break;
                    }
                case "Quinn":
                    {
                        menu.AddItem(new MenuItem(useMenuItemName, useMenuItemText + " W").SetValue(true));
                        break;
                    }
                case "Kalista":
                    {
                        menu.AddItem(new MenuItem(useMenuItemName, useMenuItemText + " W").SetValue(true));
                        break;
                    }
                case "Jinx":
                    {
                        menu.AddItem(new MenuItem(useMenuItemName, useMenuItemText + " E").SetValue(true));
                        break;
                    }
            }
            menu.AddItem(new MenuItem("AutoBushEnabled", "Enabled").SetValue(true));
            menu.AddItem(new MenuItem("AutoBushKey", "Key").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            new AutoBushManager();

            ChampionSpell = GetSpell();

            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static Spell GetSpell()
        {
            switch (ObjectManager.Player.CharacterData.CharacterName)
            {
                case "Corki":
                    {
                        return new Spell(SpellSlot.Q, 700);
                    }
                case "Ashe":
                    {
                        return new Spell(SpellSlot.E, 700);
                    }
                case "Quinn":
                    {
                        return new Spell(SpellSlot.W, 900);
                    }
                case "Kalista":
                    {
                        return new Spell(SpellSlot.W, 700);
                    }
                case "Jinx":
                    {
                        return new Spell(SpellSlot.E, 900);
                    }
            }
            return null;
        }

        private static InventorySlot GetWardSlot
        {
            get
            {
                return
                    wardIds.Select(x => x)
                        .Where(
                            id =>
                                //menu.Item("AutoBush." + id).GetValue<bool>() && 
                                EnsoulSharp.Common.Items.HasItem(id) &&
                                EnsoulSharp.Common.Items.CanUseItem(id))
                        .Select(
                            wardId =>
                                ObjectManager.Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId)wardId))
                        .FirstOrDefault();
            }
        }

        private static AIBaseClient GetNearObject(String name, Vector3 pos, int maxDistance)
        {
            return ObjectManager.Get<AIBaseClient>()
                .FirstOrDefault(x => x.Name == name && x.Distance(pos) <= maxDistance);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            int time = Environment.TickCount;

            if (menu.Item("AutoBushEnabled").GetValue<bool>() || menu.Item("AutoBushKey").GetValue<KeyBind>().Active)
            {

                foreach (AIHeroClient enemy in
                    AutoBushHelper.EnemyInfo.Where(
                        x =>
                        x.Player.IsValid && !x.Player.IsVisible && !x.Player.IsDead
                        && x.Player.Distance(ObjectManager.Player.Position) < 1000 && time - x.LastSeen < 2500)
                        .Select(x => x.Player))
                {
                    var wardPosition = GetWardPos(enemy.Position, 165, 2);

                    if (wardPosition != enemy.Position && wardPosition != Vector3.Zero && wardPosition.Distance(ObjectManager.Player.Position) <= 600)
                    {
                        int timedif = Environment.TickCount - lastTimeWarded;

                        if (timedif > 1250 && !(timedif < 2500 && GetNearObject("SightWard", wardPosition, 200) != null)) //no near wards
                        {
                            //var myInClause = new string[] { "Corki", "Ashe", "Quinn", "Kalista" };
                            //var results = from x in ObjectManager.Player.CharacterData.CharacterName
                            //              where myInClause.Contains(x.ToString())
                            //              select x;

                            //if (ChampionSpell.IsReady())
                            //{
                            //    ChampionSpell.Cast(wardPosition);
                            //    return;
                            //}

                            if ((ObjectManager.Player.CharacterData.CharacterName == "Corki"
                                 || ObjectManager.Player.CharacterData.CharacterName == "Ashe"
                                 || ObjectManager.Player.CharacterData.CharacterName == "Quinn"
                                 || ObjectManager.Player.CharacterData.CharacterName == "Kalista"
                                 || ObjectManager.Player.CharacterData.CharacterName == "Jinx") && ChampionSpell.IsReady())
                            {
                                ChampionSpell.Cast(wardPosition);
                                lastTimeWarded = Environment.TickCount;
                                return;
                            }

                            var wardSlot = GetWardSlot;
                            if (wardSlot != null && wardSlot.Id != ItemId.Unknown)
                            {
                                ObjectManager.Player.Spellbook.CastSpell(wardSlot.SpellSlot, wardPosition);
                                lastTimeWarded = Environment.TickCount;
                            }
                        }
                    }
                }
            }
        }

        private static Vector3 GetWardPos(Vector3 lastPos, int radius = 165, int precision = 3)
        {
            var count = precision;
            while (count > 0)
            {
                var vertices = radius;

                var wardLocations = new WardLocation[vertices];
                var angle = 2 * Math.PI / vertices;

                for (var i = 0; i < vertices; i++)
                {
                    var th = angle * i;
                    var pos = new Vector3((float)(lastPos.X + radius * Math.Cos(th)), (float)(lastPos.Y + radius * Math.Sin(angle * i)), 0);
                    wardLocations[i] = new WardLocation(pos, NavMesh.IsWallOfGrass(pos, 50));
                }

                var grassLocations = new List<GrassLocation>();

                for (var i = 0; i < wardLocations.Length; i++)
                {
                    if (!wardLocations[i].Grass)
                    {
                        continue;
                    }
                    if (i != 0 && wardLocations[i - 1].Grass)
                    {
                        grassLocations.Last().Count++;
                    }
                    else
                    {
                        grassLocations.Add(new GrassLocation(i, 1));
                    }
                }

                var grassLocation = grassLocations.OrderByDescending(x => x.Count).FirstOrDefault();

                if (grassLocation != null)
                {
                    var midelement = (int)Math.Ceiling(grassLocation.Count / 2f);
                    lastPos = wardLocations[grassLocation.Index + midelement - 1].Pos;
                    radius = (int)Math.Floor(radius / 2f);
                }

                count--;
            }

            return lastPos;
        }

        private class WardLocation
        {
            public readonly Vector3 Pos;

            public readonly bool Grass;

            public WardLocation(Vector3 pos, bool grass)
            {
                Pos = pos;
                Grass = grass;
            }
        }

        private class GrassLocation
        {
            public readonly int Index;

            public int Count;

            public GrassLocation(int index, int count)
            {
                Index = index;
                Count = count;
            }
        }
    }
}
