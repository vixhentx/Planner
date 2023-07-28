namespace Planner.Classes
{
    public class VBarItem
    {
        public string Value { get; set; }
        public string Filter { get; set; }
        public int TabHeight { get; set; }
        public double Height => (TabHeight * ((double)App.Current.FindResource("CellHeight")) + (TabHeight - 1) * 4);//用来对齐
        public VBarItem()
        {
            Filter = string.Empty;
            TabHeight = 1;
        }
        public bool ShouldSerializeHeight()
        {
            return false;
        }
    }
    public class WeekItem
    {

        public string Monday { get; set; }
        public string Tuesday { get; set; }
        public string Wednesday { get; set; }
        public string Thursday { get; set; }
        public string Friday { get; set; }
        public double Height => (double)App.Current.FindResource("CellHeight");
        public void SetFromIndex(int i, string value)
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
            string ret = string.Empty;
            switch (i)
            {
                case 0:
                    ret = Monday;
                    break;
                case 1:
                    ret = Tuesday;
                    break;
                case 2:
                    ret = Wednesday;
                    break;
                case 3:
                    ret = Thursday;
                    break;
                case 4:
                    ret = Friday;
                    break;
            }
            return ret;
        }
        public string Filter { get; set; }

        public WeekItem()
        {
            Filter = string.Empty;
        }
        public bool ShouldSerializeHeight()
        {
            return false;
        }
    }

}
