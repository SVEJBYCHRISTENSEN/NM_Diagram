using ConcreteSection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;
using OxyPlot.SkiaSharp;

using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;
using PdfSharp.Charting;
using OxyPlot.Axes;
using static ConcreteSection.RectangleSection;
using System.DirectoryServices.ActiveDirectory;
using Materials;
using HarfBuzzSharp;
using static SkiaSharp.HarfBuzz.SKShaper;
using System.Windows.Annotations;
using System.Windows.Controls;
using System.IO;



namespace NM_Diagram
{
    public class Calculation
    {
        public RectangleSection ConcreteSection { get; set; }
        public List<double> NRd { get; private set; }
        public List<double> MRd { get; private set; }

        public PlotModel testPlot { get; private set; }
        
        public Dictionary<string, SectionResults> plotResults {get; private set;}

        public Calculation(RectangleSection section) 
        { 
            ConcreteSection = section;
            calculation(); // Test calculation 
            // Calc();
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

        public (List<double> N, List<double> M) calculation()
        {
            List<double> N = new List<double>();
            List<double> M = new List<double>();

            List<double> Ntest = new List<double>();
            List<double> Mtest = new List<double>();

            Dictionary<string, SectionResults> sectionRes = new Dictionary<string, SectionResults>();
            // Create a new plot model
            var plotModel = new PlotModel { Title = "NM-Diagram" };
            PlotModel teststrain = null;


            double xB = findX("+", 100000);
            double xBneg = findX("-", 100000);
            double dStrain = this.ConcreteSection.Concrete.ecu3 / xB;

            double sectionStrainCpos = this.ConcreteSection.StrainBottom + (this.ConcreteSection.StrainTop - this.ConcreteSection.StrainBottom) * this.ConcreteSection.reinforcementLayers.First().DistanceFromBottom;
            double sectionStrainCneg = this.ConcreteSection.StrainTop + (this.ConcreteSection.StrainBottom - this.ConcreteSection.StrainTop) * this.ConcreteSection.reinforcementLayers.Last().DistanceFromTop;


            strainPoint A = new strainPoint { Top = -this.ConcreteSection.Concrete.ecu3, Bottom = -this.ConcreteSection.Concrete.ecu3 };
            strainPoint B = new strainPoint { Top = this.ConcreteSection.Concrete.ecu3, Bottom = this.ConcreteSection.Concrete.ecu3 - this.ConcreteSection.Concrete.ecu3 / xB * this.ConcreteSection.Height };
            strainPoint C = new strainPoint { Top = this.ConcreteSection.Concrete.ecu3, Bottom = this.ConcreteSection.Concrete.ecu3 + (-this.ConcreteSection.reinforcementLayers.First().Material.eyd() - this.ConcreteSection.Concrete.ecu3) / this.ConcreteSection.reinforcementLayers.First().DistanceFromTop * this.ConcreteSection.Height - this.ConcreteSection.reinforcementLayers.First().InitialStrain + this.ConcreteSection.StrainBottom};
            strainPoint D = new strainPoint { Top = this.ConcreteSection.Concrete.ecu3, Bottom = 0 };
            strainPoint E = new strainPoint { Top = this.ConcreteSection.Concrete.ec3, Bottom = this.ConcreteSection.Concrete.ec3 };
            strainPoint Dneg = new strainPoint { Top = 0, Bottom = this.ConcreteSection.Concrete.ecu3 };
            strainPoint Cneg = new strainPoint { Top = this.ConcreteSection.Concrete.ecu3 + (-this.ConcreteSection.reinforcementLayers.Last().Material.eyd() - this.ConcreteSection.Concrete.ecu3) / this.ConcreteSection.reinforcementLayers.Last().DistanceFromBottom * this.ConcreteSection.Height - this.ConcreteSection.reinforcementLayers.Last().InitialStrain + this.ConcreteSection.StrainTop, Bottom = this.ConcreteSection.Concrete.ecu3 };
            strainPoint Bneg = new strainPoint { Top = this.ConcreteSection.Concrete.ecu3 - this.ConcreteSection.Concrete.ecu3 / xBneg * this.ConcreteSection.Height, Bottom = this.ConcreteSection.Concrete.ecu3 };

            List<string> namesPoints = new List<string>() {"A", "B+", "C+", "D+", "E", "D-", "C-", "B-" };

            List<strainPoint> strainList = new List<strainPoint>() { A, B, C, D};
            List<strainPoint> strainNegList = new List<strainPoint>() { Dneg, Cneg, Bneg, A };
            int itter = 100;
            for (int i = 0; i < strainList.Count-1; i++)
            {
                for (int j = 0; j < itter+1; j++)
                {
                    strainPoint tmpStrain = new strainPoint { Top = strainList[i].Top + (strainList[i+1].Top - strainList[i].Top) / itter * j, Bottom = strainList[i].Bottom + (strainList[i+1].Bottom - strainList[i].Bottom) / itter * j };
                    var res = calculationFromStrain(tmpStrain, "+");
                    N.Add(res.N); M.Add(res.M);

                    if (j == 0)
                    {
                        var annotation = new TextAnnotation
                        {
                            Text = $"{namesPoints[i]} ({(res.N / 1000).ToString("F1")}kN, {(res.M / 1000000).ToString("F1")}kNm)",
                            FontSize = 11,
                            TextPosition = new DataPoint(res.N / 1000, res.M / 1000000),
                            Stroke = OxyColors.Transparent,
                            TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center
                        };
                        plotModel.Annotations.Add(annotation);
                        sectionRes.Add(namesPoints[i], strainSectionPlot(namesPoints[i], strainList[i], res.restultList));
                    }
                    else if (j == itter && i == strainList.Count() - 2)
                    {
                        var annotation = new TextAnnotation
                        {
                            Text = $"{namesPoints[i+1]} ({(res.N / 1000).ToString("F1")}kN, {(res.M / 1000000).ToString("F1")}kNm)",
                            FontSize = 11,
                            TextPosition = new DataPoint(res.N / 1000, res.M / 1000000),
                            Stroke = OxyColors.Transparent,
                            TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center
                        };
                        plotModel.Annotations.Add(annotation);
                        sectionRes.Add(namesPoints[i+1], strainSectionPlot(namesPoints[i+1], strainList[i+1], res.restultList));

                    }
                
                }
                
            }
            var resE = calculationFromStrain(E, "E");
            N.Add(resE.N); M.Add(resE.M);
            var PointEPlot = new TextAnnotation
            {
                Text = $"{namesPoints[strainList.Count]} ({(resE.N / 1000).ToString("F1")}kN, {(resE.M / 1000000).ToString("F1")}kNm)",
                FontSize = 11,
                TextPosition = new DataPoint(resE.N / 1000, resE.M / 1000000),
                Stroke = OxyColors.Transparent,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center
            };
            plotModel.Annotations.Add(PointEPlot);
            sectionRes.Add("E", strainSectionPlot("E", E, resE.restultList));

            for (int i = 0; i < strainNegList.Count - 1; i++)
            {
                
                for (int j = 0; j < itter; j++)
                {
                    strainPoint tmpStrain = new strainPoint { Top = strainNegList[i].Top + (strainNegList[i + 1].Top - strainNegList[i].Top) / itter * j, Bottom = strainNegList[i].Bottom + (strainNegList[i + 1].Bottom - strainNegList[i].Bottom) / itter * j };
                    var res = calculationFromStrain(tmpStrain, "-");
                    N.Add(res.N); M.Add(res.M);

                    if (j == 0)
                    {
                        var annotation = new TextAnnotation
                        {
                            Text = $"{namesPoints[strainList.Count +1+ i]} ({(res.N / 1000).ToString("F1")}kN, {(res.M / 1000000).ToString("F1")}kNm)",
                            FontSize = 11,
                            TextPosition = new DataPoint(res.N / 1000, res.M / 1000000),
                            Stroke = OxyColors.Transparent,
                            TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center
                        };
                        plotModel.Annotations.Add(annotation);
                        sectionRes.Add(namesPoints[strainList.Count + 1 + i], strainSectionPlot(namesPoints[strainList.Count + 1+ i], strainNegList[i], res.restultList));
                    }
                }

            }

            var NAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,  // Set X axis at the bottom
                Minimum = 2.7/1000 * N.Min(),                     // Set minimum value of Y axis
                Maximum = 1.4/1000 * N.Max(),//width/2 + strainRefLine + forceRefLine + forceRefSize,                    // Set maximum value of Y axis
                Title = "Normal capacity [kN]",
                IsAxisVisible = true
            };
            var MAxis = new LinearAxis
            {
                Position = AxisPosition.Left,  // Set X axis at the bottom
                Minimum = 1.2/1000000 * M.Min(),                     // Set minimum value of Y axis
                Maximum = 1.2/1000000 * M.Max(),//width/2 + strainRefLine + forceRefLine + forceRefSize,                    // Set maximum value of Y axis
                Title = "Bending capacity [kNm]",
                IsAxisVisible = true
            };
            plotModel.Axes.Add(NAxis); plotModel.Axes.Add(MAxis);


            // Define a line series
            var lineSeries = new LineSeries
            {
                MarkerType = MarkerType.None,
                MarkerSize = 3,
                MarkerFill = OxyColors.Red,
                Title = "NM-Diagram"
            };

            // Add the points to the line series
            for (int i = 0; i < N.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(N[i] / 1000, M[i] / 1000000));
            }

