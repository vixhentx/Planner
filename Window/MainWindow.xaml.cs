using Microsoft.Win32;
using Planner.Classes;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;
using System.IO;
using System.Threading;
using System.Data;
using Microsoft.Office.Interop.Excel;
using Application = System.Windows.Application;
using System.Threading.Tasks;
using Action = System.Action;
using Planner.Window;
using System.Linq;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text;
using TextBox = System.Windows.Controls.TextBox;
using System.Configuration;

namespace Planner
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        #region 窗口事件
        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (UndoStack.Count<=0||MessageBox.Show("确定打开文件吗?未保存的数据将丢失!", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    //变为加载状态
                    mainPanel.Opacity = 0.6;
                    loadingCircle.Visibility = Visibility.Visible;

                    await Task.Run(() =>
                    {
                        SmartOpenFile(files[0]);
                    });

                    //结束加载状态
                    mainPanel.Opacity = 1.0;
                    loadingCircle.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = UndoStack.Count > 0 ? MessageBox.Show("是否保存所作更改?", "Planner", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) : MessageBoxResult.No;
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                ApplicationCommands.Save.Execute(null, null);
            }
            else if (messageBoxResult == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            //保存一些设置
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["锁定时间事件视图"].Value = checkboxTEViewLock.IsChecked.ToString();
            config.AppSettings.Settings["锁定学生视图"].Value = btnLockView.IsChecked.ToString();
            config.AppSettings.Settings["显示学生栏"].Value = btnHideLeft.IsChecked.ToString();
            config.AppSettings.Settings["显示设置栏"].Value = btnHideRight.IsChecked.ToString();
            config.AppSettings.Settings["学生栏只读"].Value = btnDisableEdit.IsChecked.ToString();
            config.AppSettings.Settings["字号"].Value = textFontSize.Value.ToString();
            config.AppSettings.Settings["行高"].Value = textCellHeight.Value.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        #endregion
        #region 菜单栏按钮
       
        private async void btnData_Click(object sender, RoutedEventArgs e)
        {
            DataWindow dataWindow = new DataWindow()
            {
                Owner = this
            };
            dataWindow.ShowDialog();
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                foreach (Student student in dataWindow.Students)
                {
                    listContent.Add(student);
                }
                listboxFilter.Items.Refresh();

            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
        }

        public void OpenPT(string path)
        {
            JsonHelper.SaveTemplate saveTemplate = JsonHelper.ReadPT(path);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                timeBar.ItemsSource = null;
                eventBar.ItemsSource = null;
                timeBar.ItemsSource = saveTemplate.timeItems;
                eventBar.ItemsSource = saveTemplate.eventItems;
            }));
        }

        public void OepnPD(string path)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {

                listboxFilter.ItemsSource = null;
                students = JsonHelper.ReadPD(path);
            }));


        }

        public void OpenPPJ(string path)
        {
            JsonHelper.SaveHelper saveHelper = JsonHelper.ReadPPJ(path);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                timeBar.ItemsSource = null;
                eventBar.ItemsSource = null;
                weekBar.ItemsSource = null;
                listboxFilter.ItemsSource = null;

                students = saveHelper.students;
                timeBar.ItemsSource = saveHelper.template.timeItems;
                eventBar.ItemsSource = saveHelper.template.eventItems;
                weekBar.ItemsSource = saveHelper.weekItems;
            }));
            filename = path;
            UndoStack.Clear();
            RedoStack.Clear();
        }



        private async void btnCreateTable_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("功能开发中，敬请期待!");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "Excel表格(*.xlsx)|*.xlsx",
                OverwritePrompt = true,
                CreatePrompt = true
            };
            saveFileDialog.ShowDialog();
            if (string.IsNullOrEmpty(saveFileDialog.FileName)) return;

            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;
            await Task.Run( () =>
            {
                try
                {
                    Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application()
                    {
                        Visible = false,
                        AlertBeforeOverwriting = false,
                        DisplayAlerts = false
                    };
                    excel.Workbooks.Add();

                    // 获取当前工作簿
                    Worksheet worksheet = (Worksheet)excel.ActiveSheet;

                    // 将表头写入Excel
                    string[] headers = { "时间", "事件", "周一", "周二", "周三", "周四", "周五" };
                    for (int i = 1; i <= headers.Length; i++)
                    {
                        worksheet.Cells[1, i] = headers[i - 1];
                    }
                    int nowHeight = 2;
                    //将TimeBar写入Excel
                    for (int i = 0; i < timeItems.Count; i++)
                    {
                        VBarItem timeItem = timeItems[i];
                        worksheet.Cells[nowHeight, 1] = timeItem.Value;
                        Range mergeRange = worksheet.Range[string.Format("A{0}:A{1}", nowHeight, (object)(nowHeight + timeItem.TabHeight - 1))];
                        mergeRange.Merge();
                        nowHeight += timeItem.TabHeight;
                    }
                    nowHeight = 2;
                    //将EventBar写入Excel
                    for (int i = 0; i < eventItems.Count; i++)
                    {
                        VBarItem eventItem = eventItems[i];
                        worksheet.Cells[nowHeight, 2] = eventItem.Value;
                        Range mergeRange = worksheet.Range[string.Format("B{0}:B{1}", nowHeight, (object)(nowHeight + eventItem.TabHeight - 1))];
                        mergeRange.Merge();
                        nowHeight += eventItem.TabHeight;
                    }
                    //将WeekBar写入Excel
                    nowHeight = 2;
                    for (int i = 0; i < weekItems.Count; i++)
                    {
                        WeekItem weekItem = weekItems[i];
                        for (int j = 0; j <= 5; j++)
                        {
                            worksheet.Cells[nowHeight, j + 3] = weekItem.GetFromIndex(j);
                        }
                        nowHeight += 1;
                    }
                    //sign一手
                    worksheet.Cells[nowHeight + 1, 9] = "表格由Planner生成";
                    worksheet.Cells[nowHeight + 1, 11] = "powered by vix_hentx";
                    //写入并关闭
                    worksheet.SaveAs(saveFileDialog.FileName);
                    excel.Quit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;


        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
        private void btnHideLeft_Click(object sender, RoutedEventArgs e)
        {
            if (gridProperty.Visibility == Visibility.Visible) gridProperty.Visibility = Visibility.Collapsed;
            else gridProperty.Visibility = Visibility.Visible;
        }

        private void btnHideRight_Click(object sender, RoutedEventArgs e)
        {
            if (gridPanel.Visibility == Visibility.Visible) gridPanel.Visibility = Visibility.Collapsed;
            else gridPanel.Visibility = Visibility.Visible;
        }
        private void btnDeleteSelBar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (selected)
                {
                    case MergedSelections.TimeBar:
                        DeleteVBarItems(timeBar);
                        break;
                    case MergedSelections.EventBar:
                        DeleteVBarItems(eventBar);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        private void btnResetView_Click(object sender, RoutedEventArgs e)
        {
            LoadDefault();
        }
        #endregion
        #region 学生数据管理
        private void listboxFilter_GotFocus(object sender, RoutedEventArgs e)
        {
            selected = MergedSelections.ListboxFilter;
        }
        private void btnAddData_Click(object sender, RoutedEventArgs e)
        {
            Action redo_action = () =>
            {
                listContent.Add(new Student());
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            Action undo_action = () =>
            {
                listContent.RemoveAt(listContent.Count - 1);
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            /*
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            */
            redo_action();
        }

        private void btnRemoveData_Click(object sender, RoutedEventArgs e)
        {
            StudentPropertyRemove();
        }

        private void StudentPropertyRemove()
        {
            List<ListContentDelta> deltas = new List<ListContentDelta>();
            var sel = listboxFilter.SelectedItems;
            foreach (Student item in sel)
            {
                int _y=listContent.IndexOf(item);
                ListContentDelta delta = new ListContentDelta()
                {
                    y = _y,
                    preId = item.Id,
                    preName = item.Name,
                    preRoom = item.Room,
                    preSex = item.Sex
                };
                deltas.Add(delta);
            }
            Action redo_action = () =>
            {
                foreach (Student item in sel)
                {
                    listContent.Remove(item);
                }
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            Action undo_action = () =>
            {
                foreach (ListContentDelta delta in deltas)
                {
                    Student student = new Student()
                    {
                        Name = delta.preName,
                        Room = delta.preRoom,
                        Sex = delta.preSex,
                        Id = delta.preId,
                    };
                    listContent.Insert(delta.y,student);
                }
                listboxFilter.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            /*
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            */
            redo_action();
        }

        private void textNameFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lastText == string.Empty)
            {
                students = listboxFilter.ItemsSource as List<Student>;
            }
            lastText = textNameFilter.Text;
            listboxFilter.ItemsSource = null;
            listboxFilter.ItemsSource = GetFilteredStudents(textNameFilter.Text);
        }

        private List<Student> GetFilteredStudents(string filterText)
        {
            List<Student> ret = new List<Student>();
            string[] sProps = filterText.Split(' ');
            foreach (Student student in listContent)
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
            StudentPropertyCopy();
        }

        private void StudentPropertyCopy()
        {
            var sel = listboxFilter.SelectedItems;
            string ret = string.Empty;
            foreach (Student item in sel)
            {
                ret += item.toString();
            }
            Clipboard.SetDataObject(ret);
            listboxFilter.Focus();
        }

        private void btnMoveData_Click(object sender, RoutedEventArgs e)
        {
            StudentPropertyMove();
        }

        private void StudentPropertyMove()
        {
            var sel = listboxFilter.SelectedItems;
            string ret = string.Empty;
            foreach (Student item in sel)//其实就是先复制再删除
            {
                ret += item.toString();
            }
            Clipboard.SetDataObject(ret);
            StudentPropertyRemove();
        }

        private void btnPasteData_Click(object sender, RoutedEventArgs e)
        {
            StudentPropertyPaste();
        }

        private void StudentPropertyPaste()
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
            Action redo_action = () =>
            {
                foreach (string data in datas)
                {
                    Student student = Student.fromString(data);
                    if (student != null)
                    {
                        listContent.Add(student);
                    }
                }
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            Action undo_action = () =>
            {
                listContent.RemoveRange(listContent.Count-datas.Length, listboxFilter.Items.Count);
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            /*
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            */
            redo_action();
            
        }

        private void btnClearData_Click(object sender, RoutedEventArgs e)
        {
            StudentPropertyClear();
        }

        private void StudentPropertyClear()
        {
            if (MessageBox.Show("您确定要删除所有行吗?", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            List<Student> preStudents = new List<Student>(listContent);
            Action redo_action = () =>
            {
                students.Clear();
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            Action undo_action = () =>
            {
                students = preStudents;
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            /*
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            */
            redo_action();

        }

        private void btnClearEmpty_Click(object sender, RoutedEventArgs e)
        {
            StudentPropertyClearEmpty();
        }

        private void StudentPropertyClearEmpty()
        {
            List<Student> ret = new List<Student>();
            foreach (Student student in listContent)
            {
                if (!string.IsNullOrWhiteSpace(student.Name + student.Room + student.Sex + student.Id))//取并集为空就是全为空
                {
                    ret.Add(student);
                }
            }
            List<Student> preListContent = new List<Student>(listContent);
            Action redo_action = () =>
            {
                listContent = ret;
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            Action undo_action = () =>
            {
                listContent = preListContent;
                listboxFilter.Items.Refresh();
                listboxFilter.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            /*
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            */
            redo_action();
            
        }

        private void listboxFilter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(StudentPropertyConfirm), DispatcherPriority.Background);
            weekBar.Focus();
        }

        private void StudentPropertyConfirm()
        {
            if (listboxFilter.SelectedItems.Count < 0) return;
            if (selY < 0 || selX < 0) return;
            Student student = listboxFilter.SelectedItems[0] as Student;
            if (selY >= weekItems.Count) return;
            //Undo&Redo

            weekItemDelta delta = new weekItemDelta()
            {
                x = selX,
                y = selY,
                preContent = weekItems[selY].GetFromIndex(selX),
                curContent = student.Name
            };
            weekBarEdit(delta);
        }

        private void btnDisableEdit_Click(object sender, RoutedEventArgs e)
        {
            listboxFilter.IsReadOnly = !listboxFilter.IsReadOnly;
        }
        private void btnLockView_Click(object sender, RoutedEventArgs e)
        {
            only_view_filtered = !only_view_filtered;
        }
        #endregion
        #region 属性管理
        private void btnAddSelBar_Click(object sender, RoutedEventArgs e)
        {
            VBarItem VBarItem = new VBarItem()
            {
                Value = textTabValue.Text,
                TabHeight = (int)(textTabHeight.Value < 1 ? 1 : textTabHeight.Value),
                Filter = textTabFilter.Text
            };
            if (selected == MergedSelections.TimeBar)
            {
                AddVBarItem(VBarItem,timeBar);
                timeBar.Focus();
            }
            else if (selected == MergedSelections.EventBar)
            {
                AddVBarItem(VBarItem,eventBar);
                eventBar.Focus();
            }
        }

        private void btnInsSelBar_Click(object sender, RoutedEventArgs e)
        {
            VBarItem VBarItem = new VBarItem()
            {
                Value = textTabValue.Text,
                TabHeight = (int)(textTabHeight.Value < 1 ? 1 : textTabHeight.Value),
                Filter = textTabFilter.Text
            };
            if (selected == MergedSelections.TimeBar)
            {
                AddVBarItem(VBarItem, timeBar);
                timeBar.Focus();
            }
            else if (selected == MergedSelections.EventBar)
            {
                AddVBarItem(VBarItem, eventBar);
                eventBar.Focus();
            }
        }
        private void btnConfirmPrep_Click(object sender, RoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.No:
                    return;
                case MergedSelections.TimeBar:
                    VBarConfirmPrep(timeBar);
                    break;
                case MergedSelections.EventBar:
                    VBarConfirmPrep(eventBar);
                    break;
            }
        }

        

        private void VBar_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataGrid VBar = sender as DataGrid;
                if (VBar == null) return;
                int _y = e.Row.GetIndex();
                VItemDelta delta = new VItemDelta()
                {
                    y = _y,
                    curValue = ((TextBox)e.EditingElement).Text,
                    preValue = ((List<VBarItem>)VBar.ItemsSource)[_y].Value,
                    curTabHeight = ((List<VBarItem>)VBar.ItemsSource)[_y].TabHeight,
                    preTabHeight = ((List<VBarItem>)VBar.ItemsSource)[_y].TabHeight,
                    curFilter = ((List<VBarItem>)VBar.ItemsSource)[_y].Filter,
                    preFilter = ((List<VBarItem>)VBar.ItemsSource)[_y].Filter
                };
                if (delta.curValue == delta.preValue) return;
                VBarEdit(delta,VBar);
            }
        }

        private void VBarEdit(VItemDelta delta,DataGrid VBar)
        {
            Action redo_action = () =>
            {
                ChangeVBarContent(delta.y, delta.curValue, VBar);
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].TabHeight = delta.curTabHeight;
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].Value = delta.curValue;
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].Filter = delta.curFilter;
            };
            Action undo_action = () =>
            {
                ChangeVBarContent(delta.y, delta.preValue, VBar);
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].TabHeight = delta.preTabHeight;
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].Value = delta.preValue;
                ((List<VBarItem>)VBar.ItemsSource)[delta.y].Filter = delta.preFilter;
            };
            ActionPair pair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(pair);
            RedoStack.Clear();
            redo_action();
        }
        public void ChangeVBarContent(int y,string content,DataGrid VBar)
        {
            //即时更改
            DataGridCellInfo cellInfo = new DataGridCellInfo(VBar.Items[y], VBar.Columns[0]);
            DataGridCell cell = cellInfo.Column.GetCellContent(cellInfo.Item).Parent as DataGridCell;
            TextBlock textBlock = cell.Content as TextBlock;
            if (textBlock != null)
                textBlock.Text = content;
        }
        #endregion
        #region 站岗管理
        private void btnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            if (listboxFilter.SelectedItems.Count <= 0) return;
            if (selY < 0 || selX < 0) return;
            List<weekItemDelta> deltas=new List<weekItemDelta>();
            for (int i = 0; i < listboxFilter.SelectedItems.Count; i++)
            {
                Student student = listboxFilter.SelectedItems[i] as Student;
                if (selY + i >= weekItems.Count) break;//使用return:第一个超出范围则后面都超出范围，进行剪枝处理
                weekItemDelta delta = new weekItemDelta()
                { 
                    x = selX,
                    y = selY + i,
                    curContent = student.Name,
                    preContent = weekItems[selY + i].GetFromIndex(selX)
                };
                deltas.Add(delta);
            }
            weekBarEdit(deltas);
            weekBar.Focus();
        }


        private async void btnGenTable_Click(object sender, RoutedEventArgs e)
        {
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run((Action)(() =>
            {


                int height = CalculateHeight();
                for (int i = weekItems.Count; i < height; i++)
                {
                    try
                    {
                        VBarItem timeItem = GetTimeItemFromHeight(i);
                        VBarItem eventItem = GetEventItemFromHeight(i);
                        if (timeItem == null)
                        {
                            timeItem = new VBarItem()
                            {
                                Filter = string.Empty
                            };
                        }
                        if (eventItem == null)
                        {
                            eventItem = new VBarItem()
                            {
                                Filter = string.Empty
                            };
                        }
                        WeekItem weekItem = new WeekItem()
                        {
                            Filter = timeItem.Filter + " " + eventItem.Filter
                        };
                        weekItems.Add(weekItem);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        break;
                    }
                }
            }));
            weekBar.Items.Refresh();
            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;

        }



        private void btnClearTable_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确认删除值周表全部信息吗", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Action redo_action = () =>
                {
                    int height = CalculateHeight();
                    List<WeekItem> weekItems = new List<WeekItem>(height);
                    weekBar.ItemsSource = null;
                    weekBar.ItemsSource = weekItems;
                    {
                        btnGenTable_Click(sender, e);
                    }
                };
                List<WeekItem> preWeekItems=new List<WeekItem>(weekItems);
                Action undo_action = () =>
                {
                    weekBar.ItemsSource = null;
                    weekBar.ItemsSource = preWeekItems;
                    weekBar.SelectAllCells();
                };
                ActionPair actionPair = new ActionPair()
                {
                    RedoAction = redo_action,
                    UndoAction = undo_action
                };
                UndoStack.Push(actionPair);
                RedoStack.Clear();
                redo_action();
            }
        }

        private async void btnFlashFilter_Click(object sender, RoutedEventArgs e)
        {
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                FillFilters();
            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 填充[l,r)的限制条件
        /// </summary>
        /// <param name="l">区间左端点</param>
        /// <param name="r">区间右断点,-1表示weekItems.Count</param>
        private void FillFilters(int l = 0, int r = -1)
        {
            if (r < 0) r = weekItems.Count;
            for (int i = l; i < r; i++)//经典不带优化的O(n)
            {
                try
                {
                    VBarItem timeItem = GetTimeItemFromHeight(i);
                    VBarItem eventItem = GetEventItemFromHeight(i);
                    if (timeItem == null)
                    {
                        timeItem = new VBarItem()
                        {
                            Filter = string.Empty
                        };
                    }
                    if (eventItem == null)
                    {
                        eventItem = new VBarItem()
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
        }

        private void btnSwapStudent_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void btnAutoPlan_Click(object sender, RoutedEventArgs e)
        {

            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;
            List<weekItemDelta> deltas = new List<weekItemDelta>();
            await Task.Run(() =>
            {
                FillFilters();
                weekItemDelta delta;
                List<Student> students;
                WeekItem weekItem;
                Random rand = new Random();
                int tHeight = timeItems.Count;
                int p = 0;
                for (int i = 0; i < 5; i++)//竖着排
                {
                    p = 0;
                    for (int j = 0; j < tHeight; j++)
                    {
                        weekItem = weekItems[p];
                        if (string.IsNullOrWhiteSpace(weekItem.Filter))
                        {
                            students = new List<Student>(listContent);
                        }
                        else
                        {
                            students = GetFilteredStudents(weekItem.Filter);
                        }
                        if (students.Count <= 0) continue;
                        for (int k = p; k < p + timeItems[j].TabHeight && students.Count > 0; k++)
                        {
                            if (!string.IsNullOrWhiteSpace(weekItems[k].GetFromIndex(i))) continue;
                            int r = rand.Next(students.Count);
                            delta = new weekItemDelta()
                            {
                                x = i,
                                y = k,
                                preContent = weekItems[k].GetFromIndex(i),
                                curContent = students[r].Name
                            };//预存修改
                            deltas.Add(delta);
                            students.RemoveAt(r);//模拟不放回抽取
                        }
                        p += timeItems[j].TabHeight;
                    }
                }
            });
            //weekBar.Items.Refresh();
            weekBarEdit(deltas);
            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
            weekBar.Focus();
        }
        private void weekBar_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                int _x = e.Column.DisplayIndex,
                    _y = e.Row.GetIndex();
                weekItemDelta delta = new weekItemDelta()
                {
                    x = _x,
                    y = _y,
                    preContent = weekItems[_y].GetFromIndex(_x),
                    curContent = ((TextBox)e.EditingElement).Text
                };
                if (delta.curContent == delta.preContent) return;
                //weekBarEdit(delta); //不需要进行一次redo
                Action undoAction = () =>
                {
                    weekItems[delta.y].SetFromIndex(delta.x, delta.preContent);
                    ChangeWeekCellContent(delta.x, delta.y, delta.preContent);
                };

                Action redoAction = () =>
                {
                    weekItems[delta.y].SetFromIndex(delta.x, delta.curContent);
                    ChangeWeekCellContent(delta.x, delta.y, delta.curContent);
                };
                ActionPair actionPair = new ActionPair()
                {
                    UndoAction = undoAction,
                    RedoAction = redoAction
                };
                UndoStack.Push(actionPair);
                RedoStack.Clear();
            }
        }
        #endregion
        #region 3个datagrid
        private void AddVBarItem(VBarItem item, DataGrid VBar)
        {
            Action redo_action = () =>
            {
                ((List<VBarItem>)VBar.ItemsSource).Add(item);
                FillFilters();
                VBar.Items.Refresh();
                VBar.SelectedItems.Clear();
                VBar.SelectedItems.Add(item);
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                ((List<VBarItem>)VBar.ItemsSource).RemoveAt(((List<VBarItem>)VBar.ItemsSource).Count-1);
                FillFilters();
                VBar.Items.Refresh();
                VBar.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            redo_action();
        }
        private void InsertVBarItem(VBarItem item, DataGrid VBar, bool refresh = true)
        {
            Action redo_action = () =>
            {
                if (VBar.SelectedIndex < 0)
                {
                    ((List<VBarItem>)VBar.ItemsSource).Add(item);
                    FillFilters();
                }
                else
                {
                    ((List<VBarItem>)VBar.ItemsSource).Insert(VBar.SelectedIndex + 1, item);
                    FillFilters();
                }
                if (refresh) VBar.Items.Refresh();
                VBar.SelectedItems.Clear();
                VBar.SelectedItems.Add(item);
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                if (VBar.SelectedIndex < 0)
                {
                    ((List<VBarItem>)VBar.ItemsSource).RemoveAt(((List<VBarItem>)VBar.ItemsSource).Count - 1);
                    FillFilters();
                }
                else
                {
                    ((List<VBarItem>)VBar.ItemsSource).RemoveAt(VBar.SelectedIndex + 1);
                    FillFilters();
                }
                if (refresh) VBar.Items.Refresh();
                VBar.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            redo_action();
        }
        private void AddVBarItems(List<VBarItem> items, DataGrid VBar)
        {
            Action redo_action = () =>
            {
                ((List<VBarItem>)VBar.ItemsSource).AddRange(items);
                FillFilters();
                VBar.Items.Refresh();
                VBar.SelectedItems.Clear();
                VBar.SelectedItems.Add(items);
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                ((List<VBarItem>)VBar.ItemsSource).RemoveRange(((List<VBarItem>)VBar.ItemsSource).Count- items.Count, items.Count);
                FillFilters();
                VBar.Items.Refresh();
                VBar.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            redo_action();
        }
        private void InsertVBarItems(List<VBarItem> items, DataGrid VBar, bool refresh = true)
        {
            Action redo_action = () =>
            {
                if (VBar.SelectedIndex < 0)
                {
                    ((List<VBarItem>)VBar.ItemsSource).AddRange(items);
                    FillFilters();
                }
                else
                {
                    ((List<VBarItem>)VBar.ItemsSource).InsertRange(VBar.SelectedIndex + 1, items);
                    FillFilters();
                }
                if (refresh) VBar.Items.Refresh();
                VBar.SelectedItems.Clear();
                VBar.SelectedItems.Add(items);
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                if (VBar.SelectedIndex < 0)
                {
                    ((List<VBarItem>)VBar.ItemsSource).RemoveRange(((List<VBarItem>)VBar.ItemsSource).Count-items.Count,items.Count);
                    FillFilters();
                }
                else
                {
                    ((List<VBarItem>)VBar.ItemsSource).RemoveRange(VBar.SelectedIndex + 1, items.Count);
                    FillFilters();
                }
                if (refresh) VBar.Items.Refresh();
                VBar.Focus();
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            redo_action();
        }
        private void DeleteVBarItems(DataGrid VBar)
        {
            if (VBar.SelectedItems.Count <= 0) return;
            List<VItemDelta> deltas = new List<VItemDelta>();
            foreach (var item in VBar.SelectedItems)
            {
                int _y = VBar.Items.IndexOf(item);
                deltas.Add(new VItemDelta()
                {
                    y = _y,
                    preValue = ((List<VBarItem>)VBar.ItemsSource)[_y].Value,
                    preTabHeight = ((List<VBarItem>)VBar.ItemsSource)[_y].TabHeight,
                    preFilter = ((List<VBarItem>)VBar.ItemsSource)[_y].Filter
                });
            }
            Action redo_action = () =>
            {
                foreach (VItemDelta delta in deltas)
                {
                    ((List<VBarItem>)VBar.ItemsSource).RemoveAt(delta.y);
                }
                FillFilters();
                if (((List<VBarItem>)VBar.ItemsSource).Count <= 0) ((List<VBarItem>)VBar.ItemsSource).Add(new VBarItem());//防删空
                VBar.Items.Refresh();
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                foreach (VItemDelta delta in deltas)
                {
                    VBarItem item = new VBarItem()
                    {
                        Filter = delta.preFilter,
                        Value = delta.preValue,
                        TabHeight = delta.preTabHeight
                    };
                    ((List<VBarItem>)VBar.ItemsSource).Insert(delta.y, item);
                }
                FillFilters();
                VBar.Items.Refresh();
                VBar.SelectedItems.Clear();
                foreach (VItemDelta delta in deltas)
                {
                    VBar.SelectedItems.Add(VBar.Items[delta.y]);
                }
                VBar.Focus();
            };
            redo_action();
            RedoStack.Clear();
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
        }
        private void CopyVBarItems(DataGrid VBar)
        {
            try
            {
                List<VBarItem> VBarItems = VBar.SelectedItems.OfType<VBarItem>().ToList();
                if (VBarItems.Count <= 0) return;
                JsonHelper.PutVBarItems(VBarItems);
                VBar.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void PasteVBarItems(DataGrid VBar)
        {
            try
            {
                List<VBarItem> VBarItems = JsonHelper.GetVBarItems();
                if (VBarItems == null || VBarItems.Count <= 0) return;
                InsertVBarItems(VBarItems, VBar);//已包含撤回
                VBar.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void VBarConfirmPrep(DataGrid VBar)
        {
            List<VItemDelta> deltas=new List<VItemDelta>();
            var selectedItems= VBar.SelectedItems;
            var VBarItems = (List<VBarItem>)VBar.ItemsSource;
            foreach (VBarItem item in selectedItems)
            {
                VItemDelta delta = new VItemDelta()
                {
                    y=VBar.Items.IndexOf(item),
                    preFilter = item.Filter,
                    preTabHeight = item.TabHeight,
                    preValue = item.Value,
                    curFilter = textTabFilter.Text,
                    curValue = textTabValue.Text,
                    curTabHeight = (int)textTabHeight.Value
                };
                deltas.Add(delta);
            }
            Action redo_action = () =>
            {
                foreach(VItemDelta delta in deltas)
                {
                    VBarItems[delta.y] = new VBarItem()
                    {
                        Value=delta.curValue,
                        Filter=delta.curFilter,
                        TabHeight=delta.curTabHeight
                    };
                }
                //虽然效率比较傻逼,但是,什么年代的电脑了O(n)算法1秒跑不完?
                FillFilters();
                VBar.Items.Refresh();
                VBar.Focus();
            };
            Action undo_action = () =>
            {
                foreach (VItemDelta delta in deltas)
                {
                    VBarItems[delta.y] = new VBarItem()
                    {
                        Value = delta.preValue,
                        Filter = delta.preFilter,
                        TabHeight = delta.preTabHeight
                    };
                }
                //虽然效率比较傻逼,但是,什么年代的电脑了O(n)算法1秒跑不完?
                FillFilters();
                VBar.Items.Refresh();
                VBar.Focus();
            };

            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undo_action,
                RedoAction = redo_action
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            redo_action();
        }
        private void TimeBar_Focused()
        {
            Dispatcher.BeginInvoke(new Action(() =>//使用委托来延迟执行，保证获取到选中项
            {
                selected = MergedSelections.TimeBar;
                groupbox_TimeEvent.IsEnabled = true;
                eventBar.SelectedIndex = -1;
                eventBar.SelectedItem = null;
                VBarItem timeItem = (VBarItem)timeBar.SelectedItem;
                if (timeItem == null) return;
                if (!lockedTEView)
                {
                    textTabValue.Text = timeItem.Value;
                    textTabHeight.Value = timeItem.TabHeight;
                    textTabFilter.Text = timeItem.Filter.ToString();
                }
            }), DispatcherPriority.Background);
        }


        private void EventBar_Focused()
        {
            Dispatcher.BeginInvoke(new Action(() =>//使用委托来延迟执行，保证获取到选中项
            {
                selected = MergedSelections.EventBar;
                groupbox_TimeEvent.IsEnabled = true;
                timeBar.SelectedIndex = -1;
                timeBar.SelectedItem = null;
                VBarItem eventItem = (VBarItem)eventBar.SelectedItem;
                if (eventItem == null) return;
                if (!lockedTEView)
                {
                    textTabValue.Text = eventItem.Value;
                    textTabHeight.Value = eventItem.TabHeight;
                    textTabFilter.Text = eventItem.Filter.ToString();
                }
            }), DispatcherPriority.Background);
        }
        private void weekBar_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            WeekBar_Focused();
        }
        private void weekBar_LostFocus(object sender, RoutedEventArgs e)
        {
            WeekBar_Focused();
        }
        private void WeekBar_Focused()
        {
            if (weekBar.SelectedCells.Count > 0)
            {
                selected = MergedSelections.WeekBar;
                groupbox_TimeEvent.IsEnabled = false;//选中WeekBar后禁止编辑Time/Event的属性
                var cellInfo = weekBar.SelectedCells[0];
                selX = cellInfo.Column.DisplayIndex;
                selY = weekBar.Items.IndexOf(cellInfo.Item);
                if (only_view_filtered == true)
                {
                    listboxFilter.ItemsSource = null;
                    listboxFilter.ItemsSource = GetFilteredStudents(weekItems[selY].Filter);
                }
            }
        }
        private void ChangeWeekCellContent(int x, int y, string content)
        {
            //即时更改
            DataGridCellInfo cellInfo = new DataGridCellInfo(weekBar.Items[y], weekBar.Columns[x]);
            DataGridCell cell = cellInfo.Column.GetCellContent(cellInfo.Item).Parent as DataGridCell;
            TextBlock textBlock = cell.Content as TextBlock;
            if (textBlock != null)
                textBlock.Text = content;
        }
        private void timeBar_GotFocus(object sender, RoutedEventArgs e)
        {
            TimeBar_Focused();
        }

        private void eventBar_GotFocus(object sender, RoutedEventArgs e)
        {
            EventBar_Focused();
        }
        private void DeleteWeekCells()
        {
            List<DataGridCellInfo> selectedCells = new List<DataGridCellInfo>(weekBar.SelectedCells);
            List<weekItemDelta> deltas=new List<weekItemDelta>();
            // 逐个删除选中的单元格的数据
            int _x, _y;
            foreach (DataGridCellInfo cellInfo in selectedCells)
            {
                _x = cellInfo.Column.DisplayIndex;
                _y = weekBar.Items.IndexOf(cellInfo.Item);
                weekItemDelta delta = new weekItemDelta()
                {
                    x = _x,
                    y = _y,
                    preContent = weekItems[_y].GetFromIndex(_x),
                    curContent = null
                };
                deltas.Add(delta);
            }
            weekBarEdit(deltas);
        }
        private void CopyWeekCells()
        {
            if (weekBar.SelectedCells.Count <= 0) return;
            int _x, _y, x0, y0;
            var cellInfos = weekBar.SelectedCells;
            x0 = cellInfos[0].Column.DisplayIndex;
            y0 = weekBar.Items.IndexOf(cellInfos[0].Item);
            List<JsonHelper.WeekItemMeta> metas = new List<JsonHelper.WeekItemMeta>();
            foreach (var cellInfo in cellInfos)
            {
                _x = cellInfo.Column.DisplayIndex;
                _y = weekBar.Items.IndexOf(cellInfo.Item);
                JsonHelper.WeekItemMeta meta = new JsonHelper.WeekItemMeta()
                {
                    x = _x - x0,//相对位置以压缩状态
                    y = _y - y0,
                    content = weekItems[_y].GetFromIndex(_x)
                };
                metas.Add(meta);
            }
            JsonHelper.PutWeekItems(metas);
        }
        private void PasteWeekCells()
        {
            if (weekBar.SelectedCells.Count <= 0) return;
            try
            {
                int _x, _y, x0, y0;
                var cellInfos = weekBar.SelectedCells;
                x0 = cellInfos[0].Column.DisplayIndex;
                y0 = weekBar.Items.IndexOf(cellInfos[0].Item);
                List<JsonHelper.WeekItemMeta> metas = JsonHelper.GetWeekItems();
                List<weekItemDelta> deltas = new List<weekItemDelta>();
                foreach (var meta in metas)
                {
                    _x = meta.x;
                    _y = meta.y;
                    if (x0 + _x >= 5 || x0 + _x < 0 || y0 + _y >= weekItems.Count || y0 + _y < 0) continue;//跳过超出范围的
                    weekItemDelta delta = new weekItemDelta()
                    {
                        x = x0 + _x,
                        y = y0 + _y,
                        preContent = weekItems[y0 + _y].GetFromIndex(x0 + _x),
                        curContent = meta.content
                    };
                    deltas.Add(delta);
                }
                weekBarEdit(deltas);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void timeBar_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            selected = MergedSelections.TimeBar;
            eventBar.SelectedIndex = -1;
            eventBar.SelectedItem = null;
            VBarItem timeItem = (VBarItem)timeBar.SelectedItem;
            if (timeItem == null) return;
            textTabHeight.Value = timeItem.TabHeight;
            textTabFilter.Text = timeItem.Filter.ToString();
        }
        private void ScrollMainPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scrollSpeed = (double)Application.Current.Resources["CellHeight"] + 4;
            double scrollAmount = e.Delta > 0 ? -scrollSpeed : scrollSpeed;
            scrollMainPanel.ScrollToVerticalOffset(scrollMainPanel.VerticalOffset + scrollAmount);

            // 标记事件已处理，防止事件继续冒泡
            e.Handled = true;
        }
        #endregion
        #region 显示设置
        private void btnDisplayApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Resources["CellHeight"] = textCellHeight.Value;
                Application.Current.Resources["FontSize"] = textFontSize.Value;
                timeBar.Items.Refresh();
                eventBar.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion
        #region 命令
        public class ActionPair
        {
            public Action UndoAction, RedoAction;
            public ActionPair GetReverse => new ActionPair() { UndoAction = RedoAction, RedoAction = UndoAction };
        }

        Stack<ActionPair> UndoStack=new Stack<ActionPair>(), RedoStack=new Stack<ActionPair>();
        
        //可撤销的操作
        public void weekBarEdit(weekItemDelta delta)
        {
            Action undoAction = () =>
            {
                weekItems[delta.y].SetFromIndex(delta.x, delta.preContent);
                ChangeWeekCellContent(delta.x, delta.y, delta.preContent);
            };

            Action redoAction = () =>
            {
                weekItems[delta.y].SetFromIndex(delta.x, delta.curContent);
                ChangeWeekCellContent(delta.x, delta.y, delta.curContent);
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undoAction,
                RedoAction = redoAction
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            // 执行操作
            redoAction();
        }
        public void weekBarEdit(List<weekItemDelta> deltas)
        {
            Action undoAction = () =>
            {
                weekBar.SelectedCells.Clear();
                foreach (weekItemDelta delta in deltas)
                {
                    weekItems[delta.y].SetFromIndex(delta.x, delta.preContent);
                    ChangeWeekCellContent(delta.x, delta.y, delta.preContent);
                    //选中修改的内容
                    var info = new DataGridCellInfo(weekBar.Items[delta.y], weekBar.Columns[delta.x]);
                    weekBar.SelectedCells.Add(info);
                }
                
            };

            Action redoAction = () =>
            {
                weekBar.SelectedCells.Clear();
                foreach (weekItemDelta delta in deltas)
                {
                    weekItems[delta.y].SetFromIndex(delta.x, delta.curContent);
                    ChangeWeekCellContent(delta.x, delta.y, delta.curContent);
                    //选中修改的内容
                    var info = new DataGridCellInfo(weekBar.Items[delta.y], weekBar.Columns[delta.x]);
                    weekBar.SelectedCells.Add(info);
                }
            };
            ActionPair actionPair = new ActionPair()
            {
                UndoAction = undoAction,
                RedoAction = redoAction
            };
            UndoStack.Push(actionPair);
            RedoStack.Clear();
            // 执行操作
            redoAction();
        }
        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.ListboxFilter:
                    StudentPropertyCopy();
                    break;
                case MergedSelections.TimeBar:
                    CopyVBarItems(timeBar);
                    break;
                case MergedSelections.EventBar:
                    CopyVBarItems(eventBar);
                    break;
                case MergedSelections.WeekBar:
                    CopyWeekCells();
                    break;
            }
        }



        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.ListboxFilter:
                    StudentPropertyCopy();
                    StudentPropertyRemove();
                    break;
                case MergedSelections.TimeBar:
                    CopyVBarItems(timeBar);
                    DeleteVBarItems(timeBar);
                    break;
                case MergedSelections.EventBar:
                    CopyVBarItems(eventBar);
                    DeleteVBarItems(eventBar);
                    break;
                case MergedSelections.WeekBar:
                    CopyWeekCells();
                    DeleteWeekCells();
                    break;
            }
        }



        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.ListboxFilter:
                    StudentPropertyPaste();
                    break;
                case MergedSelections.TimeBar:
                    PasteVBarItems(timeBar);
                    break;
                case MergedSelections.EventBar:
                    PasteVBarItems(eventBar);
                    break;
                case MergedSelections.WeekBar:
                    PasteWeekCells();
                    break;
            }
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.ListboxFilter:
                    StudentPropertyRemove();
                    break;
                case MergedSelections.TimeBar:
                    DeleteVBarItems(timeBar);
                    break;
                case MergedSelections.EventBar:
                    DeleteVBarItems(eventBar);
                    break;
                case MergedSelections.WeekBar:
                    DeleteWeekCells();
                    break;
            }
        }
        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (selected)
            {
                case MergedSelections.ListboxFilter:
                    listboxFilter.SelectAll();
                    break;
                case MergedSelections.TimeBar:
                    timeBar.SelectAll(); 
                    break;
                case MergedSelections.EventBar:
                    eventBar.SelectAll();
                    break;
                case MergedSelections.WeekBar:
                    weekBar.SelectAllCells();
                    break;
            }
        }
        private async void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (filename == null || filename == string.Empty)
            {
                var saveas = ApplicationCommands.SaveAs;
                if (saveas.CanExecute(null, null))
                    saveas.Execute(null, null); return;
            }
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                JsonHelper.SavePPJ(filename, new JsonHelper.SaveHelper()
                {
                    students = students,
                    template = new JsonHelper.SaveTemplate()
                    {
                        eventItems = (List<VBarItem>)eventBar.ItemsSource,
                        timeItems = (List<VBarItem>)timeBar.ItemsSource,
                    },
                    weekItems = (List<WeekItem>)weekBar.ItemsSource
                });

            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
        }

        private async void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
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
            filename = path;
            //MessageBox.Show(path);
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                switch (dialog.FilterIndex)
                {
                    case 1:
                        JsonHelper.SavePPJ(path, new JsonHelper.SaveHelper()
                        {
                            students = students,
                            template = new JsonHelper.SaveTemplate()
                            {
                                eventItems = (List<VBarItem>)eventBar.ItemsSource,
                                timeItems = (List<VBarItem>)timeBar.ItemsSource,
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
                            eventItems = (List<VBarItem>)eventBar.ItemsSource,
                            timeItems = (List<VBarItem>)timeBar.ItemsSource,
                        });
                        break;
                    default:
                        MessageBox.Show("你是怎么选到这个的?");
                        return;
                }
            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
        }

        private async void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UndoStack.Count>0&&MessageBox.Show("确定打开文件吗?未保存的数据将丢失!", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
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
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
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
                        break;
                }

            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;
        }

        

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UndoStack.Count > 0)
            {
                ActionPair actionPair = UndoStack.Pop();
                RedoStack.Push(actionPair.GetReverse);
                actionPair.UndoAction.Invoke();
            }
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (RedoStack.Count > 0)
            {
                ActionPair actionPair = RedoStack.Pop();
                UndoStack.Push(actionPair.GetReverse);
                actionPair.UndoAction.Invoke();
            }
        }
        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = UndoStack.Count > 0;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = RedoStack.Count > 0;
        }
        #endregion
        #region 调试
        private async void btnTestLoading_Click(object sender, RoutedEventArgs e)
        {
            //变为加载状态
            mainPanel.Opacity = 0.6;
            loadingCircle.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                Thread.Sleep(3000);
            });

            //结束加载状态
            mainPanel.Opacity = 1.0;
            loadingCircle.Visibility = Visibility.Collapsed;

        }
        private void btnTestAbsRandom_Click(object sender, RoutedEventArgs e)
        {
            // old:
            List<Student> students;
            Student student;
            Random rand = new Random();
            foreach (WeekItem weekItem in weekItems)
            {
                if (string.IsNullOrWhiteSpace(weekItem.Filter))
                {
                    students = listContent;
                }
                else
                {
                    students = GetFilteredStudents(weekItem.Filter);
                }
                if (students.Count <= 0) continue;
                for (int i = 0; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(weekItem.GetFromIndex(i))) continue;
                    student = students[rand.Next(0, students.Count)];
                    weekItem.SetFromIndex(i, student.Name);
                }
            }
            weekBar.Items.Refresh();
        }
        private void btnWeekBarMultiTest_Click(object sender, RoutedEventArgs e)
        {
            var sels = weekBar.SelectedCells;
            return;
        }
        private void btnClearStack_Click(object sender, RoutedEventArgs e)
        {
            UndoStack.Clear();
            RedoStack.Clear();
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
            ListboxFilter,
            TimeBar,
            EventBar,
            WeekBar
        }
        private string lastText;
        public string _filename;
        private MergedSelections selected;
        private int _selX = -1, _selY = -1;
        private bool only_view_filtered = true;

        private List<VBarItem> eventItems => (List<VBarItem>)eventBar.ItemsSource;

        private List<VBarItem> timeItems => (List<VBarItem>)timeBar.ItemsSource;
        private List<WeekItem> weekItems => (List<WeekItem>)weekBar.ItemsSource;

        public int selX
        {
            get
            {
                return _selX;
            }
            set
            {
                statSelX.Text = ((_selX = value) + 1).ToString();
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
                statSelY.Text = ((_selY = value) + 1).ToString();
                statFilter.Text = ((List<WeekItem>)weekBar.ItemsSource)[selY].Filter;
            }
        }

        public string filename
        {
            set
            {
                _filename = value;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Title = "东莞实验中学值周安排器2023 - \"" + _filename + "\"";
                }));
            }
            get
            {
                return _filename;
            }
        }
        private int CalculateHeight()
        {
            int timeHeight = 0, eventHeight = 0;
            foreach (VBarItem timeItem in timeItems)
            {
                timeHeight += timeItem.TabHeight;
            }
            foreach (VBarItem eventItem in eventItems)
            {
                eventHeight += eventItem.TabHeight;
            }
            return Math.Max(timeHeight, eventHeight);
        }
        private VBarItem GetTimeItemFromHeight(int height)
        {
            int ret = 0;
            foreach (VBarItem timeItem in timeItems)
            {
                ret += timeItem.TabHeight;
                if (ret > height)
                {
                    return timeItem;
                }
            }
            return null;
        }
        private VBarItem GetEventItemFromHeight(int height)
        {
            int ret = 0;
            foreach (VBarItem eventItem in eventItems)
            {
                ret += eventItem.TabHeight;
                if (ret > height)
                {
                    return eventItem;
                }
            }
            return null;
        }
        private void PasteStudent()
        {
            if (selX < 0 || selY < 0) return;
            string text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text)) return;
            weekItems[selY].SetFromIndex(selX, text);
            DataGridCellInfo cellInfo = new DataGridCellInfo(weekBar.Items[selY], weekBar.Columns[selX]);
            DataGridCell cell = cellInfo.Column.GetCellContent(cellInfo.Item).Parent as DataGridCell;
            TextBlock textBlock = cell.Content as TextBlock;
            textBlock.Text = text;
        }
        private void CopyStudent()
        {
            if (selX < 0 || selY < 0) return;
            Clipboard.SetText(weekItems[selY].GetFromIndex(selX));
        }
        private void DeleteStudent()
        {
            if (selX < 0 || selY < 0) return;
            // 将该单元格的值设置为null或者空字符串
            weekItems[selY].SetFromIndex(selX, string.Empty);

            //即使修改
            DataGridCellInfo cellInfo = new DataGridCellInfo(weekBar.Items[selY], weekBar.Columns[selX]);
            DataGridCell cell = cellInfo.Column.GetCellContent(cellInfo.Item).Parent as DataGridCell;
            TextBlock textBlock = cell.Content as TextBlock;
            textBlock.Text = String.Empty;
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
        #endregion
        List<VBarItem> defaultTimeItems = new List<VBarItem>();
        List<VBarItem> defaultEventItems = new List<VBarItem>();
        List<WeekItem> defaultWeekItems = new List<WeekItem>();

        

        

        

        private bool lockedTEView=>(bool)checkboxTEViewLock.IsChecked;

        public MainWindow()
        {
            InitializeComponent();
            students = new List<Student>();
            
            defaultTimeItems.Add(new VBarItem());
            defaultEventItems.Add(new VBarItem());
            defaultWeekItems.Add(new WeekItem());
            timeBar.ItemsSource = defaultTimeItems;
            eventBar.ItemsSource = defaultEventItems;
            weekBar.ItemsSource = defaultWeekItems;
            //读取设置
            try
            {
                checkboxTEViewLock.IsChecked = bool.Parse(ConfigurationManager.AppSettings["锁定时间事件视图"]);
                if (bool.Parse(ConfigurationManager.AppSettings["锁定学生视图"]))
                {
                    only_view_filtered = true;
                    btnLockView.IsChecked = true;
                }
                if (!bool.Parse(ConfigurationManager.AppSettings["显示学生栏"]))
                {
                    gridProperty.Visibility = Visibility.Collapsed;
                    btnHideLeft.IsChecked = false;
                }
                if (!bool.Parse(ConfigurationManager.AppSettings["显示设置栏"]))
                {
                    gridPanel.Visibility = Visibility.Collapsed;
                    btnHideRight.IsChecked = false;
                }
                if (!bool.Parse(ConfigurationManager.AppSettings["学生栏只读"]))
                {
                    listboxFilter.IsReadOnly = false;
                    btnDisableEdit.IsChecked = false;
                }
                textFontSize.Value = double.Parse(ConfigurationManager.AppSettings["字号"]);
                textCellHeight.Value = double.Parse(ConfigurationManager.AppSettings["行高"]);
            }
            catch
            {
                LoadDefault();
            }
        }

        private void LoadDefault()
        {
            checkboxTEViewLock.IsChecked = false;

            only_view_filtered = false;
            btnLockView.IsChecked = false;

            gridProperty.Visibility = Visibility.Visible;
            btnHideLeft.IsChecked = true;

            gridPanel.Visibility = Visibility.Visible;
            btnHideRight.IsChecked = true;

            listboxFilter.IsReadOnly = true;
            btnDisableEdit.IsChecked = true;

            textFontSize.Value = (double)Application.Current.Resources["FontSize"];
            textCellHeight.Value = (double)Application.Current.Resources["CellHeight"];
        }
    }
}

