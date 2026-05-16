using System;

namespace ET
{

	public enum VersionMode
	{
		Alpha = 0,              //内测服
		Beta = 1,               //正式服
		BanHao = 2,				//版号服
		
		/*Test      = 0,    // 测试服 / 内测服（你原来的 Alpha）
		Release   = 1,    // 正式服（你原来的 Beta）
		BanHao    = 2,    // 版号服（国内特殊，只能保留拼音）*/
	}

	public static class EnumHelper
	{
		public static int EnumIndex<T>(int value)
		{
			int i = 0;
			foreach (object v in Enum.GetValues(typeof (T)))
			{
				if ((int) v == value)
				{
					return i;
				}
				++i;
			}
			return -1;
		}

		public static T FromString<T>(string str)
		{
            if (!Enum.IsDefined(typeof(T), str))
            {
                return default(T);
            }
            return (T)Enum.Parse(typeof(T), str);
        }
    }
}