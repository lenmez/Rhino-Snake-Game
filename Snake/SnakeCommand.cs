using Rhino;
using Rhino.Commands;
using System;

namespace Snake
{
    public class SnakeCommand : Command
    {
        /// <summary>
        /// Switch the Game on or off
        /// </summary>
        public bool SwitchedFlag { get; set; } = false;

        /// <summary>
        /// True, If game is running 
        /// </summary>
        public bool GamePlay { get; set; } = false;

        public Game Game { get; set; }

        bool altArrowNudge = Rhino.ApplicationSettings.ModelAidSettings.AltPlusArrow;
        double nudgeAmount = Rhino.ApplicationSettings.ModelAidSettings.NudgeKeyStep;
        public SnakeCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SnakeCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "RhinoSnake"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (Game == null)
            {
                
                Rhino.ApplicationSettings.ModelAidSettings.AltPlusArrow = false;
                Rhino.ApplicationSettings.ModelAidSettings.NudgeKeyStep = 0;
                Game = Game.StartGame();

                RhinoApp.Idle += OnIdle;
                
            }
            return Result.Success;
        }

       

        private void OnIdle(object sender, EventArgs e)
        {
            if (Game!=null && Game.GameOver)
            {
                try
                {
                    Game.Stop();
                    Game.Dispose();

                }

                finally
                {
                    Game = null;
                    RhinoApp.Idle -= OnIdle;
                    Rhino.ApplicationSettings.ModelAidSettings.AltPlusArrow = altArrowNudge;
                    Rhino.ApplicationSettings.ModelAidSettings.NudgeKeyStep = nudgeAmount;
                }
            }
        }

    }
}
