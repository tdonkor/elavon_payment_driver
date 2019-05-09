//using Acrelec.Library.Logger;
//using Acrelec.Mockingbird.Payment;
//using Acrelec.Mockingbird.Payment.Configuration;
//using Acrelec.Mockingbird.Payment.Contracts;
//using ECRUtilATLLib;
//using System;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;

//namespace Acrelec.Mockingbird.Payment.Settlement
//{

//    public class SettlementListener : IDisposable
//    {
//        Thread listenerThread;
//        DoPrintJob doPrintJob;

//    public SettlementListener()
//    {
//        //check for Settlement
//        Log.Info("Auto settlement has been triggered...");

//         ExecuteSettlement();
//    }

//    public void Dispose()
//    {
//        Log.Info("Disposing");
//    }

//    public void ExecuteSettlement()
//    {
//      using (var api = new ECRUtilATLApi())
//      {
//        var config = RuntimeConfiguration.Instance;
//        var configFile = AppConfiguration.Instance;

//        doPrintJob = new DoPrintJob();

//          //listen for the Batch End Of Day
//          listenerThread = new Thread(doPrintJob.DoThePrintJob);
//          listenerThread.Start();
                

//        api.Connect(configFile.IpAddress);

//        var result = api.DoSettlementReport();
//        if (result == null)
//        {
//            Log.Info($"Error executing settlement: {result}");
//        }
//        else
//        {
//            Log.Info("Auto settlement executed.");
           
//        }
//    }
//}
        

//  }
//}
