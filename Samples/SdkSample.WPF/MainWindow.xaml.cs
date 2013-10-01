using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Disk.SDK;
using Disk.SDK.Provider;

using Microsoft.Win32;

using SdkSample.WPF.Properties;

namespace SdkSample.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private IDiskSdkClient sdk;
        private ObservableCollection<DiskItemInfo> folderItems;
        private readonly ICollection<DiskItemInfo> selectedItems = new Collection<DiskItemInfo>();
        private readonly ICollection<DiskItemInfo> cutItems = new Collection<DiskItemInfo>();
        private readonly ICollection<DiskItemInfo> copyItems = new Collection<DiskItemInfo>();
        private LoginWindow loginWindow;
        private string currentPath, previousPath, downloadFileName;
        private bool isLaunch;

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public static string AccessToken { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            this.DataContext = this;
            this.CreateSdkClient();
            this.ShowLoginWindow();
        }

        private string GetUniqueFileName(string sourceItem)
        {
            var currentFileName = Path.GetFileName(sourceItem);
            if (this.folderItems.Any(item => item.OriginalDisplayName == currentFileName))
            {
                var fileName = Path.GetFileNameWithoutExtension(currentFileName);
                var extension = Path.GetExtension(currentFileName);
                var uniqueName = string.Format("{0} - Copy{1}", fileName, extension);
                const string FILE_NAME_FORMAT = "{0} - Copy ({1}){2}";
                int uniqueIndex = 1;
                while (this.folderItems.Any(item => item.OriginalDisplayName == uniqueName))
                {
                    uniqueIndex++;
                    uniqueName = string.Format(FILE_NAME_FORMAT, fileName, uniqueIndex, extension);
                }

                return uniqueName;
            }

            return currentFileName;
        }

        private string GetUniqueDirectoryName(string sourceName)
        {
            if (this.folderItems.Any(item => item.OriginalDisplayName == sourceName))
            {
                const string DIR_NAME_FORMAT = "{0} ({1})";
                int uniqueIndex = 2;
                var uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                while (this.folderItems.Any(item => item.OriginalDisplayName == uniqueName))
                {
                    uniqueIndex++;
                    uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                }

                return uniqueName;
            }

            return sourceName;
        }

        private void InitFolder(string path)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
                this.CurrentPath = path;
                this.sdk.GetListAsync(path);
            }
        }

        private void NotifyMenuItems()
        {
            this.OnPropertyChanged("IsExistItems");
            this.OnPropertyChanged("IsSingleSelected");
            this.OnPropertyChanged("IsSelected");
            this.OnPropertyChanged("IsDownloadAvailable");
        }

        private string GetFilePath(string fileName)
        {
            using (var isolatedStorageFile = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (var oStream = new IsolatedStorageFileStream(fileName, FileMode.OpenOrCreate, isolatedStorageFile))
                {
                    return oStream.GetType()
                               .GetField("m_FullPath", BindingFlags.Instance | BindingFlags.NonPublic)
                               .GetValue(oStream).ToString();
                }
            }
        }

        private void LaunchFile(string filePath)
        {
            Process.Start(filePath);
        }

        private bool IsFileExist(string fileName)
        {
            using (var isolatedFile = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                return isolatedFile.FileExists(fileName);
            }
        }

        private void ChangeVisibilityOfProgressBar(Visibility visibility, bool isIndeterminate = true)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.progressBar.Value = 0;
                    this.progressBar.Visibility = visibility;
                    this.progressBar.IsIndeterminate = isIndeterminate;
                }));
        }

        private void ShowProgress(bool isIndeterminate = true)
        {
            this.progressBar.Visibility = Visibility.Visible;
            this.progressBar.IsIndeterminate = isIndeterminate;
        }

        private void UpdateProgress(ulong current, ulong total)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.progressBar.Value = current;
                    this.progressBar.Maximum = total;
                }));
        }

        private void ShowLoginWindow()
        {
            this.loginWindow = new LoginWindow(this.sdk);
            this.loginWindow.AuthCompleted += this.SdkOnAuthorizeCompleted;
            this.loginWindow.ShowDialog();
        }

        private void CreateSdkClient()
        {
            this.sdk = new DiskSdkClient(AccessToken);
            this.AddCompletedHandlers();
        }

        private void ProcessError(SdkException ex)
        {
            Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("SDK error: " + ex.Message)));
        }

        public ObservableCollection<DiskItemInfo> FolderItems
        {
            get { return this.folderItems; }
            set
            {
                this.folderItems = value;
                this.OnPropertyChanged("FolderItems");
            }
        }

        /// <summary>
        /// Gets or sets the current path.
        /// </summary>
        /// <value>The current path.</value>
        public string CurrentPath
        {
            get
            {
                return this.currentPath != null
                           ? Uri.UnescapeDataString(this.currentPath)
                           : string.Empty;
            }
            set
            {
                if (this.currentPath != value)
                {
                    this.currentPath = value;
                    this.OnPropertyChanged("CurrentPath");
                    this.OnPropertyChanged("WindowTitle");
                }
            }
        }

        /// <summary>
        /// Gets the window title.
        /// </summary>
        /// <value>
        /// The window title.
        /// </value>
        public string WindowTitle
        {
            get { return string.Format("SDK Sample - {0}", this.CurrentPath); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is exist items.
        /// </summary>
        /// <value><c>true</c> if this instance is exist items; otherwise, <c>false</c>.</value>
        public bool IsExistItems
        {
            get { return this.copyItems.Any() || this.cutItems.Any(); }
        }

        public bool IsSingleSelected
        {
            get { return this.selectedItems.Count() == 1; }
        }

        /// <summary>
        /// Gets a value indicating whether item is selected.
        /// </summary>
        /// <value><c>true</c> if this item is selected; otherwise, <c>false</c>.</value>
        public bool IsSelected
        {
            get { return this.selectedItems.Count > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is resharing available.
        /// </summary>
        /// <value><c>true</c> if this instance is resharing available; otherwise, <c>false</c>.</value>
        public bool IsDownloadAvailable
        {
            get { return this.selectedItems.Count(item => !item.IsDirectory) == 1 && this.selectedItems.Count == 1; }
        }

        /// <summary>
        /// Gets a value indicating whether current user is logged in.
        /// </summary>
        /// <value><c>true</c> if this user is logged in; otherwise, <c>false</c>.</value>
        public bool IsLoggedIn
        {
            get { return !string.IsNullOrEmpty(AccessToken); }
        }

        public bool IsLoggedOut
        {
            get
            {
                return !IsLoggedIn;
            }
        }

        private void gridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var items = e.AddedItems.Cast<DiskItemInfo>();
                foreach (var item in items)
                {
                    this.selectedItems.Add(item);
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                var items = e.RemovedItems.Cast<DiskItemInfo>();
                foreach (var item in items)
                {
                    this.selectedItems.Remove(item);
                }
            }

            this.NotifyMenuItems();
        }

        private void gridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = this.gridItems.SelectedItem as DiskItemInfo;
            if (item != null)
            {
                if (!item.IsDirectory)
                {
                    if (this.IsFileExist(item.OriginalDisplayName))
                    {
                        var path = this.GetFilePath(item.OriginalDisplayName);
                        File.Delete(path);
                    }
        
                    this.isLaunch = true;
                    this.ShowProgress(false);
                    this.downloadFileName = item.OriginalDisplayName;
                    var fileStream = File.OpenWrite(this.GetFilePath(this.downloadFileName));
                    this.sdk.DownloadFileAsync(item.OriginalFullPath, fileStream, new AsyncProgress(this.UpdateProgress), this.SdkOnDownloadCompleted);
                }
                else
                {
                    this.previousPath = this.currentPath;
                    this.InitFolder(item.OriginalFullPath);
                }
            }
        }

        private void goUp_Click(object sender, RoutedEventArgs e)
        {
            var delimeterIndex = this.currentPath.Length > 1 ? this.currentPath.LastIndexOf("/", this.currentPath.Length - 2) : 0;
            var topPath = this.currentPath.Substring(0, delimeterIndex + 1);
            this.previousPath = this.currentPath;
            this.InitFolder(topPath);
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            this.previousPath = this.currentPath;
            this.InitFolder("/");
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            var previous = this.previousPath;
            this.previousPath = this.currentPath;
            this.InitFolder(previous);
        }

        private void makeDir_Click(object sender, RoutedEventArgs e)
        {
            this.popupNewFolder.IsOpen = true;
        }

        private void createFolder_Click(object sender, RoutedEventArgs e)
        {
            this.popupNewFolder.IsOpen = false;
            this.ShowProgress();
            var uniqueName = this.GetUniqueDirectoryName(this.txtNewFolderName.Text);
            this.sdk.MakeDirectoryAsync(this.currentPath + uniqueName);
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress();
            foreach (var item in this.selectedItems)
            {
                this.sdk.RemoveAsync(item.FullPath);
            }
        }

        private void trash_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress();
            foreach (var item in this.selectedItems)
            {
                this.sdk.TrashAsync(item.FullPath);
            }
        }

        private void copy_Click(object sender, RoutedEventArgs e)
        {
            this.cutItems.Clear();
            foreach (var selectedItem in this.selectedItems)
            {
                this.copyItems.Add(selectedItem);
            }

            this.NotifyMenuItems();
        }

        private void cut_Click(object sender, RoutedEventArgs e)
        {
            this.copyItems.Clear();
            foreach (var selectedItem in this.selectedItems)
            {
                this.cutItems.Add(selectedItem);
            }

            this.NotifyMenuItems();
        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress();
            foreach (var copyItem in this.copyItems)
            {
                var uniqueName = this.GetUniqueFileName(copyItem.OriginalDisplayName);
                this.sdk.CopyAsync(copyItem.OriginalFullPath, this.currentPath + Uri.EscapeDataString(uniqueName));
            }

            foreach (var cutItem in this.cutItems)
            {
                var uniqueName = this.GetUniqueFileName(cutItem.OriginalDisplayName);
                this.sdk.MoveAsync(cutItem.OriginalFullPath, this.currentPath + Uri.EscapeDataString(uniqueName));
            }

            this.copyItems.Clear();
            this.cutItems.Clear();
            this.NotifyMenuItems();
        }

        private void publish_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress();
            this.sdk.PublishAsync(this.selectedItems.First().OriginalFullPath);
        }

        private void unPublish_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress();
            this.sdk.UnpublishAsync(this.selectedItems.First().OriginalFullPath);
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            this.ShowProgress(false);
            var selectedItem = this.selectedItems.First(); // must contains only 1 file
            var saveDialog = new SaveFileDialog();
            saveDialog.FileName = selectedItem.OriginalDisplayName;
            saveDialog.Filter = "All files (*.*)|*.*";
            if (saveDialog.ShowDialog() == true)
            {
                this.downloadFileName = saveDialog.FileName;
                this.sdk.DownloadFileAsync(selectedItem.OriginalFullPath, saveDialog.OpenFile(), new AsyncProgress(this.UpdateProgress), this.SdkOnDownloadCompleted);
            }
            else
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
            }
        }

        private void upload_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog() == true)
            {
                this.ShowProgress(false);
                var stream = openDialog.OpenFile();
                var fileName = Path.GetFileName(openDialog.FileName);
                var uniqueName = this.GetUniqueFileName(fileName);
                var filePath = this.currentPath + uniqueName;
                this.sdk.UploadFileAsync(filePath, stream, new AsyncProgress(this.UpdateProgress), this.SdkOnUploadCompleted);
            }
            else
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            this.InitFolder(this.currentPath);
        }

        private void login_Click(object sender, RoutedEventArgs e)
        {
            AccessToken = string.Empty;
            this.CreateSdkClient();
            this.FolderItems = null;
            this.CurrentPath = string.Empty;
            this.OnPropertyChanged("IsLoggedIn");
            this.ShowLoginWindow();
        }

        private void AddCompletedHandlers()
        {
            this.sdk.CopyCompleted += this.SdkOnCopyCompleted;
            this.sdk.GetListCompleted += this.SdkOnGetListCompleted;
            this.sdk.MakeFolderCompleted += this.SdkOnMakeFolderCompleted;
            this.sdk.MoveCompleted += this.SdkOnMoveCompleted;
            this.sdk.PublishCompleted += this.SdkOnPublishCompleted;
            this.sdk.RemoveCompleted += this.SdkOnRemoveCompleted;
            this.sdk.TrashCompleted += this.SdkOnTrashCompleted;
            this.sdk.UnpublishCompleted += this.SdkOnUnpublishCompleted;
        }

        private void SdkOnDownloadCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                if (this.isLaunch)
                {
                    var fileName = Path.GetFileName(this.downloadFileName);
                    var filePath = this.GetFilePath(fileName);

                    this.LaunchFile(filePath);
                    this.isLaunch = false;
                }
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnUploadCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnUnpublishCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnPublishCompleted(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Error == null)
            {
                this.Dispatcher.BeginInvoke(
                    new Action(
                        () =>
                        {
                            this.popupLink.IsOpen = true;
                            this.txtLink.Text = e.Result;
                            Clipboard.SetText(e.Result);
                        }));
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnTrashCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnRemoveCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnMoveCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnMakeFolderCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnGetListCompleted(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            if (e.Error == null)
            {
                this.Dispatcher.BeginInvoke(new Action(() => { this.FolderItems = new ObservableCollection<DiskItemInfo>(e.Result); }));
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnCopyCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error == null)
            {
                this.InitFolder(this.currentPath);
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnAuthorizeCompleted(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Error == null)
            {
                AccessToken = e.Result;
                this.CreateSdkClient();
                this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.OnPropertyChanged("IsLoggedIn");
                        this.OnPropertyChanged("IsLoggedOut");
                        this.InitFolder("/");
                    }));
            }
            else
            {
                this.ProcessError(e.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
