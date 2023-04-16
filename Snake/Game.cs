using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Snake
{
    public class Game : IDisposable
    {
        /// <summary>
        /// Keyboard listener to listen to keyboard key press events
        /// </summary>
        KeyboardListener keyboardListener;

        /// <summary>
        /// Play each step sound when snake moves
        /// </summary>
        SoundPlayer StepPlayer { get; set; }

        /// <summary>
        /// Play sound when snake eats food
        /// </summary>
        SoundPlayer FoodPlayer { get; set; }

        /// <summary>
        /// List of current connecting points of snake
        /// </summary>
        List<Point3d> SnakePoints { get; set; }

        /// <summary>
        /// Current movement diretion of snake
        /// </summary>
        Vector3d SnakeDirection { get; set; }

        /// <summary>
        /// Boundary curve of the game
        /// </summary>
        Curve BoundaryCurve { get; set; }

        /// <summary>
        /// Solid boundary brep created from boundary curve
        /// </summary>
        Brep BoundayBrep { get; set; }

        //Maximum and Minimum X and Y limits for the snake points
        double XLimitMax { get; set; } = 100;

        double YLimitMax { get; set; } = 100;

        double XLimitMin { get; set; } = -100;

        double YLimitMin { get; set; } = -100;

        /// <summary>
        /// Current Food point of snake
        /// </summary>
        Point3d Food { get; set; }

        /// <summary>
        /// random number generator to generate coordinates for snake food point
        /// </summary>
        Random Random { get; set; }


        PointDisplay PointDisplay { get; set; }

        /// <summary>
        /// Displa conduit displaying game boundary
        /// </summary>
        BrepDisplay Boundary { get; set; }

        BrepDisplay Pipe { get; set; }

        bool DirectionChanged { get; set; }

        Keys PressedKey { get; set; }

        System.Timers.Timer Timer { get; set; }

        /// <summary>
        /// game speed, progressively increases as snake eats food
        /// </summary>
        double Speed { get; set; } = 300;

        bool ChangeSpeed { get; set; } = false;
        public bool GameOver { get; private set; }
        public Game()
        {
            Rhino.ApplicationSettings.ModelAidSettings.NudgeKeyStep = 0;
            keyboardListener = new KeyboardListener();
            Random = new Random(25);
            SnakePoints = new List<Point3d>();

            StepPlayer = new SoundPlayer(Properties.Resources.sfx_movement_ladder1a);
            FoodPlayer = new SoundPlayer(Properties.Resources.sfx_movement_jump19);

            //Initial snake points
            SnakePoints.Add(new Point3d(-30, 0, 0));
            SnakePoints.Add(new Point3d(-20, 0, 0));
            SnakePoints.Add(new Point3d(-10, 0, 0));
            SnakePoints.Add(new Point3d(0, 0, 0));
            SnakePoints.Add(new Point3d(10, 0, 0));

            //Initial snake direction
            SnakeDirection = Plane.WorldXY.XAxis;
            BoundaryCurve = GetBoundaryCurve();
            CreateBoundaryBrep();
            CreateFood();
            keyboardListener.OnKeyPressed += OnKeyPress;

            keyboardListener.HookKeyboard();
        }


        /// <summary>
        /// Create boundary Brep for game
        /// </summary>
        public void CreateBoundaryBrep()
        {
            Curve offset = BoundaryCurve.DuplicateCurve();
            offset.Offset(Plane.WorldXY, -10, 0.001, CurveOffsetCornerStyle.None);
            offset.Scale(0.9);
            var planarBreps = Brep.CreatePlanarBreps(new List<Curve>() { BoundaryCurve, offset }, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            BoundayBrep = planarBreps[0];
            var offsetBrep = Brep.CreateOffsetBrep(BoundayBrep, -10, true, false, 0.001, out Brep[] blends, out Brep[] walls);
            BoundayBrep = offsetBrep[0];
            Boundary = new BrepDisplay(new List<Brep> { offsetBrep[0] }, new DisplayMaterial(System.Drawing.Color.DarkRed, System.Drawing.Color.White, System.Drawing.Color.White, System.Drawing.Color.Black, 5, 0));
            Boundary.Enabled = true;
        }

        /// <summary>
        /// Create food point based on Random number generator, within boundary
        /// </summary>
        public void CreateFood()
        {
            int x = Random.Next(-9, 9);
            int y = Random.Next(-9, 9);

            Food = new Point3d(x * 10, y * 10, 0);
            if (PointDisplay == null)
            {
                PointDisplay = new PointDisplay(Food);
                PointDisplay.Enabled = true;
            }
            else
                PointDisplay.ChangePoint(Food);
        }

        public Curve GetBoundaryCurve()
        {
            Point3d pt1 = new Point3d(XLimitMax, YLimitMax, 0);
            Point3d pt2 = new Point3d(XLimitMax, YLimitMin, 0);
            Point3d pt3 = new Point3d(XLimitMin, YLimitMin, 0);
            Point3d pt4 = new Point3d(XLimitMin, YLimitMax, 0);

            Polyline line = new Polyline(new List<Point3d>() { pt1, pt2, pt3, pt4, pt1 });
            return line.ToNurbsCurve();
        }

        /// <summary>
        /// Move snake one step, based on direction.
        /// This moves all snake points based on given vector
        /// </summary>
        public void MoveOneStep()
        {
            List<Point3d> oldList = new List<Point3d>(SnakePoints);

            for (int i = SnakePoints.Count - 1; i >= 0; i--)
            {
                Point3d current = SnakePoints[i];

                if (i == SnakePoints.Count - 1)
                {
                    Point3d currentCopy = new Point3d(current);
                    currentCopy.Transform(Transform.Translation(SnakeDirection * 10));
                    AdjustCoordinatesByLimit(ref currentCopy);

                    SnakePoints[i] = currentCopy;

                    if (AteFood(currentCopy))
                        GameLevelUp();
                    else
                        StepPlayer.Play();
                    continue;
                }

                SnakePoints[i] = oldList[i + 1];
            }

        }

        /// <summary>
        /// Compares XY coordinates of point based on given limits, and adjusts them, if they exceed
        /// [So the snake doesnt go out of boundaries]
        /// </summary>
        /// <param name="point"></param>
        void AdjustCoordinatesByLimit(ref Point3d point)
        {
            if (point.X >= XLimitMax)
                point.X = XLimitMin + 10;
            else if (point.X <= XLimitMin)
                point.X = XLimitMax - 10;

            if (point.Y >= YLimitMax)
                point.Y = YLimitMin + 10;
            else if (point.Y <= YLimitMin)
                point.Y = YLimitMax - 10;
        }

        /// <summary>
        /// True, if snake ate food. This will increase game speed
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool AteFood(Point3d pt)
        {
            if (Math.Round(pt.DistanceTo(Food), 3) == 0)
            {
                if (SnakePoints.Count % 2 == 0 && Speed > 100)
                    ChangeSpeed = true;
                return true;

            }
            return false;
        }

        /// <summary>
        /// Grow snake and speed up the game [When food eaten]
        /// </summary>
        void GameLevelUp()
        {
            FoodPlayer.Play();
            AddNewPoint();
            CreateFood();
            if (ChangeSpeed)
            {
                if (Timer.Interval > 100)
                    Timer.Interval -= 50;
                ChangeSpeed = false;

            }
        }

        /// <summary>
        /// SnakePoints will get a new point in movement direction to simulate movement
        /// </summary>
        private void AddNewPoint()
        {
            Line line = new Line(SnakePoints[SnakePoints.Count - 1], SnakeDirection * 10);
            SnakePoints.Add(line.To);
        }

        public List<Curve> GetCurve()
        {
            List<Curve> crvs = new List<Curve>();
            for (int i = 0; i < SnakePoints.Count - 1; i++)
            {
                Point3d first = SnakePoints[i];
                Point3d second = SnakePoints[i + 1];

                if (Math.Round(first.DistanceTo(second), 3) == 10)
                    crvs.Add(new LineCurve(new Line(first, second)));
            }

            if (Coincident())
                GameOver = true;
            return crvs;
        }

        /// <summary>
        /// Create display pipes using curve. To visualize snake as a 3D pipe 
        /// </summary>
        public void CreatePipes()
        {
            Curve[] crvs = Curve.JoinCurves(GetCurve());
            List<Brep> pipes = new List<Brep>();
            foreach (Curve c in crvs)
                pipes.AddRange(Brep.CreatePipe(c, 2, true, PipeCapMode.Flat, true, 0.001, 0.001));
            if (Pipe == null)
            {
                DisplayMaterial displayMaterial = new DisplayMaterial(Color.DarkGreen, Color.White, Color.Gray, Color.Black, 5, 0.1);
                Pipe = new BrepDisplay(pipes, displayMaterial);
                Pipe.Enabled = true;
            }
            else
            {
                Pipe.ChangeCurves(pipes);
            }
        }

        /// <summary>
        /// True, If snake collides with self
        /// </summary>
        /// <returns></returns>
        public bool Coincident()
        {
            var similar = SnakePoints.GroupBy(x => x).Where(x => x.Count() > 1).ToList();
            if (similar.Count > 0)
                return true;
            return false;
        }


        /// <summary>
        /// Start the snake game
        /// </summary>
        /// <returns></returns>
        public static Game StartGame()
        {
            Game game = new Game();
            game.CreatePipes();
            game.Timer = new System.Timers.Timer(game.Speed);
            game.Timer.Start();
            game.Timer.Elapsed += ElapsedHandler;
            return game;

            void ElapsedHandler(object sender, ElapsedEventArgs e)
            {
                try
                {
                    if (game.GameOver)
                    {
                       

                        return;
                    }
                    if (game.DirectionChanged)
                        game.ChangeDirection();
                    game.MoveOneStep();
                    game.CreatePipes();
                    RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                }
                catch
                {
                    game.Dispose();
                }
            }
        }

        /// <summary>
        /// Display "Game Over" test when game is over
        /// </summary>
        public void DisplayGameOver()
        {
            TextEntity textEntityGO = new TextEntity();
            textEntityGO.Font = new Rhino.DocObjects.Font("BANKGOTHIC MD BT");
            textEntityGO.RichText = "GAME OVER";
            textEntityGO.Translate(-Plane.WorldXY.XAxis * 80);
            textEntityGO.Translate(Plane.WorldXY.YAxis * 20);
            Brep[] brepsGameOver = textEntityGO.CreateSurfaces(new Rhino.DocObjects.DimensionStyle() { TextHeight = 12, Font = new Rhino.DocObjects.Font("BANKGOTHIC MD BT") });
            List<Brep> extrudedGO = new List<Brep>();
            foreach (Brep brep in brepsGameOver)
            {
                var offsetBrep = Brep.CreateOffsetBrep(brep, -5, true, false, 0.001, out Brep[] blends, out Brep[] walls);
                extrudedGO.Add(offsetBrep[0]);
            }

            TextEntity textEntityBW = new TextEntity();
            textEntityBW.Font = new Rhino.DocObjects.Font("BANKGOTHIC MD BT");
            textEntityBW.PlainText = "Back to Work now!!";
            textEntityBW.Translate(-Plane.WorldXY.XAxis * 85);
            textEntityBW.Translate(Plane.WorldXY.YAxis * -10);
            Brep[] brepsBW = textEntityBW.CreateSurfaces(new Rhino.DocObjects.DimensionStyle() { TextHeight = 8, Font = new Rhino.DocObjects.Font("BANKGOTHIC MD BT") });
            List<Brep> extrudedBW = new List<Brep>();
            foreach (Brep brep in brepsBW)
            {
                var offsetBrep = Brep.CreateOffsetBrep(brep, -5, true, false, 0.001, out Brep[] blends, out Brep[] walls);
                extrudedBW.Add(offsetBrep[0]);
            }

            var brepsList = extrudedGO.Union(extrudedBW).ToList();
            brepsList.Add(BoundayBrep);
            Boundary.ChangeCurves(brepsList.ToList());
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
            StepPlayer.Stop();
            var over = new SoundPlayer(Properties.Resources.sfx_sounds_falling3);
            over.Play();
        }

        /// <summary>
        /// Change snake direction based on arrow key pressed
        /// </summary>
        public void ChangeDirection()
        {
            DirectionChanged = false;
            Vector3d newVector = SnakeDirection;
            if (PressedKey == Keys.Up)
            {
                if (!IsOnBorder())
                    newVector = Plane.WorldXY.YAxis;
            }

            else if (PressedKey == Keys.Down)
            {
                if (!IsOnBorder())
                    newVector = Plane.WorldXY.YAxis * -1;
            }

            if (PressedKey == Keys.Left)
            {
                if (!IsOnBorder())
                    newVector = Plane.WorldXY.XAxis * -1;
            }

            if (PressedKey == Keys.Right)
            {
                if (!IsOnBorder())
                    newVector = Plane.WorldXY.XAxis;
            }

            double angle = Vector3d.VectorAngle(SnakeDirection, newVector);
            if (Math.Round(angle, 3) == 0)
                return;
            else if (Math.Round(angle, 3) == Math.Round(Math.PI, 3))
                return;

            SnakeDirection = newVector;
        }

        public bool IsOnBorder()
        {
            Point3d leader = SnakePoints[SnakePoints.Count - 1];

            if (leader.X == XLimitMax || leader.X == XLimitMin)
                return true;
            else if (leader.Y == YLimitMax || leader.Y == YLimitMin)
                return true;
            return false;
        }

        /// <summary>
        /// Stop game
        /// </summary>
        public void Stop()
        {
            Pipe.Enabled = false;
            PointDisplay.Enabled = false;
            if (GameOver)
            {
                DisplayGameOver();
                RhinoDoc.ActiveDoc.Views.Redraw();

                System.Threading.Thread.Sleep(2000);
            }
            Boundary.Enabled = false;
            Timer.Stop();
            SnakePoints = null;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }




        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                DirectionChanged = true;
                PressedKey = Keys.Up;

                return;
            }

            else if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                DirectionChanged = true;
                PressedKey = Keys.Down;
                return;
            }

            else if (e.KeyCode == Keys.Left)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                DirectionChanged = true;
                PressedKey = Keys.Left;
                return;
            }

            else if (e.KeyCode == Keys.Right)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                DirectionChanged = true;
                PressedKey = Keys.Right;
                return;
            }

            else if (e.KeyCode == Keys.Escape)
            {
                Stop();
                GameOver = true;
            }
        }

        public void Dispose()
        {
            GameOver = true;
            Pipe.Enabled = false;
            PointDisplay.Enabled = false;
            Boundary.Enabled = false;
            Timer.Stop();
            Timer.Dispose();
            SnakePoints = null;
            keyboardListener.UnHookKeyboard();
        }
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
}
