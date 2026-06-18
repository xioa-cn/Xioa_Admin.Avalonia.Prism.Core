using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace Ava.Xioa.Common.Utils;

 public static class DeepCopyHelper
    {
        /// <summary>
        /// JSON序列化深拷贝（.NET Core 3.0+ / .NET 5+ / .NET 6+ 推荐）
        /// 优点：无需标记[Serializable]，支持大多数对象，简洁轻量
        /// 注意：不支持私有字段、循环引用、委托/事件
        /// </summary>
        public static T? JsonClone<T>(T obj)
        {
            if (obj == null)
                return default;

            try
            {
                var json = JsonSerializer.Serialize(obj);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("JSON深拷贝失败，请检查对象是否包含循环引用/不可序列化成员", ex);
            }
        }

        /// <summary>
        /// XML序列化深拷贝
        /// 优点：跨平台、通用
        /// 限制：必须有无参构造函数，仅支持公共成员，不支持私有字段
        /// </summary>
        public static T? XmlClone<T>(T obj) where T : class
        {
            if (obj == null)
                return null;

            try
            {
                using var stream = new MemoryStream();
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, obj);
                stream.Position = 0;
                return serializer.Deserialize(stream) as T;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("XML深拷贝失败，请检查对象是否有无参构造函数、公共属性", ex);
            }
        }
        
        /// <summary>
        /// 深拷贝（无警告、无依赖、通用）
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T? JsonCloneByBytes<T>(T obj)
        {
            if (obj == null) return default;

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(obj, GlobalJsonOptions.SerializeOptions);
            return JsonSerializer.Deserialize<T>(bytes, GlobalJsonOptions.SerializeOptions);
        }
    }