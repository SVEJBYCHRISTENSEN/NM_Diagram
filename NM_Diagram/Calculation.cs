using ConcreteSection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;



namespace NM_Diagram
{
    public class Calculation
    {
        public RectangleSection ConcreteSection { get; set; }
        public List<double> NRd { get; private set; }
        public List<double> MRd { get; private set; }
        public List<resultPoint> DiagramPoints { get; private set; }
        public PlotModel DiagramPlot { get; private set; }
        public resultPoint A { get; private set; }
        public resultPoint B { get; private set; }
        public resultPoint BNeg{ get; private set; }
        public resultPoint C { get; private set; }
        public resultPoint CNeg { get; private set; }
        public resultPoint D { get; private set; }
        public resultPoint DNeg { get; private set; }
        public resultPoint E { get; private set; }


        public Calculation(RectangleSection section) 
        { 
            ConcreteSection = section;
            Calc();
        }

        public class resultPoint
        {
            public string Point { get; private set; }
            public double NR { get; private set; }
            public double MR { get; private set; }
            public resultPoint(string point, (double N, double M) result)
            {
                Point = point;
                NR = result.N;
                MR = result.M;
            }
        }
        public void Calc()
        {
            List<double> N = new List<double>();
            List<double> M = new List<double>();
            List<resultPoint> diagramPoints = new List<resultPoint>();
            // Point A
            A = new resultPoint("A", calcA());
            N.Add(A.NR);
            M.Add(A.MR);

            

            // Point B
            B = new resultPoint("B+", calcB("Top"));
            BNeg = new resultPoint("B-", calcB("Bottom"));
            N.Add(B.NR); M.Add(B.MR); 
            N.Add(BNeg.NR); M.Add(BNeg.MR);

            // Point C
            C = new resultPoint("C+", calcC("Top"));
            CNeg = new resultPoint("C-", calcC("Bottom"));
            N.Add(C.NR); M.Add(C.MR);
            N.Add(CNeg.NR); M.Add(CNeg.MR);

            // Point D
            D = new resultPoint("D", calcD("Top"));
            DNeg = new resultPoint("D-", calcD("Bottom"));
            N.Add(D.NR); M.Add(D.MR);
            N.Add(DNeg.NR); M.Add(DNeg.MR);


            // Point E
            E = new resultPoint("E", calcE());
            N.Add(E.NR);
            M.Add(E.MR);

            diagramPoints.Add(A); diagramPoints.Add(B); diagramPoints.Add(C); diagramPoints.Add(D); diagramPoints.Add(E); diagramPoints.Add(DNeg); diagramPoints.Add(CNeg); diagramPoints.Add(BNeg);
            DiagramPoints = diagramPoints;
            DiagramPlot = CreateClosedPlot(diagramPoints);

            NRd = N;
            MRd = M;
        }


        private (double N, double M) calcA()
        {
            double NRdA = 0.0, MRdA = 0.0;
            foreach (var reinforcementLayer in this.ConcreteSection.reinforcementLayers)
            {
                var material = reinforcementLayer.Material;
                NRdA += -reinforcementLayer.Area * material.Fyd();
                MRdA += (this.ConcreteSection.Centroid() - reinforcementLayer.DistanceFromBottom) * reinforcementLayer.Area * material.Fyd();
            }

            return (NRdA, MRdA);
        }

        private (double N, double M) calcB(string direction)
        {
            double MRdB = 0;
            if (direction == "Top")
            { MRdB = ConcreteSection.OmegaBottom() * (1.0 - 0.5 * ConcreteSection.OmegaBottom()) * ConcreteSection.Concrete.Eta * ConcreteSection.Width * Math.Pow(ConcreteSection.EffectiveHeightBottom(), 2) * ConcreteSection.Concrete.Fcd(); }
            else if (direction == "Bottom")
            { MRdB = -ConcreteSection.OmegaTop() * (1.0 - 0.5 * ConcreteSection.OmegaTop()) * ConcreteSection.Concrete.Eta * ConcreteSection.Width * Math.Pow(ConcreteSection.EffectiveHeightTop(), 2) * ConcreteSection.Concrete.Fcd(); }
            else { new Exception("Error direction needs to be set to top of bottom"); }

            return (0, MRdB);
        }

