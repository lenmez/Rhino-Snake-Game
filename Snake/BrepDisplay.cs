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
    /// To display breps as display conduits 
    /// </summary>
    public class BrepDisplay : DisplayConduit
    {
        List<Brep> Breps { get; set; }
        DisplayMaterial DisplayMaterial { get; set; }
        public BrepDisplay(List<Brep> breps, DisplayMaterial displayMaterial)
        {
            this.Breps = breps;
            this.DisplayMaterial = displayMaterial;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            foreach (Brep brep in Breps)
                e.Display.DrawBrepShaded(brep, DisplayMaterial);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            foreach (Brep brep in Breps)
                e.IncludeBoundingBox(brep.GetBoundingBox(true));

        }

        public void ChangeCurves(List<Brep> breps)
        {
            this.Enabled = false;
            this.Breps = breps;
            this.Enabled = true;
        }
    }
}
