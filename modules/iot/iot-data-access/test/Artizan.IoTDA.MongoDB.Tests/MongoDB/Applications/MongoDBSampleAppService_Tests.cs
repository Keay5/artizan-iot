using Artizan.IoTDA.MongoDB;
using Artizan.IoTDA.Samples;
using Xunit;

namespace Artizan.IoTDA.MongoDb.Applications;

[Collection(MongoTestCollection.Name)]
public class MongoDBSampleAppService_Tests : SampleAppService_Tests<IoTDAMongoDbTestModule>
{

}
