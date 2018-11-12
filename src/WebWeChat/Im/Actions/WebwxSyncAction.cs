﻿using FclEx;
using FclEx.Http.Core;
using FclEx.Http.Event;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebWeChat.Im.Bean;
using WebWeChat.Im.Core;

namespace WebWeChat.Im.Actions
{
    /// <summary>
    /// 消息同步
    /// </summary>
    [Description("消息同步")]
    public class WebwxSyncAction : WebWeChatAction
    {
        public WebwxSyncAction(IWeChatContext context, ActionEventListener listener = null) : base(context, listener)
        {
        }

        protected override HttpReq BuildRequest()
        {
            var url = string.Format(ApiUrls.WebwxSync, Session.BaseUrl, Session.Sid, Session.Skey, Session.PassTicket);
            // var url = string.Format(ApiUrls.WebwxSync, Session.BaseUrl);
            var obj = new
            {
                Session.BaseRequest,
                Session.SyncKey,
                rr = ~Timestamp // 注意是按位取反
            };
            var req = new HttpReq(HttpMethodType.Post, url)
            {
                ByteArrayData = obj.ToJson().ToBytes(Encoding.UTF8),
                ContentType = HttpConstants.JsonContentType,
                Referrer = "https://wx.qq.com/?&lang=zh_CN"
            };
            return req;
        }

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes response)
        {
            var str = response.ResponseString;
            var json = JObject.Parse(str);
            if (json["BaseResponse"]["Ret"].ToString() == "0")
            {
                Session.SyncKey = json["SyncCheckKey"];
                var list = json["AddMsgList"].ToObject<List<Message>>();
                var newMsgs = list.Where(m => m.MsgType != MessageType.GetContact).ToList();
                newMsgs.ForEach(m =>
                {
                    m.FromUser = Store.ContactMemberDic.GetOrDefault(m.FromUserName);
                });
                return NotifyOkEventAsync(newMsgs);
            }
            throw WeChatException.CreateException(WeChatErrorCode.ResponseError);

        }
    }
}
