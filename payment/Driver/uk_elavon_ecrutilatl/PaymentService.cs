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

            //initalise confguration file instance
            var configFile = AppConfiguration.Instance;

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

                if  (string.IsNullOrEmpty(configFile.IpAddress))
                {
                    Log.Info(".ini file IP Address value must be set.");
                    return ResultCode.GenericError;
                }

                Log.Info($"Configuration default IPAddress: {configuration.IpAddress}.");
                Log.Info($".ini file IPAddress: {configFile.IpAddress}.");

                using (var api = new ECRUtilATLApi())
                {
                    // var connectResult = api.Connect(configuration.IpAddress);
                    var connectResult = api.Connect(configFile.IpAddress);

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
                  
                    if (payResult != DiagnosticErrMsg.OK)
                    {
                        Log.Error($"Pay Result Not OK: {payResult}");
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                     data.Result = (PaymentResult)Utils.GetTransactionOutResult(payResponse.TransactionStatusOut);

                    //check if Transaction is a reversal and fail if it is fail the Payment result
                    if (Utils.GetTransactionTypeString(Convert.ToInt16(payResponse.TransactionStatusOut)) == "REVERSAL")
                    {
                        data.Result = PaymentResult.Failed;
                    }


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
                    if ((DiagnosticErrMsg)Convert.ToInt16(payResponse.DiagRequestOut) == DiagnosticErrMsg.OK)
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

                    // shut down the 
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

                customerReceipt.Append($"\tCUSTOMER RECEIPT\n");
                customerReceipt.Append($"\t=======================\n");
                customerReceipt.Append($"\tMerchant Name: {result.MerchantNameOut}\n");
                customerReceipt.Append($"\tMerchant Addr1: {result.MerchantAddress1Out}\n");
                customerReceipt.Append($"\tMerchant Addr2: {result.MerchantAddress2Out}\n");
                customerReceipt.Append($"\tMerchant Addr3: {result.MerchantAddress3Out}\n");
                customerReceipt.Append($"\tMerchant Addr4: {result.MerchantAddress4Out}\n");
                customerReceipt.Append($"\tAcquirer MerchantId: {result.AcquirerMerchantIDOut}\n");
                customerReceipt.Append($"\tTID: {result.TerminalIdentifierOut}\n");
                customerReceipt.Append($"\tCard Scheme Name: {result.CardSchemeNameOut}\n");
                customerReceipt.Append($"\tAID: {result.AIDOut}\n");
                customerReceipt.Append($"\tPAN: {result.PANOut}\n");
                customerReceipt.Append($"\tPAN SEQ NUM:{result.PANSequenceNumberOut}\n");
                customerReceipt.Append($"\tEntry Method: {Utils.GetEntryMethodString(result.EntryMethodOut)}\n");
                customerReceipt.Append($"\tTransaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(result.TransactionStatusOut))}\n");
                customerReceipt.Append("\tCARD HOLDER COPY\n");
                customerReceipt.Append($"\tTerminal Currency: {Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut)}\n");
                customerReceipt.Append($"\tPurchase Amount: {Utils.GetCurrencyChar(Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut))}{Utils.FormatReceiptAmount(result.TotalAmountOut)}\n");
                customerReceipt.Append($"\tTransaction Date/Time:\n{DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}\n");
                customerReceipt.Append("\n\tThank you\n");
                customerReceipt.Append($"\tHost Message:{result.HostTextOut}\n");          // Host Message
                customerReceipt.Append("\n\t=====================\n");
                customerReceipt.Append($"\t{Utils.TransactionOutResult(result.TransactionStatusOut)}\n");
                customerReceipt.Append("\t=====================\n");


                //get the reponse details for the ticket
                merchantReceipt.Append($"\n\tMERCHANT RECEIPT\n");
                merchantReceipt.Append($"\t================\n");
                merchantReceipt.Append($"\tAcquirer Merchant ID: {result.AcquirerMerchantIDOut}\n");
                merchantReceipt.Append($"\tMerchant NAME:  {result.MerchantNameOut}\n");
                merchantReceipt.Append($"\tMerchant ADDR1: {result.MerchantAddress1Out}\n");
                merchantReceipt.Append($"\tMerchant ADDR2: {result.MerchantAddress2Out}\n");
                merchantReceipt.Append($"\tMerchant ADDR3: {result.MerchantAddress3Out}\n");
                merchantReceipt.Append($"\tMerchant ADDR4: {result.MerchantAddress4Out}\n");
                merchantReceipt.Append($"\tEntry Method: {Utils.GetEntryMethodString(result.EntryMethodOut)}\n");
                merchantReceipt.Append($"\tTID: {result.TerminalIdentifierOut}\n");
                merchantReceipt.Append($"\tAID: {result.AIDOut}\n");
                merchantReceipt.Append($"\tCARD SCHEME NAME: {result.CardSchemeNameOut}\n");
                merchantReceipt.Append($"\tPAN: {result.PANOut}\n");
                merchantReceipt.Append($"\tPAN Sequence Number: {result.PANSequenceNumberOut}\n");
                merchantReceipt.Append($"\tTransaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(result.TransactionStatusOut))}\n");
                merchantReceipt.Append($"\tTerminal Currency: {Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut)}\n");
                merchantReceipt.Append($"\tPurchase Amount: {Utils.GetCurrencyChar(Utils.GetCurrencySymbol(result.TerminalCurrencyCodeOut))}{Utils.FormatReceiptAmount(result.TotalAmountOut)}\n");
                merchantReceipt.Append($"\tTransaction Date/Time:\n{DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}\n");
                merchantReceipt.Append($"\tHost Message: {result.HostTextOut}\n");
                merchantReceipt.Append($"\tAcquirer Response Code: {result.AcquirerResponseCodeOut}\n");
                merchantReceipt.Append("\n\t=======================\n");
                merchantReceipt.Append($"\t{Utils.TransactionOutResult(result.TransactionStatusOut)}\n");
                merchantReceipt.Append("\t========================\n");

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
            
            ticketContent.Append($"\tCUSTOMER RECEIPT\n");
            ticketContent.Append($"\t_______________________\n");
            ticketContent.Append($"\tMerchant Name: {ticket.MerchantNameOut}\n");         
            ticketContent.Append($"\tMerchant Addr1: {ticket.MerchantAddress1Out}\n");   
            ticketContent.Append($"\tMerchant Addr2: {ticket.MerchantAddress2Out}\n");     
            ticketContent.Append($"\tAcquirer MerchantId: {ticket.AcquirerMerchantIDOut}\n");   
            ticketContent.Append($"\tTID: {ticket.TerminalIdentifierOut}\n");             
            ticketContent.Append($"\tCard Scheme Name: {ticket.CardSchemeNameOut}\n");       
            ticketContent.Append($"\tAID: {ticket.AIDOut}\n");                   
            ticketContent.Append($"\tPAN: {ticket.PANOut}\n");                         
            ticketContent.Append($"\tPAN SEQ NUM:{ticket.PANSequenceNumberOut}\n");      
            ticketContent.Append($"\tEntry Method: {Utils.GetEntryMethodString(ticket.EntryMethodOut)}\n");
            ticketContent.Append($"\tTransaction Type: {Utils.GetTransactionTypeString(Convert.ToInt16(ticket.TransactionStatusOut))}\n");   
            ticketContent.Append("\tCARD HOLDER COPY\n");
            ticketContent.Append($"\tTerminal Currency: {Utils.GetCurrencySymbol(ticket.TerminalCurrencyCodeOut)}\n");
            ticketContent.Append($"\tPurchase Amount: {Utils.GetCurrencyChar(Utils.GetCurrencySymbol(ticket.TerminalCurrencyCodeOut))}{Utils.FormatReceiptAmount(ticket.TotalAmountOut)}\n");
            ticketContent.Append($"\tTransaction Date/Time:\n{DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}\n");
            ticketContent.Append("\n\tThank you\n");
            ticketContent.Append($"\tHost Message: {ticket.HostTextOut}\n");          // Host Message
            ticketContent.Append("\n\t_______________________\n");
            ticketContent.Append($"\t\t{Utils.TransactionOutResult(ticket.TransactionStatusOut)}\n");
            ticketContent.Append("\t_______________________\n");


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
