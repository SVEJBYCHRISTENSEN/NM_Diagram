using System;
using System.Collections.Generic;
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
using System.Xml;
using System.Xml.Linq;

namespace NM_Concrete
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MaterialManagerWindow : Window
    {

        private string _xmlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", "Materials.xml");
        private XDocument _xmlDoc;
        private XElement _currentMaterialElement;

        public MaterialManagerWindow()
        {
            InitializeComponent();
            LoadXml();
        }

        private void LoadXml()
        {
            _xmlDoc = XDocument.Load(_xmlFilePath);
        }

        private void MaterialTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaterialTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string materialType = selectedItem.Content.ToString();
                LoadMaterials(materialType);
            }
        }

        private void LoadMaterials(string materialType)
        {
            var materials = _xmlDoc.Root.Element(materialType)
                ?.Elements("Material")
                .Select(x => new
                {
                    id = x.Attribute("id")?.Value,
                    Element = x
                }).ToList();

            MaterialListBox.ItemsSource = materials;
            MaterialListBox.DisplayMemberPath = "id";
            _currentMaterialElement = null;
        }

        private void MaterialListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaterialListBox.SelectedItem != null)
            {
                var selectedMaterial = MaterialListBox.SelectedItem.GetType().GetProperty("Element")?.GetValue(MaterialListBox.SelectedItem) as XElement;
                if (selectedMaterial != null)
                {
                    _currentMaterialElement = selectedMaterial;
                    DisplayMaterialDetails(_currentMaterialElement);
                }
            }
        }

        
        private readonly Dictionary<string, List<string>> MaterialProperties = new()
        {
            { "Concrete", new List<string> { "Density", "CompressiveStrength", "Ecm", "CO2Equivalent", "ecu3", "ec3", "eta", "lambda" } },
            { "Steel", new List<string> { "Density", "YieldStrength", "YoungModulus", "CO2Equivalent" } },
            { "Rebar", new List<string> { "Density", "YieldStrength", "YoungModulus", "CO2Equivalent" } },
            { "Timber", new List<string> { "Density", "BendingStrength", "CO2Equivalent" } }
        };

        private void DisplayMaterialDetails(XElement materialElement)
        {
            IdTextBox.Text = materialElement.Attribute("id")?.Value ?? string.Empty;
            //TypeTextBox.Text = materialElement.Element("Type")?.Value ?? string.Empty;

            // Clear existing property fields
            PropertiesStackPanel.Children.Clear();

            // Get the type of material
            string materialType = materialElement.Element("Type")?.Value;
            if (!string.IsNullOrEmpty(materialType) && MaterialProperties.TryGetValue(materialType, out var properties))
            {
                // Generate input fields for each property
                foreach (var property in properties)
                {
                    var value = materialElement.Element(property)?.Value ?? string.Empty;
                    var unit = materialElement.Element(property)?.Attribute("unit")?.Value ?? "";

                    // Create property label and textbox
                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    stackPanel.Children.Add(new TextBlock { Text = $"{property} ({unit}):", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    stackPanel.Children.Add(new TextBox { Name = $"{property}TextBox", Text = value, Width = 300 });

                    PropertiesStackPanel.Children.Add(stackPanel);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string materialType = (MaterialTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string id = IdTextBox.Text;

            if (string.IsNullOrWhiteSpace(materialType))
            {
                MessageBox.Show("Please select a material type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Please enter a valid ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if the ID already exists for the selected material type
            var existingMaterial = _xmlDoc.Root.Element(materialType)?.Elements("Material")
                .FirstOrDefault(m => m.Attribute("id")?.Value == id);

            if (existingMaterial != null)
            {
                MessageBox.Show("A material with this ID already exists. Please use a different ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Define default units for each property by material type
            var unitsByMaterialType = new Dictionary<string, Dictionary<string, string>>
    {
        {
            "Concrete", new Dictionary<string, string>
            {
                { "Density", "kg/m3" },
                { "CompressiveStrength", "MPa" },
                { "Ecm", "MPa" },
                { "CO2Equivalent", "kgCO2/m3" },
                { "ecu3", "" },
                { "ec3", "" },
                { "eta", "" },
                { "lambda", "" }
            }
        },
        {
            "Steel", new Dictionary<string, string>
            {
                { "Density", "kg/m3" },
                { "YieldStrength", "MPa" },
                { "YoungModulus", "MPa" },
                { "CO2Equivalent", "kgCO2/ton" }
            }
        },
        {
            "Rebar", new Dictionary<string, string>
            {
                { "Density", "kg/m3" },
                { "YieldStrength", "MPa" },
                { "YoungModulus", "MPa" },
                { "CO2Equivalent", "kgCO2/ton" }
            }
        },
        {
            "Timber", new Dictionary<string, string>
            {
                { "Density", "kg/m3" },
                { "BendingStrength", "MPa" },
                { "CO2Equivalent", "kgCO2/m3" }
            }
        }
    };

            // Create a new material element
            var newMaterial = new XElement("Material", new XAttribute("id", id));

            // Add the Type element dynamically based on the selected material type
            newMaterial.Add(new XElement("Type", materialType));

            // Get the default units for the selected material type
            if (unitsByMaterialType.TryGetValue(materialType, out var propertyUnits))
            {
                // Add dynamic properties with their respective units
                foreach (var child in PropertiesStackPanel.Children.OfType<StackPanel>())
                {
                    var textBox = child.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox != null)
                    {
                        string propertyName = textBox.Name.Replace("TextBox", "");
                        string value = textBox.Text;

                        if (propertyUnits.TryGetValue(propertyName, out var unit))
                        {
                            // Add the property with the appropriate unit
                            var propertyElement = new XElement(propertyName, value);
                            propertyElement.SetAttributeValue("unit", unit);
                            newMaterial.Add(propertyElement);
                        }
                    }
                }
            }

            // Add the new material to the XML
            _xmlDoc.Root.Element(materialType)?.Add(newMaterial);

            // Save the XML with formatting
            SaveXmlFormatted();

            // Refresh the materials list
            LoadMaterials(materialType);

            // Clear the input fields
            ClearMaterialDetails();

            MessageBox.Show("Material added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaterialElement != null)
            {
                // Update existing material logic
                _currentMaterialElement.SetAttributeValue("id", IdTextBox.Text);
                //_currentMaterialElement.Element("Type")?.SetValue(TypeTextBox.Text);

                // Save dynamic properties
                foreach (var child in PropertiesStackPanel.Children.OfType<StackPanel>())
                {
                    var textBox = child.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox != null)
                    {
                        string propertyName = textBox.Name.Replace("TextBox", "");
                        string value = textBox.Text;

                        // Update or create the property element
                        var propertyElement = _currentMaterialElement.Element(propertyName);
                        if (propertyElement != null)
                        {
                            propertyElement.Value = value;
                        }
                        else
                        {
                            _currentMaterialElement.Add(new XElement(propertyName, value));
                        }
                    }
                }
            }
            else
            {
                // Logic for adding a new material
                string materialType = (MaterialTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(materialType))
                {
                    var newMaterial = new XElement("Material",
                        new XAttribute("id", IdTextBox.Text));
                        //new XElement("Type", TypeTextBox.Text));

                    foreach (var child in PropertiesStackPanel.Children.OfType<StackPanel>())
                    {
                        var textBox = child.Children.OfType<TextBox>().FirstOrDefault();
                        if (textBox != null)
                        {
                            string propertyName = textBox.Name.Replace("TextBox", "");
                            string value = textBox.Text;
                            newMaterial.Add(new XElement(propertyName, value));
                        }
                    }

                    _xmlDoc.Root.Element(materialType)?.Add(newMaterial);
                }
            }

            // Save changes to XML
            _xmlDoc.Save(_xmlFilePath);
            LoadMaterials((MaterialTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearMaterialDetails()
        {
            IdTextBox.Text = string.Empty;
            //TypeTextBox.Text = string.Empty;
            PropertiesStackPanel.Children.Clear();
            _currentMaterialElement = null;
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaterialElement != null)
            {
                // Confirm deletion
                var result = MessageBox.Show(
                    "Are you sure you want to delete this material?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove the selected material from the XML
                    _currentMaterialElement.Remove();

                    // Save the updated XML
                    _xmlDoc.Save(_xmlFilePath);

                    // Refresh the materials list
                    LoadMaterials((MaterialTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());

                    // Clear the displayed material details
                    ClearMaterialDetails();
                }
            }
            else
            {
                MessageBox.Show("No material selected to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveXmlFormatted()
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ", // Use two spaces for indentation
                NewLineOnAttributes = false
            };

            using (var writer = XmlWriter.Create(_xmlFilePath, settings))
            {
                _xmlDoc.Save(writer);
            }
        }
    }
}
