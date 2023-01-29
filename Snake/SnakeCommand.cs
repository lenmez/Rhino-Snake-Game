using Rhino;
using Rhino.Commands;


namespace Snake
{
    public class SnakeCommand : Command
    {
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
            //Changes the switchedFlag, and rest will be handled in RhinoApp.Idle event
            SnakePlugIn.Instance.SwitchedFlag = !SnakePlugIn.Instance.SwitchedFlag;

            return Result.Success;
        }

        
    }
}
