using AI.Talk.Editor.Api;
//  csc /r:"C:\Program Files/\AI/\AIVoice/\AIVoiceEditor/\AI.Talk.Editor.Api.dll"

using System;

class Program
{
  static void Main(string[] args)
  {
    TtsControl pTtsControl = new TtsControl();
    string[] hosts = pTtsControl.GetAvailableHostNames();
    pTtsControl.Initialize(hosts[0]);
    if (pTtsControl.Status != HostStatus.NotRunning)
    {
      Console.Write("True");
    }
    else
    {
      pTtsControl.StartHost();
      Console.Write("False");
    }
  }
}