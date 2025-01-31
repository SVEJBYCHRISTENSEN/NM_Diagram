using ConcreteSection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NM_Concrete
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ReinforcementWindow : Window
    {
        public RectangleSection section = null;
        private string _xmlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", "Materials.xml");
        private XDocument _xmlDoc;
        public ReinforcementWindow(RectangleSection sectionInput)
        {
            InitializeComponent();
            section = sectionInput;
            LoadMaterials("Rebar");
        }

        private void LoadMaterials(string materialType)
        {
            _xmlDoc = XDocument.Load(_xmlFilePath);
            var materials = _xmlDoc.Root.Element(materialType)
                ?.Elements("Material")
                .Select(x => x.Attribute("id")?.Value)
                .Where(id => id != null) // Ensures null IDs are excluded
                .ToList();

            rebarMaterialInput.ItemsSource = materials;
            //MaterialListBox.DisplayMemberPath = "id";
            //_currentMaterialElement = null;
        }

        private void addRenforcementToSectionButton_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(diameterInput.Text, out double diameter);
            int.TryParse(numberInput.Text, out int n);
            double.TryParse(cInput.Text, out double c);
            double.TryParse(gammaInput.Text, out double gamma);

            Materials.RebarMaterial rebarMaterial = new Materials.RebarMaterial(new Materials.Material(), rebarMaterialInput.Text, gamma);

            bool? prestressing = prestressCheck.IsChecked;
            double alpha = rebarMaterial.Es / section.Concrete.Ecm;
            if ((bool)prestressing)
            {
                double.TryParse(prestressInput.Text, out double intialForce);
                double area = 0.25 * Math.PI * Math.Pow(diameter, 2);
                double strain = intialForce * 1000 / area / rebarMaterial.Es;
                
                if (strain > rebarMaterial.eyd())
                {
                    strain = rebarMaterial.eyd();
                    MessageBox.Show("Pre-stressing results in strain larger than the yield strain - Pre-stress strain is reduced");
                }

                section.AddReinforcementLayer(rebarMaterial, diameter, n, c, alpha, -strain);
            }
            else { section.AddReinforcementLayer(rebarMaterial, diameter, n, c, alpha); }
        }

        private void rebarEditButton_Click(object sender, RoutedEventArgs e)
        {
            MaterialManagerWindow materialWindow = new MaterialManagerWindow();

            // Show the pop-up window
            materialWindow.ShowDialog(); // Opens the window as a non-modal window
            LoadMaterials("Rebar");
        }

        private void rebarCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

       
    }
}
