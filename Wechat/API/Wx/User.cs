﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wechat.API
{
    public class User
    {
        public long Uin;
        public string UserName;
        public string NickName;
        public string HeadImgUrl;
        public int ContactFlag;
        public int MemberCount;
        public User[] MemberList;
        public string RemarkName;
        public int Sex;
        public string Signature;
        public int VerifyFlag;
        public int OwnerUin;
        public string PYInitial;
        public string PYQuanPin;
        public string RemarkPYInitial;
        public string RemarkPYQuanPin;
        public int StarFriend;
        public int AppAccountFlag;
        public int Statues;
        public long AttrStatus;
        public string Province;
        public string City;
        public string Alias;
        public int SnsFlag;
        public int UniFriend;
        public string DisplayName;
        public int ChatRoomId;
        public string KeyWord;
        public string EncryChatRoomId;
		public User() {

		}
		public User(Contact from)
		{
			FromContact(from);
		}
		public void FromContact(Contact from)
		{
			this.UserName = from.ID;
			this.NickName = from.NickName;
			this.RemarkName= from.RemarkName;
		}
    }
}
