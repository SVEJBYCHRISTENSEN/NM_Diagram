﻿using System.Xml.Linq;
using System.IO;

namespace Materials
{
    public class Material
    {
        public XDocument MaterialDatabase { get; private set; }
        public MaterialLibrary Library { get; private set; }

        public Material()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", "Materials.xml");
            MaterialDatabase = XDocument.Load(filePath);
            Library = new MaterialLibrary(this);
        }

        public class MaterialLibrary
        {
            public List<string> Types { get; private set; }
            public Dictionary<string, XElement> MaterialDict { get; private set; }

            public MaterialLibrary(Material materialsInstance)
            {
                MaterialDict = new Dictionary<string, XElement>();
                XElement materialsRoot = materialsInstance.MaterialDatabase.Root;

                // Retrieve types of material to categories
                Types = new List<string>();
                if (materialsRoot != null)
                {
                    foreach (XElement materialType in materialsRoot.Elements())
                    {
                        Types.Add(materialType.Name.LocalName);

                        // Get materials within the types/category:
                        foreach (XElement material in materialType.Elements("Material"))
                        {
                            string materialID = material.Attribute("id")?.Value;
                            if (!string.IsNullOrEmpty(materialID))
                            {
                                MaterialDict[materialID] = material;
                            }
                        }
                    }
                }
            }
        }
    }

    public class ConcreteMaterial
    {
        public string Name { get; private set; }
        public double GammaC { get; private set; }
        public double Fck { get; private set; }
        public double Ecm { get; private set; }
        public double ecu3 { get; private set; }
        public double ec3 { get; private set; }

        public double Eta { get; private set; }
        public double LambdaC { get; private set; }

        public ConcreteMaterial(Material materialsInstance, string name, double gammaC)
        {
            Name = name;
            GammaC = gammaC;
            XElement materialsRoot = materialsInstance.MaterialDatabase.Root;

            if (materialsRoot != null)
            {
                foreach (XElement material in materialsRoot.Descendants("Material"))
                {
                    if (material.Attribute("id")?.Value == Name)
                    {
                        Fck = double.Parse(material.Element("CompressiveStrength")?.Value ?? "0");
                        Ecm = double.Parse(material.Element("Ecm")?.Value ?? "0");
                        ecu3 = double.Parse(material.Element("ecu3")?.Value ?? "0");
                        ec3 = double.Parse(material.Element("ec3")?.Value ?? "0");
                        Eta = double.Parse(material.Element("eta")?.Value ?? "0");
                        LambdaC = double.Parse(material.Element("lambda")?.Value ?? "0");
                        break;
                    }
                }
            }
        }

        public double Fcd()
        {
            return Fck / GammaC;
        }

        public override string ToString()
        {
            return $"Concrete material: {Name}, Fck = {Fck}, Ecm = {Ecm}, εcu3 = {ecu3}";
        }
    }

    public class RebarMaterial
    {
        public string Name { get; private set; }
        public double GammaS { get; private set; }
        public double Fyk { get; private set; }
        public double Es { get; private set; }
        public double ey { get; private set; }
        

        public RebarMaterial(Material materialsInstance, string name, double gammaS)
        {
            Name = name;
            GammaS = gammaS;
            XElement materialsRoot = materialsInstance.MaterialDatabase.Root;

            if (materialsRoot != null)
            {
                foreach (XElement material in materialsRoot.Descendants("Material"))
                {
                    if (material.Attribute("id")?.Value == Name)
                    {
                        Fyk = double.Parse(material.Element("YieldStrength")?.Value ?? "0");
                        Es = double.Parse(material.Element("YoungModulus")?.Value ?? "0");
                        ey = Fyk / Es;
                        break;
                    }
                }
            }
        }

        public double Fyd()
        {
            return Fyk / GammaS;
        }
        public double eyd()
        {
            return ey / GammaS;
        }

        public override string ToString()
        {
            return $"Rebar material: {Name}, Fyk = {Fyk}, Es = {Es}, εyd = {eyd()}";
        }

    }

}
