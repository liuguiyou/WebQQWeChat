﻿using FclEx;

namespace WebQQ.Im.Bean.Group
{
    public class GroupMemberInfo
    {
        public virtual long Uin { get; set; }
        public string Nick { get; set; }
        public string Province { get; set; }
        public string Gender { get; set; }
        public string Country { get; set; }
        public string City { get; set; }

        public string ShowName => Nick.IsNullOrEmpty() ? Uin.ToString() : Nick;
    }
}
