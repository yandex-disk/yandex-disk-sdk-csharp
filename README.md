# Yandex Disk SDK for .NET, Windows Phone and Windows Store applications

## What this is

A wrapper for the Yandex.Disk Cloud API.

The current implementation is based on OAuth 2 for authorization, and on WebDAV for working with the cloud storage.


## Required reading

Please check out the [Yandex Disk API page][DISKAPI],
and [Yandex OAuth 2 API page][AUTHAPI].


## Installing

### Register for a Yandex API key.

You can register your app at the: [Yandex OAuth app registration page][REGISTER].


### Include the code

To start using Yandex.Disk API:

- Copy the Yandex.Disk SDK source code into your Visual Studio project.
- Add the references in your application to Disk.Sdk and the corresponding Disk.SDK.Provider based on your application type (.NET, Windows Phone or Windows Store).

## Structure of the SDK

The SDK consists of Disk.SDK Visual Studio project (compiled into portable assembly) and three projects containing code specific for each supported application type:
 - Disk.SDK.Provider (.NET 4.0) – for .NET 4.0 applications (including WPF and WinForms)
 - Disk.SDK.Provider (WinPhone) – for Windows Phone (7.5 and higher) applications
 - Disk.SDK.Provider (WinStore) – for Windows Store applications

There are three sample application projects for each platform:
 - SdkSamples.WPF
 - SdkSamples.WinPhone
 - SdkSamples.WinStore

## Samples

There are three samples included in the SDK. 

- SdkSamples.WinPhone: a very simple Yandex.Disk browser application for Windows Phone (7.5 and higher).
- SdkSamples.WinRT: a very simple Yandex.Disk browser application for Windows Store.
- SdkSamples.WPF: a very simple Yandex.Disk browser application based on WPF.
Important: It’s required to set ClientId and CallbackUrl in the downloaded source code of the samples before it would be possible to run them. To get these register your application here: [Yandex OAuth app registration page][REGISTER].
Note: Model-View-ViewModel (MVVM) pattern was not used in the samples. It’s done this way in order to make the code browsing and understanding easier for the SDK user. 

## Using the Yandex Disk SDK from your code

### Preparations

The Yandex.Disk SDK is asynchronous, and is using events to signal when an operation is complete. To access the cloud storage and authentication, your application should create a new instance of DiskSdkClient class and subscribe to its events.

In order to perform an operation, your code should call asynchronous method and display data after a corresponding event will be fired. In the event handler check event’s arguments for Error property, it will contain null in case the operation was successful or an exception thrown during the execution of the operation. 

### Authenticate

To authenticate a user and get his OAuth token to access Yandex.Disk API follow these steps:
 - Show a page or window hosting web browser component (not needed for Windows Store applications). 
 - Call platform-specific method AuthorizeAsync for IDiskSdkClient object.
- Process result as a user’s token value if the authorization was successful.

Windows Store application:

```
String accessToken = await this.sdk.AuthorizeAsync(CLIENT_ID, RETURN_URL);
```

Windows Phone application (browser an object is of type Microsoft.Phone.Controls.WebBrowser):

```
this.sdkClient.AuthorizeAsync(new WebBrowserWrapper(browser), CLIENT_ID, RETURN_URL, this.CompleteCallback);
…
private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
{
    this.NavigationService.Navigate(new Uri("/MainPage.xaml?token=" + e.Result, UriKind.Relative));
}
```

WPF application (browser an object is of type System.Windows.Controls.WebBrowser):

```
this.sdkClient.AuthorizeAsync(new WebBrowserBrowser(browser), CLIENT_ID, RETURN_URL, this.CompleteCallback);
…
private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
{
    if (this.AuthCompleted != null)
    {
        this.AuthCompleted(this, new GenericSdkEventArgs<string>(e.Result));
    }

    this.Close();
}

public event EventHandler<GenericSdkEventArgs<string>> AuthCompleted;
```

In case you have a token already, you can skip the authorization and just pass the token to constructor of DiskSdkClient:

```
var sdk = new DiskSdkClient(AccessToken);
```

### List the root directory

Using an authorized instance of DiskSdkClient:
 - Call sdkClient.GetListAsync(“/”)
 - In the event handler of GetListCompleted event receive the collection of items:
 
```
if (e.Error == null)
{
   IEnumerable<DiskItemInfo> items = e.Result;
}
```

### IDiskSdkClient interface

Class DiskSdkClient implements interface IDiskSdkClient with the following methods and events and properties:

```
namespace Disk.SDK
{
    public interface IDiskSdkClient
    {
        event EventHandler<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>> GetListCompleted;
        event EventHandler<GenericSdkEventArgs<DiskItemInfo>> GetItemInfoCompleted;
        event EventHandler<SdkEventArgs> MakeFolderCompleted;
        event EventHandler<SdkEventArgs> RemoveCompleted;
        event EventHandler<SdkEventArgs> TrashCompleted;
        event EventHandler<SdkEventArgs> MoveCompleted;
        event EventHandler<SdkEventArgs> CopyCompleted;
        event EventHandler<SdkEventArgs> UnpublishCompleted;
        event EventHandler<GenericSdkEventArgs<string>> PublishCompleted;
        event EventHandler<GenericSdkEventArgs<string>> IsPublishedCompleted;

        string AccessToken { get; }

        void GetListAsync(string path = "/");
        void GetListPageAsync(string path, int pageSize, int pageIndex);
        void GetItemInfoAsync(string path);
        void MakeDirectoryAsync(string fullPath);
        void RemoveAsync(string path);
        void TrashAsync(string path);
        void MoveAsync(string source, string destination);
        void CopyAsync(string source, string destination);
        void PublishAsync(string path);
        void UnpublishAsync(string path);
        void IsPublishedAsync(string path);
    }
}
```

Methods and events have descriptive names, they are documented in standard XML C# format. 

## Where to continue from here

Great, you made it so far. Now feel free to dig into the SDK, fork, remove, patch, add whatever you think things should be done different. And if you feel that your changes are ready for a broader audience, send us your pull requests.


## FAQ

### Which types of applications and frameworks are supported by the SDK?

.NET 4.0 applications.
Windows Phone applications (7.5 and higher).
Windows RT (Windows Store) applications.

### Is there any more documentation available?

The SDK classes, interfaces, functions have comments in standard C# XML format. Besides that there is documentation about the web API on the [Yandex Disk API page][DISKAPI] and [Yandex OAuth 2 API page][AUTHAPI].

## License

Лицензионное соглашение на использование набора средств разработки «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement

License agreement on use of Toolkit «SDK Яндекс.Диска» available at: http://legal.yandex.ru/sdk_agreement


[LICENSE]: http://legal.yandex.ru/sdk_agreement
[DISKAPI]: http://api.yandex.ru/disk/ "Yandex Disk API page"
[AUTHAPI]: http://api.yandex.ru/oauth/ "Yandex OAuth 2 API page"
[REGISTER]: https://oauth.yandex.ru/client/new "Yandex OAuth app registration page"

