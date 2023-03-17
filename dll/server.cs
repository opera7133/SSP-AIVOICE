using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Threading;
using AI.Talk.Editor.Api;
//  csc /r:"C:\Program Files/\AI/\AIVoice/\AIVoiceEditor/\AI.Talk.Editor.Api.dll"

//match
using System.Text.RegularExpressions;
namespace MyServer
{
  public class Program
  {
    private static int numThreads = 8;
    //end判定用。すべてのwhileに適応。
    public static bool ContinueProgram = true;
    //CPUの処理を下げたい。
    public static int WaitTime = 2000;

    //受け取ったテキストを共有する。
    public static string TalkText = "";
    //現在喋っているボイスのプリセットを共有する。
    public static string TalkingChar = "";
    public static bool TalkingCancel = false;

    static void Main(string[] args)
    {
      TtsControl pTtsControl = new TtsControl();
      pTtsControl = new TtsControl();
      string[] hosts = pTtsControl.GetAvailableHostNames();
      pTtsControl.Initialize(hosts[0]);
      if (pTtsControl.Status != HostStatus.NotRunning)
      {
        pTtsControl.StartHost();
      }
      if (pTtsControl.Status != HostStatus.NotRunning)
      {
        Parallel.Invoke(
          () =>
            {
              RecieveServerTask("UkagakaPlugin/AIVOICE").Wait();
            },
          () =>
            {
              TalkingTask().Wait();
            }
        );
        pTtsControl.TerminateHost();
      }
    }

    public static Task RecieveServerTask(string pipeName)
    {
      return Task.Run(() =>
      {
        NamedPipeServerStream pipeServer = null;
        AIVOICE aivoice = new AIVOICE();
        TtsControl pTtsControl = new TtsControl();
        string[] hosts = pTtsControl.GetAvailableHostNames();
        pTtsControl.Initialize(hosts[0]);
        if (pTtsControl.Status == HostStatus.NotRunning)
        {
          pTtsControl.StartHost();
        }
        if (pTtsControl.Status != HostStatus.Busy && pTtsControl.Status == HostStatus.NotConnected)
        {
          pTtsControl.Connect();
        }

        while (ContinueProgram)
        {
          try
          {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, numThreads);

            // クライアントの接続待ち
            pipeServer.WaitForConnection();
            StreamString ss = new StreamString(pipeServer);

            while (ContinueProgram)
            {
              string writeData = "";
              var readData = ss.ReadString();

              if (readData == "end")
              {
                ss.WriteString("endClient");
                Program.TalkText = readData;
                pTtsControl.Disconnect();
                ContinueProgram = false;
              }
              else if (readData == "GetCharList")
              {
                writeData = aivoice.GetCharList();
                ss.WriteString(writeData);
              }
              else
              {
                writeData = "Server read OK.";
                ss.WriteString(writeData);
                Program.TalkingCancel = true;
                if (Program.TalkingChar != "")
                {
                  pTtsControl.CurrentVoicePresetName = Program.TalkingChar;
                  pTtsControl.Stop();
                }
                Program.TalkText = readData;
              }
            }
          }
          catch (OverflowException e)
          {
            //Console.WriteLine( e.Message );
          }
          finally
          {
            pipeServer.Close();
          }
        }
      });
    }

