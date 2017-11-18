using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackParser;

namespace WindowsDevCenterStacktraceSymbolicater
{
    class Program
    {
        const string DevCenterApiUri = "https://manage.devcenter.microsoft.com";
        private static bool _interactive = false;
        private static string _accessToken = null;
        private static string _tenantId = null;
        private static string _clientId = null;
        private static string _clientSecret = null;
        private static string _x86Pdb = null;
        private static string _x64Pdb = null;
        private static string _armPdb = null;
        private static string _xboxPdb = null;
        private static string _appId = null;
        private static string _appVersion = null;
        private static string _outputPath = null;
        private static string _tsvPath = null;
        private static string _start = null;
        private static string _end = null;
        private static string _failureHash = null;
        private static bool _preventDuplication = true;
        private static TextWriter _writer = Console.Out;

        static void Main(string[] args)
        {

            try
            {
                Console.SetIn(new StreamReader(Console.OpenStandardInput(), Console.InputEncoding,false,bufferSize: 1024));
                if (args.Length == 0)
                {
                    PrintUsage();
                    _interactive = true;
                    Console.WriteLine("Enter arguments:");
                    string enteredArgs = Console.ReadLine();
                    args = ParseText(enteredArgs, ' ', '"').ToArray();
                    if (args.Length == 0 || String.IsNullOrEmpty(enteredArgs))
                    {
                        return;
                    }
                }

                ParseArguments(args);

                if (!String.IsNullOrEmpty(_outputPath))
                {
                    // Create file (appened if it exists)
                    _writer = new StreamWriter(_outputPath, true);
                }



                // Retrieve an Azure AD access token

                // Call the Windows Store analytics API
                if (!String.IsNullOrEmpty(_x64Pdb))
                {
                    SymbolicateInRightMode(_x64Pdb,"x64", "These are the symbolicated crashes for x64-Architecture:");
                }
                if (!String.IsNullOrEmpty(_x86Pdb))
                {
                    SymbolicateInRightMode(_x86Pdb, "x86", "These are the symbolicated crashes for x86-Architecture:");
                }
                if (!String.IsNullOrEmpty(_armPdb))
                {
                    SymbolicateInRightMode(_armPdb, "arm", "These are the symbolicated crashes for arm-Architecture:");
                }
                if (!String.IsNullOrEmpty(_xboxPdb))
                {
                    SymbolicateInRightMode(_xboxPdb, "x64", "These are the symbolicated crashes for the Xbox One:");
                }

                if (_interactive || String.IsNullOrEmpty(_outputPath))
                {
                    Console.WriteLine("Press a key to exit");
                    Console.Read();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error has occured:\n" + e);
                if (_interactive || String.IsNullOrEmpty(_outputPath))
                {
                    Console.WriteLine("Press a key to exit");
                    Console.Read();
                }
            }

            _writer?.Close();
        }

        private static void SymbolicateInRightMode(string pdb, string arch, string header)
        {
            string fullPdbPath = Path.GetFullPath(pdb);
            if (!String.IsNullOrEmpty(_tsvPath))
            {
                HandleTsvStacktrace(Path.GetFullPath(_tsvPath), DiaHelper.LoadPDB(fullPdbPath));
            }
            else if (!String.IsNullOrEmpty(_failureHash))
            {
                _accessToken = GetClientCredentialAccessToken(_tenantId, _clientId, _clientSecret, DevCenterApiUri).Result;
                HandleCrash(_failureHash, DiaHelper.LoadPDB(fullPdbPath), 1);
            }
            else
            {
                _accessToken = GetClientCredentialAccessToken(_tenantId, _clientId, _clientSecret, DevCenterApiUri).Result;
                var crashes = GetCrashes(arch);
                HandleCrashes(crashes, fullPdbPath, header);
            }
        }

        private static dynamic GetCrashes(string architecture)
        {
            string requestUri;
            
            //// Get app failures
            bool useTimeFilter = !String.IsNullOrEmpty(_start) && !String.IsNullOrEmpty(_end);
            if (useTimeFilter)
            {
                requestUri = $"https://manage.devcenter.microsoft.com/v1.0/my/analytics/failurehits?applicationId={_appId}&startDate={_start}&endDate={_end}&top=1000&skip=0&filter=packageVersion%20eq%20%27{_appVersion}%27%20and%20osArchitecture%20eq%20%27{architecture}%27";
            }
            else 
            {
                requestUri = $"https://manage.devcenter.microsoft.com/v1.0/my/analytics/failurehits?applicationId={_appId}&top=1000&skip=0&filter=packageVersion%20eq%20%27{_appVersion}%27%20and%20osArchitecture%20eq%20%27{architecture}%27";
            }

            dynamic result;
            using (HttpClient client = new HttpClient(new HttpClientHandler()))
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        result = JsonConvert.DeserializeObject(responseContent);
                    }
                }
            }

