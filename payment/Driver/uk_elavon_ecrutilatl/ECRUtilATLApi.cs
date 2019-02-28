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
        StatusClass termimalStatus;
        InitTxnReceiptPrint initTxnReceiptPrint;
        TransactionClass transaction;
        TimeDateClass timeDate;
        TransactionResponse transactionResponse;
        SignatureClass checkSignature;
        VoiceReferralClass checkVoiceReferral;
        Thread SignatureVerificationThread;
        Thread VoiceReferralThread;
     //   SettlementClass getSettlement;
     //   SettlementRequest settlementRequest;


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

        /// <summary>
        /// Check the prerequistes have been set:
        /// IP Address is called
        /// Status is at IDLE
        /// Disable the printing to print from the Transaction response
        /// </summary>
        /// <returns></returns>
        public ECRUtilATLErrMsg Connect(string ipAddress)
        {
            //set static IP address
            termimalIPAddress = new TerminalIPAddress();
            termimalIPAddress.IPAddressIn = ipAddress;
            termimalIPAddress.SetIPAddress();

            //check the output IPAddress
            Log.Info($"IpAddress output = {termimalIPAddress.IPAddressOut}");

            // check the status is at IDLE
            string status = string.Empty;
            termimalStatus = new StatusClass();
            termimalStatus.GetTerminalState();
            Log.Info($"Check Terminal at Idle: {Utils.DisplayTerminalStatus(Convert.ToInt16(termimalStatus.StateOut))}");

            // disable the receipt Printing
            initTxnReceiptPrint = new InitTxnReceiptPrint();
            initTxnReceiptPrint.StatusIn = (short)TxnReceiptState.TXN_RECEIPT_DISABLED;
            initTxnReceiptPrint.SetTxnReceiptPrintStatus();

            //check printing disabled
            if (initTxnReceiptPrint.DiagRequestOut == 0)
                Log.Info("apiInitTxnReceiptPrint OFF");
            else
                Log.Info("apiInitTxnReceiptPrint ON");

            //Set the time 
            timeDate = new TimeDateClass();
            timeDate.YearIn = DateTime.Now.Year.ToString();
            timeDate.MonthIn = DateTime.Now.Month.ToString();
            timeDate.DayIn = DateTime.Now.Day.ToString();
            timeDate.HourIn = DateTime.Now.Hour.ToString();
            timeDate.MinuteIn = DateTime.Now.Minute.ToString();
            timeDate.SecondIn = DateTime.Now.Second.ToString();

            timeDate.SetTimeDate();

            //check the connection result 
            return (ECRUtilATLErrMsg)Convert.ToInt32(termimalIPAddress.DiagRequestOut);
        }

       

        /// <summary>
        /// Disconect the transaction
        /// </summary>
        public ECRUtilATLErrMsg Disconnect()
        {
            Log.Info("Disconnecting...Reset the transaction");
          //  transaction.Reset();

            ECRUtilATLErrMsg disconnResult = ECRUtilATLErrMsg.UknownValue;

            if ((ECRUtilATLErrMsg)Convert.ToInt32(transactionResponse.DiagRequestOut) == ECRUtilATLErrMsg.OK)
                disconnResult = ECRUtilATLErrMsg.OK;
            else
                disconnResult = ECRUtilATLErrMsg.UknownValue;

            return disconnResult;

        }

        /// <summary>
        /// The transaction Payment
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLErrMsg Pay(int amount, out TransactionResponse result)
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
            return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);
        }

        /// <summary>
        /// Payment Reversal
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLErrMsg Reverse(int amount, out TransactionResponse result)
        {
            int intAmount;
            Log.Info($"Executing Reversal - Amount: {amount/100.0}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");
           
            DoTransaction(amount, TransactionType.Reversal.ToString());
            result = PopulateResponse(transaction);

            return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);

        }

        /// <summary>
        /// End of day report
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        //public SettlementClass EndOfDayReport()
        //{
        //    Log.Info("Printing end of day report...");

        //    // Get Acquirer List
        //    // getAcquirerList.Launch();

        //    getSettlement = new SettlementClass();
        //    settlementRequest = new SettlementRequest();

        //    // do the settlement
        //    getSettlement.AcquirerIndexIn = settlementRequest.AcquirerIndex;
        //    getSettlement.SettlementParamIn = (short)settlementRequest.SettlementParameter;
        //    getSettlement.DoSettlement();

        //    if ((ECRUtilATLErrMsg)(Convert.ToInt32(getSettlement.DiagRequestOut)) == ECRUtilATLErrMsg.OK)
        //        return getSettlement;
        //    else return null;
        //}
 

        /// <summary>
        ///  Do the transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="transactionType"></param>
        public void DoTransaction(int amount, string transactionType)
        {
            Random randomNum = new Random();
            Log.Info($"Selected Transaction type:{Utils.GetSelectedTransaction(transactionType).ToString()}");
          
            transaction.MessageNumberIn = randomNum.Next(100).ToString();
            transaction.TransactionTypeIn = Utils.GetSelectedTransaction(transactionType).ToString();
            transaction.Amount1In = amount.ToString();
            transaction.Amount1LabelIn = "Amount1";

          

            //set signature verification
            SignatureVerificationThread = new Thread(SignatureVerification);
            SignatureVerificationThread.Start();

            VoiceReferralThread = new Thread(VoiceReferralAuthorisation);
            VoiceReferralThread.Start();

            
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

            // Trying to abort the voice referral authorisation thread if it is alive
            try { VoiceReferralThread.Abort(); }
            catch (Exception ThreadException) { Log.Error(ThreadException.StackTrace); }
            VoiceReferralThread = null;

            Log.Info($"Transaction Card scheme out: {transaction.CardSchemeNameOut}");
            Log.Info($"Transaction Entry Method out:{Utils.CardEntryMethod(transaction.EntryMethodOut)}");
            Log.Info($"Transaction Total amount: £{Convert.ToSingle(transaction.TotalAmountOut)/100.0}");
            Log.Info($"Transaction Terminal Identity out: {transaction.TerminalIdentifierOut}");           
        }

        /// <summary>
        /// Verify Signature
        /// </summary>
        public void SignatureVerification()
        {
            //local variables
            int ret = 0;
            string CheckSignatureStatus = string.Empty;

            checkSignature = new SignatureClass();
            checkSignature.GetSignatureData();

            Log.Info("Running Check Signature");
            
            ret = checkSignature.DiagRequestOut;
            Log.Info($" checkSignature = {ret}");

            //switch (ret)
            //{
            //    case 0: CheckSignatureStatus = "Signature In Progress"; break; // RET_OK
            //    case 1: CheckSignatureStatus = "Server Down"; break;           // RET_SIGN_SERVER_DOWN
            //    case 2: CheckSignatureStatus = "Timeout"; break;               // RET_TIMEOUT
            //    case 3: CheckSignatureStatus = "Bad Request Size"; break;      // RET_BAD_REQUEST_SIZE
            //    case 4: CheckSignatureStatus = "Bad Request Format"; break;    // RET_BAD_REQUEST_FORMAT
            //    case 8: CheckSignatureStatus = "Ped Not Auth"; break;          // RET_PED_NOT_AUTHENTICATED
            //    default:CheckSignatureStatus = "Unknown Status"; break;       // Unknown Status
            //}

            if (ret != 0) //Error Case
            {
                Log.Error("Error in CheckSignature return ");
            }
            else
            {
                //set the signature Status
                checkSignature.SignatureStatusIn = 2; //assign
                checkSignature.SetSignStatus();
                Log.Info($" SetSignStatus = {checkSignature.DiagRequestOut}");
            }            
        }


        public void VoiceReferralAuthorisation()
        {

            checkVoiceReferral = new VoiceReferralClass();
            checkVoiceReferral.GetVoiceReferralData();
            Log.Info($"checkVoiceReferral out: {checkVoiceReferral.DiagRequestOut}");

            //decline the voice referral 
            checkVoiceReferral.AuthorisationStatusIn = 1; // Decline
            checkVoiceReferral.AuthorisationCodeIn = "";
            checkVoiceReferral.SetAuthorisation();
            checkVoiceReferral = null;
        }

        /// <summary>
        /// Populate the transaction response Object
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>transactionResponse</returns>
        private TransactionResponse PopulateResponse(TransactionClass transaction)
        {
            Log.Info("Populating Transaction Response");

            transactionResponse.AcquirerMerchantIDOut = transaction.AcquirerMerchantIDOut;
            transactionResponse.AcquirerNameOut = transaction.AcquirerNameOut;
            transactionResponse.MerchantAddress1Out = transaction.MerchantAddress1Out;
            transactionResponse.MerchantAddress2Out = transaction.MerchantAddress2Out;
            transactionResponse.MerchantAddress3Out = transaction.MerchantAddress3Out;
            transactionResponse.MerchantAddress4Out = transaction.MerchantAddress4Out;
            transactionResponse.MerchantNameOut = transaction.MerchantNameOut;
            transactionResponse.AcquirerResponseCodeOut = transaction.AcquirerResponseCodeOut;
            transactionResponse.AuthorisationCodeOut = transaction.AuthorisationCodeOut;
            transactionResponse.CardSchemeNameOut = transaction.CardSchemeNameOut;
            transactionResponse.TransactionCurrencyCodeOut = transaction.TransactionCurrencyCodeOut;
            transactionResponse.CardCurrencyCodeOut = transaction.CardCurrencyCodeOut;
            transactionResponse.DateTimeOut = transaction.DateTimeOut;
            transactionResponse.EntryMethodOut = transaction.EntryMethodOut;
            transactionResponse.ExpiryDateOut = transaction.ExpiryDateOut;
            transactionResponse.MessageNumberOut= transaction.MessageNumberOut;
            transactionResponse.PANOut = transaction.PANOut;
            transactionResponse.AIDOut = transaction.AIDOut;
            transactionResponse.PANsequenceNumberOut = transaction.PANSequenceNumberOut;
            transactionResponse.DateTimeOut = transaction.StartDateOut;
            transactionResponse.TerminalIdentifierOut = transaction.TerminalIdentifierOut;
            transactionResponse.TotalAmountOut = transaction.TotalAmountOut;
            transactionResponse.IsDCCTransactionOut = transaction.IsDCCTransactionOut;
            transactionResponse.DCCAmountOut = transaction.DCCAmountOut;
            transactionResponse.IsDCCTransactionOut = transaction.IsDCCTransactionOut;
            transactionResponse.FXExponentAppliedOut = transaction.FXExponentAppliedOut;
            transactionResponse.LoyaltyTransactionInfoOut = transaction.LoyaltyTransactionInfoOut;
            transactionResponse.DonationAmountOut = transaction.DonationAmountOut;
            transactionResponse.InvoiceNumberOut = transaction.InvoiceNumberOut;
            transactionResponse.HostTextOut = transaction.HostTextOut;
            transactionResponse.IsFanfareTransactionOut = transaction.IsFanfareTransactionOut;
            transactionResponse.DiagRequestOut = transaction.DiagRequestOut;
            transactionResponse.TransactionCurrencyCodeOut = transaction.TransactionCurrencyCodeOut;
            transactionResponse.TransactionStatusOut = transaction.TransactionStatusOut;
            transactionResponse.IsSignatureRequiredOut = transaction.IsSignatureRequiredOut;
         

            return transactionResponse;
        }
    }
}