        private (double N, double M) calcC(string direction)
        {
            double NRdC = 0;
            double MRdC = 0;

            RectangleSection section = this.ConcreteSection;
            Materials.ConcreteMaterial concrete = this.ConcreteSection.Concrete;

            // Calc variables:
            double xCompression = 0;
            double xTension = 0;
            double strainTension = 0;
            if (direction == "Top")
            {
                xCompression = concrete.ecu3 / (concrete.ecu3 + section.reinforcementLayers[0].Material.eyd()) * section.EffectiveHeightBottom();
                xTension = section.Height - xCompression - section.reinforcementLayers[0].DistanceFromBottom; 
                strainTension = section.reinforcementLayers[0].Material.eyd();
                NRdC += concrete.LambdaC * xCompression * section.Width * concrete.Fcd();
                MRdC += concrete.LambdaC * xCompression * section.Width * concrete.Fcd() * (section.Centroid() - 0.5 * concrete.LambdaC * xCompression);
                foreach (var layer in section.reinforcementLayers)
                {
                    double stress = 0;
                    double strain = 0;
                    bool compression;
                    double M = 0;
                    if (section.Height - xCompression > layer.DistanceFromBottom)
                    {
                        // Tenssion
                        strain = Math.Abs(strainTension / xTension * (section.Height - xCompression- layer.DistanceFromBottom));
                        stress = -layer.Material.Es * new List<double>() { strain, layer.Material.eyd() }.Min();
                        compression = false;
                    }
                    else
                    {
                        // Compression
                        strain = Math.Abs(concrete.ecu3 / xCompression * (layer.DistanceFromBottom - section.Height - xCompression));
                        stress = layer.Material.Es * new List<double>() { strain, layer.Material.eyd() }.Min();
                        compression = true;
                    }

                    if (layer.DistanceFromBottom <= section.Centroid())
                    {
                        double F = Math.Abs(stress * layer.Area);
                        M = F * (section.Centroid() - layer.DistanceFromBottom);
                        if (compression)
                        { M = -M; }
                    }
                    else if (layer.DistanceFromBottom > section.Centroid())
                    {
                        double F = Math.Abs(stress * layer.Area);
                        M = F * (layer.DistanceFromBottom - section.Centroid());
                        if (!compression)
                        { M = -M; }
                    }
                    NRdC += layer.Area * stress;
                    MRdC += M;
                }
            }
            else if (direction == "Bottom")
            {
                xCompression = concrete.ecu3 / (concrete.ecu3 + section.reinforcementLayers.Last().Material.eyd()) * section.EffectiveHeightTop();
                xTension = section.reinforcementLayers.Last().DistanceFromBottom - xCompression;
                strainTension = section.reinforcementLayers.Last().Material.eyd();
                NRdC += concrete.LambdaC * xCompression * section.Width * concrete.Fcd();
                MRdC -= concrete.LambdaC * xCompression * section.Width * concrete.Fcd() * (section.Centroid() - 0.5*concrete.LambdaC*xCompression);

                foreach (var layer in section.reinforcementLayers)
                {
                    double stress = 0;
                    double strain = 0;
                    bool compression;
                    double M = 0;
                    if (xCompression < layer.DistanceFromBottom)
                    {
                        // Tension
                        strain = Math.Abs(strainTension / xTension * layer.DistanceFromBottom);
                        stress = -layer.Material.Es * new List<double>() { strain, layer.Material.eyd() }.Min();
                        compression = false;
                    }
                    else
                    {
                        // Compression
                        strain = Math.Abs(concrete.ecu3 / xCompression * (xCompression - layer.DistanceFromBottom));
                        stress = layer.Material.Es * new List<double>() { strain, layer.Material.eyd() }.Min();
                        compression = true;
                    }

                    if (layer.DistanceFromBottom <= section.Centroid())
                    {
                        double F = Math.Abs(stress * layer.Area);
                        M = F * (section.Centroid() - layer.DistanceFromBottom);
                        if (compression)
                        { M = -M; }
                    }
                    else if (layer.DistanceFromBottom > section.Centroid())
                    {
                        double F = Math.Abs(stress * layer.Area);
                        M = F * (layer.DistanceFromBottom - section.Centroid());
                        if (!compression)
                        { M = -M; }
                    }
                    NRdC += layer.Area * stress;
                    MRdC += M;
                }
            }
            else { new Exception("Error direction needs to be set to top of bottom"); }

            return (NRdC, MRdC);
        }