            return result.Value;

        }

        private static dynamic GetCrashDetails(string crashHash)
        {
            string requestUri = $"https://manage.devcenter.microsoft.com/v1.0/my/analytics/failuredetails?applicationId={_appId}&failureHash={crashHash}";


            dynamic result;
            using (HttpClient client = new HttpClient(new HttpClientHandler()))
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        result = JsonConvert.DeserializeObject(responseContent);
                    }
                }
            }

            return result.Value;

        }

        private static dynamic GetCrashStacktrace(string cabId)
        {
            string requestUri = $"https://manage.devcenter.microsoft.com/v1.0/my/analytics/stacktrace?applicationId={_appId}&cabId={cabId}";


            dynamic result;
            using (HttpClient client = new HttpClient(new HttpClientHandler()))
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        result = JsonConvert.DeserializeObject(responseContent);
                    }
                }
            }

            return result.Value;

        }


        private static void HandleCrashes(dynamic crashes, string pdbFile, string header)
        {
            var pdbSession = DiaHelper.LoadPDB(pdbFile);
            int crashCount = 0;
            _writer.WriteLine();
            _writer.WriteLine();
            _writer.WriteLine();
            _writer.WriteLine();
            _writer.WriteLine();
            _writer.WriteLine(header);
            foreach (var crash in crashes)
            {
                crashCount++;
                string hash = crash.failureHash;
                HandleCrash(hash,pdbSession,crashCount);
            }

        }

        private static void HandleCrash(string hash, IDiaSession pdbSession, int crashCount)
        {
            if (!String.IsNullOrEmpty(hash))
            {
                var crashDetails = GetCrashDetails(hash);
                int crashOccurence = 0;
                foreach (var crashDetail in crashDetails)
                {
                    crashOccurence++;
                    string cabId = crashDetail.cabId;
                    if (!String.IsNullOrEmpty(cabId))
                    {
                        var stackTrace = GetCrashStacktrace(cabId);
                        _writer.WriteLine();
                        _writer.WriteLine();
                        _writer.WriteLine();
                        _writer.WriteLine($"Crash #{crashCount}.{crashOccurence}: {crashDetail.failureName}");
                        _writer.WriteLine();
                        if (stackTrace != null)
                        {
                            foreach (var stackLine in stackTrace)
                            {
                                string methodName = stackLine.function;
                                if (String.IsNullOrEmpty(methodName) || methodName == "null")
                                {
                                    string fullOffset = stackLine.offset;
                                    string hexOffset = fullOffset.Replace("0x", "");
                                    uint offset = HexToUint(hexOffset);
                                    if (pdbSession != null && offset != 0)
                                    {
                                        methodName = DiaHelper.GetMethodName(pdbSession, offset);
                                    }
                                }
                                _writer.WriteLine($"at {stackLine.image}!{methodName}");
                            }
                            if (_preventDuplication)
                            {
                                break;
                            }
                        }

                    }
                }

            }
        }

        private static void HandleTsvStacktrace(string tsvPath, IDiaSession pdbSession)
        {
            var lines= File.ReadAllLines(tsvPath);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] frags = lines[i].Split('\t');
                string methodName = "";
                string module = "";
                if (frags.Length >= 4)
                {
                    module = frags[1];
                    string rvaStr = frags[3];
                    string rva = rvaStr.Replace("0x", "");
                    if (frags[2] != "null")
                    {
                        methodName = frags[2];
                    }
                    uint offset = HexToUint(rva);
                    if (pdbSession != null && offset != 0)
                    {
                        string tmpMethodName = DiaHelper.GetMethodName(pdbSession, offset);
                        if (!String.IsNullOrEmpty(tmpMethodName))
                        {
                            methodName = tmpMethodName;
                        }
                    }
                }
                _writer.WriteLine($"at {module}!{methodName}");
            }
        }

        static uint HexToUint(string hexStr)
        {
            try
            {
                return Convert.ToUInt32(hexStr, 16);
            }
            catch (FormatException)
            {
                return 0;
            }
        }

        public static async Task<string> GetClientCredentialAccessToken(string tenantId, string clientId, string clientSecret, string scope)
        {
            string tokenEndpointFormat = "https://login.microsoftonline.com/{0}/oauth2/token";
            string tokenEndpoint = string.Format(tokenEndpointFormat, tenantId);

            dynamic result;
            using (HttpClient client = new HttpClient())
            {
                string tokenUrl = tokenEndpoint;
                using (HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Post,tokenUrl))
                {
                    string content = $"grant_type=client_credentials&client_id={clientId}&client_secret={clientSecret}&resource={scope}";

                    request.Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject(responseContent);
                    }
                }
            }

            return result.access_token;
        }

        static void PrintUsage()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Choose exact one argument:");
            Console.WriteLine("-version <your app version> (This will symbolicate multiple crashes specific to a version)");
            Console.WriteLine("-failureHash <failureHash> (This will symbolicate one crash. You can find this in the url of a crash in the Dev Center)");
            Console.WriteLine("-tsv <path to .tsv stacktrace> (Enables OFFLINE mode: Symbolicates the downloaded stacktrace from the Dev Center)");
            Console.WriteLine();
            Console.WriteLine("Additional required arguments if not in OFFLINE mode: ");
            Console.WriteLine("-tenant <your Azure AD tenant id>");
            Console.WriteLine("-client <your client id of the Azure AD application>");
            Console.WriteLine("-key <your secret key of the Azure AD application>");
            Console.WriteLine("-app <your app id>");
            Console.WriteLine();
            Console.WriteLine("PDB locations (1 required)");
            Console.WriteLine("-x86 <path to x86-pdb>");
            Console.WriteLine("-x64 <path to x64-pdb>");
            Console.WriteLine("-arm <path to arm-pdb>");
            Console.WriteLine("-xbox <path to xbox-pdb>");
            Console.WriteLine();
            Console.WriteLine("Optional:");
            Console.WriteLine("-output <path to output file> (Default=console>");
            Console.WriteLine("-start <start date> (Format: MM-DD-YYYY)");
            Console.WriteLine("-end <end date> (Format MM-DD-YYYY)");
            Console.WriteLine("-preventDuplication <true|false> (If true, it will process only one crash occurence of the same crash. Default=true)");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine();
            Console.WriteLine("Symbolicates a single .tsv file (offline mode):");
            Console.WriteLine("-tsv stackTrace.tsv -x64 MyApp.pdb -output result.txt");
            Console.WriteLine();
            Console.WriteLine("Symbolicates one crash using failureHash:");
            Console.WriteLine("-failureCrash 1234abcd-1234-abcd-1234-1234abcd1234 -tenant abcd-1234-abcd-1234-abcdefghijkl -client abcd1234-abcd-1234-abcd-abcdefghijkl -key 123456789ABFGHRFKHHHHHHHHEEEDDSWWHHOORFRDFG= -app ABCDEDFG1234 -x64 MyApp.pdb -output result.txt");
            Console.WriteLine();
            Console.WriteLine("Symbolicates multiple crashes specified in a range:");
            Console.WriteLine("-version 1.0.0.0 -tenant abcd-1234-abcd-1234-abcdefghijkl -client abcd1234-abcd-1234-abcd-abcdefghijkl -key 123456789ABFGHRFKHHHHHHHHEEEDDSWWHHOORFRDFG= -app ABCDEDFG1234 -x86 MyApp-x86.pdb -x64 MyApp-x64.pdb -arm MyApp-arm.pdb -xbox MyApp-xbox.pdb -start 11/15/2017 -end 11/20/2017 -output result.txt");
            Console.WriteLine();
            Console.WriteLine();

        }

        private static void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    if (args[i] == "-tenant")
                    {
                        _tenantId = args[i + 1];
                    }
                    else if (args[i] == "-client")
                    {
                        _clientId = args[i + 1];
                    }
                    else if (args[i] == "-key")
                    {
                        _clientSecret = args[i + 1];
                    }
                    else if (args[i] == "-app")
                    {
                        _appId = args[i + 1];
                    }
                    else if (args[i] == "-version")
                    {
                        _appVersion = args[i + 1];
                    }
                    else if (args[i] == "-tsv")
                    {
                        _tsvPath = args[i + 1];
                    }
                    else if (args[i] == "-failureHash")
                    {
                        _failureHash = args[i + 1];
                    }
                    else if (args[i] == "-x86")
                    {
                        _x86Pdb = args[i + 1];
                    }
                    else if (args[i] == "-x64")
                    {
                        _x64Pdb = args[i + 1];
                    }
                    else if (args[i] == "-arm")
                    {
                        _armPdb = args[i + 1];
                    }
                    else if (args[i] == "-xbox")
                    {
                        _xboxPdb = args[i + 1];
                    }
                    else if (args[i] == "-output")
                    {
                        _outputPath = args[i + 1];
                    }
                    else if (args[i] == "-start")
                    {
                        _start = args[i + 1];
                    }
                    else if (args[i] == "-end")
                    {
                        _end = args[i + 1];
                    }
                    else if (args[i] == "-preventDuplication")
                    {
                        if (args[i + 1] == "false")
                        {
                            _preventDuplication = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        private static IEnumerable<String> ParseText(String line, Char delimiter, Char textQualifier)
        {

            if (line == null)
                yield break;
            else
            {
                Char prevChar = '\0';
                Char nextChar = '\0';
                Char currentChar = '\0';

                Boolean inString = false;

                StringBuilder token = new StringBuilder();

                for (int i = 0; i < line.Length; i++)
                {
                    currentChar = line[i];

                    if (i > 0)
                        prevChar = line[i - 1];
                    else
                        prevChar = '\0';

                    if (i + 1 < line.Length)
                        nextChar = line[i + 1];
                    else
                        nextChar = '\0';

                    if (currentChar == textQualifier && (prevChar == '\0' || prevChar == delimiter) && !inString)
                    {
                        inString = true;
                        continue;
                    }

                    if (currentChar == textQualifier && (nextChar == '\0' || nextChar == delimiter) && inString)
                    {
                        inString = false;
                        continue;
                    }

                    if (currentChar == delimiter && !inString)
                    {
                        yield return token.ToString();
                        token = token.Remove(0, token.Length);
                        continue;
                    }

                    token = token.Append(currentChar);

                }

                yield return token.ToString();

            }
        }
    }
}
