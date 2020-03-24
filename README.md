# HttpHost
An HttpListener wrapper class that utilizes asynchronous HTTP request processing in the C# language.

This is an example of how to utilize the `System.Net.HttpListener` class within .NET with multithreaded operations allowing for custom API or web server configurations.

## Usage
Using the HttpHost class is easy, for both Asynchronous or synchronous operation. The difficult part is programing the Windows HTTP API to allow communication through the ports and prefixes you desire.

### Step 1: Configure Windows HTTP API
If you're using HTTPS, you must install the certificate to your local machine's certificate store before running these commands. If you plan on using both HTTP and HTTPS, you must run both commands. If you are only using one, you just need to run the one you need.
| Protocol | Command Usage |
| --------- | ------------- |
| HTTP | `netsh http add urlacl url=[URIPrefix] user=[Username] listen=yes`  |
| HTTPS | `netsh http add sslcert ipport=[URIPrefix] certhash=[CertificateThumbprint] appid={[AppGUID]}`  |

**Parameters**:
1. `[URIPrefix]` - The URI that your app will listen to. Make sure that the 'http' prefix matches the protocol type you are binding it to. More documentation here. Examples are:

| Prefix | Outcome |
| --------- | ------------- |
| http://+:80/ | all requests to port 80. |
| http://+:80/MyApp/ | all requests to port 80, directed to the 'MyApp' directory. |
| http://192.168.5.100:990/ | all requests on a specific local adapter on port 990 |
| https://+:443/ | all requests to default HTTPS port 443. |


2. `[Username]` - The "DOMAIN\User" or User Group permitted to utilize the binding ("Everyone" is allowed).
3. `[CertificateThumbprint]` - The SHA Hash of the certificate being bound to the prefix (Found on the "Details" tab on the certificate's properties window. Only works if certificate is installed to local store).
4. `[AppGUID]` - The GUID of the assembly that will utilize the binding. In Visual Studio, this can be found by opening your project's Properties window, and clicking the "Assembly Information" button. the GUID must be encapsulated in curly brackets {}.

### Step 2: C# utilization
Code:
```csharp
static void Main(string[] args) {
    List<string> prefixes = new List<string>();
    prefixes.Add("http://+:80/");
    prefixes.Add("https://+:443/");

    oobi.HttpHost host = new oobi.HttpHost(prefixes);

    Write("Starting server");
    host.StartHostAsync(Message_GetRequest);
    do {
        Write(".");
    } while (host.isStarting == true);
    WriteLine("OK!");
    
    do {
        // simulate other application work here
        string input = ReadLine();
        WriteLine(input);
    } while (host.isRunning == true);
    
    ReadLine();
}

private static string Message_GetRequest(oobi.HttpHost.StateInfo scene) {
    scene.Listener............... //HttpListener listening for requests.
    scene.Context................ //HttpContext that is responsible for processing this request.
    scene.Request................ //HttpRequest that contains the payload sent by the requestor.
    scene.Response............... //HttpResponse that will contain and deliver the server response payload.
    scene.AdditionalArguments.... //A List<object> that contains other potential classes you would like
                                  //to pass to this funtion.
    
    scene.CreationTime
    
    return("<b>This string is only sent if the response stream has not been closed.</b>");
}
```
