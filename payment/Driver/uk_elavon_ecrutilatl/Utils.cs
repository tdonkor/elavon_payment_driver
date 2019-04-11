﻿using System;
using System.Text;
using Acrelec.Library.Logger;

namespace Acrelec.Mockingbird.Payment
{

    public enum TransactionType : short
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

    public enum TxnReceiptState : short
    {
        TXN_RECEIPT_DISABLED = 0,
        TXN_RECEIPT_ENABLED = 1,
    }

    public enum DiagnosticErrMsg : short
    {
        OK = 0,
        UnableToConnect = 1,
        UnableToSendRequest = 2,
        BadRequestFormat = 3,
        ReceptionTimeout = 5,
        ReceptionError = 6,
        BadResponseFormat = 7,
        BadResponseSize = 7,
        PEDNotAuthenticated = 8,
        UknownValue = 9,
        NoReceivedData = 10,
        FailureToStartServer = 11,
        ServerAlreadyStarted = 12,
        FailureToStopServer = 13,
        ServerAlreadyStopped = 14,
        ErrorToFindWinsockLibrary = 15,
        ErrorSocketCreation = 16,
        ErrorBindingSocket = 17,
        ErrorListeningSocket = 18,
        ErrortoAcceptConnection = 19
    }
    public enum TransactionResult
    {
        Successful = 48,
        Cancelled = 54,
        Failed = 55,
        RequestReceived = 57
    }

    public enum EventId : short
    {
        EV_NONE = 0x00,
        EV_CHECK_SIGNATURE = 0x01,
        EV_VOICE_REFERRAL = 0x02,
        EV_DCC_QUOTATION = 0x03,
        EV_EFT_AUTOMATIC_SETTLEMENT = 0x04,
        EV_MTU_AUTOMATIC_SETTLEMENT = 0x05,
        EV_PASSWORD_ENTRY = 0x06,
        EV_PARTIAL_AUTH = 0x07,
        EV_AVS_REJECTION = 0x08,
        EV_SUSPECTED_FRAUD = 0x09,
        EV_BATCH_AUTO_CLOSE = 0x0A,
        EV_GLA_PARTIAL_AUTH = 0x0B,
        EV_EFT_HOST_DECLINED = 0x0C,
        EV_DCC_REFUND_CONFIRMATION = 0x0D,
        EV_MTU_HOST_DECLINED = 0x0E,
        EV_MTU_OUT_OF_PAPER = 0x0F,
        EV_AMOUNT_NOT_ELIGIBLE = 0x10,
        EV_TEAR_REPORT = 0x11,
        EV_TEAR_RECEIPT = 0x12,
        EV_TIP_AMOUNT_BYPASS = 0x13,
        EV_TIP_AMOUNT_END = 0x14,
        EV_VOID_FAILURE = 0x15,
        EV_CLEAR_JOURNAL = 0x16,
        EV_LOYALTY_MEMBER_BYPASS = 0x17,
        EV_LOYALTY_MEMBER_END = 0x18,
        EV_CASHBACK_AMOUNT = 0x19,
        EV_CASHBACK_SELECTION = 0x1A,
        EV_COMMERCIAL_CODE = 0x1B,
        EV_PRINT_CUSTOMER_RECEIPT = 0x1C
    }
    public struct TransactionResponse
    {
        public string MessageNumberOut;
        public string TransactionStatusOut;
        public string EntryMethodOut;
        public string AcquirerMerchantIDOut;
        public string DateTimeOut;
        public string CardSchemeNameOut;
        public string PANOut;
        public string StartDateOut;
        public string ExpiryDateOut;
        public string AuthorisationCodeOut;
        public string AcquirerResponseCodeOut;
        public string MerchantNameOut;
        public string MerchantAddress1Out;
        public string MerchantAddress2Out;
        public string MerchantAddress3Out;
        public string MerchantAddress4Out;
        public string TransactionCurrencyCodeOut;
        public short TransactionCurrencyExpOut;
        public string CardCurrencyCodeOut;
        public short CardCurrencyExpOut;
        public string TotalAmountOut;
        public string AdditionalAmountOut;
        public string EMVCardExpiryDateOut;
        public string AppEffectiveDateOut;
        public string AIDOut;
        public string AppPreferredNameOut;
        public string AppLabelOut;
        public string TerminalIdentifierOut;
        public string EMVTransactionTypeOut;
        public string AppCryptogramOut;
        public string RetrievalReferenceNumOut;
        public string InvoiceNumberOut;
        public string BatchNumberOut;
        public string AcquirerNameOut;
        public string CustomLine1Out;
        public string CustomLine2Out;
        public string CustomLine3Out;
        public string CustomLine4Out;
        public short IsDCCTransactionOut;
        public string DCCAmountOut;
        public string ConversionRateOut;
        public short FXExponentAppliedOut;
        public string CommissionOut;
        public string TerminalCountryCodeOut;
        public string TerminalCurrencyCodeOut;
        public short TerminalCurrencyExpOut;
        public string FXMarkupOut;
        public string PANSequenceNumberOut;
        public string CashierIDOut;
        public string TableIdentifierOut;
        public string CardholderNameOut;
        public string AvailableBalanceOut;
        public string PreAuthRefNumOut;
        public string HostTextOut;
        public short IsTaxFreeRequiredOut;
        public short IsExchangeRateUpdateRequiredOut;
        public string ApplicationIDOut;
        public string CommercialCodeDataOut;
        public string CardResponseValueOut;
        public string DonationAmountOut;
        public string AVSResponseOut;
        public string PartialAuthAmountOut;
        public string SpanishOpNumberOut;
        public short IsSignatureRequiredOut;
        public short IsFanfareTransactionOut;
        public string LoyaltyTransactionInfoOut;
        public string FanfareTransactionIdentifierOut;
        public string FanfareApprovalCodeOut;
        public string LoyaltyDiscountAmountOut;
        public string FanfareWebURLOut;
        public string LoyaltyProgramNameOut;
        public string FanfareIdentifierOut;
        public string LoyaltyAccessCodeOut;
        public string LoyaltyMemberTypeOut;
        public short FanfareBalanceCountOut;
        public short LoyaltyPromoCodeCountOut;
        public short DiagRequestOut;
    }

