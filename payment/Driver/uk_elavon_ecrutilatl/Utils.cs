﻿using System;
using Acrelec.Library.Logger;

namespace Acrelec.Mockingbird.Payment
{
    public enum TxnReceiptState : ushort
    {
        TXN_RECEIPT_DISABLED = 0,
        TXN_RECEIPT_ENABLED = 1,
    }

    public enum CancellationState : ushort
    {
        CANCELLATION_ERROR = 0,
        NOT_IN_TRANSACTION = 1,
        CANNOT_BE_CANCELLED = 2,
        MARKED_FOR_CANCELLATION = 3,
    }
    public enum TransactionType : ushort
    {
        Sale = 0,
        Refund = 1,
        Void = 2,
        Duplicata = 3,
        CashAdvance = 4,
        PWCB = 5,
        PreAuth = 6,
        Completion = 7,
        VerifyAccount = 8,
        Reversal = 9,
        Force = 10,
        MotoSale = 11,
        MotoRefund = 12,
        PreAuthReversal = 13,
        Cancel = 14,
        PreAuthIncrement = 15,
    }

    public enum SignStatus : ushort
    {
        SIGN_NOT_ACCEPTED = 0,
        SIGN_CANCELLED = 1,
        SIGN_ACCEPTED = 2
    }

    public enum OfferPWCBState : ushort
    {
        PWCB_DISABLED = 0,
        PWCB_ENABLED = 1,
    }

    public enum CashbackAdditionStatus : ushort
    {
        CASHBACK_NOT_ACCEPTED = 0,
        CASHBACK_ACCEPTED = 1,
    }

    public enum TxnCancellationStatus
    {
        CANCELLATION_ERROR = 0,
        NOT_IN_TRANSACTION = 1,
        CANNOT_BE_CANCELLED = 2,
        MARKED_FOR_CANCELLATION = 3,
    }

    public enum ECRUtilATLErrMsg
    {
        OK = 0,
        UnableToConnect = 1,
        UnableToSendRequest = 2,
        BadRequestFormat = 3,
        ReceptionTimeout = 4,
        ReceptionError = 5,
        BadResponseFormat = 6,
        BadResponseSize = 7,
        PEDNotAuthenticated = 8,
        UknownValue = 9
    }
    public enum TransactionResult
    {
        Successful = 48,
        Cancelled = 54,
        Failed = 55,
        RequestReceived = 57
    }
   

    public struct TransactionResponse
    {
        public string AcquirerMerchantIDOut;
        public string AcquirerResponseCodeOut;
        public string AcquirerNameOut;
        public string AdditionalAmountOut;
        public string AIDOut;
        public string AppCryptogramOut;
        public string AppEffectiveDateOut;
        public string ApplicationIDOut;
        public string AppLabelOut;
        public string AppPreferredNameOut;
        public string AuthorisationCodeOut;
        public string AVSResponseOut;
        
        public string BatchNumberOut;
        public string CardCurrencyCodeOut;
        public short CardCurrencyExponentOut;
        public string CardholderNameOut;
        public string CardResponseValueOut;
        public string CardSchemeNameOut;
        public string CashierIdentifierOut;
        public string CommercialCodeDataOut;
        public string CommissionOut;
        public string ConversionRateOut;
        public string CustomLine1Out;
        public string DateTimeOut;
        public string DCCAmountOut;
        public string DonationAmountOut;

        //public string DCCAmount;
        //public string DCCCurrency;
        //public string DCCCurrencyExp;
       
