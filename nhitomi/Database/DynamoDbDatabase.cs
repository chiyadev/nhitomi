using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;

namespace nhitomi.Database
{
    public class DynamoDbDatabase : IDatabase
    {
        readonly AppSettings _settings;
        readonly AmazonDynamoDBClient _client;
        readonly ILogger<DynamoDbDatabase> _logger;

        public DynamoDbDatabase(
            IOptions<AppSettings> options,
            ILogger<DynamoDbDatabase> logger)
        {
            _settings = options.Value;
            _client = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_settings.Db.AccessKey, _settings.Db.SecretKey),
                RegionEndpoint.GetBySystemName(_settings.Db.RegionEndpoint));
            _logger = logger;
        }

        DynamoDBContext CreateContext() => new DynamoDBContext(_client);

        public async Task<TagSubscriptionInfo[]> GetAllTagSubscriptionsAsync(
            CancellationToken cancellationToken = default)
        {
            var subscriptions = new List<TagSubscriptionInfo>();

            var lastEvaluatedKey = new Dictionary<string, AttributeValue>();

            do
            {
                var request = new ScanRequest
                {
                    TableName = _settings.Db.TagSubscriptionTable,
                    ExclusiveStartKey = lastEvaluatedKey,
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        {"#users", "userList"}
                    },
                    // ensure has subscriptions
                    FilterExpression = "attribute_exists (#users)"
                };

                var response = await _client.ScanAsync(request, cancellationToken);

                // paginating to retrieve all subscriptions
                lastEvaluatedKey = response.LastEvaluatedKey;

                // map response to model
                using (var context = CreateContext())
                {
                    subscriptions.AddRange(
                        context.FromDocuments<TagSubscriptionInfo>(response.Items.Select(
                            Document.FromAttributeMap)));
                }
            } while (lastEvaluatedKey.Count != 0);

            return subscriptions
                .OrderBy(s => s.TagName)
                .ToArray();
        }

        public async Task<string[]> GetTagSubscriptionsAsync(ulong userId,
            CancellationToken cancellationToken = default)
        {
            var tags = new List<string>();

            var lastEvaluatedKey = new Dictionary<string, AttributeValue>();

            do
            {
                var request = new ScanRequest
                {
                    TableName = _settings.Db.TagSubscriptionTable,
                    ExclusiveStartKey = lastEvaluatedKey,
                    // retrieve only tagName
                    ProjectionExpression = "tagName",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        {"#users", "userList"}
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":userId", new AttributeValue {N = userId.ToString()}}
                    },
                    // filter userList contains userId
                    FilterExpression = "contains (#users, :userId)"
                };

                var response = await _client.ScanAsync(request, cancellationToken);

                // paginating to retrieve all subscriptions
                lastEvaluatedKey = response.LastEvaluatedKey;

                tags.AddRange(response.Items.Select(d => d["tagName"].S));
            } while (lastEvaluatedKey.Count != 0);

            return tags
                .OrderBy(t => t)
                .ToArray();
        }

        public async Task<bool> TryAddTagSubscriptionAsync(ulong userId, string tagName,
            CancellationToken cancellationToken = default)
        {
            tagName = tagName.ToLowerInvariant();

            var request = new UpdateItemRequest
            {
                TableName = _settings.Db.TagSubscriptionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    // update item by tagName
                    {"tagName", new AttributeValue {S = tagName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#users", "userList"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":userId", new AttributeValue {N = userId.ToString()}},
                    {":userIdSet", new AttributeValue {NS = new List<string> {userId.ToString()}}}
                },
                UpdateExpression = "add #users :userIdSet",
                ConditionExpression = "not contains (#users, :userId)"
            };

            try
            {
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Added user '{userId}' subscription '{tagName}'.");

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to add user '{userId}' subscription '{tagName}'.");

                throw;
            }
        }

        public async Task<bool> TryRemoveTagSubscriptionAsync(ulong userId, string tagName,
            CancellationToken cancellationToken = default)
        {
            tagName = tagName.ToLowerInvariant();

            var request = new UpdateItemRequest
            {
                TableName = _settings.Db.TagSubscriptionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    // update item by tagName
                    {"tagName", new AttributeValue {S = tagName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#users", "userList"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":userId", new AttributeValue {N = userId.ToString()}},
                    {":userIdSet", new AttributeValue {NS = new List<string> {userId.ToString()}}}
                },
                UpdateExpression = "delete #users :userIdSet",
                ConditionExpression = "contains (#users, :userId)"
            };

            try
            {
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Removed user '{userId}' subscription '{tagName}'.");

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to remove user '{userId}' subscription '{tagName}'.");

                throw;
            }
        }

        public async Task ClearTagSubscriptionsAsync(ulong userId, CancellationToken cancellationToken = default)
        {
            // simply get all subscriptions and remove them in parallel
            var tags = await GetTagSubscriptionsAsync(userId, cancellationToken);

            await Task.WhenAll(tags.Select(t => TryRemoveTagSubscriptionAsync(userId, t, cancellationToken)));
        }

        public async Task<string[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default)
        {
            var request = new QueryRequest
            {
                TableName = _settings.Db.CollectionTable,
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#user", "userId"},
                    {"#map", "items"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":user", new AttributeValue {N = userId.ToString()}},
                    {":zero", new AttributeValue {N = "0"}}
                },
                KeyConditionExpression = "#user = :user",
                FilterExpression = "size (#map) > :zero",
                ProjectionExpression = "collectionName"
            };

            var response = await _client.QueryAsync(request, cancellationToken);

            return response.Items
                .Select(d => d["collectionName"].S)
                .OrderBy(n => n)
                .ToArray();
        }

        public async Task<CollectionItemInfo[]> GetCollectionAsync(ulong userId, string collectionName,
            CancellationToken cancellationToken = default)
        {
            collectionName = collectionName.ToLowerInvariant();

            var request = new GetItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"userId", new AttributeValue {N = userId.ToString()}},
                    {"collectionName", new AttributeValue {S = collectionName}}
                }
            };

            var response = await _client.GetItemAsync(request, cancellationToken);

            // map response to model
            CollectionInfo collection;

            using (var context = CreateContext())
                collection = context.FromDocument<CollectionInfo>(Document.FromAttributeMap(response.Item));

            if (collection.Items == null || collection.Items.Count == 0)
                return null;

            // ensure collection item Source and Id are set, because they are stored as the key for Items map
            var items = collection.Items.Select(p =>
            {
                var parts = p.Key.Split('/', 2);
                var item = p.Value;

                item.Source = parts[0];
                item.Id = parts[1];

                return item;
            });

            // apply collection sorting
            switch (collection.SortAttribute)
            {
                case CollectionSortAttribute.Time:
                    items = items.OrderByDescending(i => i.AddTime);
                    break;

                case CollectionSortAttribute.Id:
                    items = items
                        .OrderBy(i => i.Source)
                        .ThenBy(i => i.Id.PadLeft(10, '0'));
                    break;

                case CollectionSortAttribute.Name:
                    items = items.OrderBy(i => i.Name);
                    break;

                case CollectionSortAttribute.Artist:
                    items = items.OrderBy(i => i.Artist);
                    break;
            }

            if (collection.SortDescending)
                items = items.Reverse();

            return items.ToArray();
        }

        public async Task<bool> TryAddToCollectionAsync(ulong userId, string collectionName, IDoujin doujin,
            CancellationToken cancellationToken = default)
        {
            collectionName = collectionName.ToLowerInvariant();

            Dictionary<string, AttributeValue> itemAttributes;

            // create attribute map for this collection item
            using (var context = CreateContext())
                itemAttributes = context.ToDocument(new CollectionItemInfo(doujin)).ToAttributeMap();

            var request = new UpdateItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"userId", new AttributeValue {N = userId.ToString()}},
                    {"collectionName", new AttributeValue {S = collectionName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#map", "items"},
                    {"#key", $"{doujin.Source.Name}/{doujin.Id}"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":item", new AttributeValue {M = itemAttributes}}
                },
                UpdateExpression = "set #map.#key = :item",
                ConditionExpression = "not attribute_exists (#map.#key)"
            };

            try
            {
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Added doujin '{doujin}' to collection '{collectionName}' of user {userId}.");

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (AmazonDynamoDBException)
            {
                // could not set map value -- meaning map doesn't exist, so we have to create a new collection
                await CreateCollectionAsync(userId, collectionName, doujin, cancellationToken);

                return true;
            }
        }

        async Task CreateCollectionAsync(ulong userId, string collectionName, IDoujin doujin,
            CancellationToken cancellationToken = default)
        {
            collectionName = collectionName.ToLowerInvariant();

            Dictionary<string, AttributeValue> itemAttributes;

            // create attribute map for this collection
            using (var context = CreateContext())
            {
                itemAttributes = context.ToDocument(new CollectionInfo
                {
                    UserId = userId,
                    CollectionName = collectionName,
                    SortAttribute = CollectionSortAttribute.Time,
                    Items = new Dictionary<string, CollectionItemInfo>
                    {
                        {$"{doujin.Source.Name}/{doujin.Id}", new CollectionItemInfo(doujin)}
                    }
                }).ToAttributeMap();
            }

            var request = new PutItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Item = itemAttributes
            };

            try
            {
                await _client.PutItemAsync(request, cancellationToken);

                _logger.LogDebug($"Create collection '{collectionName}' by user {userId} with doujin '{doujin}'.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to create collection '{collectionName}' by user {userId}.");

                throw;
            }
        }

        public async Task<bool> TryRemoveFromCollectionAsync(ulong userId, string collectionName,
            CollectionItemInfo item, CancellationToken cancellationToken = default)
        {
            var request = new UpdateItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"userId", new AttributeValue {N = userId.ToString()}},
                    {"collectionName", new AttributeValue {S = collectionName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#map", "items"},
                    {"#key", $"{item.Source}/{item.Id}"}
                },
                UpdateExpression = "remove #map.#key",
                ConditionExpression = "attribute_exists (#map.#key)",
                ReturnValues = ReturnValue.ALL_OLD
            };

            try
            {
                var response = await _client.UpdateItemAsync(request, cancellationToken);

                // set name of the deleted item
                item.Name = response.Attributes["items"].M[$"{item.Source}/{item.Id}"].M["name"].S;

                _logger.LogDebug($"Removed doujin '{item}' from collection '{collectionName}' of user {userId}.");

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Failed to remove doujin '{item}' from collection '{collectionName}' of user {userId}.");

                throw;
            }
        }

        public async Task<bool> TryDeleteCollectionAsync(ulong userId, string collectionName,
            CancellationToken cancellationToken = default)
        {
            var request = new DeleteItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"userId", new AttributeValue {N = userId.ToString()}},
                    {"collectionName", new AttributeValue {S = collectionName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#user", "userId"},
                    {"#map", "items"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":zero", new AttributeValue {N = "0"}}
                },
                ConditionExpression = "attribute_exists (#user) and size (#map) > :zero"
            };

            try
            {
                await _client.DeleteItemAsync(request, cancellationToken);

                _logger.LogDebug($"Deleted collection '{collectionName}' of user {userId}.");

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Failed to delete collection '{collectionName}' of user {userId}.");

                throw;
            }
        }

        public async Task SetCollectionSortAsync(ulong userId, string collectionName, CollectionSortAttribute attribute,
            CancellationToken cancellationToken = default)
        {
            var request = new UpdateItemRequest
            {
                TableName = _settings.Db.CollectionTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"userId", new AttributeValue {N = userId.ToString()}},
                    {"collectionName", new AttributeValue {S = collectionName}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#attribute", "sortAttribute"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":attribute", new AttributeValue {N = ((int) attribute).ToString()}}
                },
                UpdateExpression = "set #attribute = :attribute"
            };

            try
            {
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Set collection '{collectionName}' sort attribute '{attribute}' of user {userId}.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Failed to Set collection '{collectionName}' sort attribute '{attribute}' of user {userId}.");

                throw;
            }
        }
    }
}