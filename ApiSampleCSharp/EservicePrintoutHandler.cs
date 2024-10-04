using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EcrLibrary;
using Serilog;

namespace ApiSampleCSharp
{
    class EservicePrintoutHandler
    {
        private const int LINE_LENGTH = 40;

        private static SimplePrintoutHandler simplePrintoutHandler;
        private static ExtendedPrintoutHandler extendedPrintoutHandler;

        public static void generateCustomerPrintout()
        {
            simplePrintoutHandler = EcrLib.getTransactionCustomerPrintoutHandler();
            generatePrintout();
        }

        public static void generateMerchantPrintout()
        {
            simplePrintoutHandler = EcrLib.getTransactionMerchantPrintoutHandler();
            generatePrintout();
        }

        public static void generatePrintout()
        {
            simplePrintoutHandler.setNormalLineLen(LINE_LENGTH);
            simplePrintoutHandler.setSmallLineLen(LINE_LENGTH);
            simplePrintoutHandler.setBigLineLen(LINE_LENGTH);

            pr_Result result = simplePrintoutHandler.preparePrintout();
            if (pr_Result.PRINTOUT_OK != result)
            {
                Console.WriteLine("Printout result error.");
                Log.Error("Printout result error.");

                return;
            }

            Console.WriteLine("Printout:");
            Log.Information("Printout:");

            EcrPrintoutLine line;
            while (pr_Result.PRINTOUT_OK == simplePrintoutHandler.getNextLine(out line))
            {
                Console.WriteLine(line.lineNumber.ToString() + " " + line.text);
            }
        }

        public static void generateReportFromBatch()
        {
            ecr_status status;
            extendedPrintoutHandler = EcrLib.getClosingDayPrintoutHandler();

            extendedPrintoutHandler.setNormalLineLen(LINE_LENGTH);
            extendedPrintoutHandler.setSmallLineLen(LINE_LENGTH);
            extendedPrintoutHandler.setBigLineLen(LINE_LENGTH);

            pr_Result result = extendedPrintoutHandler.startPrintout();
            if (pr_Result.PRINTOUT_OK != result)
            {
                Console.WriteLine("Printout report start error.");
                return;
            }

            uint iterator = 1;
            while (true)
            {
                status = EcrLib.setTransactionId(iterator++);
                if (ecr_status.ECR_OK != status)
                {
                    Console.WriteLine("Set transaction id error.");
                    return;
                }

                status = EcrLib.getSingleTransactionFromBatch();
                if (ecr_status.ECR_OK == status)
                {
                    result = extendedPrintoutHandler.addPrintoutEntry();
                }
                else if (ecr_status.ECR_NO_TERMINAL_DATA == status)
                {
                    Console.WriteLine("End of data.");
                    break;
                }
                else
                {
                    Console.WriteLine("Reading trans data error.");
                    return;
                }
            }

            status = EcrLib.getBatchSummary();
            if (ecr_status.ECR_OK != status)
            {
                Console.WriteLine("Get batch summary error.");
                return;
            }

            result = extendedPrintoutHandler.finishPrintout();
            if (pr_Result.PRINTOUT_OK != result)
            {
                Console.WriteLine("Summary printout result error.");
                return;
            }

            Console.WriteLine("Printout:");
            EcrPrintoutLine line;
            while (pr_Result.PRINTOUT_OK == extendedPrintoutHandler.getNextLine(out line))
            {
                Console.WriteLine(line.lineNumber.ToString() + " " + line.text);
            }
        }

        public static bool isMerchantPrintoutNecessary()
        {
            AuthorizationMethod method;
            ecr_status status = EcrLib.readAuthorizationMethod(out method);
            if (ecr_status.ECR_OK != status ||
                (AuthorizationMethod.AUTH_METHOD_SIGN != method && AuthorizationMethod.AUTH_METHOD_PIN_SIGN != method))
            {
                return true;
            }

            ecr_transactionResult result;
            status = EcrLib.readTransactionResult(out result);
            if (ecr_status.ECR_OK != status)
            {
                return true;
            }

            return ecr_transactionResult.RESULT_TRANS_ACCEPTED != result;
        }
    }
}
