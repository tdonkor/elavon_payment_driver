using System;
using System.Runtime.InteropServices;
using System.Threading;
using Acrelec.Library.Logger;
using ECRUtilATLLib;




namespace Acrelec.Mockingbird.Payment
{
    public class ECRUtilATLApi: IDisposable
    {
        TerminalIPAddress termimalIPAddress;
        TerminalEvent terminalEvent;
        InitTxnReceiptPrint initTxnReceiptPrint;
        TimeDate pedDateTime;
        Status pedStatus;
        TransactionClass transaction;
        TransactionResponse transactionResponse;
        Signature checkSignature;
        Thread SignatureVerificationThread;
        ECRUtilATLLib.Settlement getSettlement;


        /// <summary>
        /// Constructor initialise All the objects needed
        /// </summary>
        public ECRUtilATLApi()
        {
            transaction = new TransactionClass();
        }

        public void Dispose()
        {
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        public DiagnosticErrMsg Connect(string ipAddress)
        {

            DiagnosticErrMsg diagErr = DiagnosticErrMsg.OK;

            //Check IP Address
            diagErr = CheckIPAddress(ipAddress);
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //Check ECR Server
            diagErr = CheckECRServer();
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //Check Reciept status
            diagErr = CheckReceiptInit();
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //Check the status
            diagErr = CheckStatus();
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //set the PED timeDate
            diagErr = SetTimeDate();
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //no error
            return diagErr;
        }



        /// <summary>
        /// Disconect the transaction
        /// </summary>
        public DiagnosticErrMsg Disconnect()
        {
            Log.Info("Disconnecting...Stop the ECR Server");

            //check server is not running.
            terminalEvent.StopServer();
            Log.Info($"\nTerminal Stop Check: {Utils.GetDiagRequestString(terminalEvent.DiagRequestOut)}");

            DiagnosticErrMsg disconnResult = DiagnosticErrMsg.UknownValue;

            if ((DiagnosticErrMsg)Convert.ToInt16(transactionResponse.DiagRequestOut) == DiagnosticErrMsg.OK)
                disconnResult = DiagnosticErrMsg.OK;
            else
                disconnResult = DiagnosticErrMsg.UknownValue;

            return disconnResult;

        }

        /// <summary>
        /// The transaction Payment
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public DiagnosticErrMsg Pay(int amount, out TransactionResponse result)
        {
            int intAmount;
            Log.Info($"Executing payment - Amount: {amount/100.0}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");

            // Transaction Sale details to be executed
            //
            DoTransaction(amount, TransactionType.Sale.ToString());

            result = PopulateResponse(transaction);
            return (DiagnosticErrMsg)Convert.ToInt32(transaction.DiagRequestOut);
        }



        /// <summary>
        /// End of day report
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLLib.Settlement EndOfDayReport()
        {
            Log.Info("Printing end of day report...");
            
            getSettlement = new ECRUtilATLLib.Settlement();

            // do the settlement
            getSettlement.MessageNumberIn = transaction.MessageNumberOut;
            getSettlement.DoSettlement();

            if ((DiagnosticErrMsg)(Convert.ToInt16(getSettlement.DiagRequestOut)) == DiagnosticErrMsg.OK)
                return getSettlement;
            else return null;
        }


        /// <summary>
        ///  Do the transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="transactionType"></param>
        public void DoTransaction(int amount, string transactionType)
        {
            Random randomNum = new Random();
            Log.Info($"Selected Transaction type:{Utils.GetTransactionTypeString(Convert.ToInt16(transactionType))}");


            transaction.MessageNumberIn = randomNum.Next(100).ToString();
            transaction.TransactionTypeIn = Utils.GetTransactionTypeString(Convert.ToInt16(transactionType));
            transaction.Amount1In = amount.ToString();
            transaction.Amount1LabelIn = "Amount1";
            

            //set signature verification
            SignatureVerificationThread = new Thread(CheckSignatureVerification);
            SignatureVerificationThread.Start();
 
            // Launches the transaction
            transaction.DoTransaction();

            if (transaction.DiagRequestOut == 0 /*No Error*/)
            {
                // Display all the returned data
                Log.Info("TransactionStatusOut " +  transaction.TransactionStatusOut);
                Log.Info("EntryMethodOut " + transaction.EntryMethodOut);
            }
            else
            {
                Log.Error("Transaction Error, DiagRequestOut = " + transaction.DiagRequestOut);
            }

            // Trying to abort the signature verification thread if it is alive
            try { SignatureVerificationThread.Abort(); }
            catch (Exception ThreadException) { Log.Error(ThreadException.StackTrace); }
            SignatureVerificationThread = null;


            Log.Info($"Transaction Card scheme out: {transaction.CardSchemeNameOut}");
            Log.Info($"Transaction Entry Method out:{Utils.GetEntryMethodString(transaction.EntryMethodOut)}");
            Log.Info($"Currency: {Utils.GetCurrencySymbol(transaction.TerminalCurrencyCodeOut)}");
            Log.Info($"Transaction Total amount: {Convert.ToSingle(transaction.TotalAmountOut)/100.0}");
            Log.Info($"Transaction Terminal Identity out: {transaction.TerminalIdentifierOut}");           
        }

        /// <summary>
        /// Verify if a Signature is needed 
        /// then Void the transaction if it is
        /// </summary>
        public void CheckSignatureVerification()
        {
            checkSignature = new ECRUtilATLLib.Signature();

            Log.Info("Running Check Signature - call the wait terminal event");

            terminalEvent.WaitTerminalEvent();
            Log.Info($"Terminal Event = {terminalEvent.EventIdentifierOut}");

            //void the signature if set
            checkSignature.SignatureStatusIn = 0x00; /* SIGN_NOT_ACCEPTED */
            checkSignature.SetSignStatus();

            Log.Info($"Signature status : {checkSignature.DiagRequestOut}");


        }

        /// <summary>
        /// Set the Ped Date/Time
        /// </summary>
        /// <returns>diagnostic value</returns>
        private DiagnosticErrMsg SetTimeDate()
        {
            //Set the PED Date time inputs
            pedDateTime = new TimeDate();
            pedDateTime.YearIn = DateTime.Now.Year.ToString();
            pedDateTime.MonthIn = DateTime.Now.Month.ToString();
            pedDateTime.DayIn = DateTime.Now.Day.ToString();
            pedDateTime.HourIn = DateTime.Now.Hour.ToString();
            pedDateTime.MinuteIn = DateTime.Now.Minute.ToString();
            pedDateTime.SecondIn = DateTime.Now.Second.ToString();

            //check the connection result 
            return (DiagnosticErrMsg)(pedDateTime.DiagRequestOut);
        }

        /// <summary>
        /// Check the PED Status
        /// </summary>
        /// <returns>Diagnostic value</returns>
        private DiagnosticErrMsg CheckStatus()
        {
            //Check status at Idle
            pedStatus = new Status();
            pedStatus.GetTerminalState();
            Log.Info($"Status: {Utils.DisplayTerminalStatus(pedStatus.StateOut)}");

            //check the connection result 
            return (DiagnosticErrMsg)(pedStatus.DiagRequestOut);
        }

        /// <summary>
        /// Disable reciept printing
        /// </summary>
        /// <returns>Diagnostic value</returns>
        private DiagnosticErrMsg CheckReceiptInit()
        {
            // disable the receipt Printing
            initTxnReceiptPrint = new InitTxnReceiptPrint();
            initTxnReceiptPrint.StatusIn = (short)TxnReceiptState.TXN_RECEIPT_DISABLED;
            initTxnReceiptPrint.SetTxnReceiptPrintStatus();

            //check printing disabled
            if (initTxnReceiptPrint.DiagRequestOut == 0)
                Log.Info("apiInitTxnReceiptPrint OFF");
            else
                Log.Info("apiInitTxnReceiptPrint ON");

            //check the connection result 
            return (DiagnosticErrMsg)(initTxnReceiptPrint.DiagRequestOut);
        }

        /// <summary>
        /// Check ECR Server is running
        /// </summary>
        /// <returns>Diagnostic value</returns>
        private DiagnosticErrMsg CheckECRServer()
        {
            //Set the Event Server 
            terminalEvent = new TerminalEvent();

            // Start the ECR server
            terminalEvent.StartServer();

            terminalEvent.GetServerState();
            Log.Info($"Terminal Start Check: {Utils.GetDiagRequestString(terminalEvent.DiagRequestOut)}");
            //check the connection result 
            return (DiagnosticErrMsg)(terminalEvent.DiagRequestOut);
        }

        /// <summary>
        /// Check the IP Address
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>Diagnostic value</returns>
        private DiagnosticErrMsg CheckIPAddress(string ipAddress)
        {
            //set static IP address
            termimalIPAddress = new TerminalIPAddress();
            termimalIPAddress.IPAddressIn = ipAddress;
            termimalIPAddress.SetIPAddress();
            Log.Info($"IP Address Out: {termimalIPAddress.IPAddressOut}");

            //check the connection result 
            return (DiagnosticErrMsg)(termimalIPAddress.DiagRequestOut);
        }

       

        /// <summary>
        /// Populate the transactionResponse object
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>the transaction response</returns>
        private TransactionResponse PopulateResponse(Transaction transaction)
        {
            Log.Info("Populating Transaction Response");
            /* Set the transaction output */
            transactionResponse.MessageNumberOut = transaction.MessageNumberOut;
            transactionResponse.TransactionStatusOut = transaction.TransactionStatusOut;
            transactionResponse.EntryMethodOut = transaction.EntryMethodOut;
            transactionResponse.AcquirerMerchantIDOut = transaction.AcquirerMerchantIDOut;
            transactionResponse.DateTimeOut = transaction.DateTimeOut;
            transactionResponse.CardSchemeNameOut = transaction.CardSchemeNameOut;
            transactionResponse.PANOut = transaction.PANOut;
            transactionResponse.StartDateOut = transaction.StartDateOut;
            transactionResponse.ExpiryDateOut = transaction.ExpiryDateOut;
            transactionResponse.AuthorisationCodeOut = transaction.AuthorisationCodeOut;
            transactionResponse.AcquirerResponseCodeOut = transaction.AcquirerResponseCodeOut;
            transactionResponse.MerchantNameOut = transaction.MerchantNameOut;
            transactionResponse.MerchantAddress1Out = transaction.MerchantAddress1Out;
            transactionResponse.MerchantAddress2Out = transaction.MerchantAddress2Out;
            transactionResponse.MerchantAddress3Out = transaction.MerchantAddress3Out;
            transactionResponse.MerchantAddress4Out = transaction.MerchantAddress4Out;
            transactionResponse.TransactionCurrencyCodeOut = transaction.TransactionCurrencyCodeOut;
            transactionResponse.TransactionCurrencyExpOut = transaction.TransactionCurrencyExponentOut;
            transactionResponse.CardCurrencyCodeOut = transaction.CardCurrencyCodeOut;
            transactionResponse.CardCurrencyExpOut = transaction.CardCurrencyExponentOut;
            transactionResponse.TotalAmountOut = transaction.TotalAmountOut;
            transactionResponse.AdditionalAmountOut = transaction.AdditionalAmountOut;
            transactionResponse.EMVCardExpiryDateOut = transaction.EMVCardExpirationDateOut;
            transactionResponse.AppEffectiveDateOut = transaction.AppEffectiveDateOut;
            transactionResponse.AIDOut = transaction.AIDOut;
            transactionResponse.AppPreferredNameOut = transaction.AppPreferredNameOut;
            transactionResponse.AppLabelOut = transaction.AppLabelOut;
            transactionResponse.TerminalIdentifierOut = transaction.TerminalIdentifierOut;
            transactionResponse.EMVTransactionTypeOut = transaction.EMVTransactionTypeOut;
            transactionResponse.AppCryptogramOut = transaction.AppCryptogramOut;
            transactionResponse.RetrievalReferenceNumOut = transaction.RetrievalReferenceNumberOut;
            transactionResponse.InvoiceNumberOut = transaction.InvoiceNumberOut;
            transactionResponse.BatchNumberOut = transaction.BatchNumberOut;
            transactionResponse.AcquirerNameOut = transaction.AcquirerNameOut;
            transactionResponse.CustomLine1Out = transaction.CustomLine1Out;
            transactionResponse.CustomLine2Out = transaction.CustomLine2Out;
            transactionResponse.CustomLine3Out = transaction.CustomLine3Out;
            transactionResponse.CustomLine4Out = transaction.CustomLine4Out;
            transactionResponse.IsDCCTransactionOut = transaction.IsDCCTransactionOut;
            transactionResponse.DCCAmountOut = transaction.DCCAmountOut;
            transactionResponse.ConversionRateOut = transaction.ConversionRateOut;
            transactionResponse.FXExponentAppliedOut = transaction.FXExponentAppliedOut;
            transactionResponse.CommissionOut = transaction.CommissionOut;
            transactionResponse.TerminalCountryCodeOut = transaction.TerminalCountryCodeOut;
            transactionResponse.TerminalCurrencyCodeOut = transaction.TerminalCurrencyCodeOut;
            transactionResponse.TerminalCurrencyExpOut = transaction.TerminalCurrencyExponentOut;
            transactionResponse.FXMarkupOut = transaction.FXMarkupOut;
            transactionResponse.PANSequenceNumberOut = transaction.PANSequenceNumberOut;
            transactionResponse.CashierIDOut = transaction.CashierIdentifierOut;
            transactionResponse.TableIdentifierOut = transaction.TableIdentifierOut;
            transactionResponse.CardholderNameOut = transaction.CardholderNameOut;
            transactionResponse.AvailableBalanceOut = transaction.AvailableBalanceOut;
            transactionResponse.PreAuthRefNumOut = transaction.PreAuthRefNumOut;
            transactionResponse.HostTextOut = transaction.HostTextOut;
            transactionResponse.IsTaxFreeRequiredOut = transaction.IsTaxFreeRequiredOut;
            transactionResponse.IsExchangeRateUpdateRequiredOut = transaction.IsExchangeRateUpdateRequiredOut;
            transactionResponse.ApplicationIDOut = transaction.ApplicationIDOut;
            transactionResponse.CommercialCodeDataOut = transaction.CommercialCodeDataOut;
            transactionResponse.CardResponseValueOut = transaction.CardResponseValueOut;
            transactionResponse.DonationAmountOut = transaction.DonationAmountOut;
            transactionResponse.AVSResponseOut = transaction.AVSResponseOut;
            transactionResponse.PartialAuthAmountOut = transaction.PartialAuthAmountOut;
            transactionResponse.SpanishOpNumberOut = transaction.SpanishOpNumberOut;
            transactionResponse.IsSignatureRequiredOut = transaction.IsSignatureRequiredOut;
            transactionResponse.IsFanfareTransactionOut = transaction.IsFanfareTransactionOut;
            transactionResponse.LoyaltyTransactionInfoOut = transaction.LoyaltyTransactionInfoOut;
            transactionResponse.FanfareTransactionIdentifierOut = transaction.FanfareTransactionIdentifierOut;
            transactionResponse.FanfareApprovalCodeOut = transaction.FanfareApprovalCodeOut;
            transactionResponse.LoyaltyDiscountAmountOut = transaction.LoyaltyDiscountAmountOut;
            transactionResponse.FanfareWebURLOut = transaction.FanfareWebURLOut;
            transactionResponse.LoyaltyProgramNameOut = transaction.LoyaltyProgramNameOut;
            transactionResponse.FanfareIdentifierOut = transaction.FanfareIdentifierOut;
            transactionResponse.LoyaltyAccessCodeOut = transaction.LoyaltyAccessCodeOut;
            transactionResponse.LoyaltyMemberTypeOut = transaction.LoyaltyMemberTypeOut;
            transactionResponse.FanfareBalanceCountOut = transaction.FanfareBalanceCountOut;
            transactionResponse.LoyaltyPromoCodeCountOut = transaction.LoyaltyPromoCodeCountOut;
            transactionResponse.DiagRequestOut = transaction.DiagRequestOut;

            return transactionResponse;
        }
    }
}
