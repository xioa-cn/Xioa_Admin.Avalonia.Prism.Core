using System.Text;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Services;
using Avalonia.Threading;

namespace AvaloniaApplication;

public partial class App
{
    public static SharedMemoryPubSub? _sharedMemoryPubSub;

    private const string AppOpen = "OPEN";

    public static bool Detection
    {
        get
        {
            _sharedMemoryPubSub ??= new SharedMemoryPubSub(nameof(AvaloniaApplication));
            string? mName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.ModuleName;
            string? pName = System.IO.Path.GetFileNameWithoutExtension(mName);
            if (System.Diagnostics.Process.GetProcessesByName(pName).Length > 1)
            {
                _sharedMemoryPubSub.Publish(MessageTopics.STATUS_UPDATE,
                    Encoding.UTF8.GetBytes(AppOpen)
                );
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void SubscribeMessage()
    {
        _sharedMemoryPubSub!.Subscribe(MessageTopics.STATUS_UPDATE, OnApplicationOpen);
    }

    private void OnApplicationOpen((int MessageId, int TopicId, byte[] Data) obj)
    {
        if (obj.TopicId != MessageTopics.STATUS_UPDATE)
            return;
        // 将消息的数据转换为字符串
        var ms = Encoding.UTF8.GetString(obj.Data).TrimEnd('\0');
        // 如果字符串等于AppOpen，则调用App.MainShow()方法
        if (ms != AppOpen)
        {
            return;
        }

        Dispatcher.UIThread.Invoke(OpenMainWindow);
    }
}