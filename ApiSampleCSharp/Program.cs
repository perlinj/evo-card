using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Serilog;

namespace ApiSampleCSharp
{
    public enum MenuOption
    {
        UNKNOWN = 0,
        GET_STATUS = 1,
        TERMINAL_INFO = 2,
        SALE_TRANSACTION = 3,
        HANDLE_BATCH = 4,
        RECONCILIATION = 5,
        CONNECTION_TEST = 6,
        CONNECTION_TO_TMS = 7,
        EXIT = 8
    }

    class ApiSampleCSharp
    {
        private static EserviceApi eserviceApi;

        static void Main(string[] args)
        {
            eserviceApi = new EserviceApi();

            initializeConnection();
            eserviceApi.initTerminalSettings();
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/application_log.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging initialization failed: {ex.Message}");
            }
            mainLoop();
            // Configure Serilog to write to a single log file
            
        }

        //private static bool getAmountAndExecuteSaleTransaction(bool anotherTry, decimal amount)
        //{
        //    Console.Write("Sale transaction amount: ");
        //    int amountInCents = (int)(amount * 100);
        //    Console.WriteLine($"Sale transaction amount2: {amountInCents}");
        //    Console.WriteLine($"Sale amount to string :{amountInCents.ToString()}");
        //    return eserviceApi.executeSaleTransaction(amountInCents.ToString(), anotherTry);
        

        //}

        private static string getAmountAndExecuteSaleTransaction(bool anotherTry, decimal amount)
        {
            // You no longer need to prompt for input, since amount is now a parameter

            Console.WriteLine($"Sale transaction amount: {amount}");
            Log.Information($"Sale transaction amount: {amount}");
            amount = Convert.ToInt32(amount * 100);
            Console.WriteLine($"Sale transaction amount2: {amount}");
            Console.WriteLine($"Sale amount to string :{amount.ToString()}");
            // Convert amount to string and pass to eserviceApi
            if (eserviceApi.executeSaleTransaction(amount.ToString(), anotherTry))
            {
                return "success";
            }
            else
            {
                return "error";
            }



        }


        private static MenuOption getMenuOption()
        {
            Console.WriteLine("Choose action: ");
            Console.Write((int)MenuOption.GET_STATUS + ". Get terminal status.\n"
                + (int)MenuOption.TERMINAL_INFO + ". Get terminal info.\n"
                + (int)MenuOption.SALE_TRANSACTION + ". Perform sale transaction.\n"
                + (int)MenuOption.HANDLE_BATCH + ". Handle batch.\n"
                + (int)MenuOption.RECONCILIATION + ". Perform reconciliation.\n"
                + (int)MenuOption.CONNECTION_TEST + ". Perform connection test.\n"
                + (int)MenuOption.CONNECTION_TO_TMS + ". Perform connection to TMS.\n"
                + (int)MenuOption.EXIT + ". Exit.\n");
            try
            {
                return (MenuOption)Convert.ToInt32(Console.ReadLine());
            }
            catch (System.FormatException e)
            {
                return MenuOption.UNKNOWN;
            }
        }

        private static bool getConnectionDataFromUser(out String terminalIp, out ushort terminalPort, out String terminalSerialPort)
        {
            string answer;
            string ip;
            ushort port;
            string portStr;

            terminalIp = "";
            terminalPort = 0;
            terminalSerialPort = "";

            //Console.WriteLine("Pass terminal ip or port name if you are using serial link" +
            //    "(for ex. '127.0.0.1' or 'COM1'):");
            //answer = Console.ReadLine();
            answer = "192.168.1.45";
            if (answer.Contains("."))
            {
                ip = answer;
                //Console.WriteLine("Pass terminal port or leave empty to default(3000):");
                //portStr = Console.ReadLine();
                portStr = "";
                if (String.IsNullOrEmpty(portStr))
                {
                    portStr = "3000";
                }
                try
                {
                    port = Convert.ToUInt16(portStr);
                }
                catch (System.FormatException e)
                {
                    return false;
                }
                if (validateIPv4(ip) && port < UInt16.MaxValue && port > UInt16.MinValue)
                {
                    terminalIp = ip;
                    terminalPort = port;
                    return true;
                }
                return false;
            }
            else
            {
                terminalSerialPort = answer;
                return !String.IsNullOrEmpty(terminalSerialPort);
            }
        }


