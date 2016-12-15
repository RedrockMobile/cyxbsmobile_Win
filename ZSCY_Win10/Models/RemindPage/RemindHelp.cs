﻿//using System;
//using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using System;
using ZSCY_Win10.Models.RemindPage;
using ZSCY_Win10.Util;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using Windows.Security.Credentials;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace ZSCY_Win10.Models.RemindPage
{
    public static class RemindHelp
    {
        public static async Task AddRemind(MyRemind remind)
        {
            remind.Id_system = Guid.NewGuid();
            await Task.Run(delegate
            {
                addNotification(remind);
            });
        }
        private static void addNotification(MyRemind remind)
        {

            ScheduledToastNotification scheduledNotifi = GenerateAlarmNotification(remind);

            ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduledNotifi);
        }

        private static ScheduledToastNotification GenerateAlarmNotification(MyRemind remind)
        {
            ToastContent content = new ToastContent()
            {
                Scenario = ToastScenario.Alarm,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                                            {
                                                new AdaptiveText()
                                                {
                                                    Text = $"提醒: {remind.Title}"
                                                },

                                                new AdaptiveText()
                                                {
                                                    Text = remind.Content
                                                }
                                            }
                    }
                },
                Actions = new ToastActionsSnoozeAndDismiss()//自动创建一个自动本地化的有延迟提醒时间间隔，贪睡和取消的按钮，贪睡时间由系统自动处理
            };
            return new ScheduledToastNotification(content.GetXml(), remind.time)
            {
                Tag = GetTag(remind)
            };

        }

        private static string id;
        public static string GetTag(MyRemind remind)
        {
            string temp = remind.Id_system.GetHashCode().ToString();
            id += temp + ",";
            // Tag needs to be 16 chars or less, so hash the Id
            return temp;
        }

        public static async Task<string> AddAllRemind(MyRemind remind, TimeSpan beforeTime)
        {
            id = "";
            for (int i = 0; i < App.selectedWeekNumList.Count; i++)
            {

                for (int r = 0; r < 6; r++)
                {
                    for (int c = 0; c < 7; c++)
                    {
                        if (App.timeSet[r, c].IsCheck)
                        {
                            remind.time = App.selectedWeekNumList[i].WeekNumOfMonday.AddDays(c) + App.timeSet[r, c].Time - beforeTime;
                            if (remind.time < DateTime.Now.ToUniversalTime())
                            {

                            }
                            else
                            {
                                await AddRemind(remind);
                            }

                        }
                    }
                }
            }
            return id;
        }
        public static async Task<string> SyncAllRemind(MyRemind remind)
        {

            id = "";
            if (remind.Time != null)
            {
                int min = int.Parse(remind.Time) % 60;
                int hour = int.Parse(remind.Time) / 60;
                int day = hour / 24;
                TimeSpan beforeTime = new TimeSpan(day, hour, min, 0);


                List<SelectedWeekNum> weeklist = new List<SelectedWeekNum>();
                foreach (var item in remind.DateItems)
                {
                    var itemWeekList = item.Week.Split(',');
                    var itemClassList = int.Parse(item.Class);
                    var itemDayList = int.Parse(item.Day);
                    TimeSet classTime = new TimeSet();
                    classTime.Set(itemClassList);
                    for (int i = 0; i < itemWeekList.Count(); i++)
                    {
                        SelectedWeekNum swn = new SelectedWeekNum();
                        //TODO 周数是是从第一周开始所以week设置要减一
                        swn.SetWeekTime(int.Parse(itemWeekList[i]) - 1);
                        remind.time = swn.WeekNumOfMonday.AddDays(itemDayList) + classTime.Time - beforeTime;
                        if (remind.time.Ticks < DateTime.Now.Ticks)
                        {

                        }
                        else
                        {
                            await AddRemind(remind);
                        }
                    }
                }
            }
            return id;
        }
        public static async void SyncRemind()
        {
            List<KeyValuePair<string, string>> paramList = new List<KeyValuePair<string, string>>();
            PasswordCredential user = GetCredential.getCredential("ZSCY");
            paramList.Add(new KeyValuePair<string, string>("stuNum", user.UserName));
            paramList.Add(new KeyValuePair<string, string>("idNum", user.Password));
            string content = "";
            try
            {
                content = await NetWork.httpRequest(ApiUri.getRemindApi, paramList);
            }
            catch
            {

                Debug.WriteLine("网络问题请求失败");
            }
            //相当于MyRemind
            GetRemindModel getRemid = JsonConvert.DeserializeObject<GetRemindModel>(content);

            try
            {

                getRemid = await JsonConvert.DeserializeObjectAsync<GetRemindModel>(content);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
            List<string> getRemindList_json = new List<string>();
            List<MyRemind> remindList = new List<MyRemind>();
            #region 返回的json格式和添加的风格不一样，转换
            foreach (var item in getRemid.DataItems)
            {
                //getRemindList_json.Add(getRemid.DataItems[0].Id.ToString());
                MyRemind mr = new MyRemind();
                List<DateItemModel> dim = new List<DateItemModel>();
                //每个MyRemind的date
                foreach (var itemData in item.DateItems)
                {
                    DateItemModel dateitme = new DateItemModel();
                    string week = "";
                    foreach (var itemWeek in itemData.WeekItems)
                    {
                        week += itemWeek + ",";
                    }
                    week = week.Remove(week.Length - 1);
                    dateitme.Class = itemData.Class.ToString();
                    dateitme.Day = itemData.Day.ToString();
                    dateitme.Week = week;
                    dim.Add(dateitme);
                }
                mr.Title = item.Title;
                mr.Content = item.Content;
                mr.DateItems = dim;
                mr.Time = item.Time;
                mr.Id = item.Id.ToString();
                remindList.Add(mr);

            }
            #endregion
            List<string> RemindTagList = new List<string>();
            RemindTagList = DatabaseMethod.ClearRemindItem() as List<string>;
            var notifier = ToastNotificationManager.CreateToastNotifier();
            if (RemindTagList != null)
            {

                for (int i = 0; i < RemindTagList.Count(); i++)
                {
                    var scheduledNotifs = notifier.GetScheduledToastNotifications()
                  .Where(n => n.Tag.Equals(RemindTagList[i]));

                    // Remove all of those from the schedule
                    foreach (var n in scheduledNotifs)
                    {
                        notifier.RemoveFromSchedule(n);
                    }
                }
            }

            foreach (var remindItem in remindList)
            {
                string id = await SyncAllRemind(remindItem);
                DatabaseMethod.ToDatabase(remindItem.Id, JsonConvert.SerializeObject(remindItem), id);
            }
            DatabaseMethod.ReadDatabase(Windows.UI.Xaml.Visibility.Collapsed);
        }
     

    }




}