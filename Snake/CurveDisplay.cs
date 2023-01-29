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
    /// To display curves as display conduits
    /// </summary>
    public class CurveDisplay : DisplayConduit
    {
        List<Curve> curves { get; set; }

        public CurveDisplay(List<Curve> curvea)
        {
            this.curves = curves;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            foreach (Curve curve in curves)
                e.Display.DrawCurve(curve, System.Drawing.Color.Black, 12);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            foreach (Curve curve in curves)
                e.IncludeBoundingBox(curve.GetBoundingBox(true));
        }

        public void ChangeCurves(List<Curve> curves)
        {
            this.Enabled = false;
            this.curves = curves;
            this.Enabled = true;
        }
    }
}
