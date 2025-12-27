using Artizan.IoT.MongoDB;
using Artizan.IoT.Samples;
using Xunit;

namespace Artizan.IoT.MongoDb.Applications;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleAppService_Tests : SampleAppService_Tests<IoTMongoDbTestModule>
{

}