        // Helper method to send response to a specific URL
        //private static async Task SendResponseToUrlAsync(string url, int status, string orderid)
        //{
        //    // Configure HttpClientHandler to handle SSL/TLS issues
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        // Bypass SSL certificate validation (useful for testing in development)
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    // Set security protocol explicitly to TLS 1.2
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //    using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) }) // Increase timeout to 30 seconds
        //    {
        //        try
        //        {
        //            // Create the response data
        //            var responseData = new
        //            {
        //                status = status,
        //                orderid = orderid
        //            };

        //            // Serialize the response data to JSON
        //            var jsonContent = JsonConvert.SerializeObject(responseData);
        //            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //            Console.WriteLine("Sending request to " + url);
        //            Console.WriteLine("Request content: " + jsonContent);

        //            // Send the POST request
        //            HttpResponseMessage response = await client.PostAsync(url, content);

        //            // Check if the response is successful
        //            if (response.IsSuccessStatusCode)
        //            {
        //                Console.WriteLine("Response successfully sent to " + url);
        //            }
        //            else
        //            {
        //                Console.WriteLine($"Failed to send response to {url}. Status code: {response.StatusCode}");
        //                string responseBody = await response.Content.ReadAsStringAsync();
        //                Console.WriteLine("Response body: " + responseBody);
        //            }
        //        }
        //        catch (HttpRequestException e)
        //        {
        //            Console.WriteLine("HTTP Request error: " + e.Message);
        //            if (e.InnerException != null)
        //            {
        //                Console.WriteLine("Inner exception: " + e.InnerException.Message);
        //            }
        //        }
        //        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        //        {
        //            Console.WriteLine("Request timed out: " + e.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Error sending response to URL: " + ex.Message);
        //            if (ex.InnerException != null)
        //            {
        //                Console.WriteLine("Inner exception: " + ex.InnerException.Message);
        //            }
        //        }
        //    }
        //}


