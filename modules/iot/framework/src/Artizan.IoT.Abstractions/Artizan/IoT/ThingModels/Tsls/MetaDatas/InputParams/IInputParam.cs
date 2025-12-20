namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

 /// <summary>
 /// 输入参数接口，统一约束所有输入参数类型
 /// </summary>
 public interface IInputParam
 {
     // 基础属性：所有输入参数都应包含标识符
     string Identifier { get; set; }
 }