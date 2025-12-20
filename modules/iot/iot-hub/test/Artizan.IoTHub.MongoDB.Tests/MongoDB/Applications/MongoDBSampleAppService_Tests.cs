using Artizan.IoTHub.MongoDB;
using Artizan.IoTHub.Samples;
using Xunit;

namespace Artizan.IoTHub.MongoDb.Applications;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleAppService_Tests : SampleAppService_Tests<IoTHubMongoDbTestModule>
{

}
