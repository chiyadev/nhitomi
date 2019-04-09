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
using Microsoft.Extensions.Options;
using nhitomi.Database;

namespace nhitomi.Services
{
    public class DynamoDBDatabase : IDatabase
    {
        readonly AppSettings _settings;
        readonly AmazonDynamoDBClient _client;

        public DynamoDBDatabase(
            IOptions<AppSettings> options)
        {
            _settings = options.Value;
            _client = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_settings.Db.AccessKey, _settings.Db.SecretKey),
                RegionEndpoint.GetBySystemName(_settings.Db.RegionEndpoint));
        }

        DynamoDBContext CreateContext() => new DynamoDBContext(_client);

        public async Task<TagSubscriptionInfo[]> GetTagSubscriptionsAsync(
            CancellationToken cancellationToken = default)
        {
            using (var context = CreateContext())
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

                    subscriptions.AddRange(
                        context.FromDocuments<TagSubscriptionInfo>(response.Items.Select(
                            Document.FromAttributeMap)));
                } while (lastEvaluatedKey.Count != 0);

                return subscriptions.ToArray();
            }
        }
    }
}