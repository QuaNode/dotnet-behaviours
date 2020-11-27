using IniParser;
using IniParser.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuaNode {

    static class Cache {

        public static object getValueForParameter(Dictionary<string, object> parameter, Dictionary<string, object> data, string key,
            string name) {

            if (data[key] != null) return data[key];
            if (parameter["value"] != null) return (parameter["value"] as Func<string, Dictionary<string, object>, object>) != null ?
                ((Func<string, Dictionary<string, object>, object>) parameter["value"])(name, data) : (string)parameter["value"];
            else if (isEqual(parameter["source"], true)) {

                if ((getParameter()[key] as Dictionary<string, object>) != null) {

                    Dictionary<string, object> param = (Dictionary<string, object>) getParameter()[key];
                    if (param["value"] != null) return param["value"];
                }
            }
            return null;
        }

        public static void setParameter(Dictionary<string, object> data) {

            string filePath = Path.Combine(getStoragePath(), "Behaviours.ini");
            string sectionName = "Behaviors";
            FileIniDataParser fileParser = new FileIniDataParser();
            IniData iniData;
            if (!File.Exists(filePath)) {

                FileStream fileStream = File.Create(filePath);
                fileStream.Close();
                iniData = fileParser.ReadFile(filePath);
                iniData.Sections.AddSection(sectionName);
            } else {

                iniData = fileParser.ReadFile(filePath);
            }            
            foreach(var item in data) {

                if (iniData[sectionName].ContainsKey(item.Key)) iniData[sectionName][item.Key] = JsonConvert.SerializeObject(item.Value);
                else iniData[sectionName].AddKey(item.Key, JsonConvert.SerializeObject(item.Value));
            }
            fileParser.WriteFile(filePath, iniData);
        }

        public static Dictionary<string, object> getParameter() {            

            string filePath = Path.Combine(getStoragePath(), "Behaviours.ini");
            string sectionName = "Behaviors";
            if (!File.Exists(filePath)) return null;
            FileIniDataParser fileParser = new FileIniDataParser();
            IniData iniData = fileParser.ReadFile(filePath);
            Dictionary<string, object> data = new Dictionary<string, object>();
            foreach (var item in iniData[sectionName]) data.Add(item.KeyName, JsonConvert.DeserializeObject(item.Value));            
            return data;
        }

        private static string getStoragePath () {

            if (String.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath)) {

                return AppDomain.CurrentDomain.BaseDirectory; //exe folder for WinForms, Consoles, Windows Services
            } else {

                return AppDomain.CurrentDomain.RelativeSearchPath; //bin folder for Web Apps 
            }
        }

        private static bool isEqual(Object o1, Object o2) {

            return o1 != null && o1.Equals(o2);
        }
    }
}
