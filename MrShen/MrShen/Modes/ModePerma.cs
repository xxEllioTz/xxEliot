﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;
using MrShen.Champion;

namespace MrShen.Modes
{
    internal class DangerousSpells
    {
        public string ChampionName { get; private set; }
        public SpellSlot SpellSlot { get; private set; }

        public DangerousSpells(string championName, SpellSlot spellSlot)
        {
            ChampionName = championName;
            SpellSlot = spellSlot;
        }
    }
    internal class ModePerma
    {

        public static EnsoulSharp.Common.Menu LocalMenu { get; private set; }
        public static bool FlashEActive => LocalMenu.Item("Perma.FlashE").GetValue<KeyBind>().Active;
        public static bool FleeActive => LocalMenu.Item("Perma.Flee").GetValue<KeyBind>().Active;
        public static Spell Q => MrShen.Champion.PlayerSpells.Q;
        public static Spell W => MrShen.Champion.PlayerSpells.W;
        public static Spell E => MrShen.Champion.PlayerSpells.E;
        public static Spell R => MrShen.Champion.PlayerSpells.R;

        public static List<DangerousSpells> DangerousTargetedSpells = new List<DangerousSpells>();

        private static void LoadDangerousTargetedSpells()
        {
            DangerousTargetedSpells.Add(new DangerousSpells("darius", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("garen", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("leesin", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("syndra", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("warwick", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("zed", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("tristana", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("syndra", SpellSlot.R));
            DangerousTargetedSpells.Add(new DangerousSpells("malzahar", SpellSlot.R));
        }

        public static void Initialize(EnsoulSharp.Common.Menu MenuConfig)
        {
            LocalMenu = MenuConfig;
            LocalMenu.AddItem(new MenuItem("Perma.FlashE", "Flash + E:").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            LocalMenu.AddItem(new MenuItem("Perma.Flee", "Flee").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.Coral));

            LoadDangerousTargetedSpells();

            Game.OnUpdate += GameOnOnUpdate;
            //Obj_AI_Base.OnProcessSpellCast += ObjAiHeroOnOnProcessSpellCast;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (FlashEActive)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosCenter);

                var t = TargetSelector.GetTarget(E.Range + 500, EnsoulSharp.Common.TargetSelector.DamageType.Physical);

                if (!t.IsValidTarget() || !E.IsReady() || ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.FlashSlot) != SpellState.Ready)
                {
                    return;
                }

                var hithere = t.Position + Vector3.Normalize(t.Position - ObjectManager.Player.Position) * 60;
                if (ObjectManager.Player.Distance(t) > E.Range && E.IsReady() &&
                    ObjectManager.Player.Position.Distance(hithere) <= 430 + E.Range &&
                    Common.SummonerManager.FlashSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.FlashSlot) == SpellState.Ready)
                {
                    ObjectManager.Player.Spellbook.CastSpell(Common.SummonerManager.FlashSlot, t.Position);
                    Utility.DelayAction.Add(100, () => MrShen.Champion.PlayerSpells.E.Cast(hithere));
                }
            }

            if (FleeActive)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosCenter);

                if (E.IsReady())
                {
                    E.Cast(Game.CursorPosCenter);
                }
            }
        }

        private static void ObjAiHeroOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.Type != GameObjectType.AIHeroClient)
            {
                return;
            }

            if (sender.IsEnemy && sender is AIHeroClient && !args.Target.IsMe && args.Target.IsAlly && !args.Target.IsDead && PlayerSpells.R.IsReady())
            {
                var ally = (AIHeroClient)args.Target;
                foreach (
                    var c in
                        DangerousTargetedSpells.Where(
                            c => ((AIHeroClient)sender).CharacterName.ToLower() == c.ChampionName)
                            .Where(c => args.Slot == c.SpellSlot && Modes.ModeUlti.LocalMenu.Item(ally.CharacterName + ".UseRConfirm").GetValue<StringList>().SelectedIndex == 1))
                //.Where(c => args.SData.Name == ((Obj_AI_Hero)sender).GetSpell(c.SpellSlot).Name))
                {
                    PlayerSpells.R.Cast((AIHeroClient)args.Target);
                }
            }

        }
    }
}