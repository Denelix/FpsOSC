using AfterburnerDataHandler.SharedMemory.RivaTunerStatisticsServer;
using CoreOSC;

class Program
{
    private static RTSSSM rtss = new RTSSSM();
    private static UDPSender sender = new CoreOSC.UDPSender("127.0.0.1", 9000);
    static string message = " Specs in Bio."; //Space is at beginning for a reaosn. 

    static void Main(string[] args)
    {
        rtss.Start();
        while (true)
        {
            rtss.Update();
            UpdateStats("VRChat.exe");
            Thread.Sleep(3000);
        }
    }

    static void UpdateStats(string exe)
    {
        foreach (var Application in rtss.APPEntries)
        {
            if (Application.szName.Contains(exe))
            {
                var message = new OscMessage(
                    "/chatbox/input",
                    $"FPS: {(Application.dwFrames)}",
                    true,
                    false);

                sender.Send(message);
                break;
            }
        }
    }
}

