using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Planner.Classes
{
    public static class JsonHelper
    {
        public class SaveTemplate
        {
            public List<TimeItem> timeItems { get; set; }
            public List<EventItem> eventItems { get; set; }
        }
        public class SaveHelper
        {
            public List<Student> students { get; set; }
            public SaveTemplate template { get; set; }
            public List<WeekItem> weekItems { get; set; }
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
        static public void SavePPJ(string path,SaveHelper saveHelper)
        {
            string output = JsonConvert.SerializeObject(saveHelper);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
        static public void SavePD(string path,List<Student> students)
        {
            string output = JsonConvert.SerializeObject(students);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
        static public void SavePT(string path,SaveTemplate template)
        {
            string output = JsonConvert.SerializeObject(template);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(output);
            sw.Close();
        }
    }
}