   public class Utils
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

        /// <summary>
        /// checks the DiagRequestOut variable
        /// </summary>
        /// <param name="DiagRequest"></param>
        /// <returns></returns>
        public static string GetDiagRequestString(short DiagRequest)
        {
            string DiagRequestString;

            switch (DiagRequest)
            {
                case 0x00: { DiagRequestString = "OK"; } break;
                case 0x01: { DiagRequestString = "Unable To Connect"; } break;
                case 0x02: { DiagRequestString = "Unable To Send Request"; } break;
                case 0x03: { DiagRequestString = "Bad Request Format"; } break;
                case 0x04: { DiagRequestString = "Bad Request Size"; } break;
                case 0x05: { DiagRequestString = "Reception Timeout"; } break;
                case 0x06: { DiagRequestString = "Reception Error"; } break;
                case 0x07: { DiagRequestString = "Bad Response Format"; } break;
                case 0x08: { DiagRequestString = "Bad Response Size"; } break;
                case 0x09: { DiagRequestString = "PED Not Authenticated"; } break;
                case 0x0A: { DiagRequestString = "No Received Data"; } break;
                case 0x0B: { DiagRequestString = "Failure To Start Server"; } break;
                case 0x0C: { DiagRequestString = "Server Already Started"; } break;
                case 0x0D: { DiagRequestString = "Failure to Stop Server"; } break;
                case 0x0E: { DiagRequestString = "Server Already Stopped"; } break;
                case 0x0F: { DiagRequestString = "Error Winsock Library"; } break;
                case 0x10: { DiagRequestString = "Error Socket Creation"; } break;
                case 0x11: { DiagRequestString = "Error Binding Socket"; } break;
                case 0x12: { DiagRequestString = "Error Listening Socket"; } break;
                case 0x13: { DiagRequestString = "Error To Accept Connection"; } break;
                case 0x14: { DiagRequestString = "Event Server Is Down"; } break;
                default: { DiagRequestString = "Unknown"; } break;
            }

            return DiagRequestString;
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
                case 0: { result = "STATE_UNKNOWN"; } break;
                case 1: { result = "STATE_IDLE"; } break;
                case 2: { result = "STATE_BUSY"; } break;
                case 3: { result = "STATE_CARD_INSERT"; } break;
                case 4: { result = "STATE_PIN_ENTRY_FIRST_ATTEMPT"; } break;
                case 5: { result = "STATE_PIN_ENTRY_SECOND_ATTEMPT"; } break;
                case 6: { result = "STATE_PIN_ENTRY_THIRD_ATTEMPT"; } break;
                case 7: { result = "STATE_PIN_ENTRY_FAILED"; } break;
                case 8: { result = "STATE_GRATUITY_ENTRY"; } break;
                case 9: { result = "STATE_AUTHORIZING"; } break;
                case 10: { result = "STATE_COMPLETION"; } break;
                case 11: { result = "STATE_CANCELLED"; } break;
                case 12: { result = "STATE_AMOUNT_CONFIRMATION"; } break;
                case 13: { result = "STATE_SENDING"; } break;
                case 14: { result = "STATE_RECEIVING"; } break;
                case 15: { result = "STATE_UNSPECIFIED_INPUT"; } break;
                case 16: { result = "STATE_PROCESSING"; } break;
                case 17: { result = "STATE_CARD_REMOVAL"; } break;
                case 18: { result = "STATE_PRINTING_MERCHANT_COPY"; } break;
                case 19: { result = "STATE_PRINTING_CUSTOMER_COPY"; } break;
                case 20: { result = "STATE_NO_MORE_PAPER"; } break;
                case 21: { result = "STATE_LOYALTY_OPTION_SELECTION"; } break;
                case 22: { result = "STATE_PHONE_ENTRY"; } break;
                case 23: { result = "STATE_PROMO_CODE_ENTRY"; } break;
                case 24: { result = "STATE_LOYALTY_MEMBER_SELECTION"; } break;
                case 25: { result = "STATE_REWARD_OFFER"; } break;
                case 26: { result = "STATE_EXISTING_ACCOUNT"; } break;
                case 27: { result = "STATE_INVALID_ACCOUNT"; } break;
                case 28: { result = "STATE_LINK_CARD_PAYMENT"; } break;
                case 29: { result = "STATE_ADD_CARD_PAYMENT"; } break;
                case 30: { result = "STATE_CASHBACK_ENTRY"; } break;
                case 31: { result = "STATE_COMMERCIAL_CODE_ENTRY"; } break;
                case 32: { result = "STATE_WAITING_CARD"; } break;
                case 33: { result = "STATE_WAITING_DCC_ACCEPTANCE"; } break;
                default: { result = "ERROR"; } break;
            }

