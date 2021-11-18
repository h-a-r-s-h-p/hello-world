using System;
using System.Collections.Generic;
using Dashboard.Server.SessionManagement;
using Content;

namespace Dashboard.Server.Telemetry{
    ///<summary>
    /// All analytics are done in this class
    ///</summary>
    public class Telemetry: ITelemetry
    {
        /// <summary>
        ///     constructs a dictionary with DateTime as key and int as value
        ///     which indicates UserCount at corresponding DateTime 
        /// </summary>
        /// <params name= "newSession"> 
        ///     takes the session data which contains the users list 
        ///     and whenever the session data changes, Telemetry get notified, 
        ///     based on it timestamp can be stored.
        /// </params>
        public void GetUserCountVsTimeStamp(SessionData newSession)
        {
            DateTime currTime = DateTime.Now;
            userCountAtEachTimeStamp[currTime] = newSession.users.Count;
        }

        /// <summary>
        ///     constructs the dictionary of UserID as key and chatCount as value
        ///     indicating chat count of each user.
        /// </summary>
        /// <params name="allMessages"> Takes array of ChatContext object which contains information about Threads </params>
        void GetUserVsChatCount(ChatContext[] allMessages)
        {
            foreach(ChatContext currThread in allMessages)
            {
                foreach(ReceiveMessageData currMessage in currThread.MsgList)
                {
                    userIdChatCountDic[currMessage.SenderId]++;
                }
            }
        }

        /// <summary> 
        ///     Calculates the enter and exit time for each user. Whenever SessionData
        ///     changes, that means any user has either entered or exited.
        /// </summary>
        /// <params name="newSession"> Takes the session data which contains the list of users </params>
        public void CalculateEnterExitTimes(SessionData newSession)
        {
            DateTime currTime= DateTime.Now;
            foreach(UserData user_i in newSession.users )
            {
                if(userEnterTime.ContainsKey(user_i)==false)
                {
                    userEnterTime[user_i]= currTime;
                }
            }
            // if user is in userEnterTime but not in users list, that means he left the meeting.
            foreach(KeyValuePair<UserData,DateTime> user_i in userEnterTime){
                if(newSession.users.Contains(user_i.Key)==false && userExitTime.ContainsKey(user_i.Key)==false ){
                    userExitTime[user_i.Key]=currTime;
                }
            }

        }

        /// <summary>
        ///     Constructs the insincereMembers list from userEnterTime and useExitTime dictionary
        /// </summary>
        public void GetInsincereMembers()
        {
            foreach(KeyValuePair<UserData,DateTime> user_i in userEnterTime)
            {
                UserData  currUser = user_i.Key;
                // if difference of exit and enter time is less than 30 min.
                if(userExitTime[currUser].Subtract(user_i.Value).TotalMinutes<30)
                {
                    insincereMembers.Add(currUser.userID);
                }
            }
        }

        /// <summary>
        ///     appends the current session data in the previous ServerDataToSave object
        /// </summary>
        /// <params name="totalUsers"> Total number of users in the current session </params>
        /// <params name="totalChats"> Total chats in the current session </params>
        public void UpdateServerData(int totalUsers, int totalChats ){
            // retrieve the previous server data till previous session
            ServerDataToSave serverData = retrieveAllServerData(); 
            serverData.sessionCount++;
            // current session data
            SessionSummary currSessionSummary = new SessionSummary();
            currSessionSummary.userCount = totalUsers;
            currSessionSummary.chatCount = totalChats;
            currSessionSummary.score = totalChats * totalUsers;
            serverData.allSessionsSummary.Add(currSessionSummary);
            saveServerData(serverData);
        }

        /// <summary>
        ///     To get any change in the SessionData
        /// </summary>
        /// <params name="new_session"> Received new SessionData </params>
        void OnAnalyticsChanged(SessionData new_session)
        {
            GetUserCountVsTimeStamp(new_session);
            GetInsincereMembers();
        }

        /// <summary>
        ///     Used to simplify the ChatContext and saved all analytics when the session is over.
        /// </summary>
        /// <params name="allMessages"> Array of ChatContext objects which contains information about messages of each thread </params>    
        public void SaveAnalytics(ChatContext[] allMessages)
        {
            // save the session data
            GetUserVsChatCount(allMessages);
            SessionAnalytics sessionAnalyticsToSave = new SessionAnalytics();
            sessionAnalyticsToSave.chatCountForEachUser=userIdChatCountDic;
            sessionAnalyticsToSave.userCountAtAnyTime= userCountAtEachTimeStamp;
            sessionAnalyticsToSave.insincereMembers=insincereMembers;
            Save(sessionAnalyticsToSave);
            // saving server data
            int totalChats=0;
            int totalUsers=0;
            foreach(KeyValuePair<int,int> user_i in userIdChatCountDic){
                totalChats+=user_i.Value;
                totalUsers+=1;
            }
            UpdateServerData(totalUsers, totalChats);
        }

        /// <summary>
        ///     get the SessionAnalytics to transfer
        ///     back to UX module to display the analytics
        /// </summary>
        /// <params> Array of ChatContext objects which contains information about messages of each thread </params>
        /// <returns>
        ///     Returns SessionAnalytics object which contains analytics of session
        /// </returns>
        public SessionAnalytics GetTelemetryAnalytics(ChatContext[] all_messages)
        {
            SessionAnalytics sessionAnalyticsToSend = new SessionAnalytics();
            sessionAnalyticsToSend.chatCountForEachUser=userIdChatCountDic;
            sessionAnalyticsToSend.userCountAtAnyTime= userCountAtEachTimeStamp;
            sessionAnalyticsToSend.insincereMembers=insincereMembers;
            return sessionAnalyticsToSend;
        }

        Dictionary<DateTime, int> userCountAtEachTimeStamp = new Dictionary<DateTime, int>();
        Dictionary<UserData,DateTime> userEnterTime=new Dictionary<UserData, DateTime>();
        Dictionary<UserData,DateTime> userExitTime=new Dictionary<UserData, DateTime>();
        Dictionary<int, int> userIdChatCountDic= new Dictionary<int, int>();
        List<int> insincereMembers;
    }
}