using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinMolk;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Shapes;
using System.Windows.Input;

namespace MolkZip
{
    public partial class MainWindow : Window
    {
        [System.ComponentModel.Bindable(true)]
        public string SelectedValuePath { get; set; }
        WinMolk.Loading load = new WinMolk.Loading();
        

        private static long allFileSize;
        volatile StreamWriter file;

        public MainWindow()
        {
            InitializeComponent();

            FolderView.SelectedItemChanged +=
                new RoutedPropertyChangedEventHandler<object>(MyTreeView_SelectedItemChanged);
                
            FolderView.Focusable = true;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                if (args[1].EndsWith(".molk") && !Directory.Exists(args[0]))
                {
                    MolkOpen(args[1]);
                }
            }
        }

        #region file explorer functions
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(var drive in Directory.GetLogicalDrives())
            {
                var item = new TreeViewItem()
                {
                    Header = drive,
                    Tag = drive
                };
 
                item.Items.Add(null);

                item.Expanded += Folder_Expanded;

                FolderView.Items.Add(item);
            }
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {

            var item = (TreeViewItem)sender;

            if (item.Items.Count != 1 || item.Items[0] != null)
                return;
            
            item.Items.Clear();
            
            var fullPath = (string)item.Tag;


            var directories = new List<string>();

            try
            {
                var dirs = Directory.GetDirectories(fullPath);

                if (dirs.Length > 0)
                    directories.AddRange(dirs);
            }
            catch { }

            directories.ForEach(directoryPath =>
            {
                var subItem = new TreeViewItem()
                {
                    Header = GetFileFolderName(directoryPath),
                    Tag = directoryPath
                };
                if (!(new FileInfo(directoryPath)).Attributes.HasFlag(FileAttributes.Hidden))
                {
                    subItem.Items.Add(null);

                    subItem.Expanded += Folder_Expanded;

                    item.Items.Add(subItem);
                } 
            });

            var files = new List<string>();

            try
            {
                var fs = Directory.GetFiles(fullPath);

                if (fs.Length > 0)
                    files.AddRange(fs);
            }
            catch { }

            files.ForEach(filePath =>
            {
                var subItem = new TreeViewItem()
                {
                    Header = GetFileFolderName(filePath),
                    Tag = filePath
                };
                if (!(new FileInfo(filePath)).Attributes.HasFlag(FileAttributes.Hidden))
                    item.Items.Add(subItem);
            });
        }

        public static string GetFileFolderName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var normalizedPath = path.Replace('/', '\\');

            var lastIndex = normalizedPath.LastIndexOf('\\');

            if (lastIndex <= 0)
                return path;

