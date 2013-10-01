using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Disk.SDK;
using Disk.SDK.Provider;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SdkSample.WinRT
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class MainPage : INotifyPropertyChanged
    {
        /// TODO: Register your application https://oauth.yandex.ru/client/new and set here your application ID and return URL.

#error define your client_id and return_url
        private const string CLIENT_ID = "";
        private const string RETURN_URL = "";

        /// <summary>
        /// The sdk client
        /// </summary>
        private IDiskSdkClient sdk;
        /// <summary>
        /// The items of items in the current folder
        /// </summary>
        private ObservableCollection<DiskItemInfo> items;
        /// <summary>
        /// The current path
        /// </summary>
        private string currentPath;
        /// <summary>
        /// The selected disk items
        /// </summary>
        private readonly ICollection<DiskItemInfo> selectedItems;
        /// <summary>
        /// The cut items
        /// </summary>
        private static readonly ICollection<DiskItemInfo> cutItems;
        /// <summary>
        /// The copy items
        /// </summary>
        private static readonly ICollection<DiskItemInfo> copyItems;

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
            this.selectedItems = new Collection<DiskItemInfo>();
            this.DataContext = this;
            this.sdk = new DiskSdkClient(AccessToken);
            this.AddCompletedHandlers();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var diskItemInfo = e.Parameter as DiskItemInfo;
            var path = diskItemInfo != null ? diskItemInfo.OriginalFullPath : "/";
            this.InitFolder(path);
        }

        /// <summary>
        /// Initializes the folder.
        /// </summary>
        /// <param name="path">The path.</param>
        private async void InitFolder(string path)
        {
            if (this.IsLoggedIn)
            {
                this.ChangeVisibilityOfProgressBar(Visibility.Visible);
                this.CurrentPath = path;
                this.sdk.GetListAsync(path);
            }
            else
            {
                await this.Login();
            }
        }

        /// <summary>
        /// Changes the visibility of progress bar.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        private async void ChangeVisibilityOfProgressBar(Visibility visibility, bool isIndeterminate = true)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        this.progressContainer.Visibility = visibility;
                        this.progressBar.IsIndeterminate = isIndeterminate;
                    });
        }

        /// <summary>
        /// Is file exists as an asynchronous operation.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        private async Task<bool> IsFileExistsAsync(string fileName)
        {
            try
            {
                await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="total">The total.</param>
        private async void UpdateProgress(ulong current, ulong total)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.progressBar.Value = current;
                    this.progressBar.Maximum = total;
                });
        }

        private async Task Login()
        {
            AccessToken = string.Empty;
            this.Items = null;
            this.CurrentPath = string.Empty;
            try
            {
                AccessToken = await this.sdk.AuthorizeAsync(CLIENT_ID, RETURN_URL);
                this.sdk = new DiskSdkClient(AccessToken);
                this.AddCompletedHandlers();
                this.InitFolder("/");
            }
            catch (SdkException ex)
            {
                this.ShowMessage(ex.Message, "SDK exception");
            }

            this.OnPropertyChanged("IsLoggedIn");
            this.OnPropertyChanged("IsLoggedOut");
        }

        private void ShowMessage(string message, string title)
        {
            Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                    {
                        var dialog = new MessageDialog(message, title);
                        dialog.CancelCommandIndex = 0;
                        dialog.Commands.Add(new UICommand("OK"));
                        await dialog.ShowAsync();
                    });
        }

        private void ProcessError(SdkException ex)
        {
            this.ShowMessage(ex.Message, "SDK error");
        }

        private void NotifyMenuItems()
        {
            this.OnPropertyChanged("IsExistItems");
            this.OnPropertyChanged("IsSingleItemSelected");
            this.OnPropertyChanged("IsDownloadAvailable");
            this.OnPropertyChanged("IsSelectedSomething");
        }

        private string GetUniqueFileName(string sourceItem)
        {
            var currentFileName = Path.GetFileName(sourceItem);
            if (this.items.Any(item => item.OriginalDisplayName == currentFileName))
            {
                var fileName = Path.GetFileNameWithoutExtension(currentFileName);
                var extension = Path.GetExtension(currentFileName);
                var uniqueName = string.Format("{0} - Copy{1}", fileName, extension);
                const string FILE_NAME_FORMAT = "{0} - Copy ({1}){2}";
                int uniqueIndex = 1;
                while (this.items.Any(item => item.OriginalDisplayName == uniqueName))
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
            if (this.items.Any(item => item.OriginalDisplayName == sourceName))
            {
                const string DIR_NAME_FORMAT = "{0} ({1})";
                int uniqueIndex = 2;
                var uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                while (this.items.Any(item => item.OriginalDisplayName == uniqueName))
                {
                    uniqueIndex++;
                    uniqueName = string.Format(DIR_NAME_FORMAT, sourceName, uniqueIndex);
                }

                return uniqueName;
            }

            return sourceName;
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
                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public ObservableCollection<DiskItemInfo> Items
        {
            get { return this.items; }
            set
            {
                this.items = value;
                this.OnPropertyChanged();
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
        public bool IsSingleItemSelected
        {
            get { return this.selectedItems.Count() == 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is resharing available.
        /// </summary>
        /// <value><c>true</c> if this instance is resharing available; otherwise, <c>false</c>.</value>
        public bool IsDownloadAvailable
        {
            get { return this.selectedItems.Count(item => !item.IsDirectory) == 1 && this.selectedItems.Count == 1; }
        }

        public bool IsSelectedSomething 
        {
            get
            {
                return this.selectedItems.Count > 0;
            }
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
                return !this.IsLoggedIn;
            }
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

        private async void SdkOnPublishCompleted(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Error == null)
            {
                await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                        {
                            this.linkPopup.IsOpen = true;
                            this.linkPopup.HorizontalOffset = Window.Current.CoreWindow.Bounds.Right / 2 - this.linkPanel.Width / 2;
                            this.linkPopup.VerticalOffset = Window.Current.CoreWindow.Bounds.Bottom / 2 - this.linkPanel.Height / 2;
                            this.txtLink.Text = e.Result;
                            var package = new DataPackage();
                            package.SetText(e.Result);
                            Clipboard.SetContent(package);
                        });
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

        private async void SdkOnGetListCompleted(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            if (e.Error == null)
            {
                await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, 
                    () =>
                        {
                            this.Items = new ObservableCollection<DiskItemInfo>(e.Result);
                            this.NotifyMenuItems();
                        });
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

        /// <summary>
        /// Handles the SelectionChanged event of the itemListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void itemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var itemInfo = e.AddedItems[0] as DiskItemInfo;
                this.selectedItems.Add(itemInfo);
            }
            else if (e.RemovedItems.Count > 0)
            {
                var itemInfo = e.RemovedItems[0] as DiskItemInfo;
                this.selectedItems.Remove(itemInfo);
            }
            this.bottomAppBar.IsOpen = this.selectedItems.Any();
            this.NotifyMenuItems();
        }

        /// <summary>
        /// Handles the ItemClick event of the itemListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private async void itemListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as DiskItemInfo;
            if (item != null)
            {
                if (!item.IsDirectory)
                {
                    var localFolder = ApplicationData.Current.LocalFolder;
                    if (await this.IsFileExistsAsync(item.OriginalDisplayName))
                    {
                        var file = await localFolder.GetFileAsync(item.OriginalDisplayName);
                        await file.DeleteAsync();
                    }
                    
                    var local = ApplicationData.Current.LocalFolder;
                    var localFile = await local.CreateFileAsync(item.OriginalDisplayName, CreationCollisionOption.GenerateUniqueName);
                    this.ChangeVisibilityOfProgressBar(Visibility.Visible, false);
                    await this.sdk.StartDownloadFileAsync(item.OriginalFullPath, localFile, new AsyncProgress(this.UpdateProgress));
                    this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
                    await Launcher.LaunchFileAsync(localFile, new LauncherOptions());
                }
                else
                {
                    this.Frame.Navigate(typeof(MainPage), item);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the newFolderButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void newFolderButton_Click(object sender, RoutedEventArgs e)
        {
            this.newFolderPopup.IsOpen = true;
            this.newFolderPopup.HorizontalOffset = 20;
            this.newFolderPopup.VerticalOffset = Window.Current.CoreWindow.Bounds.Bottom - this.bottomAppBar.ActualHeight - this.newFolderPanel.Height - 13;
        }

        /// <summary>
        /// Handles the Click event of the editMenuButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void editMenuButton_Click(object sender, RoutedEventArgs e)
        {
            this.manageActionsPopup.Visibility = Visibility.Visible;
            this.manageActionsPopup.IsOpen = true;
            this.manageActionsPopup.HorizontalOffset = this.btnEdit.ActualWidth + 20;
            this.manageActionsPopup.VerticalOffset = Window.Current.CoreWindow.Bounds.Bottom - this.bottomAppBar.ActualHeight - this.panel.Height - 4;
        }

        /// <summary>
        /// Handles the Click event of the copyButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            cutItems.Clear();
            foreach (var selectedItem in this.selectedItems)
            {
                copyItems.Add(selectedItem);
            }
            this.manageActionsPopup.IsOpen = false;
            this.NotifyMenuItems();
        }

        /// <summary>
        /// Handles the Click event of the cutButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            copyItems.Clear();
            foreach (var selectedItem in this.selectedItems)
            {
                cutItems.Add(selectedItem);
            }
            this.manageActionsPopup.IsOpen = false;
            this.NotifyMenuItems();
        }

        /// <summary>
        /// Handles the Click event of the barLogout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void barLogin_Click(object sender, RoutedEventArgs e)
        {
            await this.Login();
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
        /// Handles the Click event of the removeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            this.manageActionsPopup.IsOpen = false;
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            foreach (var item in this.selectedItems)
            {
                this.sdk.RemoveAsync(item.FullPath);
            }
            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        /// <summary>
        /// Handles the Click event of the pasteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void pasteButton_Click(object sender, RoutedEventArgs e)
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
            this.manageActionsPopup.IsOpen = false;
        }

        /// <summary>
        /// Handles the Click event of the shareButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void shareButton_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            this.sdk.PublishAsync(this.selectedItems.First().OriginalFullPath);
        }

        /// <summary>
        /// Handles the Click event of the unshareButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void unshareButton_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible);
            this.sdk.UnpublishAsync(this.selectedItems.First().OriginalFullPath);
        }

        /// <summary>
        /// Handles the Click event of the uploadButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible, false);
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add("*");
            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                var uniqueName = this.GetUniqueFileName(file.Name);
                try
                {
                    await this.sdk.StartUploadFileAsync(this.currentPath + uniqueName, file, new AsyncProgress(this.UpdateProgress));
                    this.InitFolder(this.currentPath);
                }
                catch (SdkException ex)
                {
                    this.ProcessError(ex);
                }
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        /// <summary>
        /// Handles the Click event of the downloadButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            this.ChangeVisibilityOfProgressBar(Visibility.Visible, false);
            var selectedItem = this.selectedItems.First(); // should contains only 1 file
            var fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedFileName = selectedItem.OriginalDisplayName;
            fileSavePicker.FileTypeChoices.Add("Unknown", new List<string>() { "." });
            var file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    await this.sdk.StartDownloadFileAsync(selectedItem.OriginalFullPath, file, new AsyncProgress(this.UpdateProgress));
                }
                catch (SdkException ex)
                {
                    this.ProcessError(ex);
                }
            }

            this.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        /// <summary>
        /// Handles the Click event of the refreshButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void refreshButton_Click(object sender, RoutedEventArgs e)
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
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
