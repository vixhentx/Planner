using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Classes
{
    public class Student
    {
        public Student()
        {
            Name = string.Empty;
            Id = string.Empty;
            Sex= string.Empty;
            Room = string.Empty;
        }
        public string toString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\n", Name, Id, Sex, Room);
        }
        public static Student fromString(string s)
        {
            string[] perps;
            perps = s.Split(splitters);
            if (perps.Length < 4)
            {
                return null;
            }
            Student student = new Student();
            student.Name = perps[0];
            student.Id = perps[1];
            student.Sex = perps[2];
            student.Room = perps[3];
            return student;
        }
        private readonly static char[] splitters = { '\t', ',', ' ' };
        public string Name { get; set; }
        public string Id { get; set; }
        public string Sex { get; set; }
        public string Room { get; set; }
    }
}
