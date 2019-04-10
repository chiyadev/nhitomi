using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace nhitomi.Database
{
    public class UserInfo
    {
        [DynamoDBHashKey("userId")] public ulong UserId { get; set; }

        // dict (collectionName, dict (source/id, doujinName))
        [DynamoDBProperty("collections")]
        public Dictionary<string, Dictionary<string, string>> Collections { get; set; }
    }
}