            plotModel.Series.Add(lineSeries);
            //plotModel.Series.Add(PointSeries);
            
            testPlot = plotModel;
            plotResults = sectionRes;
            //testPlot = teststrain;

            return (N, M);
        }

        private (double N, double M, List<NMPointResult> restultList) calculationFromStrain(strainPoint strain, string bendingDirection)
        {
            double N = 0;
            double M = 0;
            List<NMPointResult> results = new List<NMPointResult>();



            // Constants
            RectangleSection section = this.ConcreteSection;
            double center = section.effectiveCentroid();
            double height = section.Height;
            double width = section.Width;
            ConcreteMaterial concrete = this.ConcreteSection.Concrete;
            double dStrain = (strain.Top - strain.Bottom) / height;
            double dSectionStrain = (section.StrainTop - section.StrainBottom) / height;


            double x = 0;
            if (bendingDirection == "+")
            { x = Math.Min(strain.Top / dStrain, height); }
            if (bendingDirection == "-")
            {x = Math.Min(-strain.Bottom / dStrain, height);}
            if (bendingDirection == "E")
            { 
                x = height;
                N += x * width * concrete.Eta * concrete.Fcd();
                M += N * (center-section.Centroid);
                results.Add(new NMPointResult { Strain = concrete.ec3, Force = N, yCoord = section.Centroid, Compressed = true, Concrete = true });
            }

            if (x > 0 && bendingDirection == "+")
            {
                N += concrete.LambdaC * x * width * concrete.Eta * concrete.Fcd();
                M += N * (height - center - 0.5 * concrete.LambdaC * x);
                results.Add(new NMPointResult { Strain = strain.Top, Force = N, yCoord = (height - 0.5 * concrete.LambdaC * x), Compressed = true, Concrete = true });
            }
            if (x > 0 && bendingDirection == "-")
            {
                N += concrete.LambdaC * x * width * concrete.Eta * concrete.Fcd();
                M -= N * (center - 0.5 * concrete.LambdaC * x);
                results.Add(new NMPointResult { Strain = strain.Bottom, Force = N, yCoord = (0.5 * concrete.LambdaC * x), Compressed = true, Concrete = true });
            }


            foreach (var reinforcementLayer in this.ConcreteSection.reinforcementLayers)
            {
                double layerStrain = strain.Bottom + dStrain * reinforcementLayer.DistanceFromBottom;
                //MessageBox.Show($"Layer: y={reinforcementLayer.DistanceFromBottom.ToString("F1")} strain: {layerStrain}");

                if (reinforcementLayer.InitialStrain != 0)
                {
                    layerStrain += reinforcementLayer.InitialStrain + section.StrainBottom + dSectionStrain * reinforcementLayer.DistanceFromBottom;
                }
                

                double F = (layerStrain / Math.Abs(layerStrain)) * Math.Min(Math.Abs(layerStrain), reinforcementLayer.Material.eyd()) * reinforcementLayer.Material.Es * reinforcementLayer.Area;
                N += F;
                M += F * (reinforcementLayer.DistanceFromBottom - center);

                results.Add(new NMPointResult { Strain = layerStrain, Force = F, yCoord = reinforcementLayer.DistanceFromBottom, Compressed = layerStrain >= 0, Concrete = false});
            }
            //MessageBox.Show($"Point strains bottom ({strain.Bottom}) & top ({strain.Top}) - > Result: x: {x}, N ({N}) & M ({M})");

            return (N, M, results);
        }

