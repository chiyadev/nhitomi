using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace nhitomi.Database
{
    public class TagSubscriptionInfo
    {
        [DynamoDBHashKey("tagName")] public string TagName { get; set; }
        [DynamoDBProperty("userList")] public List<ulong> UserList { get; set; }
    }
}