        private (double N, double M) calcD(string direction)
        {
            double NRdD = 0;
            double MRdD = 0;

            // Variables
            RectangleSection section = this.ConcreteSection;

            if (direction == "Top")
            {
                double d = section.EffectiveHeightBottom();
                NRdD += section.Concrete.LambdaC * section.Concrete.Eta * section.Width * d * section.Concrete.Fcd();
                MRdD += NRdD * (section.Centroid() - 0.5 * section.Concrete.LambdaC * d);
                foreach (var layer in section.reinforcementLayers)
                {
                    double c = section.Height - layer.DistanceFromBottom;
                    List<double> strain = new List<double>() {layer.Material.eyd(), section.Concrete.ecu3 * (d - c)/d};
                    NRdD += strain.Min() * layer.Material.Es * layer.Area;
                    if (layer.DistanceFromBottom < section.Centroid())
                    {
                        MRdD -= strain.Min() * layer.Material.Es * layer.Area * (section.Centroid() - layer.DistanceFromBottom);                        
                    }
                    else
                    { 
                        MRdD += strain.Min() * layer.Material.Es * layer.Area * (layer.DistanceFromBottom - section.Centroid());
                    }

                }
            }
            else if (direction == "Bottom")
            {
                double d = section.EffectiveHeightTop();
                NRdD += section.Concrete.LambdaC * section.Concrete.Eta * section.Width * d * section.Concrete.Fcd();
                MRdD -= NRdD * (section.Centroid() - 0.5 * section.Concrete.LambdaC * d);

                foreach (var layer in section.reinforcementLayers)
                {
                    double c = layer.DistanceFromBottom;
                    List<double> strain = new List<double>() { layer.Material.eyd(), section.Concrete.ecu3 * (d - c) / d };
                    NRdD += strain.Min() * layer.Material.Es * layer.Area;
                    if (layer.DistanceFromBottom < section.Centroid())
                    {
                        MRdD -= strain.Min() * layer.Material.Es * layer.Area * (section.Centroid() - layer.DistanceFromBottom);
                    }
                    else
                    {
                        MRdD += strain.Min() * layer.Material.Es * layer.Area * (layer.DistanceFromBottom - section.Centroid());
                    }

                }
            }
            return (NRdD, MRdD);
        }

        private (double N, double M) calcE()
        {
            double NRdE = this.ConcreteSection.Area() * this.ConcreteSection.Concrete.Fcd(); 
            double MRdE = 0.0;
            double strain = this.ConcreteSection.Concrete.ec3;
            foreach (var reinforcementLayer in this.ConcreteSection.reinforcementLayers)
            {
                var material = reinforcementLayer.Material;
                double F = reinforcementLayer.Area * material.Es * strain;
                NRdE += F;
                
                MRdE += (reinforcementLayer.DistanceFromBottom - this.ConcreteSection.Centroid()) * F; 
                
            }

            return (NRdE, MRdE);
        }

        public static PlotModel CreateClosedPlot(List<resultPoint> points)
        {
            // Create a new plot model
            var plotModel = new PlotModel { Title = "NM-Diagram" };

            // Define a line series
            var lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColors.Red,
                Title = "Point Series"
            };

            // Add the points to the line series
            foreach (var point in points)
            {
                lineSeries.Points.Add(new DataPoint(point.NR/1000, point.MR/1000000));
            }

            // Close the series by connecting the last point to the first
            if (points.Count > 0)
            {
                var firstPoint = points.First();
                lineSeries.Points.Add(new DataPoint(firstPoint.NR/1000, firstPoint.MR/1000000));
            }

            // Add the line series to the plot model
            plotModel.Series.Add(lineSeries);

            // Annotate the points with their names
            foreach (var point in points)
            {
                string N = (point.NR / 1000).ToString("F1");
                string M = (point.MR / 1000000).ToString("F1");
                var annotation = new TextAnnotation
                {
                    Text = point.Point,
                    FontSize = 26,
                    TextPosition = new DataPoint(point.NR/1000, point.MR/1000000),
                    Stroke = OxyColors.Transparent,
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    TextHorizontalAlignment = HorizontalAlignment.Center
                };
                plotModel.Annotations.Add(annotation);
                annotation = new TextAnnotation
                {
                    Text = $"({N}kN, {M}kNm)",
                    FontSize = 16,
                    TextPosition = new DataPoint(point.NR / 1000, point.MR / 1000000),
                    Stroke = OxyColors.Transparent,
                    TextVerticalAlignment = VerticalAlignment.Top,
                    TextHorizontalAlignment = HorizontalAlignment.Center
                };
                plotModel.Annotations.Add(annotation);
                
            }


            return plotModel;
        }
        public override string ToString()
        {
            return $"Section NM-Diagram: A ({NRd[0]/1000:0.00}; {MRd[0] / 1000000:0.00}), B+ ({NRd[1] / 1000:0.00}; {MRd[1] / 1000000:0.00}), B- ({NRd[2] / 1000:0.00}; {MRd[2] / 1000000:0.00}), C+ ({NRd[3] / 1000:0.00}; {MRd[3] / 1000000:0.00}), C- ({NRd[4] / 1000:0.00}; {MRd[4] / 1000000:0.00}), D+ ({NRd[5] / 1000:0.00}; {MRd[5] / 1000000:0.00}), D- ({NRd[6] / 1000:0.00}; {MRd[6] / 1000000:0.00}), E ({NRd[7] / 1000:0.00}; {MRd[7] / 1000000:0.00})";
        }

    }
}
