using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text;

// Path to your JSON file
string jsonFilePath = @"C:\Users\huawe\OneDrive\سطح المكتب\governorate.json";

// Read and parse the JSON content
string jsonContent = File.ReadAllText(jsonFilePath);
JArray jsonArray = JArray.Parse(jsonContent);

// Generate C# class definitions and the list initialization code
string classDefinitions = GenerateClassDefinitions(jsonArray, jsonFilePath);
string listInitializationCode = GenerateListInitializationCode(jsonArray, jsonFilePath);

// Path to output file
string outputFilePath = @"C:\Users\huawe\OneDrive\سطح المكتب\output.txt";

// Write the generated C# code to the output file
File.WriteAllText(outputFilePath, classDefinitions + "\n" + listInitializationCode);

Console.WriteLine("C# class definitions and list initialization code have been written to output.txt.");

// Function to generate C# class definitions based on the JSON structure and file name
string GenerateClassDefinitions(JArray jsonArray, string jsonFilePath)
{
    var classDefinitions = new StringBuilder();
    var properties = new Dictionary<string, Type>();

    // Collect all unique property names from the JSON objects
    foreach (var item in jsonArray)
    {
        ExtractProperties(item, properties, "");
    }

    // Derive the class name from the JSON file name (remove file extension and make PascalCase)
    string className = Path.GetFileNameWithoutExtension(jsonFilePath);
    className = ToPascalCase(className);

    // Generate the C# class definition
    classDefinitions.AppendLine($"public class {className}");
    classDefinitions.AppendLine("{");

    foreach (var property in properties)
    {
        string pascalCaseProperty = ToPascalCase(property.Key);
        string propertyType = GetCSharpType(property.Key, property.Value);
        classDefinitions.AppendLine($"    public {propertyType} {pascalCaseProperty} {{ get; set; }}");
    }

    classDefinitions.AppendLine("}");

    return classDefinitions.ToString();
}

// Function to generate the C# list initialization code based on the JSON data and file name
string GenerateListInitializationCode(JArray jsonArray, string jsonFilePath)
{
    // Derive the class name from the JSON file name (remove file extension and make PascalCase)
    string className = Path.GetFileNameWithoutExtension(jsonFilePath);
    className = ToPascalCase(className);

    var listInitialization = new StringBuilder();
    listInitialization.AppendLine($"List<{className}> dataList = new List<{className}> {{");

    foreach (var item in jsonArray)
    {
        listInitialization.Append("    new " + className + " {");

        foreach (var property in item.Children<JProperty>())
        {
            string propertyName = ToPascalCase(property.Name);
            string propertyValue = property.Value.ToString();

            // If the property name contains "Id", treat it as an integer
            if (propertyName.Contains("Id", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(propertyValue, out int numericValue))
                {
                    propertyValue = numericValue.ToString(); // convert to number without quotes
                }
                else
                {
                    propertyValue = "0"; // Default to 0 if the value isn't numeric
                }
            }
            else
            {
                // If it's a string, wrap it in quotes
                propertyValue = $"\"{propertyValue}\"";
            }

            listInitialization.Append($"{propertyName} = {propertyValue}, ");
        }

        // Remove the last comma and space, then close the object initializer
        listInitialization.Length -= 2;
        listInitialization.AppendLine("},");
    }

    // Remove the last comma and close the list initializer
    listInitialization.Length -= 1;
    listInitialization.AppendLine("};");

    return listInitialization.ToString();
}

// Function to get the appropriate C# type for the property based on its values and name
string GetCSharpType(string propertyName, Type type)
{
    // If the property name contains "Id", treat it as an integer
    if (propertyName.Contains("Id", StringComparison.OrdinalIgnoreCase))
    {
        return "int";
    }

    // Default type detection for non-"Id" properties
    if (type == typeof(int) || type == typeof(long) || type == typeof(short))
    {
        return "int";
    }
    if (type == typeof(float) || type == typeof(double))
    {
        return "double";
    }
    if (type == typeof(bool))
    {
        return "bool";
    }

    return "string";
}

// Function to convert a string to PascalCase and remove underscores
string ToPascalCase(string str)
{
    var result = new StringBuilder();
    bool capitalizeNext = true;

    foreach (char c in str)
    {
        if (c == '_')
        {
            capitalizeNext = true;
            continue;
        }

        result.Append(capitalizeNext ? Char.ToUpper(c) : Char.ToLower(c));
        capitalizeNext = false;
    }

    return result.ToString();
}

// Recursive function to extract nested properties and generate property paths
void ExtractProperties(JToken token, Dictionary<string, Type> properties, string parentPath)
{
    if (token is JObject obj)
    {
        foreach (var property in obj.Properties())
        {
            string fullPath = string.IsNullOrEmpty(parentPath) ? property.Name : $"{parentPath}.{property.Name}";
            ExtractProperties(property.Value, properties, fullPath);
        }
    }
    else if (token is JArray array)
    {
        foreach (var item in array)
        {
            ExtractProperties(item, properties, parentPath);
        }
    }
    else
    {
        // Determine the type of the property value
        Type valueType = token.Type switch
        {
            JTokenType.Integer => typeof(int),
            JTokenType.Float => typeof(double),
            JTokenType.String => typeof(string),
            JTokenType.Boolean => typeof(bool),
            _ => typeof(string), // Default to string if type is unknown
        };

        if (!string.IsNullOrEmpty(parentPath))
        {
            // Only set the type if it's not already set
            if (!properties.ContainsKey(parentPath))
            {
                properties[parentPath] = valueType;
            }
        }
    }
}
