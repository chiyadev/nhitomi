using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace nhitomi.Database
{
    public class CollectionInfo
    {
        [DynamoDBHashKey("userId")] public ulong UserId { get; set; }
        [DynamoDBRangeKey("collectionName")] public string CollectionName { get; set; }

        [DynamoDBProperty("sortAttribute")] public CollectionSortAttribute SortAttribute { get; set; }
        [DynamoDBProperty("sortDescending")] public bool SortDescending { get; set; }

        [DynamoDBProperty("items")] public Dictionary<string, CollectionItemInfo> Items { get; set; }
    }
}