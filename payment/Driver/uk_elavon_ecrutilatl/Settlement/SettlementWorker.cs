using Acrelec.Library.Logger;
//using com.ingenico.cli.comconcert;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using System.Text;
using ECRUtilATLLib;


namespace Acrelec.Mockingbird.Payment.Settlement
{
    public class SettlementWorker
    {
        public static void OnSettlement(Action<string> fileSender)
        {
            Log.Info("Auto settlement has been triggered...");

            try
            {
                ExecuteSettlement();

                Log.Info("Auto settlement executed!");

                Log.Info("Ziping files...");
                var files = GetFiles();
                var zipPath = ZipFiles(files);

                Log.Info("Sending files...");
                fileSender(zipPath);

                files.Add(zipPath, null);

                Log.Info("Removing sent files...");
                foreach (var file in files.Keys)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                Log.Info("Auto settlement executed succesfully!");
            }
            catch (Exception ex)
            {
                Log.Error("Could not execute auto settlement");
                Log.Error(ex);
                throw ex;
            }
        }

        private static IDictionary<string, string> GetFiles()
        {
            var outPath = Path.GetFullPath(AppConfiguration.Instance.OutPath);
            var outFiles = Directory.GetFiles(outPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("out", GetRelativePath(outPath, _)));

            var logPath = Path.GetFullPath("Logs");
            var logFiles = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories).ToDictionary(_ => _, _ => Path.Combine("logs", GetRelativePath(logPath, _)));

            return Enumerable.Concat(outFiles, logFiles).ToDictionary(_ => _.Key, _ => _.Value);
        }


        private static void ExecuteSettlement()
        {
            using (var api = new ECRUtilATLApi())
            {
                var config = RuntimeConfiguration.Instance;

                api.Connect(config.IpAddress);

                Log.Info("Executing auto settlement...");
                var result = api.EndOfDayReport();
                if (result == null)
                {
                    Log.Info($"Error executing settlement: {result}");
                }
                else
                {
                    Log.Info("Auto settlement executed.");
                    PersistReport(result);
                }
            }
        }

        private static void PersistReport(ECRUtilATLLib.Settlement report)
        {
            try
            {
                StringBuilder reportContent = new StringBuilder();
                Log.Info($"Persist Report");

                //get the reponse details for the ticket
                reportContent.Append($"End of Day\n");
                reportContent.Append($"================\n\n");

                /* Set the settlement outputs */
                reportContent.Append($"MessageNumber: {report.MessageNumberOut}\n");
                reportContent.Append($"Settlement Status: {report.SettlementStatusOut}\n");
                reportContent.Append($"Settlement Date/Time: {report.SettlementDateTimeOut}\n");
                reportContent.Append($"Settlement Response: {report.SettleResponseOut}\n");
                reportContent.Append($"HelpDesk Number: {report.HelpDeskNumberOut}\n");
                reportContent.Append($"Merchant Name: {report.MerchantNameOut}\n");
                reportContent.Append($"Merchant Addr1: {report.MerchantAddress1Out}\n");
                reportContent.Append($"Merchant Addr2: {report.MerchantAddress2Out}\n");
                reportContent.Append($"Merchant Addr3: {report.MerchantAddress3Out}\n");
                reportContent.Append($"Merchant Addr4: {report.MerchantAddress4Out}\n");
                reportContent.Append($"Acquirer Merchant ID: {report.AcquirerMerchantIDOut}\n");
                reportContent.Append($"Terminal Identifier: {report.TerminalIdentifierOut}\n");
                reportContent.Append($"Terminal Country Code: {report.TerminalCountryCodeOut}\n");
                reportContent.Append($"TerminalCurrencyCodeOut: {report.TerminalCurrencyCodeOut}\n");
                reportContent.Append($"Terminal Currency Exponent: {report.TerminalCurrencyExponentOut}\n");
                reportContent.Append($"Batch Number: {report.BatchNumberOut}\n");
                reportContent.Append($"Acquirer Name: {report.AcquirerNameOut}\n");
                reportContent.Append($"Cashbacks Amount: {report.CashbacksAmountOut}\n");
                reportContent.Append($"Cashbacks Count: {report.CashbacksCountOut}\n");
                reportContent.Append($"Debit Amount: {report.DebitAmountOut}\n");
                reportContent.Append($"Debit Count: {report.DebitCountOut}\n");
                reportContent.Append($"Credit Amount: {report.CreditAmountOut}\n");
                reportContent.Append($"Credit Count: {report.CreditCountOut}\n");
                reportContent.Append($"Sales Void Amount: {report.SalesVoidAmountOut}\n");
                reportContent.Append($"Sales Void Count{report.SalesVoidCountOut}\n");
                reportContent.Append($"Refunds Void Amount: {report.RefundsVoidAmountOut}\n");
                reportContent.Append($"Refunds Void Count: {report.RefundsVoidCountOut}\n");
                reportContent.Append($"Is Gratuity Enabled: {report.IsGratuityEnabledOut}\n");
                reportContent.Append($"Gratuity Amount: {report.GratuityAmountOut}\n");
                reportContent.Append($"Is Shift Process Enabled: {report.IsShiftProcessEnabledOut}\n");
                reportContent.Append($"Sum Or Detail Report: {report.SumOrDetailReportOut}\n");
                reportContent.Append($"Transaction Detail Data Record Number: {report.TransactionDetailDataRecordNumberOut}\n");


                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_settlement.txt");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                Log.Info($"Persist Report path: {outputPath}");
                //Write the new ticket
                File.WriteAllText(outputPath, reportContent.ToString());
            }
            catch (Exception ex)
            {
                Log.Info("PersistTicket error.");
                Log.Error(ex);
            }
        }

        private static string ZipFiles(IDictionary<string, string> files)
        {
            string targetPath = Path.Combine("compressed", $"{DateTime.Now:yyyyMMddHHmmss}.zip");

            Log.Info($"Zipping file to: {targetPath}");

            if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            using (var zipFile = ZipFile.Open(targetPath, ZipArchiveMode.Create))
            {
                foreach (var fileEntry in files)
                {
                    zipFile.CreateEntryFromFile(fileEntry.Key, fileEntry.Value);
                }
            }

            return targetPath;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            Func<string, string> appendDirectorySeparatorChar = (path) =>
            {
                // Append a slash only if the path is a directory and does not have a slash.
                if (!Path.HasExtension(path) &&
                    !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    return path + Path.DirectorySeparatorChar;
                }

                return path;
            };

            var fromUri = new Uri(appendDirectorySeparatorChar(fromPath));
            var toUri = new Uri(appendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
