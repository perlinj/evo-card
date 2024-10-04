using System;
using System.IO;
using System.Text;
using EcrLibrary;
using Serilog;

namespace ApiSampleCSharp
{
    class EserviceApi
    {
        private const char CASH_REGISTER_NUMBER = '1';
        private const uint COMMUNICATION_TIMEOUT_MS = 10000;
        private const byte TERMINAL_INDEX = 1;
        private const comm_SerialDataMode SERIAL_DATA_MODE = comm_SerialDataMode.SERIAL_COM_8N1;
        private const comm_SerialBaudrate SERIAL_BAUDRATE = comm_SerialBaudrate.SERIAL_COM_BR_115200;

        private ecr_status status;
        private ecr_terminalStatus terminalStatus;

        private ecr_communicationProtocol protocol;
        private char cashRegisterNumber;
        private ecr_HandlingTerminalRequestsMode requestsMode;

        private string lastTransactionNumber;
        private string lastTransactionTime;
        private string lastTransactionDate;

        public EserviceApi()
        {
            protocol = ecr_communicationProtocol.PROTOCOL_ESERVICE;
            cashRegisterNumber = CASH_REGISTER_NUMBER;
            requestsMode = ecr_HandlingTerminalRequestsMode.REQUESTS_HANDLE_CHOSEN_BY_TERMINAL;

            EserviceCallbacks.registerCallbacks();
            EcrLib.initialize();

            PrintoutHandler.setupDictionaryFromFile("EN.LNG");
            PrintoutHandler.setUsingSignatureVerifiedLine(true);
        }

        ~EserviceApi()
        {
            EcrLib.cleanup();
        }

        public bool connectToTerminalTCPIP(string terminalIp, ushort terminalPort)
        {
            status = EcrLib.setTcpIpLink(terminalIp, terminalPort, COMMUNICATION_TIMEOUT_MS);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Tcp connection error.");
                return false;
            }
            return true;
        }

