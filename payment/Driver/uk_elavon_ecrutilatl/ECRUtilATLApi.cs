using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using ECRUtilATLLib;

namespace Acrelec.Mockingbird.Payment
{
    public class ECRUtilATLApi: IDisposable
    {
        TerminalIPAddress termimalIPAddress;
        TerminalEventClass terminalEvent;
        InitTxnReceiptPrint initTxnReceiptPrint;
        TimeDate pedDateTime;
        Status pedStatus;
        TransactionClass transaction;
        TransactionResponse transactionResponse;
        SignatureClass checkSignature;
        SettlementClass getSettlement;
       
        
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
            CheckReceiptInit();
            
            //Check the status
            diagErr = CheckStatus();
            if (diagErr != DiagnosticErrMsg.OK) return diagErr;

            //set the PED timeDate
            SetTimeDate();
            
            //no error
            return diagErr;
        }


        /// <summary>
        /// Disconect the transaction
        /// </summary>
        public DiagnosticErrMsg Disconnect()
        {
            Log.Info("Disconnecting...");

            //reset the transaction
            transaction = null;

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
            Log.Info($"Executing payment - Amount: {amount}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");

            // Transaction Sale details to be executed
            //
            DoTransaction(amount, TransactionType.Sale.ToString());


            result = PopulateResponse(transaction);
            return (DiagnosticErrMsg)Convert.ToInt16(transaction.DiagRequestOut);
        }

        /// <summary>
        ///  Do the transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="transactionType"></param>
        public void DoTransaction(int amount, string transactionType)
        {
            Random randomNum = new Random();

            transaction.MessageNumberIn = randomNum.Next(10, 99).ToString();
            transaction.Amount1In = amount.ToString().PadLeft(12, '0');
            transaction.Amount1LabelIn = "AMOUNT";
            transaction.TransactionTypeIn = "0";

            //check if a signature needed
            CheckforEvents();

            // Launches the transaction
            transaction.DoTransaction();

            if (transaction.DiagRequestOut == 0 /*No Error*/)
            {
                // Display all the returned data
                Log.Info("Transaction Status Out: " + Utils.GetTransactionTypeString(Convert.ToInt16(transaction.TransactionStatusOut)));
                Log.Info("Entry Method Out: " + Utils.GetEntryMethodString(transaction.EntryMethodOut));
            }
            else
            {
                Log.Error("Transaction Error, DiagRequestOut = " + Utils.GetDiagRequestString(transaction.DiagRequestOut));
            }
        
        }