        private static async Task SendResponseToUrlAsync(string url, int status, string orderid)
        {
            // Configure HttpClientHandler to handle SSL/TLS issues
            HttpClientHandler handler = new HttpClientHandler
            {
                // Bypass SSL certificate validation (useful for testing in development)
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            // Set security protocol explicitly to TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
            {
                try
                {
                    // Create the response data
                    var responseData = new
                    {
                        status = status,
                        orderid = orderid
                    };

                    // Serialize the response data to JSON
                    var jsonContent = JsonConvert.SerializeObject(responseData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Debugging logs to console
                    Console.WriteLine("Sending request to " + url);
                    Console.WriteLine("Request content: " + jsonContent);

                    // Send the POST request
                    using (HttpResponseMessage response = await client.PostAsync(url, content))
                    {

                        // Check if the response is successful
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Response successfully sent to " + url);
                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("Response body: " + responseBody);
                        }
                        else
                        {
                            // Handle non-successful responses
                            Console.WriteLine($"Failed to send response to {url}. Status code: {response.StatusCode}");
                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("Response body: " + responseBody);
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    // Handle HttpRequestException
                    Console.WriteLine("HttpRequestException: " + e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: " + e.InnerException.Message);
                    }
                }
                catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
                {
                    // Handle timeout
                    Console.WriteLine("Request timed out: " + e.Message);
                }
                catch (Exception ex)
                {
                    // Handle general exceptions
                    Console.WriteLine("An error occurred: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: " + ex.InnerException.Message);
                    }
                }
                finally
                {
                    // Final log statement to confirm completion
                    Console.WriteLine("Request completed at " + DateTime.Now);
                }
            }
        }


        private static async Task SimpleTestAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent("{\"test\":\"value\"}", Encoding.UTF8, "application/json");
                try
                {
                    Console.WriteLine("Preparing to send request...");
                    HttpResponseMessage response = await client.PostAsync("http://httpbin.org/post", content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Response body: " + responseBody);
                    }
                    else
                    {
                        Console.WriteLine($"Failed. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
            }
        }

        private static async void mainLoop()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/"); // Listen on this URL
            listener.Start();
            Console.WriteLine("Listening for requests...");

            try
            {
                while (true)
                {
                    // Wait for a client request
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    // Read the request body (assuming it's POST data)
                    string requestBody;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }

                    Console.WriteLine("Received request data: " + requestBody);

                    // Parse the request data
                    var requestData = ParseRequestData(requestBody);

                    // Determine the action based on the 'type' value
                    string responseBody;
                    //int status = 0;
                    int statusCode = 200;
                    
                    if (requestData.TryGetValue("type", out string typeValue))
                    {
                        // Attempt to get the orderid from the request data
                        
                        switch (typeValue)
                        {
                            case "1": // Example: GET_STATUS
                                eserviceApi.getTerminalStatus();
                                responseBody = "Terminal status retrieved.";
                                break;
                            case "2": // Example: TERMINAL_INFO
                                eserviceApi.getTerminalInfo();
                                responseBody = "Terminal info retrieved.";
                                break;
                            case "3": // Example: SALE_TRANSACTION
                                string orderid = string.Empty;
                                if (requestData.TryGetValue("orderid", out string orderIdValue))
                                {
                                    orderid = orderIdValue; // Assign the extracted order ID
                                }
                                if (requestData.TryGetValue("amount", out string amountStr) && decimal.TryParse(amountStr, out decimal amount))
                                {

                                    responseBody = getAmountAndExecuteSaleTransaction(false, amount);
                                    if(responseBody == "success")
                                    {
                                                                              
                                        responseBody = "Transaction Accepted";
                                    }
                                    else
                                    {
                                        statusCode = 400;
                                        responseBody = "Transaction Error";
                                    }
                                    
                                }
                                else
                                {
                                    responseBody = "Amount is missing or invalid.";
                                    statusCode = 400;
                                }
                                break;
                            case "4": // Example: HANDLE_BATCH
                                eserviceApi.handleBatch();
                                responseBody = "Batch handled.";
                                break;
                            case "5": // Example: RECONCILIATION
                                eserviceApi.forceReconciliation();
                                responseBody = "Reconciliation forced.";
                                break;
                            case "6": // Example: CONNECTION_TEST
                                eserviceApi.forceConnectionTestToAuthorizationHost();
                                responseBody = "Connection test to authorization host forced.";
                                break;
                            case "7": // Example: CONNECTION_TO_TMS
                                eserviceApi.forceConnectionToTMS();
                                responseBody = "Connection to TMS forced.";
                                break;
                            case "EXIT":
                                responseBody = "Server is stopping.";
                                listener.Stop(); // Stop the listener
                                break;
                            default:
                                responseBody = "Unknown type.";
                                statusCode = 400;
                                break;
                        }
                    }
                    else
                    {
                        responseBody = "Type is missing.";
                        statusCode = 400;
                    }
                    //string url = "http://localhost/joopos1.0.9-w/public/paymentstatus";
                    //await SendResponseToUrlAsync(url, statusCode, orderid);
                    //await SimpleTestAsync();
                    // Send the response back to the PHP client
                    // Send the response
                    HttpListenerResponse response = context.Response;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = statusCode;

                    using (Stream output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer, 0, buffer.Length);
                    }

                    Console.WriteLine(responseBody);
                    // Break the loop if the EXIT type was processed
                    if (typeValue == "EXIT")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                listener.Close();
            }
        }

        private static Dictionary<string, string> ParseRequestData(string requestBody)
        {
            var data = new Dictionary<string, string>();
            var pairs = requestBody.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    data[WebUtility.UrlDecode(keyValue[0])] = WebUtility.UrlDecode(keyValue[1]);
                }
            }
            return data;
        }
        private static void initializeConnection()
        {
            string ip;
            ushort port;
            string serialPort;

            while (true)
            {
                if (!getConnectionDataFromUser(out ip, out port, out serialPort))
                {
                    Console.WriteLine("Passed data are not valid. Try again.");
                    continue;
                }
                if (!String.IsNullOrEmpty(ip))
                {
                    if (eserviceApi.connectToTerminalTCPIP(ip, port))
                    {
                        break;
                    }
                    Console.WriteLine("Passed data are not valid or connection failed. Try again.");
                }
                else
                {
                    if (eserviceApi.connectToTerminalSerialLink(serialPort))
                    {
                        break;
                    }
                    Console.WriteLine("Passed data are not valid or connection failed.Try again.");
                }
            }
        }

        private static bool validateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            foreach (string temp in splitValues)
            {
                if (!byte.TryParse(temp, out tempForParsing))
                {
                    return false;
                }
            }
            return true;
        }
    }
}