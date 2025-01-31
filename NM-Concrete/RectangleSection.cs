using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;


using Materials;
using static ConcreteSection.RectangleSection;
using OxyPlot.Series;
using System.Windows.Documents;
using OxyPlot.Annotations;
using System.Windows.Annotations;
using OxyPlot.Axes;
using System.Windows;

namespace ConcreteSection
{
    public class RectangleSection
    {
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double Centroid { get; private set; }
        
        public double Area { get; private set; }
        public double Iy { get; private set; }
        public Materials.ConcreteMaterial Concrete { get; private set; }
        public List<ReinforcementLayer> reinforcementLayers;
        public bool Prestressed { get; set; }
        public double StrainTop { get; private set; }
        public double StrainBottom { get; private set; }


        public RectangleSection(double width, double height, Materials.ConcreteMaterial material)
        {
            Width = width;
            Height = height;
            Concrete = material;
            reinforcementLayers = new List<ReinforcementLayer>();
            Prestressed = false;

            calcArea();
            calcIy();
            calcCentroid();
        }

        public void calcArea()
        {
            Area = Width * Height;
        }
        public double grossArea()
        {
            return this.Area - this.TotalReinforcementArea();
        }
        public double effectiveArea()
        {
            double effectiveArea = this.Area;
            foreach (var layer in reinforcementLayers)
            {
                effectiveArea += (layer.Alpha - 1) * layer.Area;
            }
            return effectiveArea;
        }

        public void calcIy()
        {
            Iy =  this.Width * Math.Pow(this.Height, 3) / 12;
        }

        public double effectiveIy()
        {
            double IyRebars = 0;
            double effectiveC = this.effectiveCentroid();
            foreach (var layer in this.reinforcementLayers)
            {
                IyRebars += (layer.Alpha-1) * layer.Area * Math.Pow(Math.Abs(layer.DistanceFromBottom - effectiveC), 2);
            }
            return Iy + this.Area * Math.Abs(this.Centroid - effectiveC) + IyRebars;
        }
        public void calcCentroid()
        {
            Centroid = 0.5 * Height;
        }

        public double effectiveCentroid()
        {
            
            double effectiveArea = this.effectiveArea();
            double staticMoment = this.Area * this.Centroid;
            foreach (var layer in this.reinforcementLayers)
            {
                staticMoment += (layer.Alpha-1) * layer.Area * layer.DistanceFromBottom;
            }
            return staticMoment / effectiveArea;
        }

        public void AddReinforcementLayer(Materials.RebarMaterial material, double diameter, int number, double distanceFromBottom, double alpha, double initialStrain = 0)
        {
            if (distanceFromBottom < 0 || distanceFromBottom > Height)
            {
                MessageBox.Show("Reinforcement position must be within the section height.");
            }
            else
            {
                double distanceFromTop = this.Height - distanceFromBottom;
                double area = number * 0.25 * Math.PI * Math.Pow(diameter, 2);

                if (initialStrain != 0)
                {
                    this.Prestressed = true;
                }

                reinforcementLayers.Add(new ReinforcementLayer
                {
                    Material = material,
                    Diameter = diameter,
                    Number = number,
                    DistanceFromTop = distanceFromTop,
                    DistanceFromBottom = distanceFromBottom,
                    Area = area,
                    Alpha = alpha,
                    InitialStrain = initialStrain
                });
                reinforcementLayers = reinforcementLayers.OrderBy(x => x.DistanceFromBottom).ToList();
            }
            
        }
        public void AddReinforcementLayerFromTop(Materials.RebarMaterial material, double diameter, int number, double distanceFromTop, double alpha, double initialStrain = 0)
        {
            if (distanceFromTop < 0 || distanceFromTop > Height)
            {
                throw new ArgumentOutOfRangeException("Reinforcement position must be within the section height.");
            }
            double distanceFromBottom = this.Height - distanceFromTop;
            double area = number * 0.25 * Math.PI * Math.Pow(diameter, 2);

            if (initialStrain != 0)
            {
                this.Prestressed = true;
            }

            reinforcementLayers.Add(new ReinforcementLayer
            {
                Material = material,
                Diameter = diameter,
                Number = number,
                DistanceFromTop = distanceFromTop,
                DistanceFromBottom = distanceFromBottom,
                Area = area,
                Alpha = alpha,
                InitialStrain = initialStrain
            });
            reinforcementLayers = reinforcementLayers.OrderBy(x => x.DistanceFromBottom).ToList();
        }

        public double ReinforcementCentroid()
        {
            if (!reinforcementLayers.Any())
            {
                throw new InvalidOperationException("No reinforcement layers added.");
            }

            double totalArea = TotalReinforcementArea();
            return reinforcementLayers.Sum(layer => layer.DistanceFromBottom * layer.Area) / totalArea;
        }

        public List<ReinforcementLayer> ReinforcementTop()
        {
            return reinforcementLayers.Where(layer => layer.DistanceFromBottom >= Centroid).ToList();
        }

        public List<ReinforcementLayer> ReinforcementBottom()
        {
            return reinforcementLayers.Where(layer => layer.DistanceFromBottom < Centroid).ToList();
        }

        public double AsTop()
        {
            return ReinforcementTop().Sum(layer => layer.Area);
        }

        public double AsBottom()
        {
            return ReinforcementBottom().Sum(layer => layer.Area);
        }

        public double OmegaTop()
        {
            var topLayers = ReinforcementTop();
            if (!topLayers.Any()) return 0.0;

            double Ftop = topLayers.Sum(layer => layer.Area * layer.Material.Fyd());
            return Ftop / (EffectiveHeightTop() * Width * Concrete.Fcd());
        }

