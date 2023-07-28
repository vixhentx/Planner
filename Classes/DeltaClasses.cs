using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Classes
{
    public class weekItemDelta
    {
        public int x;
        public int y;
        public string preContent, curContent;
    }
    public class VItemDelta
    {
        public int y;
        public int preTabHeight = 1, curTabHeight = 1;
        public string preValue, curValue;
        public string preFilter, curFilter;
    }
    public class ListContentDelta
    {
        public int y;
        public string preName;
        public string preId;
        public string preSex;
        public string preRoom;
        public string curName;
        public string curId;
        public string curSex;
        public string curRoom;
    }
}
