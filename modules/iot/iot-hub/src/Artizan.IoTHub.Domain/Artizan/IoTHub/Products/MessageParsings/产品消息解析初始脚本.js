// 以下为脚本模版，您可以基于以下模版进行脚本编写

/**

 * 将设备自定义topic数据转换为json格式数据, 设备上报数据到物联网平台时调用
 * 入参：topic   string 设备上报消息的topic
 * 入参：rawData byte[]数组 不能为空
 * 出参：jsonObj JSON对象 不能为空
   */
   function transformPayload(topic, rawData) {
       var jsonObj = {};
       return jsonObj;
   }

/**

 * 将设备的自定义格式数据转换为Alink协议的数据，设备上报数据到物联网平台时调用
 * 入参：rawData byte[]数组 不能为空
 * 出参：jsonObj Alink JSON对象 不能为空
   */
   function rawDataToProtocol(rawData) {
       var jsonObj = {};
       return jsonObj;
   }

/**

 *  将Alink协议的数据转换为设备能识别的格式数据，物联网平台给设备下发数据时调用
 *  入参：jsonObj Alink JSON对象  不能为空
 *  出参：rawData byte[]数组      不能为空
    *
     */
    function protocolToRawData(jsonObj) {
        var rawdata = [];
        return rawdata;
    }

