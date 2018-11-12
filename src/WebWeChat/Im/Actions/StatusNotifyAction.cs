using FclEx;
using FclEx.Http.Core;
using FclEx.Http.Event;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using WebWeChat.Im.Core;

namespace WebWeChat.Im.Actions
{
    /// <summary>
    /// 开启状态通知
    /// </summary>
    [Description("开启状态通知")]
    public class StatusNotifyAction : WebWeChatAction
    {
        public StatusNotifyAction(IWeChatContext context, ActionEventListener listener = null)
            : base(context, listener)
        {
        }

        protected override HttpReq BuildRequest()
        {
            var url = string.Format(ApiUrls.StatusNotify, Session.BaseUrl, Session.PassTicket);
            var obj = new
            {
                Session.BaseRequest,
                Code = 3,
                FromUserName = Session.UserToken["UserName"],
                ToUserName = Session.UserToken["UserName"],
                ClientMsgId = Timestamp
            };
            var req = new HttpReq(HttpMethodType.Post, url)
            {
                ByteArrayData = obj.ToJson().ToBytes(Encoding.UTF8),
                ContentType = HttpConstants.JsonContentType,
            };
            return req;
        }

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes responseItem)
        {
            /*
                {
                    "BaseResponse": {
                        "Ret": 0,
                        "ErrMsg": ""
                    },
                    "MsgID": "5895072760632094896"
                }
            */
            var str = responseItem.ResponseString;
            if (!str.IsNullOrEmpty())
            {
                var json = JObject.Parse(str);
                if (json["BaseResponse"]["Ret"].ToString() == "0")
                {
                    return NotifyOkEventAsync();
                }
                else
                {
                    throw new WeChatException(WeChatErrorCode.ResponseError, json["BaseResponse"]["ErrMsg"].ToString());
                }

            }
            throw WeChatException.CreateException(WeChatErrorCode.ResponseError);
        }
    }
}