        public short  DiagRequestOut;
        public string EMVCardExpirationDateOut;
        public string EntryMethodOut;
        public string ExpiryDateOut;
        public short FXExponentAppliedOut;
        public string FanfareTransactionIdentifierOut;
        public string FanfareApprovalCodeOut;
        public string HostTextOut;
        public string InvoiceNumberOut;
        public short IsDCCTransactionOut;
        public short IsFanfareTransactionOut;
        public short IsSignatureRequiredOut;
        public string IsLoyaltyTxn;
        public string LoyaltyTransactionInfoOut;
        public string MerchantAddress1Out;
        public string MerchantAddress2Out;
        public string MerchantAddress3Out;
        public string MerchantAddress4Out;
        public string MerchantNameOut;
        public string MessageNumberOut;
        public string PANOut;
        public string PANsequenceNumberOut;
        public string ReceiptNumber;
        public string ReferenceResp;
        public string StartDateOut;
        public string TerminalCountryCodeOut;
        public string TerminalCurrencyCodeOut;
        public string TransactionStatusOut;
        public string TransactionCurrencyCodeOut;
        public string TerminalIdentifierOut;
        public string TransactionAmount;
        public string TransactionTypeIn;
        public string TotalAmountOut;
        public string TransactionId;

    }

    class Utils
    {
        public static int GetNumericAmountValue(int amount)
        {

            if (amount <= 0)
            {
                Log.Error("Invalid pay amount");
                amount = 0;
            }

            return amount;
        }

       
        public static string GetTransactionTypeString(int TxnType)
        {
            string TxnName;

            switch (TxnType)
            {
                case (int)TransactionType.Sale: TxnName = "SALE"; break;
                case (int)TransactionType.Refund: TxnName = "REFUND"; break;
                case (int)TransactionType.PWCB: TxnName = "PWCB"; break;
                case (int)TransactionType.PreAuth: TxnName = "PREAUTH"; break;
                case (int)TransactionType.Completion: TxnName = "COMPLETION"; break;
                case (int)TransactionType.CashAdvance: TxnName = "CASHADVANCE"; break;
                case (int)TransactionType.Reversal: TxnName = "REVERSAL"; break;
                case (int)TransactionType.VerifyAccount: TxnName = "VERIFY ACCOUNT"; break;
                case (int)TransactionType.Duplicata: TxnName = "DUPLICATA"; break;
                case (int)TransactionType.Void: TxnName = "VOID"; break;
                default: TxnName = "UNKNOWN"; break;
            }

            return TxnName;
        }

        public static int GetSelectedTransaction(string TxnType)
        {
            int TxnTypeVal;

            switch (TxnType.ToUpper())
            {
                case "REFUND": TxnTypeVal = (int)TransactionType.Refund; break;
                case "CASH ADVANCE": TxnTypeVal = (int)TransactionType.CashAdvance; break;
                case "PWCB": TxnTypeVal = (int)TransactionType.PWCB; break;
                case "PRE-AUTH": TxnTypeVal = (int)TransactionType.PreAuth; break;
                case "COMPLETION": TxnTypeVal = (int)TransactionType.Completion; break;
                case "VERIFY ACCOUNT": TxnTypeVal = (int)TransactionType.VerifyAccount; break;
                case "REVERSAL": TxnTypeVal = (int)TransactionType.Reversal; break;
                case "SALE": default: TxnTypeVal = (int)TransactionType.Sale; break;
            }

            return TxnTypeVal;
        }

        /// <summary>
        /// Utility to display the diagnostics
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string DisplayDiagReqOutput(int num)
        {
            string[] ReqResult = { "OK",
                                   "Unable To Connect",
                                   "Unable To Send Request",
                                   "Bad Request Format",
                                   "Reception Timeout",
                                   "Reception Error",
                                   "Bad Response Format",
                                   "Bad Response Size",
                                   "PED Not Authenticated",
                                   "No Received Data",
                                   "Unknown Value" };

            return ReqResult[num];
        }



        /// <summary>
        /// Get the Transaction Status String
        /// </summary>
        public static string DiagTxnStatus(string TxnStatus)
        {
            int ConvTxnStatus;
            string DiagTxnStatus = "";

            try { ConvTxnStatus = int.Parse(TxnStatus); }
            catch { return ""; }

            switch (ConvTxnStatus)
            {
                case 0: DiagTxnStatus = "Authorised"; break;
                case 1: DiagTxnStatus = "Not authorised"; break;
                case 2: DiagTxnStatus = "Not processed"; break;
                case 3: DiagTxnStatus = "Unable to authorise"; break;
                case 4: DiagTxnStatus = "Unable to process"; break;
                case 5: DiagTxnStatus = "Unable to connect"; break;
                case 6: DiagTxnStatus = "Void"; break;
                case 7: DiagTxnStatus = "Cancelled"; break;
                default: /* Do Nothing */ break;
            }

            return DiagTxnStatus;
        }