        public bool connectToTerminalSerialLink(string port)
        {
            status = EcrLib.setSerialLink(port, SERIAL_DATA_MODE, SERIAL_BAUDRATE, COMMUNICATION_TIMEOUT_MS);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Serial connection error.");
                return false;
            }
            return true;
        }

        public bool executeSaleTransaction(string amount, bool anotherTry)
        {
            if (!getStatus())
            {
                Console.WriteLine("Sale transaction. Connection error.");
                Log.Warning("Sale transaction. Connection error.");
                return false;
            }

            switch (terminalStatus)
            {
                case ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN:
                    if (anotherTry)
                    {
                        if (!emergencyProcedureForTransaction(anotherTry))
                        {
                            break;
                        }
                        return true;
                    }
                    break;
                case ecr_terminalStatus.STATUS_RECON_NEEDED:
                    Log.Warning("Sale transaction. Reconciliation needed.");
                    Console.WriteLine("Sale transaction. Reconciliation needed.");
                    return false;
                case ecr_terminalStatus.STATUS_BATCH_COMPLETED:
                    Log.Warning("Sale transaction. Read batch operation needed.");
                    Console.WriteLine("Sale transaction. Read batch operation needed.");
                    return false;
                case ecr_terminalStatus.STATUS_BUSY:
                    Log.Warning("Sale transaction. Terminal busy.");
                    Console.WriteLine("Sale transaction. Terminal busy.");
                    return false;
                case ecr_terminalStatus.STATUS_APP_ERROR:
                    Console.WriteLine("Sale transaction. Problem with terminal.");
                    Log.Error("Sale transaction. Problem with terminal.");
                    return false;
                default:
                    return emergencyProcedureForTransaction(anotherTry);
            }

            status = EcrLib.setTransactionType(ecr_transactionType.TRANS_SALE);
            if (ecr_status.ECR_OK != status)
            {
                Log.Error("Set transaction type. Unexpected error.");
                Console.WriteLine("Set transaction type. Unexpected error.");
                return false;
            }

            status = EcrLib.setTransactionAmount(amount);
          
            if (ecr_status.ECR_OK != status)
            {
                Log.Error("Set transaction type. Unexpected error.");
                Console.WriteLine("Set transaction amount. Unexpected error.");
                return false;
            }

            status = EcrLib.startTransaction();
            if (ecr_status.ECR_OK != status)
            {
                return emergencyProcedureForTransaction(true);
            }

            ecr_transactionResult result;
            status = EcrLib.readTransactionResult(out result);
            if (ecr_status.ECR_OK != status)
            {
                emergencyProcedureForTransaction(true);
            }

            saveTransactionData();
            if (EservicePrintoutHandler.isMerchantPrintoutNecessary())
            {
                EservicePrintoutHandler.generateMerchantPrintout();
            }
            EservicePrintoutHandler.generateCustomerPrintout();

            return checkTransactionResult(result);
        }

        public void forceConnectionTestToAuthorizationHost()
        {
            if (!getStatus())
            {
                Console.WriteLine("Force connection test to authorization host. Connection error.");
                return;
            }

            if (!checkTerminalStatus(ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN))
            {
                Console.WriteLine("Force connection test to autorization host. Unexpected error.");
                return;
            }

            status = EcrLib.setTransactionType(ecr_transactionType.TRANS_TEST_CONNECTION);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force connection test to authorization host. Set transaction type error.");
                return;
            }

            status = EcrLib.startTransaction();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force connection test to authorization host. Start transaction error.");
                return;
            }

            ecr_transactionResult result;
            status = EcrLib.readTransactionResult(out result);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Error: No information about connection test result.");
                return;
            }

            switch (result)
            {
                case ecr_transactionResult.RESULT_TRANS_ACCEPTED:
                    Console.WriteLine("Connection test succeed.");
                    break;
                case ecr_transactionResult.RESULT_TRANS_REFUSED:
                    Console.WriteLine("Connection test failed.");
                    break;
                case ecr_transactionResult.RESULT_NO_CONNECTION:
                    Console.WriteLine("Connection test failed - no connection.");
                    break;
                case ecr_transactionResult.RESULT_TRANS_INTERRUPTED_BY_USER:
                    Console.WriteLine("Operation interrupted by user.");
                    break;
                default:
                    Console.WriteLine("Unknown operation result.");
                    break;
            }
        }

        public void forceConnectionToTMS()
        {
            if (!getStatus())
            {
                Console.WriteLine("Force connection to TMS. Connection error.");
                return;
            }

            if (!checkTerminalStatus(ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN))
            {
                Console.WriteLine("Force connection to TMS. Unexpected error.");
                return;
            }

            status = EcrLib.setTransactionType(ecr_transactionType.TRANS_TMS);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force connection to TMS. Set transaction type error.");
                return;
            }

            status = EcrLib.startTransaction();
        }

        public void forceReconciliation()
        {
            if (!getStatus())
            {
                Console.WriteLine("Force reconnciliation. Connection error.");
                return;
            }

            if (!checkTerminalStatus(ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN) &&
                !checkTerminalStatus(ecr_terminalStatus.STATUS_RECON_NEEDED))
            {
                Console.WriteLine("Forece reconciliation. Unexpected error.");
                return;
            }

            status = EcrLib.setTransactionType(ecr_transactionType.TRANS_RECONCILE);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force reconciliation. Set transaction type error.");
                return;
            }

            status = EcrLib.startTransaction();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force reconciliation. Start transaction error.");
                return;
            }

            ecr_transactionResult result;
            status = EcrLib.readTransactionResult(out result);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Force reconciliation. No information about operation result.");
                return;
            }

            switch (result)
            {
                case ecr_transactionResult.RESULT_TRANS_ACCEPTED:
                    Console.WriteLine("Reconciliation succeed.");
                    break;
                case ecr_transactionResult.RESULT_NO_CONNECTION:
                    Console.WriteLine("Reconciliation failed - no connection.");
                    break;
                default:
                    Console.WriteLine("Reconciliation failed.");
                    break;
            }
        }

        public void getTerminalInfo()
        {
            status = EcrLib.readTerminalInformationData();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Get terminal info. Connection error.");
                return;
            }

            string tid;
            status = EcrLib.readTerminalId(out tid);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Get terminal info. Failed to get terminal id.");
                return;
            }

            Console.WriteLine("Terminal info (tid): " + tid);
        }

        public void getTerminalStatus()
        {
            if (!getStatus())
            {
                Console.WriteLine("Failed to get status.");
                return;
            }

            switch (terminalStatus)
            {
                case ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN:
                    Console.WriteLine("Terminal ready for new transaction.");
                    break;
                case ecr_terminalStatus.STATUS_RECON_NEEDED:
                    Console.WriteLine("Terminal demand to perform reconciliation.");
                    break;
                case ecr_terminalStatus.STATUS_BATCH_COMPLETED:
                    Console.WriteLine("Terminal demand to perform batch read operation.");
                    break;
                case ecr_terminalStatus.STATUS_BUSY:
                    Console.WriteLine("Terminal busy.");
                    break;
                case ecr_terminalStatus.STATUS_APP_ERROR:
                    Console.WriteLine("Terminal error.");
                    break;
                default:
                    Console.WriteLine("Last operation was not completed.");
                    break;
            }
        }

        public void handleBatch()
        {
            while (true)
            {
                if (!getStatus())
                {
                    Console.WriteLine("Handle batch - connection error.");
                    return;
                }

                if (!checkTerminalStatus(ecr_terminalStatus.STATUS_BATCH_COMPLETED))
                {
                    Console.WriteLine("End of batches.");
                    return;
                }

                EservicePrintoutHandler.generateReportFromBatch();
            }
        }

        public ecr_status initTerminalSettings()
        {
            EcrLib.setProtocol(protocol);

            byte[] bytes = BitConverter.GetBytes(cashRegisterNumber);
            bytes = new byte[] { bytes[0] };
            status = EcrLib.setCashRegisterId(bytes);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Cash registration error.");
                return status;
            }

            status = EcrLib.setHandleTerminalRequests(requestsMode);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Requests mode initialization error.");
                return status;
            }

            status = EcrLib.setTerminalIndex(TERMINAL_INDEX);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Failed to set terminal index.");
            }
            return status;
        }

        private bool checkTransactionResult(ecr_transactionResult result)
        {
            switch (result)
            {
                case ecr_transactionResult.RESULT_NO_CONNECTION:
                    Console.WriteLine("Transaction result: no connection.");
                    Log.Warning("Transaction result: no connection.");

                    return false;
                case ecr_transactionResult.RESULT_TRANS_ACCEPTED:
                    Log.Information("Transaction result: accepted.");
                    Console.WriteLine("Transaction result: accepted.");
                    return true;
                case ecr_transactionResult.RESULT_TRANS_INTERRUPTED_BY_USER:
                    Log.Warning("Transaction result: interrupted by user.");
                    Console.WriteLine("Transaction result: interrupted by user.");
                    return false;
                case ecr_transactionResult.RESULT_TRANS_REFUSED:
                    Console.WriteLine("Transaction result: refused.");
                    Log.Warning("Transaction result: refused.");
                    return false;
                default:
                    Console.WriteLine("Unknown transaction result.");
                    Log.Error("Unknown transaction result.");
                    return false;
            }
        }

        private bool checkTerminalStatus(ecr_terminalStatus expectedTerminalStatus)
        {
            if (expectedTerminalStatus != terminalStatus)
            {
                Console.WriteLine("Info: terminal status = " + terminalStatus
                    + " expected terminal status = " + expectedTerminalStatus);
                return false;
            }
            return true;
        }

        private bool compareTransactions()
        {
            if (!getStatus())
            {
                Console.WriteLine("Compare transactions. Connection error.");
                return false;
            }

            if (!checkTerminalStatus(ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN))
            {
                Console.WriteLine("Compare transactions. Unexpected error.");
                return false;
            }

            status = EcrLib.getLastTransactionData();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Compare transactions - failed to get last transaction data.");
                return false;
            }

            StringBuilder stringBuilder = new StringBuilder();
            status = EcrLib.readTransactionDate(stringBuilder);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Compare transactions - failed to get last transaction date.");
                return false;
            }
            string transactionDate = stringBuilder.ToString();

            status = EcrLib.readTransactionTime(stringBuilder);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Compare transactions - failed to get last transaction time.");
                return false;
            }
            string transactionTime = stringBuilder.ToString();

            string transactionNumber = readTransactionNumber();

            if (transactionNumber.Equals(lastTransactionNumber) &&
                transactionDate.Equals(lastTransactionDate) &&
                transactionTime.Equals(lastTransactionTime))
            {
                return true;
            }
            return false;
        }

        private bool emergencyProcedureForTransaction(bool anotherTry)
        {
            terminalStatus = ecr_terminalStatus.STATUS_UNKNOWN;

            for (uint i = 0; i < 3; i++)
            {
                if (!getStatus())
                {
                    continue;
                }

                ecr_transactionResult result;
                switch (terminalStatus)
                {
                    case ecr_terminalStatus.STATUS_READY_FOR_NEW_TRAN:
                        if (compareTransactions())
                        {
                            if (!anotherTry)
                                Log.Error("Transaction failed.");
                                Console.WriteLine("Transaction failed.");
                            return false;
                        }

                        saveTransactionData();
                        if (EservicePrintoutHandler.isMerchantPrintoutNecessary())
                        {
                            EservicePrintoutHandler.generateMerchantPrintout();
                        }
                        EservicePrintoutHandler.generateCustomerPrintout();

                        status = EcrLib.readTransactionResult(out result);
                        if (ecr_transactionResult.RESULT_TRANS_ACCEPTED != result)
                        {
                            Console.WriteLine("Transaction failed");
                            Log.Error("Transaction failed.");
                            return false;
                        }
                        Log.Error("Transaction for previous receipt " +
                            "has been accepted. If previoss receipt has been " +
                            "paid by card, make reversal of previous transaction.");
                        Console.WriteLine("Transaction for previous receipt " +
                            "has been accepted. If previoss receipt has been " +
                            "paid by card, make reversal of previous transaction.");
                        return true;
                    case ecr_terminalStatus.STATUS_BUSY:
                    case ecr_terminalStatus.STATUS_BATCH_COMPLETED:
                    case ecr_terminalStatus.STATUS_APP_ERROR:
                    case ecr_terminalStatus.STATUS_RECON_NEEDED:
                    case ecr_terminalStatus.STATUS_UNKNOWN:
                        Log.Error("Transaction status is unknown." +
                            "If transaction has been approved by terminal, " +
                            "confirm payment manually.");

                       Console.WriteLine("Transaction status is unknown." +
                            "If transaction has been approved by terminal, " +
                            "confirm payment manually.");
                        return false;
                    default:
                        status = EcrLib.continueTransaction();
                        if (ecr_status.ECR_OK != status)
                        {
                            return false;
                        }

                        status = EcrLib.readTransactionResult(out result);
                        if (ecr_status.ECR_OK != status)
                        {
                            continue;
                        }
                        saveTransactionData();
                        if (EservicePrintoutHandler.isMerchantPrintoutNecessary())
                        {
                            EservicePrintoutHandler.generateMerchantPrintout();
                        }
                        EservicePrintoutHandler.generateCustomerPrintout();

                        if (anotherTry)
                        {
                            ecr_transactionType type = ecr_transactionType.TRANS_UNKNOWN;
                            status = EcrLib.readTransactionType(out type);
                            if (ecr_transactionResult.RESULT_TRANS_ACCEPTED == result &&
                                ecr_transactionType.TRANS_SALE == type)
                            {
                                Console.WriteLine("Transaction for previous receipt has been accepted. " +
                                    "If previous receipt has been paid by card, " +
                                    "make reversal of previous transaction.");
                                return true;
                            }

                            Console.WriteLine("Transaction failed.");
                            return false;
                        }

                        Console.WriteLine("Last operation was in progress. Sale transaction failed.");
                        return false;
                }
            }

            return false;
        }

        private bool getStatus()
        {
            status = EcrLib.getTerminalStatus();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Get Status operation failed. Connection error.");
                return false;
            }

            status = EcrLib.readTerminalStatus(out terminalStatus);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Get Status operation failed. Unexpected error.");
                return false;
            }
            return true;
        }

        private string readTransactionNumber()
        {
            byte[] buffer = new byte[512];
            uint readLen;
            status = EcrLib.readTag(TlvTag.TAG_TRANSACTION_NUMBER, buffer, out readLen);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Reading transaction number failed.");
                return "";
            }
            return System.Text.Encoding.GetEncoding(1250).GetString(buffer, 0, (int)readLen);
        }

        private void saveTransactionData()
        {
            string tempTransactionNumber = readTransactionNumber();
            if (tempTransactionNumber.Equals(""))
            {
                Console.WriteLine("Save transaction data - failed to get transaction number.");
                return;
            }
            lastTransactionNumber = tempTransactionNumber;

            StringBuilder stringBuilder = new StringBuilder();
            status = EcrLib.readTransactionDate(stringBuilder);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Save transaction data - failed to get transaction date.");
                return;
            }
            lastTransactionDate = stringBuilder.ToString();

            status = EcrLib.readTransactionTime(stringBuilder);
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Save transaction data - failed to get transaction time.");
                return;
            }
            lastTransactionTime = stringBuilder.ToString();
        }
    }
}