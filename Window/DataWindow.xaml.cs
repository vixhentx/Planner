using Planner.Classes;
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
using MessageBox = HandyControl.Controls.MessageBox;

namespace Planner
{
    /// <summary>
    /// DataWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataWindow : HandyControl.Controls.Window
    {
        public DataWindow()
        {
            InitializeComponent();
        }

        public List<Student> Students = new List<Student>();
        private readonly char[] splitters = { '\t', ',', ' ' };

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            string[] texts = textInput.Text.Split('\n');//拆出每行
            string[] perps;//拆出每项
            int cnt = 0;
            foreach (string s in texts)
            {
                perps = s.Split(splitters);
                if (perps.Length < 4)
                {
                    if(perps.Length>1)cnt++;
                    continue;
                }
                Student student = new Student();
                student.Name = perps[0];
                student.Id = perps[1];
                student.Sex = perps[2];
                student.Room = perps[3];
                Students.Add(student);
            }
            if(cnt > 0)
            {
                MessageBox.Show(string.Format("有{0}个项属性不完整，已忽略！", cnt.ToString()),"提示");
            }
            Close();
        }
    }
}
