namespace Planner.Classes
{
    public class TimeItem
    {
        public string Time { get; set; }
        public string Filter { get; set; }
        public int TabHeight { get; set; }
        public double Height => TabHeight * ((double)App.Current.FindResource("CellHeight") + 4) - 4;//+4用来同步broder;
    }
    public class EventItem
    {
        public string Event { get; set; }
        public string Filter { get; set; }
        public int TabHeight { get; set; }
        public double Height => TabHeight * ((double)App.Current.FindResource("CellHeight")+4)-4;//+4用来同步broder
    }
    public class WeekItem
    {
        public string Monday { get; set; }
        public string Tuesday { get; set; }
        public string Wednesday { get; set; }
        public string Thursday { get; set; }
        public string Friday { get; set; }
        public double Height => (double)App.Current.FindResource("CellHeight");
        public void SetFromIndex(int i,string value)
        {
            switch (i)
            {
                case 0:
                    Monday = value;
                    break;
                case 1:
                    Tuesday = value;
                    break;
                case 2:
                    Wednesday = value;
                    break;
                case 3:
                    Thursday = value;
                    break;
                case 4:
                    Friday = value;
                    break;
            }
        }
        public string GetFromIndex(int i)
        {
            string ret=string.Empty;
            switch (i)
            {
                case 0:
                    ret=Monday;
                    break;
                case 1:
                    ret=Tuesday ;
                    break;
                case 2:
                    ret=Wednesday ;
                    break;
                case 3:
                    ret=Thursday ;
                    break;
                case 4:
                    ret=Friday ;
                    break;
            }
            return ret;
        }
        public string Filter { get; set; }
    }
}
