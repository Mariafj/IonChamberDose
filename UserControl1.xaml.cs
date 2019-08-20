using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace IonChamberDose
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {

        public StructureSet struSet;
        public IonPlanSetup plansetup;

        public UserControl1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            dose_corr_label.Content = "(*) The dose was corrected using:";

            double radius = 7.8;
            if ((bool)double.TryParse(radius_tb.Text, out radius) == false)
            {
                radius = 7.8;
            }
            dose_corr_label.Content += string.Format("\n The radius was set to: {0} mm", radius);



            double xcoord = 0;
            double ycoord = 0;
            double zcoord = 0;

            if (Double.TryParse(xcoor.Text, out xcoord) == false)
            {
                dose_mean.Text = "NaN";
                dose_mean.Text = "NaN";
                return;
            }

            if (Double.TryParse(ycoor.Text, out ycoord) == false)
            {
                dose_mean.Text = "NaN";
                dose_mean.Text = "NaN";
                return;
            }

            if (Double.TryParse(zcoor.Text, out zcoord) == false)
            {
                dose_mean.Text = "NaN";
                dose_mean.Text = "NaN";
                return;
            }

            Beam beamtest = plansetup.IonBeams.First(n => n.Id == Fieldname.SelectedItem.ToString());
            dose_corr_label.Content += "\n The field ID is: " + beamtest.Id;

            // DOSIS normaliseres af flere parametre
            double planPerscribedPercentage = plansetup.PrescribedPercentage;
            double dosenorm = beamtest.WeightFactor * (1 / planPerscribedPercentage) * plansetup.TotalPrescribedDose.Dose / (plansetup.PlanNormalizationValue / 100); // Gy

            dose_corr_label.Content += string.Format("\n Field Weight: {0} \n Total Prescribed Dose: {1} Gy \n Plan normalization value {2} % \n Perscribed Percentage {3} %.", beamtest.WeightFactor, plansetup.TotalPrescribedDose.Dose, plansetup.PlanNormalizationValue, plansetup.PrescribedPercentage * 100);

            double beamNormalizationFactor = beamtest.NormalizationFactor;
            if (beamNormalizationFactor != 1)
            {
                dose_corr_label.Content += string.Format("\n\n NOTICE that the beam normalization factor should always be 100%. \n THIS IS NOT THE CASE FOR THIS FIELD: {0} %.\n !!!The script is probably not calculating the correct dose!!!!", beamNormalizationFactor * 100);
            }
            else
            {
                dose_corr_label.Content += string.Format("\n\n NOTICE that the beam normalization factor should always be 100%. \n For this field it is: {0} %", beamNormalizationFactor * 100);
            }

            //System.Windows.MessageBox.Show(String.Format("BeamNorm: {0} \n BeamWeight: {1} \n TotalPrescribed: {2} \n PlanNorm {3} \n PerscribedPercentage {4}.",beamNormalizationFactor, beamtest.WeightFactor, plansetup.TotalPrescribedDose.Dose, plansetup.PlanNormalizationValue, plansetup.PrescribedPercentage));

            VVector origin = struSet.Image.UserOrigin;

            double v1 = xcoord * 10.0 - origin.x;
            double v2 = -zcoord * 10.0 + origin.y;
            double v3 = ycoord * 10.0 - origin.z;

            VVector dosepoint2 = new VVector { x = v1, y = v2, z = v3 };

            Dose fielddose = plansetup.IonBeams.First(n => n.Id == Fieldname.SelectedItem.ToString()).Dose;

            Tuple<double, double> result = calculateDose(fielddose,dosepoint2,dosenorm, radius);
            dose_mean.Text = result.Item1.ToString("0.000");
            dose_std.Text = result.Item2.ToString("0.000");

            //Uncertainties
            v1 = xcoord * 10.0 - origin.x + 1;
            v2 = -zcoord * 10.0 + origin.y;
            v3 = ycoord * 10.0 - origin.z;
            VVector u1 = new VVector { x = v1, y = v2, z = v3 };
            Tuple<double, double> result_u1 = calculateDose(fielddose, u1, dosenorm, radius);

            v1 = xcoord * 10.0 - origin.x - 1;
            v2 = -zcoord * 10.0 + origin.y;
            v3 = ycoord * 10.0 - origin.z;
            VVector u2 = new VVector { x = v1, y = v2, z = v3 };
            Tuple<double, double> result_u2 = calculateDose(fielddose, u2, dosenorm, radius);

            v1 = xcoord * 10.0 - origin.x;
            v2 = -zcoord * 10.0 + origin.y;
            v3 = ycoord * 10.0 - origin.z + 1;
            VVector u3 = new VVector { x = v1, y = v2, z = v3 };
            Tuple<double, double> result_u3 = calculateDose(fielddose, u3, dosenorm, radius);

            v1 = xcoord * 10.0 - origin.x;
            v2 = -zcoord * 10.0 + origin.y;
            v3 = ycoord * 10.0 - origin.z - 1;
            VVector u4 = new VVector { x = v1, y = v2, z = v3 };
            Tuple<double, double> result_u4 = calculateDose(fielddose, u4, dosenorm,radius);

            double[] u_list = new double[4] { result_u1.Item1, result_u2.Item1, result_u3.Item1, result_u4.Item1 }; 

            max_dose_mean.Text = u_list.Max().ToString("0.000");
            min_dose_mean.Text = u_list.Min().ToString("0.000");

        }

        private Tuple<double, double> calculateDose(Dose fielddose,VVector dosepoint2,double dosenorm,double radius)
        {
            List<double> doses = new List<double>();

            //System.Windows.MessageBox.Show("Dose in point: " + (dosenorm * plansetup.Dose.GetDoseToPoint(dosepoint2).Dose / 100).ToString());

            for (double i = -radius*10.0+20.0; i < radius * 10.0 + 20.0; i++)
            {
                for (double j = -radius * 10.0 + 20.0; j < radius * 10.0 + 20.0; j++)
                {
                    if (i * i + j * j <= radius * 10.0 * radius * 10.0)
                    {
                        VVector dosepoint_vector = new VVector { x = dosepoint2.x + i / 10.0, y = dosepoint2.y, z = dosepoint2.z + j / 10.0 };

                        DoseValue dose = fielddose.GetDoseToPoint(dosepoint_vector);

                        doses.Add(dosenorm * dose.Dose / 100.0); //Gy
                    }
                }
            }

            //System.Windows.MessageBox.Show(doses.Count().ToString());
            //System.Windows.MessageBox.Show(doses.Sum().ToString());
            double mean = doses.Sum() / (double)doses.Count;
            double dup = 0;

            foreach (var dip in doses)
            {
                dup += (dip - mean) * (dip - mean);
            }
            double std = Math.Sqrt(dup / (doses.Count - 1));

            Tuple<double, double> iTuple = new Tuple<double, double>(mean, std);
            return iTuple;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            VVector origin = struSet.Image.UserOrigin;

            if (markers.SelectedItem != null)
            {
                Structure mark = struSet.Structures.First(n => n.Id == (string)markers.SelectedItem);

                xcoor.Text = ((mark.CenterPoint.x - origin.x) / 10.0).ToString("0.00");
                ycoor.Text = ((mark.CenterPoint.z - origin.z)/10.0).ToString("0.00");
                zcoor.Text = ((-mark.CenterPoint.y + origin.y)/10.0).ToString("0.00");

            }
        }
    }
}