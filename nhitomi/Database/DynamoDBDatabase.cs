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

        public async Task<TagSubscriptionInfo[]> GetTagSubscriptionsAsync(
            CancellationToken cancellationToken = default)
        {
            var subscriptions = new List<TagSubscriptionInfo>();

            var lastEvaluatedKey = new Dictionary<string, AttributeValue>();

            do
            {
                var request = new ScanRequest
                {
                    TableName = _settings.Db.TagSubscriptionTable,
                    ExclusiveStartKey = lastEvaluatedKey
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

        public async Task<string[]> GetTagSubscriptionsAsync(
            ulong userId,
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
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        // userId operand
                        {":userId", new AttributeValue {N = userId.ToString()}}
                    },
                    // filter userList contains userId
                    FilterExpression = "contains (userList, :userId)"
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

        public async Task<bool> TryAddTagSubscriptionAsync(
            ulong userId,
            string tagName,
            CancellationToken cancellationToken = default)
        {
            try
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
                        {"#userList", "userList"}
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        // userId operand
                        {":userId", new AttributeValue {NS = new List<string> {userId.ToString()}}}
                    },
                    UpdateExpression = "add #userList :userId"
                };
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Added user '{userId}' subscription '{tagName}'.");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to add user '{userId}' subscription '{tagName}'.");

                throw;
            }
        }

        public async Task<bool> TryRemoveTagSubscriptionAsync(
            ulong userId,
            string tagName,
            CancellationToken cancellationToken = default)
        {
            try
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
                        {"#userList", "userList"}
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        // userId operand
                        {":userId", new AttributeValue {NS = new List<string> {userId.ToString()}}}
                    },
                    UpdateExpression = "delete #userList :userId"
                };
                await _client.UpdateItemAsync(request, cancellationToken);

                _logger.LogDebug($"Removed user '{userId}' subscription '{tagName}'.");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to remove user '{userId}' subscription '{tagName}'.");

                throw;
            }
        }
    }
}