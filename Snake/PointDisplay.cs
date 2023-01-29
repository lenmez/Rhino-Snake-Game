using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snake
{
    /// <summary>
    /// To display points as display conduits
    /// </summary>
    public class PointDisplay: DisplayConduit
    {
        Point3d Point { get; set; }

        public PointDisplay(Point3d point)
        {
            this.Point = point;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            
                e.Display.DrawPoint(Point,PointStyle.RoundSimple, 6,System.Drawing.Color.DarkGreen);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
           
               
        }

        public void ChangePoint(Point3d pt)
        {
            this.Enabled = false;
            this.Point = pt;
            this.Enabled = true;
        }
    }
}