    public static Task TalkingTask()
    {
      string text = "";
      AIVOICE aivoice = new AIVOICE();
      return Task.Run(() =>
      {
        while (ContinueProgram)
        {
          Thread.Sleep(Program.WaitTime);
          if (Program.TalkText != "" && Program.TalkText != "end")
          {
            text = Program.TalkText;
            Program.TalkText = "";
            aivoice.talk(text);
          }
        }
      });
    }
  }

  public class StreamString
  {
    private Stream ioStream;
    private UnicodeEncoding streamEncoding;

    public StreamString(Stream ioStream)
    {
      this.ioStream = ioStream;
      streamEncoding = new UnicodeEncoding();
    }

    public string ReadString()
    {
      int len = 0;

      len = ioStream.ReadByte() * 256;
      len += ioStream.ReadByte();
      byte[] inBuffer = new byte[len];
      ioStream.Read(inBuffer, 0, len);

      return streamEncoding.GetString(inBuffer);
    }

    public int WriteString(string outString)
    {
      byte[] outBuffer = streamEncoding.GetBytes(outString);
      int len = outBuffer.Length;
      if (len > UInt16.MaxValue)
      {
        len = (int)UInt16.MaxValue;
      }
      ioStream.WriteByte((byte)(len / 256));
      ioStream.WriteByte((byte)(len & 255));
      ioStream.Write(outBuffer, 0, len);
      ioStream.Flush();

      return outBuffer.Length + 2;
    }
  }

  public class AIVOICE
  {
    private TtsControl pTtsControl;

    //使用可能なキャラリストを取得
    public string GetCharList()
    {
      pTtsControl = new TtsControl();
      string[] hosts = pTtsControl.GetAvailableHostNames();
      pTtsControl.Initialize(hosts[0]);
      if (pTtsControl.Status == HostStatus.NotRunning)
      {
        pTtsControl.StartHost();
      }
      if (pTtsControl.Status != HostStatus.Busy)
      {
        pTtsControl.Connect();
        string[] CharList = pTtsControl.VoiceNames;
        pTtsControl.Disconnect();
        return string.Join(",", CharList);
      }
      else
      {
        return "";
      }
    }

    // arg: string
    // 形式："ボイス名,音量,話速,高さ,抑揚,短ポーズ(ms),長ポーズ(ms),喜び,怒り,悲しみ"
    // 例　："結月 ゆかり,1,1.2,1,1,150,370,0,0,0,こんにちは"
    public void talk(string arg)
    {
      pTtsControl = new TtsControl();
      string[] hosts = pTtsControl.GetAvailableHostNames();
      pTtsControl.Initialize(hosts[0]);
      if (pTtsControl.Status == HostStatus.NotRunning)
      {
        pTtsControl.StartHost();
      }
      if (pTtsControl.Status == HostStatus.NotConnected)
      {
        pTtsControl.Connect();
      }
      char[] sep = new char[] { ',' };
      string[] head = arg.Split(sep, 2);
      string CharPreset = head[0];
      string[] presets = pTtsControl.VoicePresetNames;
      bool result = Array.Exists(presets, preset => preset == CharPreset + " 伺か一時プリセット");
      if (!result)
      {
        Match VoiceName = Regex.Match(pTtsControl.GetVoicePreset(CharPreset), "\"VoiceName\":\"([^\"]*)\"");
        var initPreset = new Dictionary<string, string>(){
          {"PresetName", CharPreset + " 伺か一時プリセット"},
          {"VoiceName", VoiceName.Groups[1].Value},
        };
        pTtsControl.AddVoicePreset(JsonConvert.SerializeObject(initPreset));
      }
      pTtsControl.CurrentVoicePresetName = CharPreset + " 伺か一時プリセット";
      Program.TalkingChar = CharPreset;
      Console.WriteLine("Char = " + CharPreset);

      string[] Options = arg.Split(sep, 11);

      //共通項が9
      string CurrentPreset = "{";
      //多次元連想配列の作り方がわからない
      CurrentPreset += "\"PresetName\":\"" + CharPreset + " 伺か一時プリセット" + "\",";
      CurrentPreset += "\"Volume\":" + Convert.ToDouble(Options[1]) + ",";
      CurrentPreset += "\"Speed\":" + Convert.ToDouble(Options[2]) + ",";
      CurrentPreset += "\"Pitch\":" + Convert.ToDouble(Options[3]) + ",";
      CurrentPreset += "\"PitchRange\":" + Convert.ToDouble(Options[4]) + ",";
      CurrentPreset += "\"MiddlePause\":" + Convert.ToDouble(Options[5]) + ",";
      CurrentPreset += "\"LongPause\":" + Convert.ToDouble(Options[6]) + ",";
      CurrentPreset += "\"Styles\":[{\"Name\":\"J\", \"Value\":" + Convert.ToDouble(Options[7]) + "},";
      CurrentPreset += "{\"Name\":\"A\", \"Value\":" + Convert.ToDouble(Options[8]) + "},";
      CurrentPreset += "{\"Name\":\"S\", \"Value\":" + Convert.ToDouble(Options[9]) + "}]";
      CurrentPreset += "}";

      pTtsControl.SetVoicePreset(CurrentPreset);

      string[] MySep = { "MySep" };
      string VoiceText = Options[10];

      //バルーン空打ち対策
      string CheckText = VoiceText;
      if (CheckText != "")
      {
        pTtsControl.Stop();

        string[] VoiceSep = { "," };
        string[] VoiceTexts = VoiceText.Split(VoiceSep, StringSplitOptions.None);

        Program.TalkingCancel = false;
        foreach (string Line in VoiceTexts)
        {
          //外部からトークを終了できるように。
          if (Program.TalkingCancel)
          {
            break;
          }
          if (Line != "")
          {
            Console.WriteLine(Line);
            pTtsControl.Text = Line;
            pTtsControl.Play();
          }
        }
      }
    }
  }
}
