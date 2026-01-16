using Artizan.IoTHub.Samples;
using Xunit;

namespace Artizan.IoTHub.MongoDB.Domains;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleDomain_Tests : SampleManager_Tests<IoTHubMongoDbTestModule>
{

}
