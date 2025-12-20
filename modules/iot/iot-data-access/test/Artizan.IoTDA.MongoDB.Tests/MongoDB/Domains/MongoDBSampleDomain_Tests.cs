using Artizan.IoTDA.Samples;
using Xunit;

namespace Artizan.IoTDA.MongoDB.Domains;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleDomain_Tests : SampleManager_Tests<IoTDAMongoDbTestModule>
{

}
