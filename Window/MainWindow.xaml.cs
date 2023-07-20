using Microsoft.Win32;
using Planner.Classes;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Window = HandyControl.Controls.Window;
using MessageBox = HandyControl.Controls.MessageBox;
using System.IO;

namespace Planner
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 菜单栏按钮
        private void btnData_Click(object sender, RoutedEventArgs e)
        {
            DataWindow dataWindow = new DataWindow()
            {
                Owner = this
            };
            dataWindow.ShowDialog();
            students.AddRange(dataWindow.Students);
        }
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定打开文件吗?未保存的数据将丢失!", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = "打开文件...",
                Multiselect = false,
                DefaultExt = "Plan工程文件(*.ppj)|*.ppj",
                Filter = "Planner工程文件(*.ppj)|*.ppj|Planner学生数据文件(*.pd)|*.pd|Planner表头文件(*.pt)|*.pt"
            };
            dialog.ShowDialog();
            string path = dialog.FileName;
            if (path == string.Empty) return;
            //MessageBox.Show(path);
            switch (dialog.FilterIndex)
            {
                case 1:
                    OpenPPJ(path);
                    break;
                case 2:
                    OepnPD(path);
                    break;
                case 3:
                    OpenPT(path);
                    break;
                default:
                    MessageBox.Show("你是怎么选到这个的?");
                    return;
            }
        }

        public void OpenPT(string path)
        {
            JsonHelper.SaveTemplate saveTemplate = JsonHelper.ReadPT(path);
            timeBar.ItemsSource = null;
            eventBar.ItemsSource = null;
            timeBar.ItemsSource = saveTemplate.timeItems;
            eventBar.ItemsSource = saveTemplate.eventItems;
        }

        public void OepnPD(string path)
        {
            listboxFilter.ItemsSource = null;
            students = JsonHelper.ReadPD(path);
        }

        public void OpenPPJ(string path)
        {
            JsonHelper.SaveHelper saveHelper = JsonHelper.ReadPPJ(path);
            timeBar.ItemsSource = null;
            eventBar.ItemsSource = null;
            weekBar.ItemsSource = null;
            listboxFilter.ItemsSource = null;

            students = saveHelper.students;
            timeBar.ItemsSource = saveHelper.template.timeItems;
            eventBar.ItemsSource = saveHelper.template.eventItems;
            weekBar.ItemsSource = saveHelper.weekItems;
            filename = path;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (filename == null || filename == string.Empty)
            {
                btnSaveAs_Click(sender, e); return;
            }
            JsonHelper.SavePPJ(filename, new JsonHelper.SaveHelper()
            {
                students = students,
                template = new JsonHelper.SaveTemplate()
                {
                    eventItems = (List<EventItem>)eventBar.ItemsSource,
                    timeItems = (List<TimeItem>)timeBar.ItemsSource,
                },
                weekItems = (List<WeekItem>)weekBar.ItemsSource
            });
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = "保存文件...",
                DefaultExt = "Plan工程文件(*.ppj)|*.ppj",
                Filter = "Planner工程文件(*.ppj)|*.ppj|Planner学生数据文件(*.pd)|*.pd|Planner表头文件(*.pt)|*.pt"
            };
            dialog.ShowDialog();
            string path = dialog.FileName;
            if (path == string.Empty) return;
            //MessageBox.Show(path);
            switch (dialog.FilterIndex)
            {
                case 1:
                    JsonHelper.SavePPJ(path, new JsonHelper.SaveHelper()
                    {
                        students = students,
                        template = new JsonHelper.SaveTemplate()
                        {
                            eventItems = (List<EventItem>)eventBar.ItemsSource,
                            timeItems = (List<TimeItem>)timeBar.ItemsSource,
                        },
                        weekItems = (List<WeekItem>)weekBar.ItemsSource
                    });
                    filename = path;
                    break;
                case 2:
                    JsonHelper.SavePD(path, students);
                    break;
                case 3:
                    JsonHelper.SavePT(path, new JsonHelper.SaveTemplate()
                    {
                        eventItems = (List<EventItem>)eventBar.ItemsSource,
                        timeItems = (List<TimeItem>)eventBar.ItemsSource,
                    });
                    break;
                default:
                    MessageBox.Show("你是怎么选到这个的?");
                    return;
            }
        }
        #endregion
        #region 学生数据管理
        private void btnAddData_Click(object sender, RoutedEventArgs e)
        {
            listContent.Add(new Student());
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = listContent;
        }

        private void btnRemoveData_Click(object sender, RoutedEventArgs e)
        {
            var sel = listboxFilter.SelectedItems;
            foreach (Student item in sel)
            {
                listContent.Remove(item);
            }
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = listContent;
        }
        private void textNameFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lastText == string.Empty)
            {
                students = listboxFilter.ItemsSource as List<Student>;
            }
            lastText = textNameFilter.Text;
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource=GetFilteredStudents(textNameFilter.Text);
        }

        private List<Student> GetFilteredStudents(string filterText)
        {
            List<Student> ret = new List<Student>();
            string[] sProps = filterText.Split(' ');
            foreach (Student student in students)
            {
                bool flag = true;
                foreach (string s in sProps)
                {
                    flag &= student.Name.Contains(s) || student.Id.Contains(s) || student.Sex.Contains(s) || student.Room.Contains(s);//有一项符合条件即可，利用逻辑短路来加快运行效率
                    if (!flag) break;
                }
                if (flag)
                {
                    ret.Add(student);
                }
            }
            return ret;
        }

        private void btnCopyData_Click(object sender, RoutedEventArgs e)
        {
            var sel = listboxFilter.SelectedItems;
            string ret = string.Empty;
            foreach (Student item in sel)
            {
                ret += item.toString();
            }
            Clipboard.SetDataObject(ret);
        }

        private void btnMoveData_Click(object sender, RoutedEventArgs e)
        {
            var sel = listboxFilter.SelectedItems;
            string ret = string.Empty;
            foreach (Student item in sel)//其实就是先复制再删除
            {
                ret += item.toString();
                listContent.Remove(item);
            }
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = listContent;
            Clipboard.SetDataObject(ret);
        }

        private void btnPasteData_Click(object sender, RoutedEventArgs e)
        {
            IDataObject iData = Clipboard.GetDataObject();
            string s;
            string[] datas;
            if (iData.GetDataPresent(DataFormats.Text))
            {
                s = (string)iData.GetData(DataFormats.Text);
            }
            else
            {
                MessageBox.Show("目前剪贴板中数据不可转换为文本", "错误");
                return;
            }
            datas = s.Split('\n');
            foreach (string data in datas)
            {
                Student student = Student.fromString(data);
                if (student != null)
                {
                    listContent.Add(student);
                }
            }
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = listContent;
        }

        private void btnClearData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("您确定要删除所有行吗?", "确认删除", MessageBoxButton.YesNo,MessageBoxImage.Question) == MessageBoxResult.Yes)
                students = new List<Student>();
        }

        private void btnClearEmpty_Click(object sender, RoutedEventArgs e)
        {
            List<Student> ret = new List<Student>();
            foreach (Student student in listContent)
            {
                if (!string.IsNullOrWhiteSpace(student.Name + student.Room + student.Sex + student.Id))//取并集为空就是全为空
                {
                    ret.Add(student);
                }
            }
            listContent = ret;
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = listContent;
        }
        #endregion
        #region 属性管理
        private void btnAddTimeBar_Click(object sender, RoutedEventArgs e)
        {
            TimeItem timeItem = new TimeItem()
            {
                TabHeight = int.Parse(textTabHeight.Text) < 1 ? 1 : int.Parse(textTabHeight.Text),
                Filter = textTabFilter.Text
            };
            var items = timeBar.ItemsSource as List<TimeItem>;
            items.Add(timeItem);
            timeBar.ItemsSource = null;
            timeBar.ItemsSource = items;
        }

        private void btnAddEventBar_Click(object sender, RoutedEventArgs e)
        {
            EventItem eventItem = new EventItem()
            {
                TabHeight = int.Parse(textTabHeight.Text) < 1 ? 1 : int.Parse(textTabHeight.Text),
                Filter = textTabFilter.Text
            };
            var items = eventBar.ItemsSource as List<EventItem>;
            items.Add(eventItem);
            eventBar.ItemsSource = null;
            eventBar.ItemsSource = items;
        }
        private void btnConfirmPrep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (selected)
                {
                    case MergedSelections.No:
                        return;
                    case MergedSelections.TimeBar:
                        TimeItem time = timeBar.SelectedItem as TimeItem;
                        time.TabHeight = int.Parse(textTabHeight.Text);
                        time.Filter = textTabFilter.Text;
                        break;
                    case MergedSelections.EventBar:
                        EventItem eventItem = eventBar.SelectedItem as EventItem;
                        eventItem.TabHeight = int.Parse(textTabHeight.Text);
                        eventItem.Filter = textTabFilter.Text;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            timeBar.Items.Refresh();
            eventBar.Items.Refresh();
        }
        #endregion
        #region 封装
        List<Student> listContent = new List<Student>();
        List<Student> students
        {
            get
            {
                return listContent;
            }
            set
            {
                listboxFilter.ItemsSource = listContent = value;
            }
        }

        enum MergedSelections
        {
            No,
            TimeBar,
            EventBar
        }
        private string lastText;
        public string filename;
        private MergedSelections selected;
        private int _selX =-1,_selY=-1;

        public int selX
        {
            get
            {
                return _selX;
            }
            set
            {
                statSelX.Text = ((_selX = value)+1).ToString();
            }
        }
        public int selY
        {
            get
            {
                return _selY;
            }
            set
            {
                statSelY.Text=((_selY = value)+1).ToString();
                statFilter.Text = ((List<WeekItem>)weekBar.ItemsSource)[selY].Filter;
            }
        }
        private int CalculateHeight()
        {
            int timeHeight = 0, eventHeight = 0;
            List<TimeItem> timeItems = (List<TimeItem>)timeBar.ItemsSource;
            List<EventItem> eventItems = (List<EventItem>)eventBar.ItemsSource;
            foreach(TimeItem timeItem in timeItems)
            {
                timeHeight+= timeItem.TabHeight;
            }
            foreach (EventItem eventItem in eventItems)
            {
                eventHeight += eventItem.TabHeight;
            }
            return Math.Max(timeHeight, eventHeight);
        }
        private TimeItem GetTimeItemFromHeight(int height)
        {
            int ret = 0;
            List<TimeItem> timeItems = (List<TimeItem>)timeBar.ItemsSource;
            foreach(TimeItem timeItem in timeItems)
            {
                ret += timeItem.TabHeight;
                if(ret > height)
                {
                    return timeItem;
                }
            }
            return null;
        }
        private EventItem GetEventItemFromHeight(int height)
        {
            int ret = 0;
            List<EventItem> eventItems = (List<EventItem>)eventBar.ItemsSource;
            foreach (EventItem eventItem in eventItems)
            {
                ret += eventItem.TabHeight;
                if (ret > height)
                {
                    return eventItem;
                }
            }
            return null;
        }
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            students = new List<Student>();
            timeBar.ItemsSource = new List<TimeItem>();
            eventBar.ItemsSource=new List<EventItem>();
            weekBar.ItemsSource= new List<WeekItem>();
        }

        private void btnDisplayApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Resources["CellHeight"] = double.Parse(textCellHeight.Text);
                Application.Current.Resources["FontSize"] = double.Parse(textFontSize.Text);
                timeBar.Items.Refresh();
                eventBar.Items.Refresh();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void timeBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selected = MergedSelections.TimeBar;
            eventBar.SelectedIndex = -1;
            eventBar.SelectedItem = null;
            TimeItem timeItem = (TimeItem)timeBar.SelectedItem;
            if (timeItem == null) return;
            textTabHeight.Text = timeItem.TabHeight.ToString();
            textTabFilter.Text=timeItem.Filter.ToString();
        }



        private void listboxFilter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listboxFilter.SelectedItems.Count < 0) return;
            if (selY < 0 || selX < 0) return;
            Student student = listboxFilter.SelectedItems[0] as Student;
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            if (selY >= weekItems.Count) return;
            switch(selX)
            {
                case 0:
                    weekItems[selY].Monday = student.Name;
                    break;
                case 1:
                    weekItems[selY].Tuesday = student.Name;
                    break;
                case 2:
                    weekItems[selY].Wednesday = student.Name;
                    break;
                case 3:
                    weekItems[selY].Thursday = student.Name;
                    break;
                case 4:
                    weekItems[selY].Friday = student.Name;
                    break;
            }
            weekBar.ItemsSource = null;
            weekBar.ItemsSource = weekItems;
                /*
            DataGridCellInfo cellInfo = new DataGridCellInfo(weekBar.Items[selY], weekBar.Columns[selX]);
            DataGridCell cell = cellInfo.Column.GetCellContent(cellInfo.Item).Parent as DataGridCell;
            TextBlock textBlock = cell.Content as TextBlock;
            textBlock.Text = student.Name;
                 */
        }

        private void btnDisableEdit_Click(object sender, RoutedEventArgs e)
        {
            listboxFilter.IsReadOnly = !listboxFilter.IsReadOnly;
        }

        private void weekBar_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (weekBar.SelectedCells.Count > 0)
            {
                var cellInfo = weekBar.SelectedCells[0];
                selX = cellInfo.Column.DisplayIndex;
                selY = weekBar.Items.IndexOf(cellInfo.Item);

            }

        }

        

        private void btnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            if (listboxFilter.SelectedItems.Count < 0) return;
            if (selY < 0 || selX < 0) return;
            Student student = listboxFilter.SelectedItems[0] as Student;
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            if (selY >= weekItems.Count) return;
            weekItems[selY].SetFromIndex(selX, student.Name);
            weekBar.ItemsSource = null;
            weekBar.ItemsSource = weekItems;
        }

        private void btnGenTable_Click(object sender, RoutedEventArgs e)
        {
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            int height = CalculateHeight();
            for (int i=weekItems.Count;i<height;i++)
            {
                try
                {
                    TimeItem timeItem = GetTimeItemFromHeight(i);
                    EventItem eventItem = GetEventItemFromHeight(i);
                    if (timeItem == null)
                    {
                        timeItem = new TimeItem()
                        {
                            Filter = string.Empty
                        };
                    }
                    if (eventItem == null) 
                    {
                        eventItem = new EventItem()
                        {
                            Filter = string.Empty
                        };
                    }
                    WeekItem weekItem = new WeekItem()
                    {
                        Filter = timeItem.Filter + " " + eventItem.Filter
                    };
                    //weekItems[i] = weekItem;
                    weekItems.Add(weekItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    break;
                }
            }
            weekBar.ItemsSource = null;
            weekBar.ItemsSource = weekItems;
        }

        private void btnClearTable_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("确认删除值周表全部信息吗","提示",MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int height = CalculateHeight();
                List<WeekItem> weekItems=new List<WeekItem>(height);
                weekBar.ItemsSource=null;
                weekBar.ItemsSource = weekItems;
                //if(MessageBox.Show("是否自动补全值周行","提示",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                {
                    btnGenTable_Click(sender, e);
                }
            }
        }

        private void btnFlashFilter_Click(object sender, RoutedEventArgs e)
        {
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            int count = weekItems.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    TimeItem timeItem = GetTimeItemFromHeight(i);
                    EventItem eventItem = GetEventItemFromHeight(i);
                    if (timeItem == null)
                    {
                        timeItem = new TimeItem()
                        {
                            Filter = string.Empty
                        };
                    }
                    if (eventItem == null)
                    {
                        eventItem = new EventItem()
                        {
                            Filter = string.Empty
                        };
                    }
                    weekItems[i].Filter = timeItem.Filter + " " + eventItem.Filter;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    break;
                }
            }
            weekBar.ItemsSource = null;
            weekBar.ItemsSource=weekItems;
        }

        private void btnSwapStudent_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnAutoPlan_Click(object sender, RoutedEventArgs e)
        {
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            Student student = new Student();
            List<Student> students;
            Random rand = new Random();
            foreach (WeekItem weekItem in weekItems)
            {
                if (string.IsNullOrWhiteSpace(weekItem.Filter))
                {
                    students = listContent;
                }else
                {
                    students = GetFilteredStudents(weekItem.Filter);
                }
                if (students.Count < 0) continue;
                for(int i=0;i<5;i++)
                {
                    if (!string.IsNullOrEmpty(weekItem.GetFromIndex(i))) continue;
                    student = students[rand.Next(0, students.Count)];
                    weekItem.SetFromIndex(i, student.Name);
                }
            }
            weekBar.ItemsSource = null;
            weekBar.ItemsSource = weekItems;
        }

        private void weekBar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (weekBar.SelectedCells.Count > 0)
                {
                    // 获取第一个选中单元格的行和列索引
                    int rowIndex = weekBar.Items.IndexOf(weekBar.SelectedCells[0].Item);
                    int columnIndex = weekBar.Columns.IndexOf(weekBar.SelectedCells[0].Column);

                    // 将该单元格的值设置为null或者空字符串
                    List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
                    weekItems[rowIndex].SetFromIndex(columnIndex, string.Empty);

                    // 取消选中该单元格
                    weekBar.SelectedCells.Clear();

                    // 使DataGrid重新获得焦点，避免删除操作失效
                    weekBar.Focus();

                    weekBar.Items.Refresh();
                }
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (selX < 0 || selY < 0) return;
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            Clipboard.SetText(weekItems[selY].GetFromIndex(selX));
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (selX < 0 || selY < 0) return;
            List<WeekItem> weekItems = (List<WeekItem>)weekBar.ItemsSource;
            string text = Clipboard.GetText();
            weekItems[selY].SetFromIndex(selX, text);
            weekBar.Items.Refresh();
        }

        private void btnCreateTable_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("功能开发中，敬请期待!");
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("靠自己");
        }


        private void weekBar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (weekBar.SelectedCells.Count > 0)
            {
                var cellInfo = weekBar.SelectedCells[0];
                selX = cellInfo.Column.DisplayIndex;
                selY = weekBar.Items.IndexOf(cellInfo.Item);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(MessageBox.Show("确定打开文件吗?未保存的数据将丢失!","警告",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes)
                SmartOpenFile(files[0]);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("是否保存所作更改?", "Planner", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                btnSave_Click(sender, null);
            }
            else if (messageBoxResult == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        public int SmartOpenFile(string filename)
        {
            switch (Path.GetExtension(filename).ToLower())
            {
                case ".ppj":
                    OpenPPJ(filename);
                    break;
                case ".pd":
                    OepnPD(filename);
                    break;
                case ".pt":
                    OpenPT(filename);
                    break;
                default:
                    MessageBox.Show("不支持的文件类型!", "Planner");
                    return -1;
            }
            return 0;
        }
    }
}