        public double OmegaBottom()
        {
            var bottomLayers = ReinforcementBottom();
            if (!bottomLayers.Any()) return 0.0;

            double Fbottom = bottomLayers.Sum(layer => layer.Area * layer.Material.Fyd());
            return Fbottom / (EffectiveHeightBottom() * Width * Concrete.Fcd());
        }

        public double EffectiveHeightTop()
        {
            var topLayers = ReinforcementTop();
            return topLayers.Sum(layer => layer.DistanceFromBottom) / topLayers.Count;
        }

        public double EffectiveHeightBottom()
        {
            var bottomLayers = ReinforcementBottom();
            return bottomLayers.Sum(layer => Height - layer.DistanceFromBottom) / bottomLayers.Count;
        }

        public double TotalReinforcementArea()
        {
            return reinforcementLayers.Sum(layer => layer.Area);
        }

        public class ReinforcementLayer
        {
            public double Diameter { get; set; }
            public int Number { get; set; }
            public double DistanceFromBottom { get; set; }
            public double DistanceFromTop { get; set; }
            public double Area { get; set; }
            public Materials.RebarMaterial Material { get; set; }
            public double InitialStrain { get; set; }
            public double Alpha { get; set; }
            public double Force()
            { 
                return InitialStrain * Material.Es * Area;
            }

            public override string ToString()
            {
                return $"Reinforcement: {Material.Name} {Number} Ø{Diameter} , bar area {(Area/Number).ToString("F1")}mm"+"\u00B2"+$" total area {Area.ToString("F1")}";
            }
        }

        public void StrainCalculation()
        {
            double strainTop = 0; double strainBottom = 0;
            double y = this.effectiveCentroid();
            double yTop = this.Height - y;

            double area = this.effectiveArea();
            double Iyeff = this.effectiveIy();
            foreach (var layer in reinforcementLayers)
            {
                if (layer.InitialStrain != 0)
                {
                    double force = layer.Force();
                    double strainN = force / (this.Concrete.Ecm * area);

                    double Moment = force * (layer.DistanceFromBottom - y);
                    strainTop += strainN + Moment/Iyeff * yTop / this.Concrete.Ecm;
                    strainBottom += strainN - Moment / Iyeff * yTop / this.Concrete.Ecm;
                }
            }
            StrainTop = strainTop;
            StrainBottom = strainBottom;
        }

        public OxyPlot.PlotModel Plot()
        {
            // Create a new plot model
            var plotModel = new PlotModel {};

            double L = new List<double>() {this.Width/2, this.Height/2 }.Max();

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,  // Set X axis at the bottom
                Minimum = -L - 50,                     // Set minimum value of Y axis
                Maximum = L + 50,                    // Set maximum value of Y axis
                Title = "Width",
                IsAxisVisible = false
            };
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,   // Set Y axis on the left
                Minimum = -L -50,                     // Set minimum value of Y axis
                Maximum = L + 50,                    // Set maximum value of Y axis
                Title = "Height",                 // Title for Y axis
                IsAxisVisible = false
            };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            // Define a line series
            var lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColors.Red,
                Title = "Point Series"
            };
            List<double> x = new List<double>() {-this.Width/2, this.Width / 2 , this.Width / 2 , -this.Width / 2 };
            List<double> y = new List<double>() { -this.Height / 2, -this.Height / 2, this.Height / 2, this.Height / 2 };

            // Add the section to the line series
            for (int i = 0; i<4;i++)
            {
                lineSeries.Points.Add(new DataPoint(x[i], y[i]));
            }
            lineSeries.Points.Add(new DataPoint(x[0], y[0]));
            plotModel.Series.Add(lineSeries);

            // Add the rebars to the line series
            foreach (var layer in this.reinforcementLayers)
            {
                double yRebar = layer.DistanceFromBottom - this.Centroid;
                double dx = (this.Width - 2*25)/(layer.Number-1);

                if (layer.Number != 1)
                {
                    for (int i = 0; i < layer.Number; i++)
                    {
                        double xRebar = -this.Width / 2 + 25 + dx * i;
                        
                        if (layer.InitialStrain != 0)
                        { 
                            var annotation = new PointAnnotation { X = xRebar, Y = yRebar, Size = layer.Diameter/5, Shape = MarkerType.Plus, StrokeThickness = 1, Stroke = OxyColors.Black };
                            plotModel.Annotations.Add(annotation);
                        }
                        else
                        {
                            var annotation = new EllipseAnnotation
                            {
                                X = xRebar,
                                Y = yRebar,
                                Width = layer.Diameter,
                                Height = layer.Diameter,
                                StrokeThickness = 0, // No border
                                Fill = OxyColors.Black // Set the fill color to black
                            };
                            plotModel.Annotations.Add(annotation);
                        } 
                    }
                }
                else
                {
                    double xRebar = 0;
                    if (layer.InitialStrain != 0)
                    {
                        var annotation = new PointAnnotation { X = xRebar, Y = yRebar, Size = layer.Diameter ,Shape = MarkerType.Plus, Fill = OxyColors.Black };
                        plotModel.Annotations.Add(annotation);
                    }
                    else
                    {
                        var annotation = new EllipseAnnotation
                        {
                            X = xRebar,
                            Y = yRebar,
                            Width = layer.Diameter,
                            Height = layer.Diameter,
                            StrokeThickness = 0, // No border
                            Fill = OxyColors.Black // Set the fill color to black
                        };
                        plotModel.Annotations.Add(annotation);
                    }
                }
            }
            
            return plotModel;
        }


        
        public override string ToString()
        {
            return $"Rectangular section -> width: {this.Width}, height: {this.Height}, material: {this.Concrete.Name}";
        }

    }
}
