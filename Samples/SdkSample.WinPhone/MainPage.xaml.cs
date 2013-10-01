using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

using Disk.SDK;
using Disk.SDK.Provider;

using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace SdkSample.WinPhone
{
    /// <summary>
    /// Represents main page of the application.
    /// </summary>
    public partial class MainPage : INotifyPropertyChanged
    {
        /// <summary>
        /// Represents the current state of the application
        /// </summary>
        internal enum AppState
        {
            /// <summary>
            /// The disk state
            /// </summary>
            Disk,

            /// <summary>
            /// The storage state
            /// </summary>
            Storage,

            /// <summary>
            /// The empty state
            /// </summary>
            Empty
        }

        /// <summary>
        /// The sdk client
        /// </summary>
        private IDiskSdkClient sdk;
        /// <summary>
        /// The items from current folder
        /// </summary>
        private ObservableCollection<DiskItemWrapper> items;
        /// <summary>
        /// The current path
        /// </summary>
        private string currentPath, previousPath = string.Empty;
        /// <summary>
        /// The hold item
        /// </summary>
        private DiskItemInfo holdItem;
        /// <summary>
        /// The cut items
        /// </summary>
        private static readonly ICollection<DiskItemInfo> cutItems;
        /// <summary>
        /// The copy items
        /// </summary>
        private static readonly ICollection<DiskItemInfo> copyItems;
        /// <summary>
        /// The storage items
        /// </summary>
        private ObservableCollection<string> storageItems;
        /// <summary>
        /// The selected storage items
        /// </summary>
        private ICollection<string> selectedStorageItems;
        /// <summary>
        /// The upload file stream
        /// </summary>
        private IsolatedStorageFileStream uploadFileStream;
        private bool isLaunch;

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public static string AccessToken { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="MainPage"/> class.
        /// </summary>
        static MainPage()
        {
            cutItems = new Collection<DiskItemInfo>();
            copyItems = new Collection<DiskItemInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.selectedStorageItems = new Collection<string>();
            this.DataContext = this;
            this.sdk = new DiskSdkClient(AccessToken);
            this.AddCompletedHandlers();
            
        }

        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">An object that contains the event data.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string path, token;
            if (this.NavigationContext.QueryString.TryGetValue("token", out token))
            {
                AccessToken = token;
                this.sdk = new DiskSdkClient(token);
                this.AddCompletedHandlers();
            }

            this.InitFolder(this.NavigationContext.QueryString.TryGetValue("path", out path) ? path : "/");
        }

        /// <summary>
        /// Initializes the folder.
        /// </summary>
        /// <param name="path">The path.</param>
        private void InitFolder(string path)
        {
            if (this.IsLoggedIn)
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Visible);
                this.CurrentPath = path;
                this.sdk.GetListAsync(path);
            }
            else
            {
                this.NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
            }
        }

        /// <summary>
        /// Initializes the storage.
        /// </summary>
        private void InitStorage()
        {
            using (var local = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var names = local.GetFileNames();
                this.StorageItems = new ObservableCollection<string>(names);
            }
        }

        /// <summary>
        /// Changes the visibility of progress bar.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        private void ChangeVisibilityOfProgressBar(Visibility visibility, bool isIndeterminate = true)
        {
            this.Dispatcher.BeginInvoke(
                () =>
                    {
                        this.progressBar.Visibility = visibility;
                        this.progressBar.IsIndeterminate = isIndeterminate;
                    });
        }

        /// <summary>
        /// Changes the application bar.
        /// </summary>
        /// <param name="state">The state.</param>
        private void ChangeAppBar(AppState state)
        {
            switch (state)
            {
                case AppState.Disk:
                    this.ApplicationBar = this.IsLoggedIn ? (ApplicationBar)this.Resources["diskAppBar"] : null;
                    break;
                case AppState.Storage:
                    this.ApplicationBar = this.IsLoggedIn
                                              ? (ApplicationBar)this.Resources["authStorageAppBar"]
                                              : (ApplicationBar)this.Resources["storageAppBar"];
                    break;
                case AppState.Empty:
                    this.ApplicationBar = null;
                    break;
            }
        }

        /// <summary>
        /// Launches the disk item.
        /// </summary>
        /// <param name="item">The disk item.</param>
        private void LaunchItem(DiskItemInfo item)
        {
            if (item.ContentType.ToLower().Contains("audio/") || item.ContentType.ToLower().Contains("video/"))
            {
                var mediaLauncher = new MediaPlayerLauncher();
                mediaLauncher.Media = new Uri(item.OriginalDisplayName, UriKind.Relative);
                mediaLauncher.Location = MediaLocationType.Data;
                mediaLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                try
                {
                    mediaLauncher.Show();
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(() => MessageBox.Show(ex.Message));
                }
            }
            else if (item.ContentType.ToLower().Contains("text/") || item.ContentType.ToLower().Contains("image/"))
            {
                this.NavigationService.Navigate(new Uri("/ImageViewer.xaml?path=" + Uri.EscapeDataString(item.OriginalDisplayName), UriKind.Relative));
            }
            else
            {
                this.Dispatcher.BeginInvoke(() => MessageBox.Show("Error: unknown file format"));
            }
        }

        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="total">The total.</param>
        private void UpdateProgress(ulong current, ulong total)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.progressBar.Value = current;
                this.progressBar.Maximum = total;
            });
        }

        private string GetUniqueFileName(string sourceItem)
        {
            var currentFileName = Path.GetFileName(sourceItem);
            if (this.items.Any(item => item.DiskItem.OriginalDisplayName == currentFileName))
            {
                var fileName = Path.GetFileNameWithoutExtension(currentFileName);
                var extension = Path.GetExtension(currentFileName);
                var uniqueName = string.Format("{0} - Copy{1}", fileName, extension);
                const string FILE_NAME_FORMAT = "{0} - Copy ({1}){2}";
                int uniqueIndex = 1;
                while (this.items.Any(item => item.DiskItem.OriginalDisplayName == uniqueName))
                {
                    uniqueIndex++;
                    uniqueName = string.Format(FILE_NAME_FORMAT, fileName, uniqueIndex, extension);
                }

                return uniqueName;
            }

            return currentFileName;
        }

        private string GetStorageUniqueFileName(string sourceItem)
        {
            var currentFileName = Path.GetFileName(sourceItem);
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var list = storage.GetFileNames().ToList();
                if (list.Any(item => item == currentFileName))
                {
                    var fileName = Path.GetFileNameWithoutExtension(currentFileName);
                    var extension = Path.GetExtension(currentFileName);
                    var uniqueName = string.Format("{0} - Copy{1}", fileName, extension);
                    const string FILE_NAME_FORMAT = "{0} - Copy ({1}){2}";
                    int uniqueIndex = 1;
                    while (list.Any(item => item == uniqueName))
                    {
                        uniqueIndex++;
                        uniqueName = string.Format(FILE_NAME_FORMAT, fileName, uniqueIndex, extension);
                    }

                    return uniqueName;
                }
            }
            

            return currentFileName;
        }

        private string GetUniqueDirectoryName(string sourceName)
        {
            if (this.items.Any(item => item.DiskItem.OriginalDisplayName == sourceName))
            {
                const string DIR_NAME_FORMAT = "{0} ({1})";
                int uniqueIndex = 2;
                var uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                while (this.items.Any(item => item.DiskItem.OriginalDisplayName == uniqueName))
                {
                    uniqueIndex++;
                    uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                }

                return uniqueName;
            }

            return sourceName;
        }

        private void ProcessError(SdkException ex)
        {
            Dispatcher.BeginInvoke(() => MessageBox.Show("SDK error: " + ex.Message));
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
                }
            }
        }

        private ObservableCollection<DiskItemInfo> SelectedItems
        {
            get
            {
                return new ObservableCollection<DiskItemInfo>(this.DiskItems.Where(item => item.IsSelected).Select(item => item.DiskItem));
            }
        }

        /// <summary>
        /// Gets or sets the disk items.
        /// </summary>
        /// <value>The disk items.</value>
        public ObservableCollection<DiskItemWrapper> DiskItems
        {
            get { return this.items; }
            set
            {
                this.items = value;
                this.OnPropertyChanged("DiskItems");
            }
        }

        /// <summary>
        /// Gets or sets the storage items.
        /// </summary>
        /// <value>The storage items.</value>
        public ObservableCollection<string> StorageItems
        {
            get { return this.storageItems; }
            set
            {
                this.storageItems = value;
                this.OnPropertyChanged("StorageItems");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is exist items.
        /// </summary>
        /// <value><c>true</c> if this instance is exist items; otherwise, <c>false</c>.</value>
        public bool IsExistItems
        {
            get { return copyItems.Any() || cutItems.Any(); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is file selected.
        /// </summary>
        /// <value><c>true</c> if this instance is file selected; otherwise, <c>false</c>.</value>
        public bool IsFileSelected
        {
            get { return this.SelectedItems.Count(item => !item.IsDirectory) > 0; }
        }

        public bool IsSingleSelected
        {
            get { return this.SelectedItems.Count() == 1; }
        }

        public bool IsDownloadAvailable
        {
            get { return this.SelectedItems.Count(item => !item.IsDirectory) == 1 && this.SelectedItems.Count == 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is item selected.
        /// </summary>
        /// <value><c>true</c> if this instance is item selected; otherwise, <c>false</c>.</value>
        public bool IsItemSelected
        {
            get { return this.SelectedItems.Any(); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is resharing available.
        /// </summary>
        /// <value><c>true</c> if this instance is resharing available; otherwise, <c>false</c>.</value>
        public bool IsResharingAvailable
        {
            get { return this.SelectedItems.Count(item => !item.IsDirectory) == 1 && this.SelectedItems.Count == 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is logged information.
        /// </summary>
        /// <value><c>true</c> if this instance is logged information; otherwise, <c>false</c>.</value>
        public bool IsLoggedIn
        {
            get { return !string.IsNullOrEmpty(AccessToken); }
        }

        public bool IsLoggedOut
        {
            get { return !this.IsLoggedIn; }
        }

        /// <summary>
        /// Adds the completed handlers.
        /// </summary>
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
                    this.Dispatcher.BeginInvoke(() => this.LaunchItem(this.holdItem));
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
            if (e.Error != null)
            {
                this.uploadFileStream.Dispose();
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnUnpublishCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnTrashCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }
            else
            {
                this.InitFolder(this.currentPath);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnRemoveCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }
            else
            {
                this.InitFolder(this.currentPath);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnPublishCompleted(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Error == null)
            {
                this.Dispatcher.BeginInvoke(() =>
                    {
                        this.linkPopup.IsOpen = true;
                        this.linkPopup.HorizontalOffset = (Application.Current.Host.Content.ActualWidth - this.linkPanel.Width)/2;
                        this.linkPopup.VerticalOffset = (Application.Current.Host.Content.ActualHeight - this.linkPanel.Height)/2;
                        this.txtLink.Text = e.Result;
                        Clipboard.SetText(e.Result);
                    });
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnMoveCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }
            else
            {
                this.InitFolder(this.currentPath);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnMakeFolderCompleted(object sender, SdkEventArgs e)
        {
            this.Dispatcher.BeginInvoke(() => { this.newFolderPopup.IsOpen = false; });
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }
            else
            {
                this.InitFolder(this.currentPath);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnGetListCompleted(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            if (e.Error == null)
            {
                this.Dispatcher.BeginInvoke(() => this.DiskItems = new ObservableCollection<DiskItemWrapper>(e.Result.Select(item => new DiskItemWrapper { DiskItem = item })));
            }
            else
            {
                this.ProcessError(e.Error);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        private void SdkOnCopyCompleted(object sender, SdkEventArgs e)
        {
            if (e.Error != null)
            {
                this.ProcessError(e.Error);
            }
            else
            {
                this.InitFolder(this.currentPath);
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        public void SdkOnAuthorizeCompleted(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Error == null)
            {
                AccessToken = e.Result;
                this.Dispatcher.BeginInvoke(() =>
                    {
                        this.InitFolder("/");
                        this.ChangeAppBar(AppState.Disk);
                    });
            }
            else
            {
                this.ProcessError(e.Error);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the lbDiskItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void lbDiskItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as DiskItemWrapper;
                if (item != null)
                {
                    this.holdItem = item.DiskItem;
                    if (!this.holdItem.IsDirectory)
                    {
                        using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (storage.FileExists(this.holdItem.OriginalDisplayName))
                            {
                                storage.DeleteFile(this.holdItem.OriginalDisplayName);
                            }

                            this.ChangeVisibilityOfProgressBar(Visibility.Visible, false);
                            var file = storage.CreateFile(this.holdItem.OriginalDisplayName);
                            this.sdk.DownloadFileAsync(this.holdItem.OriginalFullPath, file, new AsyncProgress(this.UpdateProgress), this.SdkOnDownloadCompleted);
                            this.isLaunch = true;
                        }
                    }
                    else
                    {
                        this.NavigationService.Navigate(new Uri("/MainPage.xaml?path=" + Uri.EscapeDataString(this.holdItem.OriginalFullPath), UriKind.Relative));
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the lbStorageItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void lbStorageItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as string;
                this.selectedStorageItems.Add(item);
            }
            if (e.RemovedItems.Count > 0)
            {
                var item = e.RemovedItems[0] as string;
                this.selectedStorageItems.Remove(item);
            }
        }

        /// <summary>
        /// Handles the Loaded event of the mainPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.diskListBox.SelectedIndex = -1;
            this.btnDisk.IsEnabled = false;
            this.ChangeAppBar(AppState.Disk);
        }

        /// <summary>
        /// Handles the Click event of the copy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copy_Click(object sender, EventArgs e)
        {
            cutItems.Clear();
            foreach (var selectedItem in this.SelectedItems)
            {
                copyItems.Add(selectedItem);
            }
        }

        /// <summary>
        /// Handles the Click event of the cut control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void cut_Click(object sender, EventArgs e)
        {
            copyItems.Clear();
            foreach (var selectedItem in this.SelectedItems)
            {
                cutItems.Add(selectedItem);
            }
        }

        /// <summary>
        /// Handles the Click event of the newFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void newFolder_Click(object sender, EventArgs e)
        {
            this.newFolderPopup.IsOpen = true;
            this.newFolderPopup.HorizontalOffset = (Application.Current.Host.Content.ActualWidth - this.newFolderPanel.Width) / 2;
            this.newFolderPopup.VerticalOffset = (Application.Current.Host.Content.ActualHeight - this.newFolderPanel.Height) / 2;
        }

        /// <summary>
        /// Handles the Click event of the cancelFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void cancelFolder_Click(object sender, RoutedEventArgs e)
        {
            this.newFolderPopup.IsOpen = false;
        }

        /// <summary>
        /// Handles the OnClick event of the btnStorage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnStorage_OnClick(object sender, RoutedEventArgs e)
        {
            this.diskListBox.Visibility = Visibility.Collapsed;
            this.lbStorageItems.Visibility = Visibility.Visible;
            this.InitStorage();
            this.btnStorage.IsEnabled = false;
            this.btnDisk.IsEnabled = true;
            VisualStateManager.GoToState(this.btnDisk, "Normal", false);
            this.previousPath = this.currentPath;
            this.CurrentPath = "Local Storage";
            this.ChangeAppBar(AppState.Storage);
        }

        /// <summary>
        /// Handles the Click event of the btnDisk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnDisk_Click(object sender, RoutedEventArgs e)
        {
            this.diskListBox.Visibility = Visibility.Visible;
            this.lbStorageItems.Visibility = Visibility.Collapsed;
            this.CurrentPath = this.previousPath;
            this.InitFolder(this.currentPath);
            this.btnStorage.IsEnabled = true;
            this.btnDisk.IsEnabled = false;
            this.ChangeAppBar(AppState.Disk);
        }

        /// <summary>
        /// Handles the Click event of the close control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void close_Click(object sender, RoutedEventArgs e)
        {
            this.linkPopup.IsOpen = false;
        }

        /// <summary>
        /// Handles the Click event of the delete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void delete_Click(object sender, EventArgs e)
        {
            if (this.selectedStorageItems.Count > 0)
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    foreach (var fileName in this.selectedStorageItems)
                    {
                        storage.DeleteFile(fileName);
                    }
                }
                this.InitStorage();
            }
        }

        /// <summary>
        /// Handles the Click event of the remove control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void remove_Click(object sender, EventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            foreach (var item in this.SelectedItems)
            {
                this.sdk.RemoveAsync(item.OriginalFullPath);
            }
        }

        /// <summary>
        /// Handles the Click event of the download control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void download_Click(object sender, EventArgs e)
        {
            if (this.IsDownloadAvailable)
            {
                var item = this.SelectedItems.First();
                this.ChangeVisibilityOfProgressBar(Visibility.Visible, false);
                var store = IsolatedStorageFile.GetUserStoreForApplication();
                var uniqueName = this.GetStorageUniqueFileName(item.OriginalDisplayName);
                var stream = store.OpenFile(uniqueName, FileMode.OpenOrCreate);
                this.sdk.DownloadFileAsync(item.OriginalFullPath, stream, new AsyncProgress(this.UpdateProgress), this.SdkOnDownloadCompleted);
            }
            else
            {
                MessageBox.Show("Download operation is not available, choose single file");
            }
        }

        /// <summary>
        /// Handles the Click event of the upload control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void upload_Click(object sender, EventArgs e)
        {
            if (this.selectedStorageItems.Count == 1)
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Visible);
                var fileName = this.selectedStorageItems.First();
                var uniqueName = this.GetUniqueFileName(fileName);
                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                this.uploadFileStream = storage.OpenFile(fileName, FileMode.Open);
                var streamReader = new StreamReader(this.uploadFileStream);
                this.sdk.UploadFileAsync(this.previousPath + uniqueName, streamReader.BaseStream, this.SdkOnUploadCompleted);
            }
        }

        /// <summary>
        /// Handles the Click event of the publish control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void publish_Click(object sender, EventArgs e)
        {
            if (this.IsSingleSelected)
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Visible);
                this.sdk.PublishAsync(this.SelectedItems.First().OriginalFullPath);
            }
            else
            {
                MessageBox.Show("Publish operation is not available, choose single item");
            }
        }

        /// <summary>
        /// Handles the Click event of the unpublish control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void unpublish_Click(object sender, EventArgs e)
        {
            if (this.IsSingleSelected)
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Visible);
                this.sdk.UnpublishAsync(this.SelectedItems.First().OriginalFullPath);
            }
            else
            {
                MessageBox.Show("Unpublish operation is not available, choose single item");
            }
        }

        /// <summary>
        /// Handles the Click event of the createFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void createFolder_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            var uniqueName = this.GetUniqueDirectoryName(this.txtFolderName.Text);
            this.sdk.MakeDirectoryAsync(this.currentPath + uniqueName);
        }

        /// <summary>
        /// Handles the Click event of the paste control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void paste_Click(object sender, EventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            foreach (var copyItem in copyItems)
            {
                var uniqueName = this.GetUniqueFileName(copyItem.OriginalDisplayName);
                this.sdk.CopyAsync(copyItem.OriginalFullPath, this.currentPath + Uri.EscapeDataString(uniqueName));
            }

            foreach (var cutItem in cutItems)
            {
                var uniqueName = this.GetUniqueFileName(cutItem.OriginalDisplayName);
                this.sdk.MoveAsync(cutItem.OriginalFullPath, this.currentPath + Uri.EscapeDataString(uniqueName));
            }

            copyItems.Clear();
            cutItems.Clear();
        }

        /// <summary>
        /// Handles the Click event of the refresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void refresh_Click(object sender, EventArgs e)
        {
            this.InitFolder(this.currentPath);
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}