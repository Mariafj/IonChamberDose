using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

        public void Execute(ScriptContext context, System.Windows.Window window)
        {
            // Create script window.
            var mainControl = new IonChamberDose.UserControl1();
            window.Content = mainControl;
            window.Width = 450;
            window.Height = 600;

            // Struktursettet skal bruges af knappen der kan trykkes på.
            mainControl.struSet = context.StructureSet;
            mainControl.plansetup = context.IonPlanSetup;

             foreach (var beam in context.IonPlanSetup.IonBeams)
            {
                mainControl.Fieldname.Items.Add(beam.Id);
            }

            foreach (var stru in context.StructureSet.Structures)
            {
                if (stru.DicomType == "MARKER")
                {
                    mainControl.markers.Items.Add(stru.Id);
                }
            }
        }
  }
}
