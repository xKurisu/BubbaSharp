using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace BubbaSharp
{
    class Bubba
    {
        public Vector2 KickPos;
        public Obj_AI_Hero HeroToKick;
        public Obj_AI_Hero TargetHero;
        public List<Obj_AI_Hero> HeroesOnSegment = new List<Obj_AI_Hero>(); 
    }

    class Program
    {

        #region Static Fields

        internal static Bubba BubbaFat;
        internal static Obj_AI_Hero Player => ObjectManager.Player;
        internal static Menu BubbaMenu;
        internal static Spell W, R;
        internal static SpellSlot Flash;


        #endregion

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName == "LeeSin")
            {
                W = new Spell(SpellSlot.W, 700f);
                R = new Spell(SpellSlot.R, 375f);

                BubbaMenu = new Menu("BubbaSharp", "bSharp", true);

                var drawings = new Menu("Draw", "draw");
                drawings.AddItem(new MenuItem("drawKickPos", "Kick Position"))
                    .SetValue(new Circle(true, System.Drawing.Color.Fuchsia));
                drawings.AddItem(new MenuItem("drawKickLine", "Kick Line Direction")).SetValue(new Circle(true, System.Drawing.Color.Fuchsia));
                drawings.AddItem(new MenuItem("drawKickTarget", "Desired Target")).SetValue(new Circle(true, System.Drawing.Color.LimeGreen));
                BubbaMenu.AddSubMenu(drawings);

                BubbaMenu.AddItem(new MenuItem("bSharpOn", "BubbaKush Key")).SetValue(new KeyBind('T', KeyBindType.Press));

                BubbaMenu.AddToMainMenu();

                Game.OnUpdate += Game_OnUpdate;
                Drawing.OnDraw += Drawing_OnDraw;

                if (Player.GetSpell(SpellSlot.Summoner1).Name.ToLower().Contains("flash"))
                    Flash = SpellSlot.Summoner1;

                if (Player.GetSpell(SpellSlot.Summoner2).Name.ToLower().Contains("flash"))
                    Flash = SpellSlot.Summoner2;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (BubbaFat != null && BubbaMenu.Item("bSharpOn").GetValue<KeyBind>().Active)
            {
                var kickLineMenu = BubbaMenu.Item("drawKickLine").GetValue<Circle>();
                if (kickLineMenu.Active)
                {
                    var start = Drawing.WorldToScreen(BubbaFat.KickPos.To3D());
                    var end = Drawing.WorldToScreen(BubbaFat.TargetHero.Position);

                    Drawing.DrawLine(start, end, 6, kickLineMenu.Color);
                }

                var kickPosMenu = BubbaMenu.Item("drawKickPos").GetValue<Circle>();
                if (kickPosMenu.Active)
                {
                    Render.Circle.DrawCircle(BubbaFat.KickPos.To3D(), 55f, kickPosMenu.Color, 6);
                }


                var kickTargetMenu = BubbaMenu.Item("drawKickTarget").GetValue<Circle>();
                if (kickTargetMenu.Active)
                {
                    Render.Circle.DrawCircle(BubbaFat.TargetHero.Position, 55f, kickTargetMenu.Color, 3);
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (BubbaMenu.Item("bSharpOn").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(1000 + W.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                {
                    BubbKushGo(t);
                }
            }
        }

        private static void BubbKushGo(Obj_AI_Hero target)
        {
            int posChecked = 0;
            int maxPosToCheck = 50;
            int posRadius = 50;
            int radiusIndex = 0;

            var bubba = new Bubba();
            var bubbaList = new List<Bubba>();

            while (posChecked < maxPosToCheck)
            {
                radiusIndex++;
                var curRadius = radiusIndex * (2 * posRadius);
                var curCurcleChecks = (int) Math.Ceiling((2 * Math.PI * curRadius) / (2 * (double) posRadius));

                for (var i = 1; i < curCurcleChecks; i++)   
                {
                    posChecked++;

                    var cRadians = (0x2 * Math.PI / (curCurcleChecks - 1)) * i;
                    var startPos = new Vector2((float) Math.Floor(target.Position.X + curRadius * 
                          Math.Cos(cRadians)), (float) Math.Floor(target.Position.Y + curRadius * Math.Sin(cRadians)));

                    var endPos = startPos.Extend(target.Position.To2D(), 1000f);
                    var targetProj = target.Position.To2D().ProjectOn(startPos, endPos);

                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget()))
                    {
                        if (hero.NetworkId != target.NetworkId && hero.Distance(targetProj.SegmentPoint) <= 1000)
                        {
                            Vector2 mPos = Prediction.GetPrediction(hero, 250 + Game.Ping / 2).UnitPosition.To2D();
                            Geometry.ProjectionInfo mProj = mPos.ProjectOn(startPos, endPos);
                            if (mProj.IsOnSegment && mProj.SegmentPoint.Distance(hero.Position) <= hero.BoundingRadius + 100)
                            {
                                if (bubba.HeroesOnSegment.Contains(hero) == false)
                                {
                                    bubba.HeroToKick = hero;
                                    bubba.TargetHero = target;
                                    bubba.KickPos = hero.Position.To2D().Extend(startPos, - (hero.BoundingRadius + 35));
                                    bubba.HeroesOnSegment.Add(hero);
                                }
                            }
                        }
                    }

                    bubbaList.Add(bubba);

                    BubbaFat =
                        bubbaList.Where(x => x.HeroesOnSegment.Count > 0)
                            .OrderByDescending(x => x.HeroesOnSegment.Count)
                            .ThenByDescending(x => x.HeroToKick.MaxHealth).FirstOrDefault();

                    if (BubbaFat != null)
                    {
                        // todo: ¯\_(ツ)_/¯
                    }
                }
            }
        }
    }
}
