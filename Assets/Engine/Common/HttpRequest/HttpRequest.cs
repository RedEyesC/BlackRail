using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GameFramework.Common
{
    public static class HttpRequest
    {

        public delegate void HttpCallBack(bool isSuccess, string str, byte[] data);


        public static void CreateGetRequest(string url, HttpCallBack callBack, int timeout = 10)
        {
            AppInterface.StartCoroutine(CreateGetRequestItor(url, callBack, timeout));
        }

        public static IEnumerator CreateGetRequestItor(string url, HttpCallBack callBack, int timeout)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = timeout;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ProtocolError || req.result == UnityWebRequest.Result.ConnectionError)
                {
                    callBack(false, req.error, null);
                }
                else
                {
                    callBack(true, null, req.downloadHandler.data);
                }
            }
        }


        public static void CreatePostRequest(string url, WWWForm form, HttpCallBack callBack, int timeout = 10)
        {

            AppInterface.StartCoroutine(CreatePostRequestItor(url, form, callBack, null, null, timeout));
        }

        public static void CreatePostRequest(string url, WWWForm form, HttpCallBack callBack, Dictionary<string, string> headers, byte[] data, int timeout = 10)
        {

            AppInterface.StartCoroutine(CreatePostRequestItor(url, form, callBack, headers, data, timeout));
        }


        public static IEnumerator CreatePostRequestItor(string url, WWWForm form, HttpCallBack callBack, Dictionary<string, string> headers, byte[] data, int timeout)
        {
            using (UnityWebRequest req = UnityWebRequest.Post(url, form))
            {

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        req.SetRequestHeader(header.Key, header.Value);
                    }

                }

                if(data != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(data);
                }
             
                req.timeout = timeout;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ProtocolError || req.result == UnityWebRequest.Result.ConnectionError)
                {
                    callBack(false, req.error, null);
                }
                else
                {
                    callBack(true, null, req.downloadHandler.data);
                }
            }
        }


    }
}
