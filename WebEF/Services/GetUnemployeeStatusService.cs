﻿using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using WebEF.Contracts;
using WebEF.Contracts.RequestTypes;
using WebEF.Contracts.ResponseTypes;
using WebEF.Data;
using WebEF.Models;
using XRoadLib.Headers;

namespace WebEF.Services
{
    public class GetUnemployeeStatusService : IGetUnemployeeStatus
    {
        private readonly IOptions<AppSettings> _appSettings;
        private ServiceManagerDbContext _db;
        public ISoapHeader XRoadHeader { get; set; }
        public GetUnemployeeStatusService(IOptions<AppSettings> appSettings, ServiceManagerDbContext db)
        {
            _appSettings = appSettings;
            _db = db;
        }
        public GetUnemployeeStatusResponse GetUnemployeeStatus(PINRequest request)
        {
            var log = new TransmitHistoryItem
            {
                EntryTime = DateTime.Now,
                InputSize = 0,
                OutputSize = 0,
                IsSuccess = false
            };
            if (XRoadHeader != null && XRoadHeader is XRoadHeader40)
            {
                var h = (XRoadHeader40)XRoadHeader;
                log.c_objectType = h.Client.ObjectType.ToString();
                log.c_xRoadInstance = h.Client.XRoadInstance;
                log.c_memberClass = h.Client.MemberClass;
                log.c_memberCode = h.Client.MemberCode;
                log.c_subsystemCode = h.Client.SubsystemCode;
                var serviceDetails = _db.ServiceDetails.FirstOrDefault(x => x.ServiceCode.Name == h.Service.ServiceCode);
                if (serviceDetails == null)
                {
                    log.ErrorMessage = string.Format("The s_serviceCode \"{0}\" is not registered in ServerManager database. Please check this entry.", h.Service.ServiceCode);
                }
                else
                {
                    log.ServiceDetailId = serviceDetails.Id;
                }
            }
            try
            {
                log.InputSize = GetSizeOfObjectInBytes(request) ?? 0;
                var apiMethod = "/Account/CheckInnHasStatus?code=63FA87FD5C35CA78C8113CE089230392CD0C34CEAACFA7321307D78C401782CA95&inn=" + request.PIN;
                var response = RequestHttp<GetUnemployeeStatusResponse>(apiMethod, request);
                log.IsSuccess = true;
                log.OutputSize = GetSizeOfObjectInBytes(response) ?? 0;
                return response;
            }
            catch (Exception e)
            {
                log.IsSuccess = false;
                log.ErrorMessage += e.GetBaseException().Message;
                throw e;
            }
            finally
            {
                log.OperationDuration = (DateTime.Now - log.EntryTime).TotalMilliseconds;
                _db.TransmitHistoryItems.Add(log);
                _db.SaveChanges();
            }
        }

        private T RequestHttp<T>(string methodName, object request)
        {
            if (string.IsNullOrEmpty(_appSettings.Value.ISRT_REST_APIHost))
            {
                throw new ApplicationException("ISRT_REST_APIHost not found in appSetting.json!");
            }
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(_appSettings.Value.ISRT_REST_APIHost + methodName);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "GET";

            /*using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(request);

                streamWriter.Write(json);
                streamWriter.Flush();
            }*/
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            var responseText = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                responseText = streamReader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<T>(responseText);
        }
        private static long? GetSizeOfObjectInBytes(object item)
        {
            if (item == null) return 0;
            try
            {
                // hackish solution to get an approximation of the size
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    MaxDepth = 10,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var formatter = new JsonMediaTypeFormatter { SerializerSettings = jsonSerializerSettings };
                using (var stream = new MemoryStream())
                {
                    formatter.WriteToStreamAsync(item.GetType(), item, stream, null, null).GetAwaiter().GetResult();
                    return stream.Length / 4; // 32 bits per character = 4 bytes per character
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
