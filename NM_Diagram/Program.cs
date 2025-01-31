using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;
using OxyPlot.Axes;


using ConcreteSection;
using Materials;
using NM_Diagram;



Materials.Material material = new Material();

Materials.ConcreteMaterial concreteMaterial = new ConcreteMaterial(material, "C30/37", 1.45);
Materials.RebarMaterial rebarMaterial = new RebarMaterial(material, "Y550", 1.2);


ConcreteSection.RectangleSection section = new ConcreteSection.RectangleSection(280, 420, concreteMaterial);
section.AddReinforcementLayer(rebarMaterial, 20, 4, 40);
section.AddReinforcementLayer(rebarMaterial, 20, 2, 420 - 40);


Calculation Results = new Calculation(section);

Console.WriteLine("Reinforcement layer:");
foreach (var layer in section.reinforcementLayers)
{
    Console.WriteLine($"{layer.Number} Ø{layer.Diameter}, {layer.Area}mm2, Cbottom: {layer.DistanceFromBottom}");
}

var plotModel = Results.DiagramPlot;
SavePlotAsPng(plotModel, "NMDiagram.pdf", 600, 600);



Console.WriteLine(section.ToString());
Console.WriteLine(Results.ToString());
Console.WriteLine("Pause");
Console.ReadLine();

static void SavePlotAsPng(PlotModel plotModel, string filePath, int width, int height)
{
    using (var stream = File.Create(filePath))
    {
        var pdfExporter = new PdfExporter { Width = width, Height = height };
        pdfExporter.Export(plotModel, stream);
    }
}
