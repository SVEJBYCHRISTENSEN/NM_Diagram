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
using System.Xml.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using OxyPlot.SkiaSharp;



// 
using ConcreteSection;
using NM_Diagram;
using OxyPlot;



namespace NM_Concrete
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Materials.ConcreteMaterial concreteMaterial;
        public Materials.RebarMaterial rebarMaterial;
        public RectangleSection section = null;
        public Calculation results = null;
        private string _xmlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", "Materials.xml");
        private XDocument _xmlDoc;

        public MainWindow()
        {
            InitializeComponent();
            LoadMaterials("Concrete");
        }

        private void LoadMaterials(string materialType)
        {
            _xmlDoc = XDocument.Load(_xmlFilePath);
            var materials = _xmlDoc.Root.Element(materialType)
                ?.Elements("Material")
                .Select(x => x.Attribute("id")?.Value)
                .Where(id => id != null) // Ensures null IDs are excluded
                .ToList();

            concreteMaterialInput.ItemsSource = materials;
            //MaterialListBox.DisplayMemberPath = "id";
            //_currentMaterialElement = null;
        }
        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            if (section != null)
            {
                section.StrainCalculation();
                results = new Calculation(section);
                //diagramPlot.Model = results.DiagramPlot;
                diagramPlot.Model = results.testPlot; 
            }
            else
            { MessageBox.Show("Section is not initialized. Please define the section before performing calculations.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            
        }

        private void addReinforcementButton_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the pop-up window
            ReinforcementWindow popup = new ReinforcementWindow(section);

            // Show the pop-up window
            popup.ShowDialog(); // Opens the window as a non-modal window
            section = (RectangleSection)popup.section;
            plotSection();
        }

        private void sectionUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(heightInput.Text, out double height);
            double.TryParse(widthInput.Text, out double width);
            double.TryParse(gammaCInput.Text, out double gamma);

            concreteMaterial = new Materials.ConcreteMaterial(new Materials.Material(), concreteMaterialInput.Text, gamma);

            try { section = new RectangleSection(width, height, concreteMaterial); }
            catch (ArgumentOutOfRangeException ex)
            { MessageBox.Show($"Error - {ex}"); }
            plotSection();

        }
        private void editMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            MaterialManagerWindow materialWindow = new MaterialManagerWindow();

            // Show the pop-up window
            materialWindow.ShowDialog(); // Opens the window as a non-modal window
            LoadMaterials("Concrete");
        }


        private void plotSection()
        {
             sectionPlot.Model = section.Plot(); 
        }

        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new PDF document
            PdfDocument pdf = new PdfDocument();
            pdf.Info.Title = "NM-Report";

            // Add a page
            PdfPage page = pdf.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 12);
            XFont boldfont = new XFont("Verdana", 12, XFontStyleEx.Bold);

            double row = 40;
            // Add text to the PDF
            gfx.DrawString("Calculation parameters:", font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
            row += 16;
            // Add a formula (as text or an image of LaTeX-rendered formula)
            gfx.DrawString(section.ToString(), font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
            row += 16;
            // Add a formula (as text or an image of LaTeX-rendered formula)
            gfx.DrawString("Reinforcement layers for the section:", font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
            row += 16;
            Dictionary<string, string> rebarMaterials = new Dictionary<string, string>();
            foreach (var layer in section.reinforcementLayers)
            {
                gfx.DrawString("  -"+layer.ToString(), font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
                row += 16;

                if (!rebarMaterials.ContainsKey(layer.Material.Name))
                {
                    rebarMaterials[layer.Material.Name] = layer.Material.ToString();
                }
            }

            // Materials
            row += 32;
            gfx.DrawString("Materials:", boldfont, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
            row += 16;
            gfx.DrawString("  -" + section.Concrete.ToString(), font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
            row += 16;
            foreach (string key in rebarMaterials.Keys)
            {
                gfx.DrawString("  -" + rebarMaterials[key], font, XBrushes.Black, new XRect(40, row, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
                row += 16;
            }
            row += 24;

            // Generate an OxyPlot chart
            // Export the OxyPlot chart to an image file
            string sectionPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chart.png");
            using (var stream = System.IO.File.Create(sectionPath))
            {
                var exporter = new PngExporter { Width = 600, Height = 600};
                exporter.Export(section.Plot(), stream);
            }

            // Add the OxyPlot chart image to the PDF
            XImage chartImage = XImage.FromFile(sectionPath);
            gfx.DrawImage(chartImage, 40, row, 200, 200);
            

            string diagramPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chart.png");
            using (var stream = System.IO.File.Create(diagramPath))
            {
                var exporter = new PngExporter { Width = 600, Height = 600 };
                exporter.Export(results.testPlot, stream);
            }

            // Add the OxyPlot chart image to the PDF
            XImage diagramImage = XImage.FromFile(diagramPath);
            gfx.DrawImage(diagramImage, page.Width - 240, row, 200, 200);
            row += 240;

            List<string> Names = new List<string>() {"A", "B+", "C+", "D+", "E", "D-", "C-", "B-" };
            double dx = 175;
            double dy = 175;
            string tmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chart.png");
            for (int i = 0; i < results.plotResults.Keys.Count(); i++) 
            {
                List<PlotModel> tmpPlots = new List<PlotModel>() { results.plotResults[Names[i]].sectionPlot,
                                                                   results.plotResults[Names[i]].strainPlot,
                                                                   results.plotResults[Names[i]].forcePlot};
                if (row + dy > page.Height-40)
                {
                    page = pdf.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    row = 40;
                }
                for (int col = 0; col < 3; col++)
                {
                    gfx.DrawString($"Strain and forces for point {Names[i]}", font, XBrushes.Black, new XRect(40, row-12, page.Width - 80, page.Height - 80), XStringFormats.TopLeft);
                    using (var stream = System.IO.File.Create(tmpPath))
                    {
                        var exporter = new PngExporter { Width = 600, Height = 600 };
                        exporter.Export(tmpPlots[col], stream);
                    }

                    // Add the OxyPlot chart image to the PDF
                    XImage DImage = XImage.FromFile(tmpPath);
                    gfx.DrawImage(DImage, 40 + dx*col, row, 150, 150);
                }
                row += dy;

            }

                      



            // Save the PDF
            string pdfPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportedDocument.pdf");
            pdf.Save(pdfPath);

            Console.WriteLine($"PDF exported successfully to: {pdfPath}");

        }
        

    }
}