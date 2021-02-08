using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PartyTreasure
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = "D:\\characters";
            if (Directory.Exists(directoryPath))
            {
                ProcessFiles(directoryPath);
            }
            else
            {
                Console.WriteLine("Directory doesn't exist");
            }

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }

        public class Serializer
        {
            public T Deserialize<T>(string input) where T : class
            {
                System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

                using (StringReader sr = new StringReader(input))
                {
                    return (T) ser.Deserialize(sr);
                }
            }

            public string Serialize<T>(T ObjectToSerialize)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());

                using (StringWriter textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, ObjectToSerialize);
                    return textWriter.ToString();
                }
            }
        }

        private static void ProcessFiles(string directoryPath)
        {
            Serializer ser = new Serializer();
            string[] fileEntries = Directory.GetFiles(directoryPath);

            var coinPath = $"{directoryPath}\\coins.csv";
            // if (File.Exists(coinPath))
            // {
            //     File.Delete(coinPath);
            // }

            var inventoryPath = $"{directoryPath}\\inventory.csv";
            // if (File.Exists(inventoryPath))
            // {
            //     File.Delete(inventoryPath);
            // }

            using (FileStream cfs = File.Create(coinPath))
            using (FileStream ifs = File.Create(inventoryPath))
            {
                foreach (string fileName in fileEntries)
                {
                    // Console.WriteLine(fileName);
                    if (fileName.EndsWith('l'))
                    {
                        string xmlCharacterData = File.ReadAllText(fileName);
                        XDocument doc = XDocument.Parse(xmlCharacterData);
                        string jsonText = JsonConvert.SerializeXNode(doc);
                        dynamic dyn = JsonConvert.DeserializeObject(jsonText);
                        PrepareCoins(dyn, cfs);
                        PrepareInventory(dyn, ifs);
                    }
                }
            }
        }

        private static void PrepareCoins(dynamic dyn, FileStream cfs)
        {
            string[] slots = {"slot1", "slot2", "slot3", "slot4", "slot5", "slot6"};
            foreach (string slot in slots)
            {
                var line = string.Empty;
                try
                {
                    line =
                        $"{dyn["root"]["character"]["name"]["#text"]}, {dyn["root"]["character"]["coins"][slot]["amount"]["#text"]}, {(dyn["root"]["character"]["coins"][slot]["name"]["#text"]).ToString().ToUpper()}\r\n";
                }
                catch
                {
                    // line = $"{dyn["root"]["character"]["name"]["#text"]}, 0, {slot}\r\n";
                }

                if (!string.IsNullOrEmpty(line))
                {
                    Byte[] lineBytes = new UTF8Encoding(true).GetBytes(line);
                    cfs.Write(lineBytes, 0, lineBytes.Length);
                }
            }
        }

        private static void PrepareInventory(dynamic dyn, FileStream ifs)
        {
            var character = dyn["root"]["character"]["name"]["#text"];

            int ii = 1;
            while (ii < 200)
            {
                try
                {
                    var key = $"id-{ii.ToString().PadLeft(5, '0')}";

                    var itemName = TryGetInventory(key, "name", dyn);
                    var itemCount = TryGetInventory(key, "count", dyn);
                    var itemStringCost = TryGetInventoryStringCost(key, "cost", dyn);
                    var itemCost = TryGetInventoryCost(key, "cost", dyn);
                    var itemCostType = TryGetInventoryCostType(key, "cost", dyn);

                    var line = string.Empty;
                    if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(itemStringCost))
                    {
                        line =
                            $"{character},{itemName},{itemCount},{itemStringCost},{itemCost},{itemCostType}\r\n";

                        Byte[] lineBytes = new UTF8Encoding(true).GetBytes(line);
                            ifs.Write(lineBytes, 0, lineBytes.Length);
                        Console.WriteLine(line);
                    }

                    ii++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    // key doesn't exist, get outta here.
                    break;
                }
            }
        }

        private static string TryGetInventory(string itemKey, string key, dynamic dyn)
        {
            try
            {
                return (dyn["root"]["character"]["inventorylist"][itemKey][key]["#text"]).ToString().Replace(",", "-");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TryGetInventoryStringCost(string itemKey, string key, dynamic dyn)
        {
            try
            {
                return (dyn["root"]["character"]["inventorylist"][itemKey][key]["#text"]).ToString().Replace(",", "");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int? TryGetInventoryCost(string itemKey, string key, dynamic dyn)
        {
            try
            {
                var cost = (dyn["root"]["character"]["inventorylist"][itemKey][key]["#text"]).ToString();
                if (int.TryParse(Regex.Replace(cost, "[^0-9]", ""), out int intCost))
                {
                    return intCost;
                }

                return null;
            }
            catch
            {
                return null;
            }

        }


        private static string TryGetInventoryCostType(string itemKey, string key, dynamic dyn)
        {
            try
            {
                var cost = (dyn["root"]["character"]["inventorylist"][itemKey][key]["#text"]).ToString().ToUpper();
                cost = Regex.Replace(cost, "[0-9\\s]", "");

                return cost;
            }
            catch
            {
                return null;
            }

        }
    }
}
