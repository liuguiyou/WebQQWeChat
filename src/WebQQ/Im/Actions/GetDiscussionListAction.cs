﻿using FclEx.Http;
using FclEx.Http.Core;
using FclEx.Http.Event;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using WebQQ.Im.Bean.Discussion;
using WebQQ.Im.Core;

namespace WebQQ.Im.Actions
{
    public class GetDiscussionListAction : WebQQInfoAction
    {
        public GetDiscussionListAction(IQQContext context, ActionEventListener listener = null) : base(context, listener)
        {
        }

        protected override void ModifyRequest(HttpReq req)
        {
            req.AddData("clientid", Session.ClientId);
            req.AddData("psessionid", Session.SessionId);
            req.AddData("vfwebqq", Session.Vfwebqq);
            req.AddData("t", Timestamp);
            req.Referrer = ApiUrls.ReferrerS;
        }

        protected override void HandleResult(JToken json)
        {
            /*
                {
                    "retcode": 0,
                    "result": {
                        "dnamelist": [
                            {
                                "name": "月光双刀、Test、月光借口、月光双",
                                "did": 522140442
                            }
                        ]
                    }
                }             
             */
            var result = json["result"];
            var list = result["dnamelist"].ToObject<List<QQDiscussion>>();
            list.ForEach(Store.AddDiscussion);
        }
    }
}
