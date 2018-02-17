﻿using System;
using System.Collections.Generic;
using System.Text;
using Wechat.API.RPC;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Net.Http;
using Wechat.tools;
using System.Net;
using System.Collections;
using System.Threading.Tasks;

namespace Wechat.API
{
    public class WechatAPIService:IDisposable
    {
        HttpClientHandler mHandler;
        HttpClient mHttpClient;
        public WechatAPIService() {
            InitHttpClient();
        }

        private void InitHttpClient()
        {
			mHandler = new HttpClientHandler()
			{
				UseCookies = true,
				AutomaticDecompression = DecompressionMethods.GZip,
				AllowAutoRedirect = true
			};

			mHttpClient = new HttpClient(mHandler);
			mHttpClient.DefaultRequestHeaders.ExpectContinue = false;
			SetHttpHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
			SetHttpHeader("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,zh-TW;q=0.4,ja;q=0.2");
			SetHttpHeader("Accept-Encoding", "gzip, deflate, sdch, br");
				
			
        }


        /// <summary>
        /// 获得二维码登录SessionID,使用此ID可以获得登录二维码
        /// </summary>
        /// <returns>Session</returns>
        public string GetNewQRLoginSessionID()
        {
            //respone like this => window.QRLogin.code = 200; window.QRLogin.uuid = "Qa_GBH_IqA==";
            string url = "https://login.wx2.qq.com/jslogin?appid=wx782c26e4c19acffb";

            SetHttpHeader("Accept", "*/*");
            mHttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wx2.qq.com/");
           
            string str = GetString(url);
            if (str == null) return null;
            var pairs = str.Split(new string[] { "\"" }, StringSplitOptions.None);
            if (pairs.Length >= 2) {
                string sessionID = pairs[1];
                return sessionID;
            }
            return null;
        }

        /// <summary>
        /// 获得登录二维码URL
        /// </summary>
        /// <param name="QRLoginSessionID"></param>
        /// <returns></returns>
        public string GetQRCodeUrl(string QRLoginSessionID) {
            string url = "https://login.weixin.qq.com/qrcode/" + QRLoginSessionID;
            return url;
        }

        /// <summary>
        /// 获得登录二维码图片
        /// </summary>
        /// <param name="QRLoginSessionID"></param>
        /// <returns></returns>
        public Image GetQRCodeImage(string QRLoginSessionID)
        {
            string url = GetQRCodeUrl(QRLoginSessionID);
            SetHttpHeader("Accept", "image/webp,image/*,*/*;q=0.8");
            mHttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wx2.qq.com/");
            try
            {
                HttpResponseMessage response = mHttpClient.GetAsync(new Uri(url)).Result;
                var bytes = response.Content.ReadAsByteArrayAsync().Result;
                if (bytes != null && bytes.Length > 0) {
                    return Image.FromStream(new MemoryStream(bytes));
                }
                return null;
            }
            catch {
                InitHttpClient();
                return null;
            }

        }

        /// <summary>
        /// 登录检查
        /// </summary>
        /// <param name="QRLoginSessionID"></param>
        /// <returns></returns>
        public LoginResult Login(string QRLoginSessionID,long t)
        {
            string url = string.Format("https://login.wx2.qq.com/cgi-bin/mmwebwx-bin/login?loginicon=true&uuid={0}&tip={1}&r={2}&_={3}",
                QRLoginSessionID,"0",t/1579,t);

            SetHttpHeader("Accept", "*/*");
            mHttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wx.qq.com/");


            string login_result = GetString(url);
            if (login_result == null) return null;

			LoginResult result = new LoginResult
			{
				code = 408
			};
			if (login_result.Contains("window.code=201")) //已扫描 未登录
            {
                string base64_image = login_result.Split(new string[] { "\'" }, StringSplitOptions.None)[1].Split(',')[1];
                result.code = 201;
                result.UserAvatar = base64_image;
            } else if (login_result.Contains("window.code=200"))  //已扫描 已登录
              {
                string login_redirect_url = login_result.Split(new string[] { "\"" }, StringSplitOptions.None)[1];
                result.code = 200;
                result.redirect_uri = login_redirect_url;
            }

            return result;
        }

        public LoginRedirectResult LoginRedirect(string redirect_uri)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            mHttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wx2.qq.com/");

            string url = redirect_uri + "&fun=new&version=v2&lang=zh_CN";
            string rep = GetString(url);
            if (rep == null) return null;

