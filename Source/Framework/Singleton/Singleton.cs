// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Reflection;

public class Singleton<T> where T : class
{
    private static volatile T instance;
    private static object syncRoot = new();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        ConstructorInfo constructorInfo = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        instance = (T)constructorInfo.Invoke(new object[0]);
                    }
                }
            }

            return instance;
        }
    }
}
