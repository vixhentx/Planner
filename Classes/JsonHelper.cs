using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Planner.Classes
{
    public static class JsonHelper
    {
        public class SaveTemplate
        {
            public List<VBarItem> timeItems { get; set; }
            public List<VBarItem> eventItems { get; set; }
        }
        public class SaveHelper
        {
            public List<Student> students { get; set; }
            public SaveTemplate template { get; set; }
            public List<WeekItem> weekItems { get; set; }
        }
        public class WeekItemMeta
        {
            public int x, y;
            public string content;
        }
        static public SaveHelper ReadPPJ(string path)
        {
            StreamReader sr = new StreamReader(path);
            string input = sr.ReadToEnd();
            sr.Close();
            return JsonConvert.DeserializeObject<SaveHelper>(input);
        }
        static public List<Student> ReadPD(string path)
        {
            StreamReader sr = new StreamReader(path);
            string input = sr.ReadToEnd();
            sr.Close();
            return JsonConvert.DeserializeObject<List<Student>>(input);
        }
        static public SaveTemplate ReadPT(string path)
        {
            StreamReader sr = new StreamReader(path);
            string input = sr.ReadToEnd();
            sr.Close();
            return JsonConvert.DeserializeObject<SaveTemplate>(input);
        }
        static public void SavePPJ(string path, SaveHelper saveHelper)
        {
            string output = JsonConvert.SerializeObject(saveHelper);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
        static public void SavePD(string path, List<Student> students)
        {
            string output = JsonConvert.SerializeObject(students);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
        static public void SavePT(string path, SaveTemplate template)
        {
            string output = JsonConvert.SerializeObject(template);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
        static public List<VBarItem> GetVBarItems()
        {
            List<VBarItem> VBarItems = JsonConvert.DeserializeObject<List<VBarItem>>(Clipboard.GetText());
            if (VBarItems == null) return null;
            return VBarItems;
        }
        static public List<WeekItemMeta> GetWeekItems()
        {
            List<WeekItemMeta> weekItems = JsonConvert.DeserializeObject<List<WeekItemMeta>>(Clipboard.GetText());
            if (weekItems == null) return null;
            return weekItems;
        }
        static public void PutVBarItems(List<VBarItem> VBarItems)
        {
            string txt=JsonConvert.SerializeObject(VBarItems);
            Clipboard.SetText(txt);
        }
        static public void PutWeekItems(List<WeekItemMeta> weekItems)
        {
            string txt=JsonConvert.SerializeObject(weekItems);
            Clipboard.SetText(txt);
        }
    }
}
