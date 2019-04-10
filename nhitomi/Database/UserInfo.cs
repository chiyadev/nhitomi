using Amazon.DynamoDBv2.DataModel;

namespace nhitomi.Database
{
    public class UserInfo
    {
        [DynamoDBHashKey("userId")] public ulong UserId { get; set; }
    }
}