            return result;
        }


        /// <summary>
        /// Get the name of Transaction
        /// </summary>
        public static string GetTransactionTypeString(short TransactionTypeNum)
        {
            string TransactionTypeString;

            switch (TransactionTypeNum)
            {
                case (short)TransactionType.Sale: TransactionTypeString = "SALE"; break;
                case (short)TransactionType.Refund: TransactionTypeString = "REFUND"; break;
                case (short)TransactionType.Void: TransactionTypeString = "VOID"; break;
                case (short)TransactionType.Duplicata: TransactionTypeString = "DUPLICATA"; break;
                case (short)TransactionType.CashAdvance: TransactionTypeString = "CASH ADVANCE"; break;
                case (short)TransactionType.PWCB: TransactionTypeString = "PWCB"; break;
                case (short)TransactionType.PreAuth: TransactionTypeString = "PRE-AUTH"; break;
                case (short)TransactionType.Completion: TransactionTypeString = "COMPLETION"; break;
                case (short)TransactionType.VerifyAccount: TransactionTypeString = "VERIFY ACCOUNT"; break;
                case (short)TransactionType.Reversal: TransactionTypeString = "REVERSAL"; break;
                case (short)TransactionType.Force: TransactionTypeString = "FORCE"; break;
                case (short)TransactionType.MotoSale: TransactionTypeString = "MOTO SALE"; break;
                case (short)TransactionType.MotoRefund: TransactionTypeString = "MOTO REFUND"; break;
                case (short)TransactionType.PreAuthReversal: TransactionTypeString = "PRE-AUTH REVERSAL"; break;
                case (short)TransactionType.Cancel: TransactionTypeString = "CANCEL"; break;
                case (short)TransactionType.PreAuthIncrement: TransactionTypeString = "PRE-AUTH INCREMENT"; break;
                default: TransactionTypeString = "Unknown"; break;
            }

            return TransactionTypeString;
        }

        public static short GetTransactionTypeNum(string TransactionTypeString)
        {
            short TransactionTypeNum;

            switch (TransactionTypeString.ToUpper())
            {
                case "SALE": { TransactionTypeNum = (short)TransactionType.Sale; } break;
                case "REFUND": { TransactionTypeNum = (short)TransactionType.Refund; } break;
                case "VOID": { TransactionTypeNum = (short)TransactionType.Void; } break;
                case "DUPLICATA": { TransactionTypeNum = (short)TransactionType.Duplicata; } break;
                case "CASH ADVANCE": { TransactionTypeNum = (short)TransactionType.CashAdvance; } break;
                case "PWCB": { TransactionTypeNum = (short)TransactionType.PWCB; } break;
                case "PRE-AUTH": { TransactionTypeNum = (short)TransactionType.PreAuth; } break;
                case "COMPLETION": { TransactionTypeNum = (short)TransactionType.Completion; } break;
                case "VERIFY ACCOUNT": { TransactionTypeNum = (short)TransactionType.VerifyAccount; } break;
                case "REVERSAL": { TransactionTypeNum = (short)TransactionType.Reversal; } break;
                case "FORCE": { TransactionTypeNum = (short)TransactionType.Force; } break;
                case "MOTO SALE": { TransactionTypeNum = (short)TransactionType.MotoSale; } break;
                case "MOTO REFUND": { TransactionTypeNum = (short)TransactionType.MotoRefund; } break;
                case "PRE-AUTH REVERSAL": { TransactionTypeNum = (short)TransactionType.PreAuthReversal; } break;
                case "CANCEL": { TransactionTypeNum = (short)TransactionType.Cancel; } break;
                case "PRE-AUTH INCREMENT": { TransactionTypeNum = (short)TransactionType.PreAuthIncrement; } break;
                default: { TransactionTypeNum = -1; } break;
            }

            return TransactionTypeNum;
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

        public static string TransactionOutResult(string transactionStatusOut)
        {
            string transactionOutStr = string.Empty;

            switch (transactionStatusOut)
            {
                case "0": { transactionOutStr = "Authorised"; } break;
                case "1": { transactionOutStr = "Not Authorised "; } break;
                case "2": { transactionOutStr = "Not processed – transaction failed before or during card entry"; } break;
                case "3": { transactionOutStr = "Unable to authorise – transaction approved by host but declined by card "; } break;
                case "4": { transactionOutStr = "Unable to process – Voice referral declined due to no referral number"; } break;
                case "5": { transactionOutStr = "Unable to connect "; } break;
                case "6": { transactionOutStr = "Void"; } break;
                case "7": { transactionOutStr = "Cancelled"; } break;
                case "8": { transactionOutStr = "Invalid password"; } break;
                case "9": { transactionOutStr = "Amount exceeded maximum limit"; } break;
                case "10": { transactionOutStr = "Connection failure"; } break;
                case "11": { transactionOutStr = "Timeout reached"; } break;
                case "12": { transactionOutStr = "Invoice not found"; } break;
                case "13": { transactionOutStr = "CashBack exceed maximum limit - Transaction Complete"; } break;
                case "14": { transactionOutStr = "CashBack not allowed"; } break;
                case "15": { transactionOutStr = "Incomplete(the ‘Do Transaction Completion’ service should be invoked)"; } break;
            }
            return transactionOutStr;
        }


        /// <summary>
        /// Get entry method string
        /// </summary>
        public static string GetEntryMethodString(string EntryMethod)
        {
            string EntryMethodString;

            switch (EntryMethod)
            {
                case "0": { EntryMethodString = "None"; } break;
                case "1": { EntryMethodString = "Keyed"; } break;
                case "2": { EntryMethodString = "Swiped"; } break;
                case "3": { EntryMethodString = "Inserted"; } break;
                case "4": { EntryMethodString = "Waved"; } break;
                case "5": { EntryMethodString = "Keyed Not Present"; } break;
                case "6": { EntryMethodString = "Swiped Fallback"; } break;
                case "7": { EntryMethodString = "PA Ref Num"; } break;
                default: { EntryMethodString = "Unknown"; } break;
            }

            return EntryMethodString;
        }

        /// <summary>
        /// Get the Currency Symbol String
        /// </summary>
        public static string GetCurrencySymbol(string CurrencyNum)
        {
            short CurrencyCode;
            string CurrencySymbol;

            try
            {
                CurrencyCode = short.Parse(CurrencyNum);
            }
            catch (Exception e)
            {
                Log.Info(e.Message);
                return "";
            }

            switch (CurrencyCode)
            {
                case 36: { CurrencySymbol = "AUD"; } break;
                case 124: { CurrencySymbol = "CAD"; } break;
                case 156: { CurrencySymbol = "CNY"; } break;
                case 203: { CurrencySymbol = "CZK"; } break;
                case 208: { CurrencySymbol = "DKK"; } break;
                case 344: { CurrencySymbol = "HKD"; } break;
                case 348: { CurrencySymbol = "HUF"; } break;
                case 376: { CurrencySymbol = "ILS"; } break;
                case 392: { CurrencySymbol = "JPY"; } break;
                case 410: { CurrencySymbol = "KRW"; } break;
                case 554: { CurrencySymbol = "NZD"; } break;
                case 578: { CurrencySymbol = "NOK"; } break;
                case 643: { CurrencySymbol = "RUB"; } break;
                case 682: { CurrencySymbol = "SAR"; } break;
                case 702: { CurrencySymbol = "SGD"; } break;
                case 710: { CurrencySymbol = "ZAR"; } break;
                case 752: { CurrencySymbol = "SEK"; } break;
                case 756: { CurrencySymbol = "CHF"; } break;
                case 784: { CurrencySymbol = "AED"; } break;
                case 826: { CurrencySymbol = "GBP"; } break;
                case 840: { CurrencySymbol = "USD"; } break;
                case 949: { CurrencySymbol = "TRY"; } break;
                case 978: { CurrencySymbol = "EUR"; } break;
                case 985: { CurrencySymbol = "PLN"; } break;
                default: { CurrencySymbol = CurrencyCode.ToString(); } break;
            }

            return CurrencySymbol;
        }

        public static char GetCurrencyChar(string currencySymbol)
        {

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            char currencyChar = ' ';


            switch (currencySymbol)
            {
                case "AUD":
                case "CAD":
                case "USD": { currencyChar = '\u0024'; } break;
                case "EUR": { currencyChar = '\u20AC'; } break;
                case "GBP": { currencyChar = '\u00A3'; } break;
                case "CNY":
                case "JPY": { currencyChar = '\u00a5'; } break;
                default: { currencyChar = ' '; } break;
            }
            return currencyChar;
        }
        public static string FormatReceiptAmount(string amountStr)
        {
            float amount = 0.0f;
            amount = float.Parse(amountStr) / 100;
            return amount.ToString("n2");
        }

        public static string GetEventRequestString(short eventId)
        {
            string eventString;

            switch (eventId)
            {
                case 0x00: { eventString = "EV_NONE"; } break;
                case 0x01: { eventString = "EV_CHECK_SIGNATURE"; } break;
                case 0x02: { eventString = "EV_VOICE_REFERRAL"; } break;
                case 0x03: { eventString = "EV_DCC_QUOTATION"; } break;
                case 0x04: { eventString = "EV_EFT_AUTOMATIC_SETTLEMENT"; } break;
                case 0x05: { eventString = "EV_MTU_AUTOMATIC_SETTLEMENT"; } break;
                case 0x06: { eventString = "EV_PASSWORD_ENTRY"; } break;
                case 0x07: { eventString = "EV_PARTIAL_AUTH"; } break;
                case 0x08: { eventString = "EV_AVS_REJECTION"; } break;
                case 0x09: { eventString = "EV_SUSPECTED_FRAUD"; } break;
                case 0x0A: { eventString = "EV_BATCH_AUTO_CLOSE"; } break;
                case 0x0B: { eventString = "EV_GLA_PARTIAL_AUTH"; } break;
                case 0x0C: { eventString = "EV_EFT_HOST_DECLINED"; } break;
                case 0x0D: { eventString = "EV_DCC_REFUND_CONFIRMATION"; } break;
                case 0x0E: { eventString = "EV_MTU_HOST_DECLINED"; } break;
                case 0x0F: { eventString = "EV_MTU_OUT_OF_PAPER"; } break;
                case 0x10: { eventString = "EV_AMOUNT_NOT_ELIGIBLE"; } break;
                case 0x11: { eventString = "EV_TEAR_REPORT"; } break;
                case 0x12: { eventString = "EV_TEAR_RECEIPT"; } break;
                case 0x13: { eventString = "EV_TIP_AMOUNT_BYPASS"; } break;
                case 0x14: { eventString = "EV_TIP_AMOUNT_END"; } break;
                case 0x15: { eventString = "EV_VOID_FAILURE"; } break;
                case 0x16: { eventString = "EV_CLEAR_JOURNAL"; } break;
                case 0x17: { eventString = "EV_LOYALTY_MEMBER_BYPASS"; } break;
                case 0x18: { eventString = "EV_LOYALTY_MEMBER_END"; } break;
                case 0x19: { eventString = "EV_CASHBACK_AMOUNT"; } break;
                case 0x1A: { eventString = "EV_CASHBACK_SELECTION"; } break;
                case 0x1B: { eventString = "EV_COMMERCIAL_CODE"; } break;
                case 0x1C: { eventString = "EV_PRINT_CUSTOMER_RECEIPT"; } break;
                default: { eventString = "Unknown"; } break;
            }

            return eventString;
        }

    }
}
