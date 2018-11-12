﻿using FclEx;
using FclEx.Http;
using FclEx.Http.Core;
using FclEx.Http.Event;
using System.Threading.Tasks;
using WebQQ.Im.Core;

namespace WebQQ.Im.Actions
{
    public class GetVfwebqqAction : WebQQAction
    {
        public GetVfwebqqAction(IQQContext context, ActionEventListener listener = null) : base(context, listener)
        {
        }

        protected override HttpReqType ReqType { get; } = HttpReqType.Get;

        protected override void ModifyRequest(HttpReq req)
        {
            req.AddData("ptwebqq", Session.Ptwebqq);
            req.AddData("clientid", Session.ClientId);
            req.AddData("psessionid", "");
            req.AddData("t", Timestamp);
            req.Referrer = ApiUrls.ReferrerS;
        }

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes response)
        {
            var json = response.ResponseString.ToJToken();
            if (json["retcode"].ToString() == "0")
            {
                var ret = json["result"];
                Session.Vfwebqq = ret["vfwebqq"].ToString();
                return NotifyOkEventAsync();
            }
            else
            {
                throw new QQException(QQErrorCode.ResponseError, response.ResponseString);
            }
        }
    }
}
