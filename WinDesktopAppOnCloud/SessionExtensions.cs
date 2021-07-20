using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinDesktopAppOnCloud
{

    // セッションにオブジェクトを設定・取得する拡張メソッドを用意する
    public static class SessionExtensions
    {
        // セッションにオブジェクトを書き込む
        public static void SetObject<TObject>(this ISession session, string key, TObject obj)
        {
            var json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
            session.SetString(key, json);
        }

        // セッションからオブジェクトを読み込む
        public static TObject GetObject<TObject>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return string.IsNullOrEmpty(json)
                ? default(TObject)
                : JsonConvert.DeserializeObject<TObject>(json);
        }
    }
}
