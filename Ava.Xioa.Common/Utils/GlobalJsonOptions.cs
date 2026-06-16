using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ava.Xioa.Common.Utils;

public class GlobalJsonOptions
{
    public static JsonSerializerOptions SerializeOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true, // 格式化输出（便于调试）
        ReferenceHandler = ReferenceHandler.IgnoreCycles, // 忽略循环引用
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 忽略中文转义和符号转义
        //Converters = { new JsonStringEnumConverter() } // 枚举序列化为字符串
    };
}