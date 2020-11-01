# HttpHost
An HttpListener wrapper class that utilizes asynchronous HTTP request processing in the C# language.

This is an example of how to utilize the `System.Net.HttpListener` class within .NET with multithreaded operations allowing for custom API or web server configurations.

## Usage
### Step 1: Configure Windows HTTP API
If you're using HTTPS, you must install the certificate to your local machine's certificate store before running these commands. If you plan on using both HTTP and HTTPS, you must run both commands. If you are only using one, you just need to run the one you need.
| Protocol | Command Usage |
| --------- | ------------- |
| HTTP | `netsh http add urlacl url=[URIPrefix] user=[Username] listen=yes`  |
| HTTPS | ```
netsh http add urlacl url=[URIPrefix] user=[Username] listen=yes
netsh http add sslcert ipport=[IPAddress]:[Port] certhash=[CertificateThumbprint] appid={[AppGUID]}
```  |

**Parameters**:
1. `[URIPrefix]` - The URI that your app will listen to. Make sure that the 'http' prefix matches the protocol type you are binding it to. Examples are:

| Prefix | Outcome |
| --------- | ------------- |
| http://+:80/ | all requests to port 80. |
| http://+:80/MyApp/ | all requests to port 80, directed to the 'MyApp' directory. |
| http://192.168.5.100:990/ | all requests on a specific local adapter on port 990 |
| https://+:443/ | all requests to default HTTPS port 443. |

2. `[IPAddress]` and `[Port]` - The IP Address and port number the listener should attach to. 0.0.0.0 is all interfaces/addresses, 127.0.0.1 restricts to localhost, otherwise use desired listening IP address.
2. `[Username]` - The "DOMAIN\User" or User Group permitted to utilize the binding (the keyword "everyone" allows all users to attach listener applications to the prefix entry).
3. `[CertificateThumbprint]` - The SHA Hash of the certificate being bound to the prefix (Found on the "Details" tab on the certificate's properties window. Only works if certificate is installed to local store).
4. `[AppGUID]` - The GUID of the assembly that will utilize the binding. In Visual Studio, this can be found by opening your project's Properties window, and clicking the "Assembly Information" button. the GUID must be encapsulated in curly brackets {}.

### Step 2: C# Code Sample
Code:
```csharp
class Program
{
    static void Main(string[] args)
    {

        //create the HttpHost class instance
        oobi.HttpHostCore.HttpHost host = new oobi.HttpHostCore.HttpHost();

        //define URL prefixes for the HttpHost to bind to
        host.PrefixBindings.Add("http://+:80/");
        host.PrefixBindings.Add("https://+:443/MyApp/");

        //start the server asynchronously (you can do other things on this thread while the listener is starting).
        Write("Starting server");
        host.StartHostAsync(Message_GetRequest);
        do
        {
            Write(".");
        } while (host.IsStarting == true);
        WriteLine("OK!");

        //while the listener is running, your main thread can still do work.
        do
        {
            // simulate other application work here
            string input = ReadLine();
            WriteLine(input);
        } while (host.IsRunning == true);

    }

    //This function is called on each incoming request. Each incoming request is designated it's own thread.
    private static string Message_GetRequest(oobi.HttpHostCore.Components.StateInfo scene)
    {

        var datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        WriteLine($"[{datetime}] REQUEST: {scene.Request.HttpMethod}: {scene.Request.RawUrl}");

        return ("<h1>It Works!</h1>");

    }
}
```
