# Windows Dev Center Symbolicater

This is a console application which allows you to symbolicate the stacktraces of .Net Native UWP apps in the Windows Dev Center.
You can use it offline with a downloaded stacktrace or it can get the stacktraces automatically from the Windows Dev Center.

### Usage with single .tsv stacktrace:

1. Download the .tsv stacktrace from the Dev Center
2. Download the corresponding PDB from the Dev Center
3. Use like this:
	```
	wdcs -tsv stackTrace.tsv -x64 MyApp.pdb -output result.txt*
    ```
    
### Usage with Dev Center API:

1. Complete prerequisites for using the Microsoft Store analytics API.
	
    Do the steps described in Step 1 in https://docs.microsoft.com/en-us/windows/uwp/monetize/access-analytics-data-using-windows-store-services#prerequisites to obtain your tenant id, client id and your secret key.
    
    NOTE: When creating an Azure AD application the Reply URL and App ID URI don't matter. Just enter something valid.


2. Download the corresponding PDB(s) from the Dev Center
3. Use like this:
	
    a) Symbolicate one crash with failureHash. You can find failureHash in URL of a crash in the Dev Center. 
	```
    wdcs -failureCrash <failureCrash> -tenant <tenant id> -client <client id> -key <secret key> -app <appId> -x64 MyApp.pdb -output result.txt
    ```
    
   b) Symbolicate multiple crashes in a specified range. 
	```
    wdcs -version 1.0.0.0 -tenant <tenant id> -client <client id> -key <secret key> -app <appId> -x86 MyApp-x86.pdb -x64 MyApp-x64.pdb -arm MyApp-arm.pdb -xbox MyApp-xbox.pdb -start 11/15/2017 -end 11/20/2017 -output result.txt
    ```


### The usage of the program is also displayed in the console window:
```
Choose exact one argument:
-version <your app version> (This will symbolicate multiple crashes specific to a version)
-failureHash <failureHash> (This will symbolicate one crash. You can find this in the url of a crash in the Dev Center)
-tsv <path to .tsv stacktrace> (Enables OFFLINE mode: Symbolicates the downloaded stacktrace from the Dev Center)

Additional required arguments if not in OFFLINE mode:
-tenant <your Azure AD tenant id>
-client <your client id of the Azure AD application>
-key <your secret key of the Azure AD application>
-app <your app id>

PDB locations (1 required)
-x86 <path to x86-pdb>
-x64 <path to x64-pdb>
-arm <path to arm-pdb>
-xbox <path to xbox-pdb>

Optional:
-output <path to output file> (Default=console>
-start <start date> (Format: MM-DD-YYYY)
-end <end date> (Format MM-DD-YYYY)
-preventDuplication <true|false> (If true, it will process only one crash occurence of the same crash. Default=true)


EXAMPLES:

Symbolicates a single .tsv file (offline mode):
-tsv stackTrace.tsv -x64 MyApp.pdb -output result.txt

Symbolicates one crash using failureHash:
-failureCrash 1234abcd-1234-abcd-1234-1234abcd1234 -tenant abcd-1234-abcd-1234-abcdefghijkl -client abcd1234-abcd-1234-abcd-abcdefghijkl -key 123456789ABFGHRFKHHHHHHHHEEEDDSWWHHOORFRDFG= -app ABCDEDFG1234 -x64 MyApp.pdb -output result.txt

Symbolicates multiple crashes specified in a range:
-version 1.0.0.0 -tenant abcd-1234-abcd-1234-abcdefghijkl -client abcd1234-abcd-1234-abcd-abcdefghijkl -key 123456789ABFGHRFKHHHHHHHHEEEDDSWWHHOORFRDFG= -app ABCDEDFG1234 -x86 MyApp-x86.pdb -x64 MyApp-x64.pdb -arm MyApp-arm.pdb -xbox MyApp-xbox.pdb -start 11/15/2017 -end 11/20/2017 -output result.txt
```




### Uses code from:

 * [StackParser](https://github.com/dotnet/corefx-tools/tree/master/src/StackParser)