        /// <summary>
        /// Check for any events
        /// </summary>
        public async void CheckforEvents() 
        {
            try
            {
                terminalEvent.GetServerState();

                Log.Info("Calling WaitTerminal Event.....");
                await Task.Run(new Action(terminalEvent.WaitTerminalEvent));

                if (terminalEvent.EventIdentifierOut != 0x00 /* EV_NONE */)
                {
                    switch (terminalEvent.EventIdentifierOut)
                    {
                        case 0x01:
                            {
                                Log.Info("Running Check Signature Event...");
                                checkSignature = new SignatureClass();

                                //void the signature if set
                                checkSignature.SignatureStatusIn = 0x00; /* SIGN_NOT_ACCEPTED */
                                checkSignature.SetSignStatus();
                                Log.Info($"Signature status : {Utils.GetDiagRequestString(checkSignature.DiagRequestOut)}");
                            }
                            break;
                        case 0x02: //Voice Verification Event
                            {
                                VoiceReferralClass voiceRef = new VoiceReferralClass();
                                voiceRef.AuthorisationCodeIn = "";
                                voiceRef.AuthorisationStatusIn = 0x00; //cancel
                                voiceRef.SetAuthorisation();
                                Log.Info($"VoiceReferral Event status : {Utils.GetDiagRequestString(voiceRef.DiagRequestOut)}");
                            }
                            break;
                        case 0x04: //Automatic Settlement Event
                            {
                                Random randomNum = new Random();
                                Log.Info("Auto settlement has been triggered...");

                                getSettlement = new SettlementClass();
                                getSettlement.MessageNumberIn = randomNum.Next(10, 99).ToString();

                                // do the settlement
                                getSettlement.DoSettlement();
                                Log.Info($"Automatic Settlement Event : {Utils.GetDiagRequestString(getSettlement.DiagRequestOut)}");
                                
                            }
                            break;
                        case 0x07: //Partial Auth Event
                            {
                                PartialAuthClass partialAuth = new PartialAuthClass();
                                partialAuth.PartialAuthStatusIn = 0x01; // decline
                                partialAuth.SetPatialAuthStatus();
                                Log.Info($"partial Auth Event status : {Utils.GetDiagRequestString(partialAuth.DiagRequestOut)}");

                            }
                            break;
                        case 0x09: //Suspected Fraud Event
                            {
                                SuspectedFraudClass susFraud = new SuspectedFraudClass();
                                susFraud.SuspectedFrdStatusIn = 0;
                                susFraud.SetSuspectedFraudStatus();
                                Log.Info($"Suspected Fraud Event status : {Utils.GetDiagRequestString(susFraud.DiagRequestOut)}");
                            }
                            break;
                        case 0x0B: //Fanfare Partial Auth Event 
                            {
                                FanfarePartialAuthClass fanfarePartialAuth = new FanfarePartialAuthClass();
                                fanfarePartialAuth.PartialAuthStatusIn = 0x01; // decline
                                Log.Info($"Fanfare Partial Auth Event status : {Utils.GetDiagRequestString(fanfarePartialAuth.DiagRequestOut)}");

                            }
                            break;
                        case 0x0C: //EFT Host Declined Event
                            {
                                EFTHostDeclinedClass eftDeclined = new EFTHostDeclinedClass();

                                //send the ackknowledgement
                                Log.Info($"Host Decline message: {eftDeclined.HostMessageOut}");
                                eftDeclined.SendHostDeclinedAck();
                                Log.Info($"EFT Host Declined Event status : {Utils.GetDiagRequestString(eftDeclined.DiagRequestOut)}");

                            }
                            break;

                        case 0x0D: //DCC Refund Confirmation Event
                            {
                                DCCRefundConfirmationClass dccRefund = new DCCRefundConfirmationClass();
                                dccRefund.DCCRefundConfirmStatusIn = 0x01; //decline dcc refund
                                dccRefund.SetDCCRefundConfirmStatus();
                                Log.Info($"CDCC Refund Confirmation Event Status  : {Utils.GetDiagRequestString(dccRefund.DiagRequestOut)}");
                            }
                            break;

                        case 0x0E: //MTU HostDeclinedEvent
                            {
                                MTUHostDeclinedClass mTUHostDeclined = new MTUHostDeclinedClass();
                                mTUHostDeclined.SendHostDeclinedAck();
                                Log.Info($"MTUHost Declined Class Event status : {Utils.GetDiagRequestString(mTUHostDeclined.DiagRequestOut)}");

                            }
                            break;

                        case 0x10: //Amount Not Eligible Event;
                            {
                                AmountNotEligibleClass amountNotEleg = new AmountNotEligibleClass();
                                amountNotEleg.SendAmountNotEligibleAck();
                                Log.Info($"Amount Not Eligible Event status : {Utils.GetDiagRequestString(amountNotEleg.DiagRequestOut)}");

                            }
                            break;

                        case 0x1A: //Cashback Selection Event
                            {
                                CashbackSelectionClass cashBackSelect = new CashbackSelectionClass();
                                //don't accept cashback
                                cashBackSelect.CashbackSelectionStatusIn = 0x01;
                                cashBackSelect.SetCashbackSelectionStatus();
                                Log.Info($"Cashback Selection Event Status : {Utils.GetDiagRequestString(cashBackSelect.DiagRequestOut)}");

                            }
                            break;

                        case 0x15: //Void Failure Event
                            {
                                VoidFailureClass voidFailure = new VoidFailureClass();
                                voidFailure.SendVoidFailureAck();
                                Log.Info($"Void Failure Event status : {Utils.GetDiagRequestString(voidFailure.DiagRequestOut)}");

                            }
                            break;
                        //ignore any of these events we don't need to deal with any of these.

                        case 0x03: //DCC Quotation Information Event
                       
                        case 0x05: //Automatic MTU Settlement Event
                        case 0x06: //Password InformationEvent
                        case 0x08: //AVS Rejection Event
                        case 0x0A: //Batch Auto Close Event
                        case 0x0F: //MTU Out Of PaperEvent
                        case 0x11: //Tear Report Event
                        case 0x12: //Tear Receipt Event
                        case 0x13: //Tip Amount By Pass Event
                        case 0x14: //Tip Amount End Event
                        case 0x16: //Clear JournalEvent
                        case 0x17: //Loyalty Member ByPass Event
                        case 0x18: //Loyalty Member End Event
                        case 0x19: //Cashback Amount Event
                        case 0x1B: //Commercial Code Event
                        case 0x1C: //Print CustReceipt Event
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {

                Log.Info("Error in Check For Events: " + ex);
            }            
        }

        /// <summary>
        /// Set the Ped Date/Time
        /// </summary>
        /// <returns>diagnostic value</returns>
        private void SetTimeDate()
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
            Log.Info("TimeDate Set : " + (DiagnosticErrMsg)(pedDateTime.DiagRequestOut));
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
        private void CheckReceiptInit()
        {
            // disable the receipt Printing
            initTxnReceiptPrint = new InitTxnReceiptPrint();
            initTxnReceiptPrint.StatusIn = (short)TxnReceiptState.TXN_RECEIPT_DISABLED;
            initTxnReceiptPrint.SetTxnReceiptPrintStatus();

            //check printing disabled
            Log.Info("Printing Disabled: " + (DiagnosticErrMsg)(initTxnReceiptPrint.DiagRequestOut));
        }

        /// <summary>
        /// Check ECR Server is running
        /// </summary>
        /// <returns>Diagnostic value</returns>
        private DiagnosticErrMsg CheckECRServer()
        {
            //Set the Event Server 
            terminalEvent = new TerminalEventClass();

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