        /// <summary>
        /// Utility to display the terminal status
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string DisplayTerminalStatus(short num)
        {
            string result = string.Empty;

            switch (num)
            {
                case 0: { result = "Unknown"; } break;
                case 1: { result = "Idle"; } break;
                case 2: { result = "Busy"; } break;
                case 3: { result = "Card Insert"; } break;
                case 4: { result = "Pin Verify"; } break;
                case 5: { result = "Authorizing"; } break;
                case 6: { result = "Completion"; } break;
                case 7: { result = "Cancelled"; } break;
                default: { result = "Error"; } break;
            }

            return result;
        }

        public static string CardEntryMethod(string num)
        {
            string result = string.Empty;

            switch (Convert.ToInt32(num))

            {
                case 0: result = "None"; break;
                case 1: result = "keyed"; break;
                case 2: result = "Swiped"; break;
                case 3: result = "Inserted"; break;
                case 4: result = "Waved"; break;
                case 5: result = "Keyed not present"; break;
                case 6: result = "Swiped Fallback"; break;
                case 7: result = "PA Ref Num CNP"; break;


            }
            return result;
        }

        public static TransactionResult GetTransactionOutResult(string transactionStatusOut)
        {
            TransactionResult result;

            switch (transactionStatusOut)
            {
                case "0":
                    result = TransactionResult.Successful;
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                    result = TransactionResult.Failed;
                    break;
                case "7":
                    result = TransactionResult.Cancelled;
                    break;
                default:
                    result = TransactionResult.RequestReceived;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Get the Currency Symbol String
        /// </summary>
        public static string GetCurrencyCodeSymbol(int CurrencyCode)
        {
            string CurrencySymbol = "";

            switch (CurrencyCode)
            {
                case 36: CurrencySymbol = "AUD"; break;
                case 124: CurrencySymbol = "CAD"; break;
                case 156: CurrencySymbol = "CNY"; break;
                case 203: CurrencySymbol = "CZK"; break;
                case 208: CurrencySymbol = "DKK"; break;
                case 344: CurrencySymbol = "HKD"; break;
                case 348: CurrencySymbol = "HUF"; break;
                case 376: CurrencySymbol = "ILS"; break;
                case 392: CurrencySymbol = "JPY"; break;
                case 410: CurrencySymbol = "KRW"; break;
                case 554: CurrencySymbol = "NZD"; break;
                case 578: CurrencySymbol = "NOK"; break;
                case 643: CurrencySymbol = "RUB"; break;
                case 682: CurrencySymbol = "SAR"; break;
                case 702: CurrencySymbol = "SGD"; break;
                case 710: CurrencySymbol = "ZAR"; break;
                case 752: CurrencySymbol = "SEK"; break;
                case 756: CurrencySymbol = "CHF"; break;
                case 784: CurrencySymbol = "AED"; break;
                case 826: CurrencySymbol = "£"; break;
                case 840: CurrencySymbol = "$"; break;
                case 949: CurrencySymbol = "TRY"; break;
                case 978: CurrencySymbol = "€"; break;
                case 985: CurrencySymbol = "PLN"; break;
                default: /* Do Nothing */ break;
            }

            return CurrencySymbol;
        }

        public static string FormatReceiptAmount(string amountStr)
        {
            float amount = 0.0f;
            amount = float.Parse(amountStr) / 100;
            return amount.ToString("n2");
        }

        //public static string CardVerification(short num)
        //{

        //    string[] CvmResult = { "CVM not available",
        //                           "No Cardholder verification",
        //                           "Cardholder verified by signature",
        //                           "Cardholder verified by PIN",
        //                           "Cardholder verified by both Signature and PIN",
        //                           "PIN was bypassed",
        //                           "Verified by Cardholder device",
        //                           };

        //    return CvmResult[num];
        //}
    }
}
