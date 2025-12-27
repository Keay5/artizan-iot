using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Newtonsoft.Json;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses
{
    /// <summary>
    /// 数组型规格（不可变值对象）
    /// 描述数组的大小和元素类型约束
    /// </summary>
    public class ArraySpecs : ISpecs
    {
        [JsonProperty("size", Order = 1)]
        public string Size { get; protected set; }

        [JsonProperty("item", Order = 2)]
        public DataType Item { get; protected set; }

        protected ArraySpecs() 
        {
        }

        /// <summary>
        /// 初始化数组规格
        /// </summary>
        /// <param name="size">数组大小约束（可为null表示无约束）</param>
        /// <param name="item">数组元素类型及规格</param>
        public ArraySpecs(string size, DataType item)
        {

            SetSzie(size);
            SetItem(item);
        }

        public ArraySpecs SetSzie(string size)
        {
            if (size.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(size), "数组元素类型不能为空");
            }

            int sizeVal = 0;
            // 校验size为非负整数
            if (!string.IsNullOrEmpty(size) && !int.TryParse(size, out sizeVal) || sizeVal < 0)
            {
                throw new ArgumentException($"数组大小{size}必须为非负整数");
            }

            Size = size;

            return this;
        }

        public ArraySpecs SetItem(DataType item) 
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "数组元素类型不能为空");
            }

            Item = item;

            return this;
        }

    }
}
