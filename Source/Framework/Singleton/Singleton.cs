// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