        private class strainPoint()
        {
            public double Top {get;set;}
            public double Bottom { get; set; }
        }
        private class NMPointResult()
        {
            public double Strain { get; set; }
            public double Force { get; set; }
            public double yCoord { get; set; }
            public bool Compressed { get; set; }
            public bool Concrete { get; set; }
        }
        public class SectionResults()
        {
            public string Name { get; set; }
            public PlotModel sectionPlot { get; set; }
            public PlotModel strainPlot { get; set; }
            public PlotModel forcePlot { get; set; }

        }
        private SectionResults strainSectionPlot(string pointName, strainPoint strainLimits, List<NMPointResult> resultList)
        {
            RectangleSection section = this.ConcreteSection;
            double height = section.Height;
            double width = section.Width;
            var largestForce = resultList
            .OrderByDescending(p => p.Force) // Order by Force descending
            .FirstOrDefault();
            var smallestForce = resultList
            .OrderByDescending(p => p.Force).Last();

            var largestStrain = resultList
            .OrderByDescending(p => p.Strain) // Order by Force descending
            .FirstOrDefault();
            var smallestStrain = resultList
            .OrderByDescending(p => p.Strain).Last();

            double maxForce = Math.Max(Math.Abs(largestForce.Force), Math.Abs(smallestForce.Force));
            double maxStrain = Math.Max(Math.Max(Math.Abs(largestStrain.Strain), Math.Abs(smallestStrain.Strain)), Math.Max(strainLimits.Top, strainLimits.Bottom));

            // References to resize

            PlotModel sectionPlot = section.Plot(); sectionPlot.Title = "Section plot";
            PlotModel strainPlot = new PlotModel { Title = "Strain plot" };
            PlotModel forcePlot = new PlotModel { Title = "Force plot" };
            
            var xAxisStrain = new LinearAxis
            {
                Position = AxisPosition.Bottom,  // Set X axis at the bottom
                Minimum = 1.1*-1000* maxStrain,                     // Set minimum value of Y axis
                Maximum = 1.1*1000* maxStrain,//width/2 + strainRefLine + forceRefLine + forceRefSize,                    // Set maximum value of Y axis
                Title = "",
                IsAxisVisible = false
            };
            var xAxisForce = new LinearAxis
            {
                Position = AxisPosition.Bottom,  // Set X axis at the bottom
                Minimum = -maxForce * 1.1,                     // Set minimum value of Y axis
                Maximum = maxForce * 1.1,//width/2 + strainRefLine + forceRefLine + forceRefSize,                    // Set maximum value of Y axis
                Title = "",
                IsAxisVisible = false
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,  // Set X axis at the bottom
                Minimum = -50,                     // Set minimum value of Y axis
                Maximum = height + 50,                    // Set maximum value of Y axis
                Title = "",
                IsAxisVisible = false
            };
            var yAxisForce = new LinearAxis
            {
                Position = AxisPosition.Left,  // Set X axis at the bottom
                Minimum = -50,                     // Set minimum value of Y axis
                Maximum = height + 50,                    // Set maximum value of Y axis
                Title = "",
                IsAxisVisible = false
            };
            strainPlot.Axes.Add(xAxisStrain); forcePlot.Axes.Add(xAxisForce);
            strainPlot.Axes.Add(yAxis); forcePlot.Axes.Add(yAxisForce);

            // Define a line series
            var strainSeries = new LineSeries
            {
                MarkerType = MarkerType.None,
                MarkerSize = 3,
                MarkerFill = OxyColors.Black,
                Title = "Strain"
            };

            var heightSeries = new LineSeries
            {
                MarkerType = MarkerType.None,
                MarkerSize = 3,
                MarkerFill = OxyColors.Black,
                Title = "Strain"
            };

            List<List<double>> strainLine = new List<List<double>>() {  new List<double>() {0,0 },
                                                                        new List<double>() {0,height },
                                                                        new List<double>() {strainLimits.Top,height },
                                                                        new List<double>() {strainLimits.Bottom,0 },
                                                                        new List<double>() {0,0}};

            List<TextAnnotation> strainAnnotations = new List<TextAnnotation>();

            double strainMax = Math.Max(Math.Abs(strainLimits.Top), Math.Abs(strainLimits.Bottom))*1000;

            strainPlot.Annotations.Add(new TextAnnotation
            {
                TextPosition = new DataPoint(-0.5*strainLimits.Top * 1000, height),
                Text = "ε = " + (strainLimits.Top / 1000).ToString("#0.00##" + '\u2030', CultureInfo.InvariantCulture),
                FontSize = 11,
                Stroke = OxyColors.Transparent,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle
            });
            strainPlot.Annotations.Add(new TextAnnotation
            {
                TextPosition = new DataPoint(-0.5*strainLimits.Bottom * 1000, 0),
                Text = "ε = " + (strainLimits.Bottom / 1000).ToString("#0.00##" + '\u2030', CultureInfo.InvariantCulture),
                FontSize = 11,
                Stroke = OxyColors.Transparent,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle
            });

            // Add the points to the line series
            foreach (List<double> pointPlot in strainLine)
            {
                strainSeries.Points.Add(new DataPoint(-pointPlot[0]*1000, pointPlot[1]));
            }
            strainPlot.Series.Add(strainSeries);

            foreach (NMPointResult result in resultList)
            {
                if (!result.Concrete)
                {
                    var tmpStrain = new LineSeries
                    {
                        MarkerType = MarkerType.None,
                        MarkerSize = 2,
                        MarkerFill = OxyColors.Black
                    };
                    tmpStrain.Points.Add(new DataPoint(0, result.yCoord));
                    tmpStrain.Points.Add(new DataPoint(- result.Strain * 1000, result.yCoord));
                    strainPlot.Series.Add(tmpStrain);

                    strainPlot.Annotations.Add(new TextAnnotation
                    {
                        TextPosition = new DataPoint(-0.5*result.Strain, result.yCoord),
                        Text = "ε = " + (result.Strain / 1000).ToString("#0.00##" + '\u2030', CultureInfo.InvariantCulture),
                        FontSize = 11,
                        Stroke = OxyColors.Transparent,
                    });
                }

            }



            // Force plot
            heightSeries.Points.Add(new DataPoint(0, 0));
            heightSeries.Points.Add(new DataPoint(0, height));
            foreach (NMPointResult res in resultList)
            {
                ArrowAnnotation annotation = null;
                TextAnnotation forceText = null;
                if (res.Concrete)
                {
                    annotation = new ArrowAnnotation
                    {
                        StartPoint = new DataPoint(res.Force, res.yCoord),
                        EndPoint = new DataPoint(0, res.yCoord),
                        Color = OxyColors.Black,
                        HeadWidth = 10
                    };
                    forceText = new TextAnnotation
                    {
                        TextPosition = new DataPoint(res.Force/2, res.yCoord),
                        Text = "Fc = " + (res.Force / 1000).ToString("F1") + "kN",
                        FontSize = 14,
                        Stroke = OxyColors.Transparent,
                        TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left
                    };
                }
                else
                {
                    DataPoint start, end;
                    OxyPlot.HorizontalAlignment placement;
                    if (res.Compressed)
                    {
                        start = new DataPoint(0, res.yCoord); end = new DataPoint(-res.Force, res.yCoord);
                        placement = OxyPlot.HorizontalAlignment.Left;
                    }
                    else
                    {
                        start = new DataPoint(0, res.yCoord); end = new DataPoint(-res.Force , res.yCoord);
                        placement = OxyPlot.HorizontalAlignment.Right;
                    }
                    annotation = new ArrowAnnotation
                    {
                        StartPoint = start,
                        EndPoint = end,
                        Color = OxyColors.Blue,
                        HeadWidth = 5
                    };
                    forceText = new TextAnnotation
                    {
                        TextPosition = new DataPoint(-0.5*res.Force, res.yCoord),
                        Text = "Fs = " + (res.Force / 1000).ToString("F1") + "kN",
                        FontSize = 11,
                        Stroke = OxyColors.Transparent,
                        TextHorizontalAlignment = placement
                    };                    
                }
                forcePlot.Annotations.Add(annotation);
                forcePlot.Annotations.Add(forceText);
            }

            forcePlot.Series.Add(heightSeries);
            return new SectionResults { Name = pointName, sectionPlot = sectionPlot, strainPlot = strainPlot, forcePlot = forcePlot };

        }

