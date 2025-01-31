using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Materials;

namespace ConcreteSection
{
    public class RectangleSection
    {
        public double Width { get; private set; }
        public double Height { get; private set; }
        public Materials.ConcreteMaterial Concrete { get; private set; }
        private List<ReinforcementLayer> reinforcementLayers;

        public RectangleSection(double width, double height, Materials.ConcreteMaterial material)
        {
            Width = width;
            Height = height;
            Concrete = material;
            reinforcementLayers = new List<ReinforcementLayer>();
        }

        public double Area()
        {
            return Width * Height;
        }

        public double Centroid()
        {
            return 0.5 * Height;
        }

        public void AddReinforcementLayer(Materials.RebarMaterial material, double diameter, int number, double distanceFromBottom)
        {
            if (distanceFromBottom < 0 || distanceFromBottom > Height)
            {
                throw new ArgumentOutOfRangeException("Reinforcement position must be within the section height.");
            }

            double area = number * 0.25 * Math.PI * Math.Pow(diameter, 2);

            reinforcementLayers.Add(new ReinforcementLayer
            {
                Material = material,
                Diameter = diameter,
                Number = number,
                DistanceFromBottom = distanceFromBottom,
                Area = area
            });
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
            return reinforcementLayers.Where(layer => layer.DistanceFromBottom > Centroid()).ToList();
        }

        public List<ReinforcementLayer> ReinforcementBottom()
        {
            return reinforcementLayers.Where(layer => layer.DistanceFromBottom < Centroid()).ToList();
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
            public double Area { get; set; }
            public Materials.RebarMaterial Material { get; set; }
        }

    }
}