            return path.Substring(lastIndex + 1);
        }
        #endregion
        public void Button_Click_Logo(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://www.molk.com/");
            }
            catch
            {
                System.Windows.MessageBox.Show("An error ocurred when launching the browser", "Error Launching Browser", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            RmvH();
        }

        private void Button_Click_Unmolk(object sender, RoutedEventArgs e)
        {
            /*
            var openDlg = new OpenFileDialog();

            var result = openDlg.ShowDialog();

            if (result == false)
                return;
                */

            var openDlg = new System.Windows.Forms.OpenFileDialog();
            openDlg.Filter = "Molk File|*.molk";
            string openPath = String.Empty;
            if(openDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                openPath = openDlg.FileName;

                if (!string.IsNullOrEmpty(openPath))
                {
                    var browseDlg = new FolderBrowserDialog();
                    string extractPath = String.Empty;
                    string arg = String.Empty;
                    if (browseDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            extractPath = browseDlg.SelectedPath.ToString();
                            arg = openPath + " -d " + extractPath;
                            MolkIt("unmolk.exe", arg);
                        }
                        catch
                        {
                            System.Windows.MessageBox.Show("No files added to compress", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        /*
                        load.Show();
                        while (true)
                        {
                            bool a = File.Exists(extractPath);
                            if (a)
                            {
                                System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                                dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                                dispatcherTimer.Start();
                                break;
                            }
                        }*/
                    }
                }
            }
            else
            {
                return;
            }
        }
        
        private void Button_Click_Molk(object sender, RoutedEventArgs e)
        {
            if (listView.HasItems)
            {
                var saveDlg = new System.Windows.Forms.SaveFileDialog();
                saveDlg.Filter = "Molk File|*.molk";
                string itemsPath = String.Empty;
                if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //itemsPath = "\"";
                    foreach (var item in listView.Items)
                    {
                        itemsPath += "\"" + ((FileInfoColumn)item).FilePathFIC.ToString() + "\" ";
                    }

                    string savePath = saveDlg.FileName;
                    string argument = $"\"{savePath}\" {itemsPath}";
                    //argument = argument.Substring(0, argument.Length - 1);
                    string driveName = savePath[0].ToString() + ":\\";
                    var freeSpace = DriveInfo.GetDrives().Where(x => x.Name == driveName && x.IsReady).FirstOrDefault().TotalFreeSpace; // disk space left in bytes

                    if(allFileSize < freeSpace)
                    {
                        if (!string.IsNullOrEmpty(savePath))
                        {
                            if(itemsPath.Contains("."))// file
                            {
                                MolkIt(@"molk\molk.exe", argument);
                                try
                                {
                                    load.Show();
                                    while (true)
                                    {
                                        bool a = File.Exists(savePath);
                                        if (a)
                                        {
                                            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                                            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                                            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                                            dispatcherTimer.Start();
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    System.Windows.MessageBox.Show("Unexpected Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else //map molk
                            {
                                MolkIt(@"molk\molk.exe", "-r " + argument);
                                try
                                {
                                    load.Show();
                                    while (true)
                                    {
                                        bool a = File.Exists(savePath);
                                        if (a)
                                        {
                                            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                                            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                                            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                                            dispatcherTimer.Start();
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    System.Windows.MessageBox.Show("Unexpected Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }

                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Could not Molk the files, no disk space available!", "No Disk Space", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    
                }
                else
                {
                    return;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No files added to compress", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            load.LoadingGrid.Visibility = Visibility.Hidden;
            load.Finished.Visibility = Visibility.Visible;
        }
        private void MolkIt(string exe, string args)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = exe;
                proc.StartInfo.Arguments = @args;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                if (exe.EndsWith("unmolk.exe") && args.StartsWith("-l "))
                {
                    file = new StreamWriter(new FileStream($"txtfiles\\{System.IO.Path.GetFileNameWithoutExtension(args.Substring(3))}_info.txt", FileMode.Create));
                    file.AutoFlush = true;
                    proc.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
                }
                proc.Start();
                proc.BeginOutputReadLine();
                proc.StandardInput.Flush();
                proc.StandardInput.Close();
                proc.WaitForExit();
            }
            catch
            {
                System.Windows.MessageBox.Show("Unexpected Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Process_OutputDataReceived(object sendingProcess, DataReceivedEventArgs data)
        {
            if (data?.Data == null) return;

            file.WriteLine(data.Data);
        }
        private void Exit(object sender, RoutedEventArgs e)
        {
            Quit();
        }        

        private void Quit()
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to quit?", "Confirmation", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    this.Close();
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

             
        private void Button_Click_Add(object sender, RoutedEventArgs e)
        {
            //var o = (TreeViewItem)FolderView.SelectedItem;
            //if (o == null) return;

            //string path = (string)o.Tag;
            //string path = selectedItems.Keys.
            //FileInfoColumn item;

            foreach (var keys in selectedItems.Keys)
            {
                string path = keys.Tag.ToString();
                AddItem(path);
            }

            //AddItem(path);

            SelectAllCheck();
        }
        
        private void AddItem(string path)
        {
            string pathName = path.Split(System.IO.Path.DirectorySeparatorChar).Last();

            if (string.IsNullOrEmpty(pathName))
            {
                pathName = path[0].ToString();
            }

            bool ListViewContainsPath()
            {
                foreach (FileInfoColumn item in listView.Items)
                {
                    if (item.FilePathFIC == path) return true;
                }
                return false;
            }

            string fileType = "";
            if (new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
                fileType = "Folder";
            else
                fileType = System.IO.Path.GetExtension(path);


            if (!ListViewContainsPath())
            {
                long fileSize = fileType == "Folder" ? DirSize(new DirectoryInfo(path)) : new FileInfo(path).Length;
                
                FileInfoColumn fileInfo = new FileInfoColumn() { FileNameFIC = pathName, FilePathFIC = path, FileTypeFIC = fileType, FileSizeFIC = fileSize == -1 ? "No Access" : fileSize == -2 ? $"> {maxGB} GB" : GetFileSize(fileSize) };
                listView.Items.Add(fileInfo);
                AllFilesSize(fileSize);
            }
            SelectAllCheck();
        }

        private static long AllFilesSize(long fileSize)
        {
            return allFileSize += fileSize;
        }

        static long maxGB = 5;
        static long maxSize = 1073741824L * maxGB;
        public static long DirSize(DirectoryInfo d)
        {
            
                try
                {
                    long size = 0;
                    // Add file sizes.
                    FileInfo[] fis = d.GetFiles();
                    foreach (FileInfo fi in fis)
                    {
                        size += fi.Length;
                        if (size > maxSize) return -2;
                    }
                    // Add subdirectory sizes.
                    DirectoryInfo[] dis = d.GetDirectories();
                    foreach (DirectoryInfo di in dis)
                    {
                        size += DirSize(di);
                        if (size > maxSize) return -2;
                    }
                    return size;
                }
                catch
                {
                    return -1;
                }
            
            // -1 = no acces, -2 = > (maxGB) GB
        }

        private void SelectAll(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
        }

        private void SelectAllCheck ()
        {
            if (listView.HasItems)
            {
                selectAll.IsEnabled = true;
            }
            else
            {
                selectAll.IsEnabled = false;
            }
        }
        private void RemoveCheck()
        {
            if (listView.SelectedItems.Count > 0)
                removeFile.IsEnabled = true;
            else
                removeFile.IsEnabled = false;
        }

        private void AddFile(object sender, RoutedEventArgs e)
        {
            AddFileH();
        }

        private void AddFileH()
        {
            var openDlg = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Multiselect = true
            };

            if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (var path in openDlg.FileNames)
                {
                    AddItem(path);
                }
            }
            SelectAllCheck();
        }

        private void AddDirectory(object sender, RoutedEventArgs e)
        {
            AddDirH();
            RemoveCheck();
        }

        private void AddDirH()
        {
            var openDlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false
            };
            if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                AddItem(openDlg.FileName);
                //listView.Items.Add(openDlg.FileName);
            }
            SelectAllCheck();
        }

        private void About(object sender, RoutedEventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }

        private void Help(object sender, RoutedEventArgs e)
        {
            var help = new WinMolk.Help();
            help.ShowDialog();
        }

        private string GetFileSize(double byteCount)
        {
            string size = "0 Bytes";
            if (byteCount >= 1073741824L)
                size = String.Format("{0:##.##}", byteCount / 1073741824L) + " GB";
            else if(byteCount>= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576L) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024L) + " KB";
            else if (byteCount > 0 && byteCount <1024L)
                size = byteCount.ToString() +" Bytes";
            return size;
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            RmvH();
        }

        private void RmvH()
        {
            for (int i = listView.SelectedItems.Count - 1; i >= 0; i--)
            {
                listView.Items.Remove(listView.SelectedItems[i]);
            }
            SelectAllCheck();
        }

        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
            Open();
        }
        private void Open()
        {
            var openDlg = new System.Windows.Forms.OpenFileDialog();
            openDlg.Filter = "Molk File|*.molk";
            if (openDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MolkOpen(openDlg.FileName);
            }
        }
        private void MolkOpen(string fileName)
        {
            string args = "-l " + fileName;
            MolkIt(@"unmolk.exe", args);
            file?.Close();
            var line = File.ReadAllText($"txtfiles\\{System.IO.Path.GetFileNameWithoutExtension(args.Substring(3))}_info.txt");
            WinMolk.OpenMolk OM = new WinMolk.OpenMolk();
            OM.infoTextBlock.Text = line.ToString();
            OM.Show();
        }

        private void OpenMolkFile(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Open();
            }
            else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                AddFileH();
            }
            else if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                AddDirH();
            }
            else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                listView.SelectAll();
            }
            else if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                RmvH();
            }
            else if (e.Key == Key.X && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Quit();
            }

        }
        #region Options-DisplaySize

        public void FirstOption(object sender, RoutedEventArgs e)
        {
            maxSize = 1 * 1073741824L;
            maxGB = 1;
            FO.IsChecked = true;
            SO.IsChecked = false;
            TO.IsChecked = false;
            FUO.IsChecked = false;
        }

        private void SecondOption(object sender, RoutedEventArgs e)
        {
            maxSize = 5 * 1073741824L;
            maxGB = 5;
            FO.IsChecked = false;
            SO.IsChecked = true;
            TO.IsChecked = false;
            FUO.IsChecked = false;
        }

        private void ThirdOption(object sender, RoutedEventArgs e)
        {
            maxSize = 10 * 1073741824L;
            maxGB = 10;
            FO.IsChecked = false;
            SO.IsChecked = false;
            TO.IsChecked = true;
            FUO.IsChecked = false;
        }

        private void FourthOption(object sender, RoutedEventArgs e)
        {
            maxSize = 20 * 1073741824L;
            maxGB = 20;
            FO.IsChecked = false;
            SO.IsChecked = false;
            TO.IsChecked = false;
            FUO.IsChecked = true;
        }
        #endregion

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveCheck();
        }
        
        private static Dictionary<TreeViewItem, string> selectedItems = new Dictionary<TreeViewItem, string>();
        private void Deselect(TreeViewItem treeViewItem)
        {
            treeViewItem.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            treeViewItem.Foreground = System.Windows.Media.Brushes.Black;
            selectedItems.Remove(treeViewItem);
        }

        private void ChangeSelectedState(TreeViewItem treeViewItem)
        {
            if (!selectedItems.ContainsKey(treeViewItem))
            { // select
                treeViewItem.Background = System.Windows.Media.Brushes.DarkOrange;
                treeViewItem.Foreground = System.Windows.Media.Brushes.White;
                selectedItems.Add(treeViewItem, null);
            }
            else
            { // deselect
                Deselect(treeViewItem);
            }
        }

        private void MyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(FolderView.SelectedItem is TreeViewItem treeViewItem))
                return;


            treeViewItem.IsSelected = false;

            treeViewItem.Focus();

            if (!CtrlPressed)
            {
                TreeViewItem[] treeViewItems = selectedItems.Keys.ToArray();
                for (int i = 0; i < treeViewItems.Length; i++)
                {
                    Deselect(treeViewItems[i]);
                }
            }

            ChangeSelectedState(treeViewItem);
        }

        bool CtrlPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftCtrl);
            }
        }


    }
}