        private double findX(string bendingDirection, int itterations)
        {
            // To find the correct compression zone an itterative process is used
            // Initiallization
            double N = 1e9;
            double x = this.ConcreteSection.Height / 3;

            // Constants
            const double Tolerance = 1e2;
            double stepSize = this.ConcreteSection.Height / 100;
            int counter = 0;

            // Newton-Raphson optimization to find x where N approx 0
            while (Math.Abs(N) > Tolerance)
            {
                double NCurrent = NBequlibrium(x, this.ConcreteSection, bendingDirection);
                double NNext = NBequlibrium(x + stepSize, this.ConcreteSection, bendingDirection);
                double dNdx = (NNext - NCurrent) / stepSize; // f'(x)

                if (Math.Abs(dNdx) < 1e-8)
                {
                    MessageBox.Show("Derivative too small; stopping iteration.");
                    break;
                }

                double xNew = x - NCurrent / dNdx;

                // Compression zone needs to be there and it needs to be within the section height
                // Enforce max/min 
                if (xNew < 0)
                { xNew = 0; }
                else if (xNew > this.ConcreteSection.Height)
                { xNew = this.ConcreteSection.Height; }

                // Update x and recalculate N
                x = xNew;
                N = NBequlibrium(x, this.ConcreteSection, bendingDirection);
                counter++;

                if (counter == itterations) { MessageBox.Show($"Itterations {itterations} reached, accept criteria of N = 0.01 kN are not satisfied"); break; }
                
            }

            return x;
        }
        private double NBequlibrium(double x, ConcreteSection.RectangleSection section, string direction)
        {
            Materials.ConcreteMaterial concrete = this.ConcreteSection.Concrete;
            double width = this.ConcreteSection.Width;
            double height = this.ConcreteSection.Height;

            double Fc = concrete.LambdaC * x * concrete.Eta * width * concrete.Fcd();
            double Fs = 0;
            foreach (var layer in this.ConcreteSection.reinforcementLayers)
            {
                double c = 0;
                double cTension = 0;
                if (direction == "+") { c = layer.DistanceFromTop; cTension = layer.DistanceFromBottom; }
                else if (direction == "-") { c = layer.DistanceFromBottom; cTension = layer.DistanceFromTop; }

                double prestressStrain = 0;
                if (layer.InitialStrain != 0 && direction == "+")
                {
                    double dSectionStrain = (section.StrainTop - section.StrainBottom) / height;
                    prestressStrain = section.StrainBottom + dSectionStrain * layer.DistanceFromBottom;
                }
                else
                {
                    double dSectionStrain = (section.StrainBottom - section.StrainTop) / height;
                    prestressStrain = section.StrainTop + dSectionStrain * layer.DistanceFromTop;
                }

                double strain = 0;
                if (c >= x) // Tension
                {
                    strain = -concrete.ecu3 / x * (height - x - cTension);
                    //MessageBox.Show($"Layer strain: {strain}, yield strain: {layer.Material.eyd()}, combined strain and initial strain: {strain + layer.InitialStrain}");
                    strain = Math.Max(-layer.Material.eyd(), strain + layer.InitialStrain + prestressStrain); 
                    
                }
                else // Compression (Relaxation)
                {
                    strain = concrete.ecu3 * (x - c) / x;
                    strain = Math.Min(layer.Material.eyd(), strain + layer.InitialStrain + prestressStrain);

                }
                Fs += strain * layer.Material.Es * layer.Area;
                //MessageBox.Show($"x: {x}, Fc: {Fc} layer force: {strain * layer.Material.Es * layer.Area}");
            }
            return Fc + Fs;
        }
        public override string ToString()
        {
            return $"Section NM-Diagram: A ({NRd[0]/1000:0.00}; {MRd[0] / 1000000:0.00}), B+ ({NRd[1] / 1000:0.00}; {MRd[1] / 1000000:0.00}), B- ({NRd[2] / 1000:0.00}; {MRd[2] / 1000000:0.00}), C+ ({NRd[3] / 1000:0.00}; {MRd[3] / 1000000:0.00}), C- ({NRd[4] / 1000:0.00}; {MRd[4] / 1000000:0.00}), D+ ({NRd[5] / 1000:0.00}; {MRd[5] / 1000000:0.00}), D- ({NRd[6] / 1000:0.00}; {MRd[6] / 1000000:0.00}), E ({NRd[7] / 1000:0.00}; {MRd[7] / 1000000:0.00})";
        }

    }
}