			LoginRedirectResult result = new LoginRedirectResult
			{
				pass_ticket = rep.Split(new string[] { "pass_ticket" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
				skey = rep.Split(new string[] { "skey" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
				wxsid = rep.Split(new string[] { "wxsid" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
				wxuin = rep.Split(new string[] { "wxuin" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
				isgrayscale = rep.Split(new string[] { "isgrayscale" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/')
			};
			return result;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="pass_ticket"></param>
        /// <param name="uin"></param>
        /// <param name="sid"></param>
        /// <param name="skey"></param>
        /// <param name="deviceID"></param>
        /// <returns></returns>
        public InitResponse Init(string pass_ticket,BaseRequest baseReq)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            mHttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wx.qq.com/");

            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r={0}&pass_ticket={1}";
            url = string.Format(url, GetR(), pass_ticket);
			InitRequest initReq = new InitRequest
			{
				BaseRequest = baseReq
			};
			string requestJson = JsonConvert.SerializeObject(initReq);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<InitResponse>(repJsonStr);
            return rep;
        }

        /// <summary>
        /// 获得联系人列表
        /// </summary>
        /// <param name="pass_ticket"></param>
        /// <param name="skey"></param>
        /// <returns></returns>

        public GetContactResponse GetContact(string pass_ticket,string skey)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact?pass_ticket={0}&r={1}&seq=0&skey={2}";
            url = string.Format(url, pass_ticket, GetR(), skey);
            string json = GetString(url);
            var rep = JsonConvert.DeserializeObject<GetContactResponse>(json);
            if (rep == null) return null;
            return rep;
        }

        /// <summary>
        /// 批量获取联系人详细信息
        /// </summary>
        /// <param name="requestContacts"></param>
        /// <param name="pass_ticket"></param>
        /// <param name="uin"></param>
        /// <param name="sid"></param>
        /// <param name="skey"></param>
        /// <param name="deviceID"></param>
        /// <returns></returns>
        public BatchGetContactResponse BatchGetContact(string[] requestContacts,string pass_ticket,BaseRequest baseReq)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxbatchgetcontact?type=ex&r={0}&lang=zh_CN&pass_ticket={1}";
            url = string.Format(url, GetR(), pass_ticket);

			BatchGetContactRequest req = new BatchGetContactRequest
			{
				BaseRequest = baseReq,
				Count = requestContacts.Length
			};

			List<BatchUser> requestUsers = new List<BatchUser>();
            for (int i = 0; i < req.Count; i++) {
				var tmp = new BatchUser
				{
					UserName = requestContacts[i],
				};
				requestUsers.Add(tmp);
            }

            req.List = requestUsers.ToArray();
            string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            var rep = JsonConvert.DeserializeObject<BatchGetContactResponse>(repJsonStr);
            if (rep == null) return null;
            return rep;
        }


        public SyncCheckResponse SyncCheck(SyncItem[] syncItems,BaseRequest baseReq,long syncCheckTimes)
        {
            SetHttpHeader("Accept", "*/*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            
            string synckey = "";
            for (int i = 0; i < syncItems.Length; i++) {
                if (i != 0) {
                    synckey += "|";
                }
                synckey += syncItems[i].Key + "_" + syncItems[i].Val;
            }
            string url = "https://webpush.wx2.qq.com/cgi-bin/mmwebwx-bin/synccheck?skey={0}&sid={1}&uin={2}&deviceid={3}&synckey={4}&_={5}&r={6}";
            url = string.Format(url,UrlEncode(baseReq.Skey), UrlEncode(baseReq.Sid), baseReq.Uin, baseReq.DeviceID,synckey,syncCheckTimes,Util.GetTimeStamp());
            string repStr =GetString(url);
            if (repStr == null) return null;
            SyncCheckResponse rep = new SyncCheckResponse();
            if (repStr.StartsWith("window.synccheck="))
            {
                repStr = repStr.Substring("window.synccheck=".Length);
                rep = JsonConvert.DeserializeObject<SyncCheckResponse>(repStr);
            }
            
            return rep;
        }

        static long GetR() {
            return Util.GetTimeStamp();
        }

        public SyncResponse Sync(SyncKey syncKey,string pass_ticket,BaseRequest baseReq)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            SetHttpHeader("Origin", "https://wx2.qq.com");

            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid={0}&skey={1}&lang=zh_CN&pass_ticket={2}";
            url = string.Format(url,baseReq.Sid,baseReq.Skey,pass_ticket);
			SyncRequest req = new SyncRequest
			{
				BaseRequest = baseReq,
				SyncKey = syncKey,
				rr = ~GetR()
			};
			string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<SyncResponse>(repJsonStr);

           


            return rep;
        }

        public StatusnotifyResponse Statusnotify(string formUser,string toUser,string pass_ticket,BaseRequest baseReq)
        {
            SetHttpHeader("Accept", "application/json, text/plain, */*");
            SetHttpHeader("Connection", "keep-alive");
            SetHttpHeader("Accept-Encoding", "gzip, deflate, br");
            SetHttpHeader("Origin", "https://wx2.qq.com");

            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxstatusnotify?lang=zh_CN&pass_ticket=" + pass_ticket;
			StatusnotifyRequest req = new StatusnotifyRequest
			{
				BaseRequest = baseReq,
				ClientMsgId = GetR(),
				FromUserName = formUser,
				ToUserName = toUser,
				Code = 3
			};
			string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<StatusnotifyResponse>(repJsonStr);//此处掉线时会报格式不正确
            return rep;
        }
		//public List<SendMsgResponse> SendMsgs(IEnumerable<Msg> msgs, string pass_ticket, BaseRequest baseReq)
		//{
		//	var results = new List<SendMsgResponse>();
		//	Func<Msg, string, BaseRequest, SendMsgResponse> sendmsg = SendMsg;
		//	foreach(var msg in msgs)
		//	{
		//		var task = new Task<SendMsgResponse>(sendmsg(msg, pass_ticket, baseReq));
		//	}
		//	Task.WaitAll();
		//	return results;
		//}
		////private Func<SendMsgResponse> SendMsgAsyn(Msg msg,string pass_ticket,BaseRequest baseReq)
		//{
		//	return SendMsg;
		//}
		public SendMsgResponse SendMsg(Msg msg, string pass_ticket,BaseRequest baseReq)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg?sid={0}&r={1}&lang=zh_CN&pass_ticket={2}";
            url = string.Format(url, baseReq.Sid, GetR(), pass_ticket);
			SendMsgRequest req = new SendMsgRequest
			{
				BaseRequest = baseReq,
				Msg = msg,
				rr = DateTime.Now.Millisecond
			};
			string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<SendMsgResponse>(repJsonStr);
            return rep;
        }
		private void ReportForWeb()
		{
			string url = "https://support.weixin.qq.com/cgi-bin/mmsupport-bin/reportforweb?rid={0}&rkey={1}&rvalue={2}";
			using (HttpClient client = new HttpClient())
			{
				client.GetAsync(string.Format(url,69373,9,1));
				client.GetAsync(string.Format(url, 63637, 72, 56));
				client.GetAsync(string.Format(url, 63637, 76, 1));
			}
		}

        public UploadmediaResponse Uploadmedia(string fromUserName,string toUserName,string id,string mime_type, int uploadType,int mediaType,byte[] buffer,string fileName,string pass_ticket,BaseRequest baseReq) {
			ReportForWeb();
			UploadmediaRequest req = new UploadmediaRequest
			{
				BaseRequest = baseReq,
				ClientMediaId = GetR(),
				DataLen = buffer.Length,
				StartPos = 0,
				TotalLen = buffer.Length,
				MediaType = mediaType,
				FromUserName = fromUserName,
				ToUserName = toUserName,
				UploadType = uploadType,
				FileMd5 = Util.GetMD5(buffer)
			};

			string url = "https://file.wx2.qq.com/cgi-bin/mmwebwx-bin/webwxuploadmedia?f=json";
			mHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Options,url));

			string requestJson = JsonConvert.SerializeObject(req);
            string mt = "doc";
            if (mime_type.StartsWith("image/")) {
                mt = "pic";
            }
            var dataTicketCookie = GetCookie("webwx_data_ticket");

			var dataContent = new MultipartFormDataContent
			{
				{ new StringContent(id), "id" },
				{ new StringContent(fileName), "name" },
				{ new StringContent(mime_type), "type" },
				{ new StringContent("Sat Dec 02 2017 18:37:52 GMT+0800 (中国标准时间)"), "lastModifiedDate" },
				{ new StringContent(buffer.Length.ToString()), "size" },
				{ new StringContent(mt), "mediatype" },
				{ new StringContent(requestJson), "uploadmediarequest" },
				{ new StringContent(dataTicketCookie.Value), "webwx_data_ticket" },
				{ new StringContent(pass_ticket), "pass_ticket" },
				{ new ByteArrayContent(buffer), "filename", fileName + "\r\n Content - Type: " + mime_type }
			};

			try
            {
                var response = mHttpClient.PostAsync(url, dataContent).Result;
                string repJsonStr = response.Content.ReadAsStringAsync().Result;
                var rep = JsonConvert.DeserializeObject<UploadmediaResponse>(repJsonStr);
                return rep;
            }
            catch {
                InitHttpClient();
                return null;
            }

        }


        public SendMsgImgResponse SendMsgImg(ImgMsg msg, string pass_ticket,BaseRequest baseReq)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsgimg?fun=async&f=json&lang=zh_CN&pass_ticket={0}";
            url = string.Format(url,pass_ticket);
			SendMsgImgRequest req = new SendMsgImgRequest
			{
				BaseRequest = baseReq,
				Msg = msg,
				Scene = 0
			};
			string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<SendMsgImgResponse>(repJsonStr);

            return rep;
        }


  
        public OplogResponse Oplog(string userName,int cmdID,int op,string RemarkName, string pass_ticket,BaseRequest baseReq)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxoplog?pass_ticket={0}";
            url = string.Format(url, pass_ticket);
			OplogRequest req = new OplogRequest
			{
				BaseRequest = baseReq,
				UserName = userName,
				CmdId = cmdID,
				OP = op,
				RemarkName = RemarkName
			};
			string requestJson = JsonConvert.SerializeObject(req);
            string repJsonStr = PostString(url, requestJson);
            if (repJsonStr == null) return null;
            var rep = JsonConvert.DeserializeObject<OplogResponse>(repJsonStr);
            return rep;
        }

        public void Logout(string skey,string sid,string uin)
        {

            string url = string.Format("https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=0&skey={0}", System.Web.HttpUtility.UrlEncode(skey));
            string requestStr = string.Format("sid={0}&uin={1}",sid,uin);
            SetHttpHeader("Cache-Control", "max-age=0");
            SetHttpHeader("Upgrade-Insecure-Requests", "1");
            SetHttpHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            SetHttpHeader("Referer", "https://wx.qq.com/");
            PostString(url, requestStr);

            url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=1&skey=" + System.Web.HttpUtility.UrlEncode(skey);
            PostString(url, requestStr);

            mHttpClient.DefaultRequestHeaders.Remove("Cache-Control");
            mHttpClient.DefaultRequestHeaders.Remove("Upgrade-Insecure-Requests");
        }



        private string GetString(string url)
        {
			try
			{
				using (HttpResponseMessage response = mHttpClient.GetAsync(url).Result)
			{
				string ret = response.Content.ReadAsStringAsync().Result;

				return ret;
			}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				InitHttpClient();
				return null;
			}

		}

        private string PostString(string url, string content)
        {
			//try
			//{
			using (HttpResponseMessage response = mHttpClient.PostAsync(new Uri(url), new StringContent(content)).Result)
			{
				string ret = response.Content.ReadAsStringAsync().Result;

				return ret;
			}
            //}
            //catch
            // {
            //     InitHttpClient();
            //     return null;
            // }

        }

        /// <summary>
        /// 获取指定cookie
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Cookie GetCookie(string name)
        {
            List<Cookie> cookies = GetAllCookies(mHandler.CookieContainer);
            foreach (Cookie c in cookies)
            {
                if (c.Name == name)
                {
                    return c;
                }
            }
            return null;
        }

        private static List<Cookie> GetAllCookies(CookieContainer cc)
        {
            List<Cookie> lstCookies = new List<Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies) lstCookies.Add(c);
            }
            return lstCookies;
        }


        private void SetHttpHeader(string name,string value)
        {
            if (mHttpClient.DefaultRequestHeaders.Contains(name)) {
                mHttpClient.DefaultRequestHeaders.Remove(name);
            }

            mHttpClient.DefaultRequestHeaders.Add(name, value);
        }
        string UrlEncode(string url)
        {
            return System.Web.HttpUtility.UrlEncode(url);
        }

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: 释放托管状态(托管对象)。
					mHttpClient.Dispose();
				}
				
				// TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
				// TODO: 将大型字段设置为 null。

				disposedValue = true;
			}
		}

		// TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
		// ~WechatAPIService() {
		//   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
		//   Dispose(false);
		// }

		// 添加此代码以正确实现可处置模式。
		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
			// TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
			// GC.SuppressFinalize(this);
		}
		#endregion



	}


}
