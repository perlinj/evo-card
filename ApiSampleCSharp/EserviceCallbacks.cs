using System;
using EcrLibrary;
using static EcrLibrary.Callbacks;

namespace ApiSampleCSharp
{
    class EserviceCallbacks
    {
        private static SignatureRequest askForSignatureDelegate;
        private static CopyRequest askForCopyDelegate;
        private static CurrencyRequest askForCurrencyDelegate;
        private static SelectionRequest askForSelectionDelegate;
        private static WaitForCard waitForCardDelegate;
        private static WaitForCardRemoval waitForCardRemovalDelegate;
        private static WaitForPin waitForPinDelegate;
        private static ShowOkScreen showOkScreenDelegate;
        private static ShowYesNoScreen showYesNoScreenDelegate;
        private static ShowPromptScreen showPromptScreenDelegate;
        private static GetCashbackAmount getCashbackAmountDelegate;
        private static GetAuthorizationCode getAuthorizationCodeDelegate;
        private static GetUserData getUserDataDelegate;
        private static GetAmount getAmountDelegate;
        private static HandleStatusChange handleStatusChangeDelegate;
        private static HandleBusLog handleBusLogDelegate;
        private static HandleDevLog handleDevLogDelegate;
        private static HandleCommLog handleCommLogDelegate;

        public static void registerCallbacks()
        {
            askForSignatureDelegate = new SignatureRequest(askForSignature);
            askForCopyDelegate = new CopyRequest(askForCopy);
            askForCurrencyDelegate = new CurrencyRequest(askForCurrency);
            askForSelectionDelegate = new SelectionRequest(askForSelection);
            waitForCardDelegate = new WaitForCard(waitForCard);
            waitForCardRemovalDelegate = new WaitForCardRemoval(waitForCardRemoval);
            waitForPinDelegate = new WaitForPin(waitForPin);
            showOkScreenDelegate = new ShowOkScreen(showOkScreen);
            showYesNoScreenDelegate = new ShowYesNoScreen(showYesNoScreen);
            showPromptScreenDelegate = new ShowPromptScreen(showPromptScreen);
            getCashbackAmountDelegate = new GetCashbackAmount(getCashbackAmount);
            getAuthorizationCodeDelegate = new GetAuthorizationCode(getAuthorizationCode);
            getUserDataDelegate = new GetUserData(getUserData);
            getAmountDelegate = new GetAmount(getAmount);
            handleStatusChangeDelegate = new HandleStatusChange(handleStatusChange);
            handleBusLogDelegate = new HandleBusLog(handleBusLog);
            handleDevLogDelegate = new HandleDevLog(handleDevLog);
            handleCommLogDelegate = new HandleCommLog(handleCommLog);

            cb_setSignatureRequest(askForSignatureDelegate);
            cb_setCopyRequest(askForCopyDelegate);
            cb_setShowYesNoScreen(showYesNoScreenDelegate);
            cb_setWaitForCardRemoval(waitForCardRemovalDelegate);
            cb_setShowPromptScreen(showPromptScreenDelegate);
            cb_setShowOkScreen(showOkScreenDelegate);
            cb_setWaitForCard(waitForCardDelegate);
            cb_setWaitForPin(waitForPinDelegate);
            cb_setGetCashbackAmount(getCashbackAmountDelegate);
            cb_setGetAmount(getAmountDelegate);
            cb_setGetAuthorizationCode(getAuthorizationCodeDelegate);
            cb_setGetUserData(getUserDataDelegate);
            cb_setCurrencyRequest(askForCurrencyDelegate);
            cb_setSelectionRequest(askForSelectionDelegate);
            cb_setHandleStatusChange(handleStatusChangeDelegate);
            cb_setHandleBusLog(handleBusLogDelegate);
            cb_setHandleDevLog(handleDevLogDelegate);
            cb_setHandleCommLog(handleCommLogDelegate);
        }

        private static bool askForSignature(string prompt)
        {
            EservicePrintoutHandler.generateMerchantPrintout();
            return getBooleanFromUser(prompt);
        }

        private static bool askForCopy(string prompt)
        {
            return getBooleanFromUser(prompt);
        }

        private static bool showYesNoScreen(string prompt)
        {
            return getBooleanFromUser(prompt);
        }

        private static void waitForCardRemoval(string prompt)
        {

        }

        private static void showPromptScreen(string prompt)
        {

        }

        private static bool waitForCard(string prompt)
        {
            return true;
        }

        private static bool waitForPin(string prompt)
        {
            return true;
        }

        private static void handleStatusChange(ecr_terminalStatus status)
        {
            if (ecr_terminalStatus.STATUS_TRAN_ACCEPTED == status)
            {
                Console.Write("Transaction accepted indicator received.\n" +
                              "To speed up customer service, you can, for example, open a cash drawer.\n" +
                              "Note: Appropriate configuration is necessary for" +
                              " this status to come from the terminal.\n" +
                              "For this purpose please contact eService support.\n");
            }
        }

        private static void showOkScreen(string prompt)
        {

        }

        private static void getCashbackAmount(string prompt, ref UserProvidedData userData)
        {
            string cashback = getStringFromUser(prompt, userData.minLen, userData.maxLen);
            userData.userData = cashback;
        }

        private static void getAuthorizationCode(string prompt, ref UserProvidedData userData)
        {
            string authCode = getStringFromUser(prompt, userData.minLen, userData.maxLen);
            userData.userData = authCode;
        }

        private static void getAmount(string prompt, ref UserProvidedData userData)
        {
            string amount = getStringFromUser(prompt, userData.minLen, userData.maxLen);
            userData.userData = amount;
        }

        private static void getUserData(string prompt, ref UserProvidedData userData, cb_isCharacterAllowed isCharacterAllowed)
        {
            string text = getStringFromUser(prompt, userData.minLen, userData.maxLen);
            userData.userData = text;
        }

        private static bool askForCurrency(string[] choices, uint choicesNum, out uint userChoice)
        {
            userChoice = 0;
            return true;
        }

        private static bool askForSelection(string[] choices, uint choicesNum, out uint userChoice, string text)
        {
            userChoice = 0;
            return true;
        }

        private static void handleBusLog(string log)
        {

        }

        private static void handleDevLog(string log)
        {

        }

        private static void handleCommLog(string log)
        {

        }

        private static bool getBooleanFromUser(string prompt)
        {
            string anwser = "";
            while (!anwser.Equals("y") && !anwser.Equals("n"))
            {
                Console.WriteLine(prompt + " (y/n)");
                anwser = Console.ReadLine();
            }
            return (anwser.Equals("y") ? true : false);
        }

        private static string getStringFromUser(string prompt, UIntPtr minLen, UIntPtr maxLen)
        {
            string data;
            Console.WriteLine(prompt);
            data = Console.ReadLine();
            return data;
        }
    }
}