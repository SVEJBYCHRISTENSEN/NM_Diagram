using System.Xml.Linq;

namespace Materials
{
    public class Material
    {
        public XDocument MaterialDatabase { get; private set; }
        public MaterialLibrary Library { get; private set; }

        public Material()
        {
            MaterialDatabase = XDocument.Load("..\\Library\\Materials.xml");
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
    }

    public class RebarMaterial
    {
        public string Name { get; private set; }
        public double GammaS { get; private set; }
        public double Fyk { get; private set; }

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
                        break;
                    }
                }
            }
        }

        public double Fyd()
        {
            return Fyk / GammaS;
        }
    }

}
