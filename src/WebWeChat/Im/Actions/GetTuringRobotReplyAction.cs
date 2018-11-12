﻿using FclEx;
using FclEx.Http.Core;
using FclEx.Http.Event;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebWeChat.Im.Core;

namespace WebWeChat.Im.Actions
{
    public class GetTuringRobotReplyAction : WebWeChatAction
    {
        private readonly string _input;
        private readonly string _key;

        public GetTuringRobotReplyAction(IWeChatContext context, string input, ActionEventListener listener = null)
            : base(context, listener)
        {
            _input = input;
            _key = Config["TulingApiKey"];
        }

        protected override ValueTask<ActionEvent> ExecuteInternalAsync(CancellationToken token)
        {
            return _key.IsNullOrEmpty() ?
                NotifyErrorEventAsync(WeChatErrorCode.ParameterError, nameof(_key)) :
                base.ExecuteInternalAsync(token);
        }

        protected override HttpReq BuildRequest()
        {
            var req = HttpReq.Create(ApiUrls.TulingRobot, HttpReqType.Json);
            var obj = new
            {
                key = _key,
                info = _input,
                userid = Session.User.UserName,
            };
            req.ByteArrayData = obj.ToJson().ToBytes(Encoding.UTF8);
            return req;
        }

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes response)
        {
            var str = response.ResponseString;
            var json = str.ToJToken();
            var code = json["code"].ToString();
            var reply = "";

            switch (code)
            {
                // 文本类
                case "100000":
                    reply = json["text"].ToString();
                    break;

                // 链接类，包括列车、航班
                case "200000":
                {
                    var text = json["text"].ToString();
                    var url = json["url"].ToString();
                    reply = $"{text} \n网址：{url}";
                    break;
                }

                // 新闻类
                case "302000":
                {
                    var text = json["text"].ToString();
                    var list = json["list"].ToObject<JArray>();
                    var sb = new StringBuilder(text);
                    foreach (var item in list)
                    {
                        var article = item["article"].ToString();
                        var source = item["source"].ToString();
                        var icon = item["icon"].ToString();
                        var detailurl = item["detailurl"].ToString();
                        sb.AppendLine(article);
                    }
                    reply = sb.ToString();
                    break;
                }

                // 菜谱类
                case "308000":
                {
                    var text = json["text"].ToString();
                    var list = json["list"].ToObject<JArray>();
                    var sb = new StringBuilder(text);
                    foreach (var item in list)
                    {
                        var name = item["name"].ToString();
                        var icon = item["icon"].ToString();
                        var info = item["info"].ToString();
                        var detailurl = item["detailurl"].ToString();
                        sb.AppendLine($"{name}：info");
                    }
                    reply = sb.ToString();
                    break;
                }

                // 异常码
                case "40001": return NotifyErrorEventAsync(WeChatException.CreateException(WeChatErrorCode.ResponseError, $"[参数key错误]:{str}"));
                case "40002": return NotifyErrorEventAsync(WeChatException.CreateException(WeChatErrorCode.ResponseError, $"[请求内容info为空]:{str}"));
                case "40004": return NotifyErrorEventAsync(WeChatException.CreateException(WeChatErrorCode.ResponseError, $"[当天请求次数已使用完]:{str}"));
                case "40007": return NotifyErrorEventAsync(WeChatException.CreateException(WeChatErrorCode.ResponseError, $"[数据格式异常]:{str}"));

                default:
                    reply = json["text"].ToString();
                    break;
            }
            return NotifyOkEventAsync(reply);
        }
    }
}
