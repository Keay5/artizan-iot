using Artizan.IoT.Samples;
using Xunit;

namespace Artizan.IoT.MongoDB.Domains;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleDomain_Tests : SampleManager_Tests<IoTMongoDbTestModule>
{

}
