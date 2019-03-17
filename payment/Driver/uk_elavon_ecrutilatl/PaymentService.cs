using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.ServiceModel;


namespace Acrelec.Mockingbird.Payment
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class PaymentService : IPaymentService
    {
        private static readonly string ticketPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ticket");

        /// <summary>
        /// Get the configuratiion data
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Result Init(RuntimeConfiguration configuration)
        {
            Log.Info("Init method started...");

            Log.Info($"IP Address value : {configuration.IpAddress}");

            try
            {
                if (configuration == null)
                {
                    Log.Info("Can not set configuration to null.");
                    return ResultCode.GenericError;
                }

                if (configuration.PosNumber <= 0)
                {
                    Log.Info($"Invalid PosNumber {configuration.PosNumber}.");
                    return ResultCode.GenericError;
                }
                if (configuration.IpAddress == string.Empty)
                {
                    Log.Info($"Invalid IPAddress {configuration.IpAddress}.");
                    return ResultCode.GenericError;
                }

                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect(configuration.IpAddress);
                    
                    Log.Info($"Connect Result: {connectResult}");

                    if (connectResult != DiagnosticErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                    RuntimeConfiguration.Instance = configuration;
                    Heartbeat.Instance.Start();
                    Log.Info("Init success!");

                    return ResultCode.Success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Init method finished.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Result Test()
        {
            var alive = Heartbeat.Instance?.Alive == true;
            Log.Debug($"Test status: {alive}");
            return alive ? ResultCode.Success : ResultCode.GenericError;
        }

        /// <summary>
        /// Payment method
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public Result<PaymentData> Pay(int amount)
        {
            Log.Info("Pay method started...");
            Log.Info($"Amount = {amount/100.0}.");
            Result<PaymentData> transactionResult = null;

            try
            {
                if (File.Exists(ticketPath))
                {
                    File.Delete(ticketPath);
                }

                if (amount <= 0)
                {
                    Log.Info("Invalid pay amount.");
                    return ResultCode.GenericError;
                }

                var config = RuntimeConfiguration.Instance;

                var data = new PaymentData();

                Log.Info("Calling payment driver...");

                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect(config.IpAddress);
                    var disconnectResult = DiagnosticErrMsg.OK;


                    Log.Info($"Connect Result: {connectResult}");

                    if (connectResult != DiagnosticErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                    var payResult = api.Pay(amount, out var payResponse);
                    Log.Info($"Pay Result: {payResult}");
                    Log.Info("Response Data: " + Utils.GetTransactionTypeString(Convert.ToInt16(payResponse.TransactionStatusOut)));

                    if (payResult != DiagnosticErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                     data.Result = (PaymentResult)Utils.GetTransactionOutResult(payResponse.TransactionStatusOut);

          
                    if (data.Result != PaymentResult.Successful)
                    {
                        if (data.Result == PaymentResult.Failed)
                        {
                            Log.Info("Payment Failed.");

                            //persist the transaction
                            PersistTransaction(payResponse);

                            //print the payment ticket for an error
                            //
                            CreateCustomerTicket("-----\n\nPayment failure with\nyour card or issuer\nNO payment has been taken.\n\nPlease try again with another card,\nor at a manned till.\n\n-----");
                            data.HasClientReceipt = true;
                        }
                     
                        return new Result<PaymentData>((ResultCode)payResult, data: data);
                    }

                    data.PaidAmount = amount;

                    //create customer receipt if successful
                    if ((DiagnosticErrMsg)Convert.ToInt32(payResponse.DiagRequestOut) == DiagnosticErrMsg.OK)
                    {
                        Log.Info($"Transaction Status: { Utils.GetTransactionTypeString(Convert.ToInt16(payResponse.TransactionStatusOut))}");

                        if (Utils.GetTransactionOutResult(payResponse.TransactionStatusOut) == TransactionResult.Successful)
                        {
                            Log.Info($"Transaction Successful");
                            transactionResult = new Result<PaymentData>(ResultCode.Success, data: data);
                            Log.Info("Payment succeeded.");

                            CreateCustomerTicket(payResponse);
                            data.HasClientReceipt = true;
                        }
                    }

                    //persist the transaction
                    PersistTransaction(payResponse);

                    disconnectResult = api.Disconnect();
                    Log.Info($"Disconnect Result: {disconnectResult}");
                }


                return transactionResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Pay method finished.");
            }
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        public void Shutdown()
        {
            Log.Info("Shutting down...");
            Program.ManualResetEvent.Set();
        }

        /// <summary>
        /// Persist the transaction as Text file
        /// with Customer and Merchant receiept
        /// </summary>
        /// <param name="result"></param>
        private void PersistTransaction(TransactionResponse result)
        {
            try
            {
                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_ticket.txt");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                StringBuilder customerReceipt = new StringBuilder();
                StringBuilder merchantReceipt = new StringBuilder();

                customerReceipt.Append($"CUSTOMER RECEIPT\n");
                customerReceipt.Append($"=======================\n");
                customerReceipt.Append($"Merchant Name: {result.MerchantNameOut}\n");
                customerReceipt.Append($"Merchant Addr1: {result.MerchantAddress1Out}\n");
                customerReceipt.Append($"Merchant Addr2: {result.MerchantAddress2Out}\n");
                customerReceipt.Append($"Merchant Addr3: {result.MerchantAddress3Out}\n");
                customerReceipt.Append($"Merchant Addr4: {result.MerchantAddress4Out}\n");
                customerReceipt.Append($"Acquirer MerchantId: {result.AcquirerMerchantIDOut}\t");
                customerReceipt.Append($"TID: {result.TerminalIdentifierOut}\n");
                customerReceipt.Append($"Card Scheme Name: {result.CardSchemeNameOut}\n");
                customerReceipt.Append($"AID: {result.AIDOut}\n");
                customerReceipt.Append($"PAN: {result.PANOut}\n");
                customerReceipt.Append($"PAN SEQ NUM:{result.PANSequenceNumberOut}\n");
                customerReceipt.Append($"Entry Method: {Utils.GetEntryMethodString(result.EntryMethodOut)}\n");
                customerReceipt.Append($"Transaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(result.TransactionStatusOut))}\n");
                customerReceipt.Append("\nCARD HOLDER COPY\n");
                customerReceipt.Append($"Currency: {Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut)}\n");
                customerReceipt.Append($"Purchase Amount: {Utils.FormatReceiptAmount(result.TotalAmountOut)}\n");
                customerReceipt.Append($"Date: {DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}\n");
                customerReceipt.Append($"Transaction Date/Time: {result.DateTimeOut}\n");
                customerReceipt.Append("\nThank you\n");
                customerReceipt.Append($"{result.HostTextOut}\n");          // Host Message
                customerReceipt.Append("\n=====================\n");
                customerReceipt.Append($"{Utils.TransactionOutResult(result.TransactionStatusOut)}\n");
                customerReceipt.Append("=====================\n");


                //get the reponse details for the ticket
                merchantReceipt.Append($"\n\nMERCHANT RECEIPT\n");
                merchantReceipt.Append($"================\n\n");
                merchantReceipt.Append($"Acquirer Merchant ID: {result.AcquirerMerchantIDOut}\n");
                merchantReceipt.Append($"Merchant NAME:  {result.MerchantNameOut}\n");
                merchantReceipt.Append($"Merchant ADDR1: {result.MerchantAddress1Out}\n");
                merchantReceipt.Append($"Merchant ADDR2: {result.MerchantAddress2Out}\n");
                merchantReceipt.Append($"Merchant ADDR3: {result.MerchantAddress3Out}\n");
                merchantReceipt.Append($"Merchant ADDR4: {result.MerchantAddress4Out}\n");
                merchantReceipt.Append($"Entry Method: {Utils.GetEntryMethodString(result.EntryMethodOut)}\n");
                merchantReceipt.Append($"TID: {result.TerminalIdentifierOut}\n");
                merchantReceipt.Append($"AID: {result.AIDOut}\n");
                merchantReceipt.Append($"CARD SCHEME NAME: {result.CardSchemeNameOut}\n");
                merchantReceipt.Append($"PAN: {result.PANOut}\n");
                merchantReceipt.Append($"PAN Sequence Number: {result.PANSequenceNumberOut}\n");
                merchantReceipt.Append($"Transaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(result.TransactionStatusOut))}\n");
                merchantReceipt.Append($"Currency: {Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut)}\n");
                merchantReceipt.Append($"Total Amount: {Utils.FormatReceiptAmount(result.TotalAmountOut)}\n");
                merchantReceipt.Append($"Transaction DateTime: {result.DateTimeOut}\n");
                merchantReceipt.Append($"Host Message: {result.HostTextOut}\n");
                merchantReceipt.Append($"Acquirer Response Code: {result.AcquirerResponseCodeOut}\n");
                merchantReceipt.Append("\n***********************\n");
                merchantReceipt.Append($"{Utils.TransactionOutResult(result.TransactionStatusOut)}\n");
                merchantReceipt.Append("***********************\n");

                Log.Info("Persisting Customer and Merchant ticket to {0}", outputPath);
                //Write the new ticket
                File.WriteAllText(outputPath, customerReceipt.ToString() + merchantReceipt.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Persist Transaction exception.");
                Log.Error(ex);
            }
        }


        //overload the customer ticket to check to return a string
        // output on error
        /// <summary>
        ///  Create Customer Ticket to output the reciept error string
        /// </summary>
        /// <param name="ticket"></param>
        private static void CreateCustomerTicket(string ticket)
        {
            try
            {
                Log.Info($"Persisting Customer ticket to {ticketPath}");

                //Write the new ticket
                File.WriteAllText(ticketPath, ticket);

            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Create Customer Ticket to output the reciept
        /// </summary>
        /// <param name="ticket"></param>
        private static void CreateCustomerTicket(TransactionResponse ticket)
        {
            StringBuilder ticketContent  = new StringBuilder();

            //get the reponse details for the ticket
            
            ticketContent.Append($"CUSTOMER RECEIPT\n");
            ticketContent.Append($"_______________________\n");
            ticketContent.Append($"Merchant Name: {ticket.MerchantNameOut}\n");         
            ticketContent.Append($"Merchant Addr1: {ticket.MerchantAddress1Out}\n");   
            ticketContent.Append($"Merchant Addr2: {ticket.MerchantAddress2Out}\n");     
            ticketContent.Append($"Acquirer MerchantId: {ticket.AcquirerMerchantIDOut}\t");   
            ticketContent.Append($"TID: {ticket.TerminalIdentifierOut}\n");             
            ticketContent.Append($"Card Scheme Name: {ticket.CardSchemeNameOut}\n");       
            ticketContent.Append($"AID: {ticket.AIDOut}\n");                   
            ticketContent.Append($"PAN: {ticket.PANOut}\n");                         
            ticketContent.Append($"PAN SEQ NUM:{ticket.PANSequenceNumberOut}\n");      
            ticketContent.Append($"Entry Method: {Utils.GetEntryMethodString(ticket.EntryMethodOut)}\n");
            ticketContent.Append($"Transaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(ticket.TransactionStatusOut))}\n");   
            ticketContent.Append("\nCARD HOLDER COPY\n");
            ticketContent.Append($"Currency: {Utils.GetCurrencySymbol(ticket.TerminalCurrencyCodeOut)}\n");
            ticketContent.Append($"Purchase Amount: {Utils.FormatReceiptAmount(ticket.TotalAmountOut)}\n");         
            ticketContent.Append($"Date: {DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}\n");
            ticketContent.Append($"Transaction Date/Time: {ticket.DateTimeOut}\n");
            ticketContent.Append("\nThank you\n");
            ticketContent.Append($"{ticket.HostTextOut}\n");          // Host Message
            ticketContent.Append("\n_______________________\n");
            ticketContent.Append($"Order: {Utils.TransactionOutResult(ticket.TransactionStatusOut)}\n");
            ticketContent.Append("_______________________\n");


            try
            {
                Log.Info($"Persisting Customer ticket to {ticketPath}");
                //Write the new ticket
                File.WriteAllText(ticketPath, ticketContent.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }
    }
}
